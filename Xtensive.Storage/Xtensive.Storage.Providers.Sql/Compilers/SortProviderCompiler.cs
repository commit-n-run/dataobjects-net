// Copyright (C) 2008 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Alexey Kochetov
// Created:    2008.07.14

using System.Collections.Generic;
using Xtensive.Core;
using Xtensive.Sql.Dom.Dml;
using Xtensive.Storage.Rse.Compilation;
using Xtensive.Storage.Rse.Providers;
using System.Linq;
using Xtensive.Storage.Rse.Providers.Compilable;

namespace Xtensive.Storage.Providers.Sql.Compilers
{
  internal sealed class SortProviderCompiler : TypeCompiler<SortProvider>
  {
    protected override ExecutableProvider Compile(SortProvider provider)
    {
      var source = (SqlProvider)Compiler.Compile(provider.Source, true);
      var queryRef = Xtensive.Sql.Dom.Sql.QueryRef(source.Query);
      SqlSelect query = Xtensive.Sql.Dom.Sql.Select(queryRef);
      query.Columns.AddRange(queryRef.Columns.Cast<SqlColumn>());
      foreach (KeyValuePair<int, Direction> sortOrder in provider.SortOrder)
        query.OrderBy.Add(sortOrder.Key, sortOrder.Value==Direction.Positive);

      return new SqlProvider(provider, query);
    }

    // Constructors

    public SortProviderCompiler(Rse.Compilation.Compiler provider)
      : base(provider)
    {
    }
  }
}