// Copyright (C) 2008 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Dmitri Maximov
// Created:    2008.09.10

using Xtensive.Sql.Dom;
using Xtensive.Sql.Dom.Database;
using Xtensive.Sql.Dom.Dml;
using Xtensive.Storage.Model;
using Xtensive.Storage.Providers.Sql;
using SqlFactory = Xtensive.Sql.Dom.Sql;

namespace Xtensive.Storage.Providers.MsSql
{
  /// <summary>
  /// Generator factory
  /// </summary>
  public sealed class KeyGeneratorFactory : Providers.KeyGeneratorFactory
  {
    /// <inheritdoc/>
    protected override KeyGenerator CreateGenerator<TFieldType>(GeneratorInfo generatorInfo)
    {
      var dh = (DomainHandler) Handlers.DomainHandler;
      Schema schema = dh.Schema;
      SqlValueType columnType = dh.ValueTypeMapper.BuildSqlValueType(generatorInfo.TupleDescriptor[0], 0);

      var genTable = schema.CreateTable(generatorInfo.MappingName);
      var column = genTable.CreateColumn("ID", columnType);
      column.SequenceDescriptor = new SequenceDescriptor(column, generatorInfo.CacheSize, generatorInfo.CacheSize);

      SqlBatch sqlNext = SqlFactory.Batch();
      SqlInsert insert = SqlFactory.Insert(SqlFactory.TableRef(genTable));
      sqlNext.Add(insert);
      SqlSelect select = SqlFactory.Select();
      select.Columns.Add(SqlFactory.Cast(SqlFactory.FunctionCall("SCOPE_IDENTITY"), columnType.DataType));
      sqlNext.Add(select);

      return new SqlCachingKeyGenerator<TFieldType>(generatorInfo, sqlNext);
    }
  }
}