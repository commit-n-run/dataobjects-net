// Copyright (C) 2009 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Alexis Kochetov
// Created:    2009.02.27

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Xtensive.Core;
using Xtensive.Core.Collections;
using Xtensive.Core.Helpers;
using Xtensive.Core.Parameters;
using Xtensive.Core.Reflection;
using Xtensive.Core.Tuples;
using Xtensive.Core.Tuples.Transform;
using Xtensive.Storage.Model;
using Xtensive.Storage.Rse;
using Xtensive.Storage.Rse.Providers.Compilable;

namespace Xtensive.Storage.Linq
{
  internal partial class Translator
  {
    private static readonly PropertyInfo keyValueAccessor;
    private static readonly PropertyInfo keyAccessor;
    private static readonly MethodInfo transformApplyMethod;
    private static readonly MethodInfo keyCreateMethod;
    private static readonly MethodInfo selectMethod;
    private static readonly MethodInfo genericAccessor;
    private static readonly MethodInfo nonGenericAccessor;
    private static readonly MethodInfo recordKeyAccessor;
    private static readonly MethodInfo keyResolveMethod;
    private readonly Parameter<List<CalculatedColumnDescriptor>> calculatedColumns = new Parameter<List<CalculatedColumnDescriptor>>();
    private readonly Parameter<ParameterExpression[]> parameters = new Parameter<ParameterExpression[]>();
    private readonly Parameter<ResultMapping> resultMapping = new Parameter<ResultMapping>();
    private readonly Parameter<ParameterExpression> tuple = new Parameter<ParameterExpression>();
    private readonly Parameter<ParameterExpression> record = new Parameter<ParameterExpression>();
    private readonly Parameter<bool> joinFinalEntity = new Parameter<bool>();
    private readonly Parameter<bool> calculateExpressions = new Parameter<bool>();
    private bool recordIsUsed;

    protected override Expression Visit(Expression e)
    {
      if (e == null)
        return null;
      if (context.Evaluator.CanBeEvaluated(e)) {
        if (context.ParameterExtractor.IsParameter(e))
          return e;
        return context.Evaluator.Evaluate(e);
      }
      return base.Visit(e);
    }

    protected override Expression VisitLambda(LambdaExpression le)
    {
      using (new ParameterScope()) {
        recordIsUsed = false;
        tuple.Value = Expression.Parameter(typeof(Tuple), "t");
        record.Value = Expression.Parameter(typeof(Record), "r");
        parameters.Value = le.Parameters.ToArray();
        calculatedColumns.Value = new List<CalculatedColumnDescriptor>();
        var body = Visit(le.Body);
        if (calculateExpressions.Value && body.GetMemberType() == MemberType.Unknown) {
          if (
            body.NodeType != ExpressionType.Call || 
            ((MethodCallExpression)body).Object == null || 
            ((MethodCallExpression)body).Object.Type != typeof (Tuple)) {

            var calculator = Expression.Lambda(Expression.Convert(body, typeof(object)), tuple.Value);
            var ccd = new CalculatedColumnDescriptor(context.GetNextColumnAlias(), body.Type, (Expression<Func<Tuple, object>>)calculator);
            calculatedColumns.Value.Add(ccd);
            int position = context.GetBound(parameters.Value[0]).RecordSet.Header.Columns.Count + calculatedColumns.Value.Count - 1;
            var method = genericAccessor.MakeGenericMethod(body.Type);
            body = Expression.Call(tuple.Value, method, Expression.Constant(position));
            resultMapping.Value.RegisterPrimitive(new Segment<int>(position, 1));
          }
        }
        if (calculatedColumns.Value.Count > 0) {
          var source = context.GetBound(le.Parameters[0]);
          var recordSet = source.RecordSet;
          recordSet = recordSet.Calculate(calculatedColumns.Value.ToArray());
          var re = new ResultExpression(source.Type, recordSet, source.Mapping, source.Projector, source.ItemProjector);
          context.ReplaceBound(le.Parameters[0], re);
        }
        var result = recordIsUsed
          ? Expression.Lambda(
            typeof (Func<,,>).MakeGenericType(typeof (Tuple), typeof (Record), body.Type),
            body,
            tuple.Value,
            record.Value)
          : Expression.Lambda(body, tuple.Value);
        return result;
      }
    }

    protected override Expression VisitMemberPath(MemberPath path, Expression e)
    {
      var pe = path.Parameter;
      var source = context.GetBound(pe);
      var mapping = source.Mapping;
      int number = 0;
      foreach (var item in path) {
        number++;
        if (item.Type == MemberType.Entity && (joinFinalEntity.Value || number != path.Count)) {
          ResultMapping innerMapping;
          var name = item.Name;
          var typeInfo = context.Model.Types[item.Expression.Type];
          if (!mapping.JoinedRelations.TryGetValue(name, out innerMapping)) {
            var joinedIndex = typeInfo.Indexes.PrimaryIndex;
            var joinedRs = IndexProvider.Get(joinedIndex).Result.Alias(context.GetNextAlias());
            var keySegment = mapping.Fields[name];
            var keyPairs = keySegment.GetItems()
              .Select((leftIndex, rightIndex) => new Pair<int>(leftIndex, rightIndex))
              .ToArray();
            var rs = source.RecordSet.Join(joinedRs, JoinType.Default, keyPairs);
            var fieldMapping = Translator.BuildFieldMapping(typeInfo, source.RecordSet.Header.Columns.Count);
            var joinedMapping = new ResultMapping(fieldMapping, new Dictionary<string, ResultMapping>());
            mapping.JoinedRelations.Add(name, joinedMapping);

            source = new ResultExpression(source.Type, rs, source.Mapping, source.Projector, source.ItemProjector);
            context.ReplaceBound(pe, source);
          }
          mapping = innerMapping;
        }
      }

      var resultType = e.Type;
      source = context.GetBound(path.Parameter);
      switch (path.PathType)
      {
        case MemberType.Primitive:
          {
            var method = resultType == typeof(object)
              ? nonGenericAccessor
              : genericAccessor.MakeGenericMethod(resultType);
            var segment = source.GetMemberSegment(path);
            resultMapping.Value.RegisterPrimitive(segment);
            return Expression.Call(tuple.Value, method, Expression.Constant(segment.Offset));
          }
        case MemberType.Key:
          {
            recordIsUsed = true;
            var segment = source.GetMemberSegment(path);
            var keyColumn = (MappedColumn)source.RecordSet.Header.Columns[segment.Offset];
            var type = keyColumn.ColumnInfoRef.Resolve(context.Model).Field.ReflectedType;
            var transform = new SegmentTransform(true, type.Hierarchy.KeyInfo.TupleDescriptor, source.GetMemberSegment(path));
            var keyExtractor = Expression.Call(keyCreateMethod, Expression.Constant(type),
                                               Expression.Call(Expression.Constant(transform), transformApplyMethod,
                                                               Expression.Constant(TupleTransformType.Auto), tuple.Value),
                                               Expression.Constant(false));
            var rm = source.GetMemberMapping(path);
            resultMapping.Value.RegisterPrimitive(segment);
            return keyExtractor;
          }
        case MemberType.Structure:
          {
            recordIsUsed = true;
            var segment = source.GetMemberSegment(path);
            var structureColumn = (MappedColumn)source.RecordSet.Header.Columns[segment.Offset];
            var field = structureColumn.ColumnInfoRef.Resolve(context.Model).Field;
            while (field.Parent != null)
              field = field.Parent;
            int groupIndex = source.RecordSet.Header.ColumnGroups.GetGroupIndexBySegment(segment);
            var result =
              Expression.MakeMemberAccess(
                Expression.Convert(
                  Expression.Call(
                    Expression.Call(record.Value, recordKeyAccessor, Expression.Constant(groupIndex)),
                    keyResolveMethod),
                  field.ReflectedType.UnderlyingType),
                field.UnderlyingProperty);
            var rm = source.GetMemberMapping(path);
            var mappedFields = rm.Fields.Where(p => p.Value.Offset >= segment.Offset && p.Value.EndOffset <= segment.EndOffset).ToList();
            var name = mappedFields.Select(pair => pair.Key).OrderBy(s => s.Length).First();
            foreach (var pair in mappedFields) {
              var key = pair.Key.TryCutPrefix(name).TrimStart('.');
              resultMapping.Value.RegisterFieldMapping(key, pair.Value);
            }
            return result;
          }
        case MemberType.Entity:
          {
            recordIsUsed = true;
            var segment = source.GetMemberSegment(path);
            int groupIndex = source.RecordSet.Header.ColumnGroups.GetGroupIndexBySegment(segment);
            var result = Expression.Convert(
              Expression.Call(
                Expression.Call(record.Value, recordKeyAccessor, Expression.Constant(groupIndex)),
                keyResolveMethod), resultType);
            var rm = source.GetMemberMapping(path);
            var name = rm.Fields.Select(pair => pair.Key).OrderBy(s => s.Length).First();
            foreach (var pair in rm.Fields) {
              var key = pair.Key.TryCutPrefix(name).TrimStart('.');
              resultMapping.Value.RegisterFieldMapping(key, pair.Value);
            }
            foreach (var pair in rm.JoinedRelations) {
              var key = pair.Key.TryCutPrefix(name).TrimStart('.');
              resultMapping.Value.RegisterJoined(key, pair.Value);
            }
            resultMapping.Value.RegisterJoined(string.Empty, rm);
            return result;
          }
        case MemberType.EntitySet:
          {
            recordIsUsed = true;
            var m = (MemberExpression)e;
            var expression = Visit(m.Expression);
            var result = Expression.MakeMemberAccess(expression, m.Member);
            return result;
          }
        case MemberType.Anonymous:
          {
            var rm = source.GetMemberMapping(path);
            resultMapping.Value.RegisterJoined(string.Empty, rm);
            if (path.Count == 0)
              return VisitParameter(path.Parameter);
            var projector = source.Mapping.AnonymousProjections[path.First().Name];
            var parameterRewriter = new ParameterRewriter(tuple.Value, record.Value);
            var result = parameterRewriter.Rewrite(projector);
            recordIsUsed |= result.Second;
            return result.First;
          }
        default:
          throw new ArgumentOutOfRangeException();
      }
    }

    protected override Expression VisitMethodCall(MethodCallExpression mc)
    {
      return base.VisitMethodCall(mc);
    }

    protected override Expression VisitUnary(UnaryExpression u)
    {
      return base.VisitUnary(u);
    }

    protected override Expression VisitBinary(BinaryExpression b)
    {
      var memberType = b.Left.GetMemberType();
      switch (memberType) {
        case MemberType.Unknown:
        case MemberType.Primitive:
          break;
        case MemberType.Key:
        case MemberType.Entity:
        case MemberType.Anonymous: 
        case MemberType.Structure: {
          Expression result = null;
          bool isKey = memberType == MemberType.Key;
          bool isEntity = memberType == MemberType.Entity;
          bool isAnonymous = memberType == MemberType.Anonymous;
          bool leftIsParameter = context.ParameterExtractor.IsParameter(b.Left);
          bool rightIsParameter = context.ParameterExtractor.IsParameter(b.Right);
          if (b.NodeType!=ExpressionType.Equal && b.NodeType!=ExpressionType.NotEqual)
            throw new NotSupportedException();
          if (isKey) {
            if (!leftIsParameter && !rightIsParameter)
              result = MakeComplexBinaryExpression(b.Left, b.Right, b.NodeType);
            else {
              var bLeft = b.Left;
              var bRight = b.Right;
              if (leftIsParameter) {
                bLeft = b.Right;
                bRight = b.Left;
              }
              var path = MemberPath.Parse(bLeft, context.Model);
              var source = context.GetBound(path.Parameter);
              var segment = source.GetMemberSegment(path);
              foreach (var pair in segment.GetItems().Select((ci, pi) => new {ColumnIndex = ci, ParameterIndex = pi})) {
                Expression left = Expression.Call(tuple.Value, nonGenericAccessor, Expression.Constant(pair.ColumnIndex));
                Expression right = Expression.Condition(
                  Expression.Equal(bRight, Expression.Constant(null, bRight.Type)),
                  Expression.Constant(null, typeof (object)),
                  Expression.Call(Expression.MakeMemberAccess(bRight, keyValueAccessor), nonGenericAccessor, Expression.Constant(pair.ParameterIndex)));
                result = MakeBinaryExpression(result, left, right, b.NodeType);
              }
            }
          }
          else if (isEntity) {
            if (!leftIsParameter && !rightIsParameter) {
              var bLeft = b.Left.NodeType == ExpressionType.Constant && ((ConstantExpression)b.Left).Value == null ? b.Left : Expression.MakeMemberAccess(b.Left, keyAccessor);
              var bRight = b.Right.NodeType == ExpressionType.Constant && ((ConstantExpression)b.Right).Value == null ? b.Right : Expression.MakeMemberAccess(b.Right, keyAccessor);
              result = MakeComplexBinaryExpression(bLeft, bRight, b.NodeType);
            }
            else {
              var bLeft = Expression.MakeMemberAccess(b.Left, keyAccessor);
              var bRight = b.Right;
              if (leftIsParameter) {
                bLeft = Expression.MakeMemberAccess(b.Right, keyAccessor);
                bRight = b.Left;
              }
              var path = MemberPath.Parse(bLeft, context.Model);
              var source = context.GetBound(path.Parameter);
              var segment = source.GetMemberSegment(path);
              foreach (var pair in segment.GetItems().Select((ci, pi) => new { ColumnIndex = ci, ParameterIndex = pi })) {
                Expression left = Expression.Call(tuple.Value, nonGenericAccessor, Expression.Constant(pair.ColumnIndex));
                Expression right = Expression.Condition(
                  Expression.Equal(bRight, Expression.Constant(null, bRight.Type)),
                  Expression.Constant(null, typeof(object)),
                  Expression.Call(
                    Expression.MakeMemberAccess(Expression.MakeMemberAccess(bRight, keyAccessor), keyValueAccessor),
                    nonGenericAccessor, Expression.Constant(pair.ParameterIndex)));
                result = MakeBinaryExpression(result, left, right, b.NodeType);
              }
            }
          }
          else if (isAnonymous) {
            if (!leftIsParameter && !rightIsParameter)
              return MakeComplexBinaryExpression(b.Left, b.Right, b.NodeType);
            throw new NotSupportedException();
          }
          else {
            if (!leftIsParameter && !rightIsParameter)
              return MakeComplexBinaryExpression(b.Left, b.Right, b.NodeType);
            throw new NotSupportedException();
          }
          return result;
        }
        case MemberType.EntitySet:
          throw new NotSupportedException();
        default:
          throw new ArgumentOutOfRangeException();
      }
      return base.VisitBinary(b);
    }

    protected override Expression VisitParameter(ParameterExpression p)
    {
      var source = context.GetBound(p);
      var rm = source.Mapping;
      foreach (var pair in rm.Fields)
        resultMapping.Value.RegisterFieldMapping(pair.Key, pair.Value);
      foreach (var pair in rm.JoinedRelations)
        resultMapping.Value.RegisterJoined(pair.Key, pair.Value);
      foreach (var pair in rm.AnonymousProjections)
        resultMapping.Value.RegisterAnonymous(pair.Key, pair.Value);
      var parameterRewriter = new ParameterRewriter(tuple.Value, record.Value);
      var result = parameterRewriter.Rewrite(source.ItemProjector.Body);
      recordIsUsed |= result.Second;
      return result.First;
    }

    protected override Expression VisitMemberAccess(MemberExpression m)
    {
      if (context.Evaluator.CanBeEvaluated(m) && context.ParameterExtractor.IsParameter(m))
        return m;
      return base.VisitMemberAccess(m);
    }

    protected override Expression VisitNew(NewExpression n)
    {
      var arguments = new List<Expression>();
      if (n.Members == null)
        throw new NotSupportedException(n.ToString(true));
      for (int i = 0; i < n.Arguments.Count; i++) {
        var arg = n.Arguments[i];
        var newArg = (Expression) null;
        var member = n.Members[i];
        var memberName = member.Name.TryCutPrefix(WellKnown.GetterPrefix);
        Func<string, string> rename = key => key.IsNullOrEmpty()
              ? memberName
              : memberName + "." + key;
        var path = MemberPath.Parse(arg, context.Model);
        if (path.IsValid || arg.NodeType == ExpressionType.New) {
          ResultMapping rm;
          using (new ParameterScope()) {
            resultMapping.Value = new ResultMapping();
            newArg = Visit(arg);
            rm = resultMapping.Value;
          }
          if (rm.MapsToPrimitive)
            resultMapping.Value.RegisterFieldMapping(memberName, rm.Segment);
          foreach (var p in rm.Fields)
            resultMapping.Value.RegisterFieldMapping(rename(p.Key), p.Value);
          foreach (var p in rm.JoinedRelations)
            resultMapping.Value.RegisterJoined(rename(p.Key), p.Value);
          foreach (var p in rm.AnonymousProjections)
            resultMapping.Value.RegisterAnonymous(rename(p.Key), p.Value);
          var memberType = arg.GetMemberType();
          if (memberType == MemberType.Anonymous || memberType == MemberType.Entity) {
            resultMapping.Value.RegisterJoined(memberName, rm);
            if (memberType == MemberType.Anonymous)
              resultMapping.Value.RegisterAnonymous(memberName, newArg);
          }
        }
        else {
          // TODO: Add check of queries
          var le = context.MemberAccessReplacer.ProcessCalculated(Expression.Lambda(arg, parameters.Value));
          var ccd = new CalculatedColumnDescriptor(context.GetNextColumnAlias(), arg.Type, (Expression<Func<Tuple, object>>) le);
          calculatedColumns.Value.Add(ccd);
          int position = context.GetBound(parameters.Value[0]).RecordSet.Header.Columns.Count + calculatedColumns.Value.Count - 1;
          var method = genericAccessor.MakeGenericMethod(arg.Type);
          newArg = Expression.Call(tuple.Value, method, Expression.Constant(position));
          resultMapping.Value.RegisterFieldMapping(memberName, new Segment<int>(position, 1));
        }
        newArg = newArg ?? Visit(arg);
        arguments.Add(newArg);
      }
      var x = DateTime.Now;
      var y = x.AddDays(1);
      return Expression.New(n.Constructor, arguments, n.Members);
    }


    #region Private helper methods

    private static IEnumerable<TResult> MakeProjection<TResult>(RecordSet rs, Expression<Func<Tuple, Record, TResult>> le)
    {
      var func = le.Compile();
      foreach (var r in rs.Parse())
        yield return func(r.Data, r);
    }

    private static Expression MakeBinaryExpression(Expression previous, Expression left, Expression right, ExpressionType operationType)
    {
      if (previous == null) {
        previous = operationType == ExpressionType.Equal
          ? Expression.Equal(left, right)
          : Expression.NotEqual(left, right);
      }
      else {
        previous = operationType == ExpressionType.Equal
          ? Expression.AndAlso(previous, Expression.Equal(left, right))
          : Expression.AndAlso(previous, Expression.NotEqual(left, right));
      }
      return previous;
    }

    private Expression MakeComplexBinaryExpression(Expression bLeft, Expression bRight, ExpressionType operationType)
    {
      Expression result = null;
      if (bLeft.NodeType == ExpressionType.Constant || bRight.NodeType == ExpressionType.Constant) {
        var constant = bLeft.NodeType == ExpressionType.Constant
          ? (ConstantExpression)bLeft
          : (ConstantExpression)bRight;
        var member = bLeft.NodeType != ExpressionType.Constant
          ? bLeft
          : bRight;
        if (constant.Value == null) {
          var path = MemberPath.Parse(member, context.Model);
          var source = context.GetBound(path.Parameter);
          var segment = source.GetMemberSegment(path);
          foreach (var i in segment.GetItems()) {
            Expression left = Expression.Call(tuple.Value, nonGenericAccessor, Expression.Constant(i));
            Expression right = Expression.Constant(null);
            result = MakeBinaryExpression(result, left, right, operationType);
          }
          return result;
        }
      }
      var leftPath = MemberPath.Parse(bLeft, context.Model);
      var leftSource = context.GetBound(leftPath.Parameter);
      var leftSegment = leftSource.GetMemberSegment(leftPath);
      var rightPath = MemberPath.Parse(bRight, context.Model);
      var rightSource = context.GetBound(rightPath.Parameter);
      var rightSegment = rightSource.GetMemberSegment(rightPath);
      foreach (var pair in leftSegment.GetItems().ZipWith(rightSegment.GetItems(), (l,r) => new {l,r})) {
        var method = genericAccessor.MakeGenericMethod(leftSource.RecordSet.Header.TupleDescriptor[pair.l]);
        Expression left = Expression.Call(tuple.Value, method, Expression.Constant(pair.l));
        Expression right = Expression.Call(tuple.Value, method, Expression.Constant(pair.r));
        result = MakeBinaryExpression(result, left, right, operationType);
      }
      return result;
    }

    #endregion




    // Type initializer

    static Translator()
    {
      keyValueAccessor = typeof(Key).GetProperty("Value");
      keyAccessor = typeof(IEntity).GetProperty("Key");
      selectMethod = typeof (Enumerable).GetMethods().Where(m => m.Name==WellKnown.Queryable.Select).First();
      keyCreateMethod = typeof (Key).GetMethod("Create", new[] {typeof (TypeInfo), typeof (Tuple), typeof (bool)});
      transformApplyMethod = typeof (SegmentTransform).GetMethod("Apply", new[] {typeof (TupleTransformType), typeof (Tuple)});
      recordKeyAccessor = typeof(Record).GetProperty("Item", typeof(Key), new[]{typeof(int)}).GetGetMethod();
      keyResolveMethod =typeof (Key).GetMethods()
        .Where(
          mi => mi.Name == "Resolve" && 
          mi.IsGenericMethodDefinition == false && 
          mi.GetParameters().Length == 0)
        .Single();
      foreach (var method in typeof(Tuple).GetMethods()) {
        if (method.Name == "GetValueOrDefault") {
          if (method.IsGenericMethod)
            genericAccessor = method;
          else
            nonGenericAccessor = method;
        }
      }
    }
  }
}