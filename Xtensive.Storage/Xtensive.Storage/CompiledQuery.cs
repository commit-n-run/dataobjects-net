// Copyright (C) 2009 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Alexis Kochetov
// Created:    2009.04.24

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Web.UI.WebControls;
using Xtensive.Core;
using Xtensive.Core.Parameters;
using Xtensive.Storage.Linq;
using Xtensive.Storage.Linq.Expressions;
using Parameter=System.Web.UI.WebControls.Parameter;

namespace Xtensive.Storage
{
  /// <summary>
  /// Provides compilation and caching of queries for reuse.
  /// </summary>
  public sealed class CompiledQuery
  {
    /// <summary>
    /// Finds compiled query in cache by provided <paramref name="query"/> delegate and executes them if it's already cached; otherwise executes the <paramref name="query"/> delegate.
    /// </summary>
    /// <typeparam name="TElement">The type of the result element.</typeparam>
    /// <param name="query">Containing query delegate.</param>
    public static IEnumerable<TElement> Execute<TElement>(Func<IQueryable<TElement>> query)
    {
      var domain = Domain.Current;
      if (domain != null) {
        Pair<MethodInfo, ResultExpression> item;
        ResultExpression resultExpression = null;
        if (domain.QueryCache.TryGetItem(query.Method, true, out item))
          resultExpression = item.Second;
        if (resultExpression == null) {
          var result = query();
          resultExpression = QueryProvider.LatestCompiledResult;
          lock (domain.QueryCache)
            if (!domain.QueryCache.TryGetItem(query.Method, false, out item))
              domain.QueryCache.Add(new Pair<MethodInfo, ResultExpression>(query.Method, resultExpression));
          return result;
        }
        return resultExpression.GetResult<IEnumerable<TElement>>();
      }
      // TODO: write error message
      throw new InvalidOperationException();
    }

    /// <summary>
    /// Finds compiled query in cache by provided <paramref name="query"/> delegate and executes them if it's already cached; otherwise executes the <paramref name="query"/> delegate.
    /// </summary>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <param name="query">Containing query delegate.</param>
    public static TResult Execute<TResult>(Func<TResult> query)
    {
      var domain = Domain.Current;
      if (domain != null) {
        Pair<MethodInfo, ResultExpression> item;
        ResultExpression resultExpression = null;
        if (domain.QueryCache.TryGetItem(query.Method, true, out item))
          resultExpression = item.Second;
        if (resultExpression == null) {
          var result = query();
          resultExpression = QueryProvider.LatestCompiledResult;
          lock (domain.QueryCache)
            if (!domain.QueryCache.TryGetItem(query.Method, false, out item))
              domain.QueryCache.Add(new Pair<MethodInfo, ResultExpression>(query.Method, resultExpression));
          return result;
        }
        return resultExpression.GetResult<TResult>();
      }
      // TODO: write error message
      throw new InvalidOperationException();
    }
  }
}