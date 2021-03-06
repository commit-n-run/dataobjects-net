// Copyright (C) 2008-2020 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.
// Created by: Nick Svetlov
// Created:    2008.01.14

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Xtensive.Core;
using Xtensive.Reflection;

namespace Xtensive.Comparison
{
  /// <summary>
  /// Default <see cref="IComparer{T}"/> provider. 
  /// Provides default comparer for the specified type.
  /// </summary>
  [Serializable]
  public class ComparerProvider : AssociateProvider,
    IComparerProvider
  {
    private static readonly ComparerProvider defaultProvider;
    private static readonly SystemComparerProvider systemProvider;
    private static readonly Type BaseComparerWrapperType;

    /// <summary>
    /// Gets default instance of this type.
    /// </summary>
    public static ComparerProvider Default
    {
      [DebuggerStepThrough]
      get { return defaultProvider; }
    }

    /// <summary>
    /// Gets system comparer provider.
    /// A shortcut to <see cref="SystemComparerProvider.Instance"/>.
    /// </summary>
    public static SystemComparerProvider System
    {
      [DebuggerStepThrough]
      get { return systemProvider; }
    }

    #region IComparerProvider Members

    /// <inheritdoc/>
    public virtual AdvancedComparer<T> GetComparer<T>()
    {
      return GetAssociate<T, IAdvancedComparer<T>, AdvancedComparer<T>>();
    }

    #endregion

    #region Protected method overrides

    /// <inheritdoc/>
    protected override TAssociate CreateAssociate<TKey, TAssociate>(out Type foundFor)
    {
      TAssociate associate = base.CreateAssociate<TKey, TAssociate>(out foundFor);
      if (associate!=null)
        return associate;
      // Ok, null, but probably just because type cast has failed;
      // let's try to wrap it. TKey is type for which we're getting
      // the comparer.
      IAdvancedComparerBase comparer = base.CreateAssociate<TKey, IAdvancedComparerBase>(out foundFor);
      if (foundFor==null) {
        CoreLog.Warning(Strings.LogCantFindAssociateFor,
          TypeSuffixes.ToDelimitedString(" \\ "),
          typeof (TAssociate).GetShortName(),
          typeof (TKey).GetShortName());
        return null;
      }
      if (foundFor==typeof (TKey))
        return (TAssociate) comparer;
      associate = BaseComparerWrapperType.Activate(new[] {typeof (TKey), foundFor}, ConstructorParams) as TAssociate;
      if (associate!=null) {
        CoreLog.Warning(Strings.LogGenericAssociateIsUsedFor,
          BaseComparerWrapperType.GetShortName(),
          typeof (TKey).GetShortName(),
          foundFor.GetShortName(),
          typeof (TKey).GetShortName());
        return associate;
      }
      else {
        CoreLog.Warning(Strings.LogGenericAssociateCreationHasFailedFor,
          BaseComparerWrapperType.GetShortName(),
          typeof (TKey).GetShortName(),
          foundFor.GetShortName(),
          typeof (TKey).GetShortName());
        return null;
      }
    }

    /// <inheritdoc/>
    protected override TResult ConvertAssociate<TKey, TAssociate, TResult>(TAssociate associate)
    {
      if (ReferenceEquals(associate, null))
        return default(TResult);
      else
        return (TResult) (object) new AdvancedComparer<TKey>((IAdvancedComparer<TKey>) associate);
    }

    #endregion


    // Constructors

    /// <summary>
    /// Initializes a new instance of this type.
    /// </summary>
    protected ComparerProvider()
    {
      TypeSuffixes = new[] {"Comparer"};
      ConstructorParams = new object[] {this, ComparisonRules.Positive};
      AddHighPriorityLocation(BaseComparerWrapperType.Assembly, BaseComparerWrapperType.Namespace);
    }

    static ComparerProvider()
    {
      BaseComparerWrapperType = typeof (BaseComparerWrapper<,>);
      defaultProvider = new ComparerProvider();
      systemProvider = SystemComparerProvider.Instance;
    }
  }
}