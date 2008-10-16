// Copyright (C) 2008 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Dmitri Maximov
// Created:    2008.09.23

using System;
using System.Data;
using System.Data.Common;
using Xtensive.Core.Reflection;
using Xtensive.Sql.Common;
using Xtensive.Sql.Dom;
using Xtensive.Storage.Providers.Sql.Mappings;
using ColumnInfo=Xtensive.Storage.Model.ColumnInfo;

namespace Xtensive.Storage.Providers.Sql
{
  public class SqlValueTypeMapper : InitializableHandlerBase
  {
    protected DomainHandler DomainHandler { get; private set; }

    /// <summary>
    /// Gets the data type mapping schema.
    /// </summary>
    protected DataTypeMappingSchema MappingSchema { get; private set; }

    /// <summary>
    /// Gets the type mapping.
    /// </summary>
    /// <param name="column">The column.</param>
    /// <returns><see cref="DataTypeMapping"/> instance for the specified <paramref name="column"/>.</returns>
    public DataTypeMapping GetTypeMapping(ColumnInfo column)
    {
      int length = column.Length.HasValue ? column.Length.Value : 0;
      Type type = column.ValueType;

      return GetTypeMapping(type, length);
    }
    /// <summary>
    /// Gets the type mapping.
    /// </summary>
    /// <param name="type">The column type.</param>
    /// <param name="length">The column length.</param>
    /// <returns><see cref="DataTypeMapping"/> instance for the specified <paramref name="type"/> and <paramref name="length"/>.</returns>
    /// <exception cref="InvalidOperationException"><param name="type">Type</param> is not supported.</exception>
    public DataTypeMapping GetTypeMapping(Type type, int length)
    {
      {
        DataTypeMapping mapping = MappingSchema.GetExactMapping(type);
        if (mapping != null)
          return mapping;
      }

      DataTypeMapping[] ambigiousMappings = MappingSchema.GetAmbigiousMappings(type);
      if (ambigiousMappings!=null) {
        foreach (DataTypeMapping mapping in ambigiousMappings) {

          StreamDataTypeInfo sdti = mapping.DataTypeInfo as StreamDataTypeInfo;
          if (sdti == null)
            return mapping;

          if (length == 0)
            return mapping;

          if (sdti.Length.MaxValue < length)
            continue;

          return mapping;
        }
      }
      throw new InvalidOperationException(string.Format("Type '{0}' is not supported.", type.GetShortName()));
    }

    /// <summary>
    /// Gets the type mapping.
    /// </summary>
    /// <param name="type">The column type.</param>
    /// <returns><see cref="DataTypeMapping"/> instance for the specified <paramref name="type"/>.</returns>
    /// <exception cref="InvalidOperationException"><param name="type">Type</param> is not supported.</exception>
    public DataTypeMapping GetTypeMapping(Type type)
    {
      return GetTypeMapping(type, 0);
    }

    public SqlValueType BuildSqlValueType(ColumnInfo columnInfo)
    {
      DataTypeMapping dtm = GetTypeMapping(columnInfo);
      return BuildSqlValueType(columnInfo, dtm);
    }

    public SqlValueType BuildSqlValueType(Type type, int length)
    {
      DataTypeMapping dtm = GetTypeMapping(type);
      return BuildSqlValueType(type, length, dtm);
    }

    private SqlValueType BuildSqlValueType(ColumnInfo column, DataTypeMapping typeMapping)
    {
      int length = column.Length.HasValue ? column.Length.Value : 0;
      Type type = column.ValueType;

      return BuildSqlValueType(type, length, typeMapping);
    }

    private SqlValueType BuildSqlValueType(Type type, int length, DataTypeMapping typeMapping)
    {
      StreamDataTypeInfo sdti = typeMapping.DataTypeInfo as StreamDataTypeInfo;
      if (sdti == null)
        return new SqlValueType(typeMapping.DataTypeInfo.SqlType);

      if (length == 0)
        return new SqlValueType(sdti.SqlType, sdti.Length.MaxValue);

      return new SqlValueType(sdti.SqlType, length);

    }

    /// <inheritdoc/>
    public override void Initialize()
    {
      DomainHandler = Handlers.DomainHandler as DomainHandler;
      MappingSchema = new DataTypeMappingSchema();
      BuildNativeTypes();
      BuildTypeSubstitutes();
    }

    private void BuildNativeTypes()
    {
      DataTypeCollection types = DomainHandler.SqlDriver.ServerInfo.DataTypes;
      BuildDataTypeMapping(types.Boolean);
      BuildDataTypeMapping(types.Byte);
      BuildDataTypeMapping(types.DateTime);
      BuildDataTypeMapping(types.Decimal);
      BuildDataTypeMapping(types.Double);
      BuildDataTypeMapping(types.Float);
      BuildDataTypeMapping(types.Guid);
      BuildDataTypeMapping(types.Int16);
      BuildDataTypeMapping(types.Int32);
      BuildDataTypeMapping(types.Int64);
      BuildDataTypeMapping(types.SByte);
      BuildDataTypeMapping(types.UInt16);
      BuildDataTypeMapping(types.UInt32);
      BuildDataTypeMapping(types.UInt64);
      BuildDataTypeMapping(types.VarBinary);
      BuildDataTypeMapping(types.VarBinaryMax);
      BuildDataTypeMapping(types.VarChar);
      BuildDataTypeMapping(types.VarCharMax);
    }

    protected virtual void BuildTypeSubstitutes()
    {
    }

    protected void BuildDataTypeMapping(DataTypeInfo dataTypeInfo)
    {
      if (dataTypeInfo == null)
        return;

      DataTypeMapping mapping = CreateDataTypeMapping(dataTypeInfo);
      MappingSchema.Register(mapping);
    }

    protected virtual DataTypeMapping CreateDataTypeMapping(DataTypeInfo dataTypeInfo)
    {
      var dataReaderAccessor = BuildDataReaderAccessor(dataTypeInfo);
      if (dataTypeInfo.Type==typeof (byte[]))
        return new DataTypeMapping(dataTypeInfo, dataReaderAccessor, DbType.Binary);
      return new DataTypeMapping(dataTypeInfo, dataReaderAccessor, GetDbType(dataTypeInfo));
    }

    protected virtual DbType GetDbType(DataTypeInfo dataTypeInfo)
    {
      DbType result = DbType.String;
      TypeCode typeCode = Type.GetTypeCode(dataTypeInfo.Type);
      switch (typeCode) {
      case TypeCode.Boolean:
        result = DbType.Boolean;
        break;
      case TypeCode.Char:
        result = DbType.StringFixedLength;
        break;
      case TypeCode.SByte:
        result = DbType.SByte;
        break;
      case TypeCode.Byte:
        result = DbType.Byte;
        break;
      case TypeCode.Int16:
        result = DbType.Int16;
        break;
      case TypeCode.UInt16:
        result = DbType.UInt16;
        break;
      case TypeCode.Int32:
        result = DbType.Int32;
        break;
      case TypeCode.UInt32:
        result = DbType.UInt32;
        break;
      case TypeCode.Int64:
        result = DbType.Int64;
        break;
      case TypeCode.UInt64:
        result = DbType.UInt64;
        break;
      case TypeCode.Single:
        result = DbType.Single;
        break;
      case TypeCode.Double:
        result = DbType.Double;
        break;
      case TypeCode.Decimal:
        result = DbType.Decimal;
        break;
      case TypeCode.DateTime:
        result = DbType.DateTime;
        break;
      case TypeCode.String:
        break;
      default:
        throw new ArgumentOutOfRangeException();
      }
      return result;
    }

    protected virtual Func<DbDataReader, int, object> BuildDataReaderAccessor(DataTypeInfo dataTypeInfo)
    {
      Type type = dataTypeInfo.Type;
      TypeCode typeCode = Type.GetTypeCode(type);
      switch (typeCode) {
      case TypeCode.Object:
          if (type == typeof(byte[]))
            return (reader, fieldIndex) => reader.GetValue(fieldIndex);
          if (type == typeof(Guid))
            return (reader, fieldIndex) => reader.GetGuid(fieldIndex);
          else
            throw new ArgumentOutOfRangeException();
      case TypeCode.Boolean:
        return (reader, fieldIndex) => reader.GetBoolean(fieldIndex);
      case TypeCode.Char:
        return (reader, fieldIndex) => reader.GetChar(fieldIndex);
      case TypeCode.SByte:
        return (reader, fieldIndex) => Convert.ToSByte(reader.GetDecimal(fieldIndex));
      case TypeCode.Byte:
        return (reader, fieldIndex) => reader.GetByte(fieldIndex);
      case TypeCode.Int16:
        return (reader, fieldIndex) => reader.GetInt16(fieldIndex);
      case TypeCode.UInt16:
        return (reader, fieldIndex) => Convert.ToUInt16(reader.GetDecimal(fieldIndex));
      case TypeCode.Int32:
        return (reader, fieldIndex) => reader.GetInt32(fieldIndex);
      case TypeCode.UInt32:
        return (reader, fieldIndex) => Convert.ToUInt32(reader.GetDecimal(fieldIndex));
      case TypeCode.Int64:
        return (reader, fieldIndex) => reader.GetInt64(fieldIndex);
      case TypeCode.UInt64:
        return (reader, fieldIndex) => Convert.ToUInt64(reader.GetDecimal(fieldIndex));
      case TypeCode.Single:
        return (reader, fieldIndex) => reader.GetFloat(fieldIndex);
      case TypeCode.Double:
        return (reader, fieldIndex) => reader.GetDouble(fieldIndex);
      case TypeCode.Decimal:
        return (reader, fieldIndex) => reader.GetDecimal(fieldIndex);
      case TypeCode.DateTime:
        return (reader, fieldIndex) => reader.GetDateTime(fieldIndex);
      case TypeCode.String:
        return (reader, fieldIndex) => reader.GetString(fieldIndex);
      default:
        throw new ArgumentOutOfRangeException();
      }
    }
  }
}