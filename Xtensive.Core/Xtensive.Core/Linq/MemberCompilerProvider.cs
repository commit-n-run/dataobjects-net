﻿// Copyright (C) 2009 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Denis Krjuchkov
// Created:    2009.02.09

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xtensive.Core;
using Xtensive.Core.Collections;
using Xtensive.Core.Helpers;
using Xtensive.Core.Reflection;
using Xtensive.Core.Resources;

using CompilerDictionary = System.Collections.Generic.Dictionary<System.Reflection.MethodInfo,
  Xtensive.Core.Pair<System.Delegate, System.Reflection.MethodInfo>>;

namespace Xtensive.Core.Linq
{
  /// <summary>
  /// Default implementation of <see cref="IMemberCompilerProvider{T}"/>.
  /// </summary>
  /// <typeparam name="T"><inheritdoc/></typeparam>
  public class MemberCompilerProvider<T> : IMemberCompilerProvider<T>
  {
    private volatile CompilerDictionary delegatesByMethodInfo = new CompilerDictionary();
    private readonly object syncRoot = new object();

    /// <inheritdoc/>
    public Func<T, T[], T> GetCompiler(MethodInfo methodInfo)
    {
      MethodInfo dummy;
      return GetCompiler(methodInfo, out dummy);
    }

    /// <inheritdoc/>
    public Func<T, T[], T> GetCompiler(MethodInfo methodInfo, out MethodInfo compilerMethodInfo)
    {
      ArgumentValidator.EnsureArgumentNotNull(methodInfo, "methodInfo");

      compilerMethodInfo = null;

      bool withMethodInfo = false;

      var mi = methodInfo;

      if (mi.IsGenericMethod) {
        mi = mi.GetGenericMethodDefinition();
        withMethodInfo = true;
      }

      var type = mi.ReflectedType;

      if (type.IsGenericType) {
        type = type.GetGenericTypeDefinition();
        mi = FindBestMethod(type, mi);
        if (mi == null)
          return null;
        withMethodInfo = true;
      }

      Pair<Delegate, MethodInfo> pair;

      if (!delegatesByMethodInfo.TryGetValue(mi, out pair))
        return null;

      if (withMethodInfo) {
        var d = pair.First as Func<MethodInfo, T, T[], T>;
        if (d == null)
          return null;
        compilerMethodInfo = pair.Second;
        return (this_, arr) => d(methodInfo, this_, arr);
      }

      compilerMethodInfo = pair.Second;
      return pair.First as Func<T, T[], T>;
    }

    /// <inheritdoc/>
    public void RegisterCompilers(Type type)
    {
      RegisterCompilers(type, ConflictHandlingMethod.ReportError);
    }

    /// <inheritdoc/>
    public void RegisterCompilers(Type type, ConflictHandlingMethod conflictHandlingMethod)
    {
      ArgumentValidator.EnsureArgumentNotNull(type, "type");

      if (type.IsGenericType)
        throw new InvalidOperationException(string.Format(
          Strings.ExTypeXShouldNotBeGeneric, type.GetFullName(true)));

      var dict = new CompilerDictionary();

      var compilers = type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
        .Where(mi => mi.IsDefined(typeof (CompilerAttribute), false) && !mi.IsGenericMethod);

      foreach (var compiler in compilers)
        RegisterCompiler(dict, compiler);

      lock (syncRoot) {
        switch (conflictHandlingMethod) {
        case ConflictHandlingMethod.KeepOld:
          delegatesByMethodInfo = MergeDicts(dict, delegatesByMethodInfo);
          break;
        case ConflictHandlingMethod.Overwrite:
          delegatesByMethodInfo = MergeDicts(delegatesByMethodInfo, dict);
          break;
        case ConflictHandlingMethod.ReportError:
          var result = new Dictionary<MethodInfo, Pair<Delegate, MethodInfo>>(delegatesByMethodInfo);
            foreach (var pair in dict) {
              if (result.ContainsKey(pair.Key))
                throw new InvalidOperationException(string.Format(
                  Strings.ExCompilerForXIsAlreadyRegistered,
                  pair.Key.GetFullName(true)));
              result.Add(pair.Key, pair.Value);
            }
          delegatesByMethodInfo = result;
          break;
        }
      }
    }

    private static CompilerDictionary MergeDicts(CompilerDictionary first, CompilerDictionary second)
    {
      var result = new Dictionary<MethodInfo, Pair<Delegate, MethodInfo>>(first);
      foreach (var pair in second)
        result[pair.Key] = pair.Value;
      return result;
    }

    private static MethodInfo FindBestMethod(Type type, MethodInfo mi)
    {
      var methods = type.GetMethods().Where(
        m => m.Name == mi.Name
          && m.GetParameters().Length == mi.GetParameters().Length
          && m.IsStatic == mi.IsStatic);

      if (methods.Count() == 1)
        return methods.First();

      var paramTypes = mi.GetParameterTypes();

      Func<Type, Type, bool> oneParamMatch =
        (l, r) => l.IsGenericParameter ? r == l : (r.IsGenericParameter || r == l);

      Func<MethodInfo, bool> allParamsMatch =
        m => paramTypes
          .ZipWith(m.GetParameterTypes(), (t1, t2) => new { t1, t2 })
          .All(a => oneParamMatch(a.t1, a.t2));

      methods = methods.Where(allParamsMatch);

      if (methods.Count() > 1)
        return null;

      return methods.FirstOrDefault();
    }

    private static Type[] ExtractParamTypesAndValidate(MethodInfo methodInfo, bool requireMethodInfo)
    {
      int length = methodInfo.GetParameters().Length;

      if (length > 4)
        throw new InvalidOperationException(string.Format(
          Strings.ExCompilerXHasTooManyParameters,
          methodInfo.GetFullName(true)));

      if (length == 0 && requireMethodInfo)
        throw new InvalidOperationException(string.Format(
          Strings.ExCompilerXShouldHaveMethodInfoParameter,
          methodInfo.GetFullName(true)));

      if (methodInfo.ReturnType != typeof(T))
        throw new InvalidOperationException(string.Format(
          Strings.ExCompilerXShouldReturnY,
          methodInfo.GetFullName(true),
          typeof(T).GetFullName(true)));

      var parameters = methodInfo.GetParameters();
      var result = new Type[length];

      for (int i = 0; i < length; i++)
      {
        var param = parameters[i];
        var requiredType = typeof(T);

        if (requireMethodInfo && i == 0)
          requiredType = typeof(MethodInfo);

        if (param.ParameterType != requiredType)
          throw new InvalidOperationException(string.Format(
            Strings.ExCompilerXShouldHaveParameterYOfTypeZ,
            methodInfo.GetFullName(true), param.Name,
            requiredType.GetFullName(true)));

        var attr = (TypeAttribute)param
          .GetCustomAttributes(typeof(TypeAttribute), false).FirstOrDefault();

        result[i] = attr == null ? null : attr.Value;
      }

      return result;
    }

    private void RegisterCompiler(CompilerDictionary dict, MethodInfo compiler)
    {
      var attr = compiler.GetAttribute<CompilerAttribute>(AttributeSearchOptions.InheritNone);

      bool isBadTargetType = attr.TargetType == null
        || (attr.TargetType.IsGenericType && !attr.TargetType.IsGenericTypeDefinition);

      if (isBadTargetType)
        throw new InvalidOperationException(string.Format(
          Strings.ExCompilerXHasBadTargetType,
          compiler.GetFullName(true)));

      bool isStatic = (attr.TargetKind & TargetKind.Static) != 0;
      bool isPropertySetter = (attr.TargetKind & TargetKind.PropertySet) != 0;
      bool isPropertyGetter = (attr.TargetKind & TargetKind.PropertyGet) != 0;
      bool isGenericMethod = attr.GenericParamsCount > 0;
      bool isGenericType = attr.TargetType.IsGenericType;
      bool isGeneric = isGenericType || isGenericMethod;

      string memberName = attr.TargetMember;

      if (memberName.IsNullOrEmpty())
        if (isPropertyGetter || isPropertySetter)
          memberName = WellKnown.IndexerPropertyName;
        else
          throw new InvalidOperationException(string.Format(
            Strings.ExCompilerXHasBadTargetMember,
            compiler.GetFullName(true)));

      var paramTypes = ExtractParamTypesAndValidate(compiler, isGeneric);
      var bindFlags = BindingFlags.Public;

      if (isGeneric)
        paramTypes = paramTypes.Skip(1).ToArray();

      if (!isStatic)
      {
        if (paramTypes.Length == 0)
          throw new InvalidOperationException(string.Format(
            Strings.ExCompilerXShouldHaveThisParameter,
            compiler.GetFullName(true)));

        paramTypes = paramTypes.Skip(1).ToArray();
        bindFlags |= BindingFlags.Instance;
      }
      else
        bindFlags |= BindingFlags.Static;

      if (isPropertyGetter)
      {
        bindFlags |= BindingFlags.GetProperty;
        memberName = WellKnown.GetterPrefix + memberName;
      }

      if (isPropertySetter)
      {
        bindFlags |= BindingFlags.SetProperty;
        memberName = WellKnown.SetterPrefix + memberName;
      }

      var genericArgNames = isGenericMethod ? new string[attr.GenericParamsCount] : null;
      var methodInfo = attr.TargetType.GetMethod(memberName, bindFlags, genericArgNames, paramTypes);

      if (methodInfo == null)
        throw new InvalidOperationException(string.Format(
          Strings.ExTargetMemberIsNotFoundForCompilerX,
          compiler.GetFullName(true)));

      if (dict.ContainsKey(methodInfo))
        throw new InvalidOperationException(string.Format(
          Strings.ExCompilerForXIsAlreadyRegistered,
          methodInfo.GetFullName(true)));

      Delegate result;

      if (isGeneric)
        result = isStatic ? CreateStaticGenericInvoke(compiler) : CreateInstanceGenericInvoke(compiler);
      else
        result = isStatic ? CreateStaticNonGenericInvoke(compiler) : CreateInstanceNonGenericInvoke(compiler);
      
      dict[methodInfo] = new Pair<Delegate, MethodInfo>(result , compiler);
    }

    private static Func<T, T[], T> CreateInstanceNonGenericInvoke(MethodInfo compiler)
    {
      var t = compiler.ReflectedType;
      string s = compiler.Name;

      switch (compiler.GetParameters().Length) {
        case 1:
          var d1 = DelegateHelper.CreateDelegate<Func<T, T>>(null, t, s);
          return (this_, arr) => d1(this_);
        case 2:
          var d2 = DelegateHelper.CreateDelegate<Func<T, T, T>>(null, t, s);
          return (this_, arr) => d2(this_, arr[0]);
        case 3:
          var d3 = DelegateHelper.CreateDelegate<Func<T, T, T, T>>(null, t, s);
          return (this_, arr) => d3(this_, arr[0], arr[1]);
        case 4:
          var d4 = DelegateHelper.CreateDelegate<Func<T, T, T, T, T>>(null, t, s);
          return (this_, arr) => d4(this_, arr[0], arr[1], arr[2]);
      }

      return null;
    }

    private static Func<MethodInfo, T, T[], T> CreateInstanceGenericInvoke(MethodInfo compiler)
    {
      var t = compiler.ReflectedType;
      string s = compiler.Name;

      switch (compiler.GetParameters().Length) {
        case 2:
          var d2 = DelegateHelper.CreateDelegate<Func<MethodInfo, T, T>>(null, t, s);
          return (mi, this_, arr) => d2(mi, this_);
        case 3:
          var d3 = DelegateHelper.CreateDelegate<Func<MethodInfo, T, T, T>>(null, t, s);
          return (mi, this_, arr) => d3(mi, this_, arr[0]);
        case 4:
          var d4 = DelegateHelper.CreateDelegate<Func<MethodInfo, T, T, T, T>>(null, t, s);
          return (mi, this_, arr) => d4(mi, this_, arr[0], arr[1]);
      }

      return null;
    }

    private static Func<T, T[], T> CreateStaticNonGenericInvoke(MethodInfo compiler)
    {
      var t = compiler.ReflectedType;
      string s = compiler.Name;

      switch (compiler.GetParameters().Length) {
        case 0:
          var d0 = DelegateHelper.CreateDelegate<Func<T>>(null, t, s);
          return (_, arr) => d0();
        case 1:
          var d1 = DelegateHelper.CreateDelegate<Func<T, T>>(null, t, s);
          return (_, arr) => d1(arr[0]);
        case 2:
          var d2 = DelegateHelper.CreateDelegate<Func<T, T, T>>(null, t, s);
          return (_, arr) => d2(arr[0], arr[1]);
        case 3:
          var d3 = DelegateHelper.CreateDelegate<Func<T, T, T, T>>(null, t, s);
          return (_, arr) => d3(arr[0], arr[1], arr[2]);
        case 4:
          var d4 = DelegateHelper.CreateDelegate<Func<T, T, T, T, T>>(null, t, s);
          return (_, arr) => d4(arr[0], arr[1], arr[2], arr[3]);
      }

      return null;
    }

    private static Func<MethodInfo, T, T[], T> CreateStaticGenericInvoke(MethodInfo compiler)
    {
      var t = compiler.ReflectedType;
      string s = compiler.Name;

      switch (compiler.GetParameters().Length) {
        case 1:
          var d1 = DelegateHelper.CreateDelegate<Func<MethodInfo, T>>(null, t, s);
          return (mi, _, arr) => d1(mi);
        case 2:
          var d2 = DelegateHelper.CreateDelegate<Func<MethodInfo, T, T>>(null, t, s);
          return (mi, _, arr) => d2(mi, arr[0]);
        case 3:
          var d3 = DelegateHelper.CreateDelegate<Func<MethodInfo, T, T, T>>(null, t, s);
          return (mi, _,arr) => d3(mi, arr[0], arr[1]);
        case 4:
          var d4 = DelegateHelper.CreateDelegate<Func<MethodInfo, T, T, T, T>>(null, t, s);
          return (mi, _, arr) => d4(mi, arr[0], arr[1], arr[2]);
      }

      return null;
    }
  }
}
