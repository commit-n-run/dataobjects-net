// Copyright (C) 2009 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Alexis Kochetov
// Created:    2009.03.05

using System;
using System.Diagnostics;
using System.Linq.Expressions;
using Xtensive.Core.Internals.DocTemplates;
using Xtensive.Core.Tuples;

namespace Xtensive.Storage.Rse.Providers.Compilable
{
  /// <summary>
  /// Produces join between <see cref="BinaryProvider.Left"/> and 
  /// <see cref="BinaryProvider.Right"/> sources by <see cref="Predicate"/>.
  /// </summary>
  [Serializable]
  public sealed class PredicateJoin : BinaryProvider
  {
    /// <summary>
    /// Indicates whether current join operation should be executed as left join.
    /// </summary>
    public bool LeftJoin { get; private set; }

    /// <summary>
    /// Gets the predicate.
    /// </summary>
    public Expression<Func<Tuple, Tuple, bool>> Predicate { get; private set; }


    // Constructors

    /// <summary>
    /// <see cref="ClassDocTemplate.Ctor" copy="true"/>
    /// </summary>  
    public PredicateJoin(CompilableProvider left, CompilableProvider right, Expression<Func<Tuple, Tuple, bool>> predicate, bool leftJoin)
      : base(ProviderType.PredicateJoin, left, right)
    {
      Predicate = predicate;
      LeftJoin = leftJoin;
    }
  }
}