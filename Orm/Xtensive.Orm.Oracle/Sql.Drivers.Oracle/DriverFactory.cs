// Copyright (C) 2009-2020 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.
// Created by: Denis Krjuchkov
// Created:    2009.07.16

using System;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Oracle.ManagedDataAccess.Client;
using Xtensive.Core;
using Xtensive.Orm;
using Xtensive.Sql.Info;
using Xtensive.Sql.Drivers.Oracle.Resources;

namespace Xtensive.Sql.Drivers.Oracle
{
  /// <summary>
  /// A <see cref="SqlDriver"/> factory for Oracle.
  /// </summary>
  public class DriverFactory : SqlDriverFactory
  {
    private const int DefaultPort = 1521;
    private const string DataSourceFormat =
      "(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST={0})(PORT={1}))(CONNECT_DATA=(SERVICE_NAME={2})))";
    private const string DatabaseAndSchemaQuery =
      "select sys_context('USERENV', 'DB_NAME'), sys_context('USERENV', 'CURRENT_SCHEMA') from dual";

    private static Version ParseVersion(string version)
    {
      var items = version.Split('.').Take(4).Select(int.Parse).ToArray();
      return new Version(items[0], items[1], items[2], items[3]);
    }

    protected override string BuildConnectionString(UrlInfo url)
    {
      SqlHelper.ValidateConnectionUrl(url);
      ArgumentValidator.EnsureArgumentNotNullOrEmpty(url.Resource, "url.Resource");

      var builder = new OracleConnectionStringBuilder();

      // host, port, database
      if (!string.IsNullOrEmpty(url.Host)) {
        var port = url.Port!=0 ? url.Port : DefaultPort;
        builder.DataSource = string.Format(DataSourceFormat, url.Host, port, url.Resource);
      }
      else {
        builder.DataSource = url.Resource; // Plain TNS name
      }

      // user, password
      if (!string.IsNullOrEmpty(url.User)) {
        builder.UserID = url.User;
        builder.Password = url.Password;
      }
      else {
        builder.UserID = "/";
      }

      // custom options
      foreach (var parameter in url.Params) {
        builder.Add(parameter.Key, parameter.Value);
      }

      return builder.ToString();
    }
    
    /// <inheritdoc/>
    protected override SqlDriver CreateDriver(string connectionString, SqlDriverConfiguration configuration)
    {
      using var connection = new OracleConnection(connectionString);
      connection.Open();
      SqlHelper.ExecuteInitializationSql(connection, configuration);
      var version = string.IsNullOrEmpty(configuration.ForcedServerVersion)
        ? ParseVersion(connection.ServerVersion)
        : new Version(configuration.ForcedServerVersion);
      var defaultSchema = GetDefaultSchema(connection);
      return CreateDriverInstance(connectionString, version, defaultSchema);
    }

    /// <inheritdoc/>
    protected override async Task<SqlDriver> CreateDriverAsync(
      string connectionString, SqlDriverConfiguration configuration, CancellationToken token)
    {
      var connection = new OracleConnection(connectionString);
      await using (connection.ConfigureAwait(false)) {
        await connection.OpenAsync(token).ConfigureAwait(false);
        await SqlHelper.ExecuteInitializationSqlAsync(connection, configuration, token).ConfigureAwait(false);
        var version = string.IsNullOrEmpty(configuration.ForcedServerVersion)
          ? ParseVersion(connection.ServerVersion)
          : new Version(configuration.ForcedServerVersion);
        var defaultSchema = await GetDefaultSchemaAsync(connection, token: token).ConfigureAwait(false);
        return CreateDriverInstance(connectionString, version, defaultSchema);
      }
    }

    private static SqlDriver CreateDriverInstance(string connectionString, Version version, DefaultSchemaInfo defaultSchema)
    {
      var coreServerInfo = new CoreServerInfo {
        ServerVersion = version,
        ConnectionString = connectionString,
        MultipleActiveResultSets = true,
        DatabaseName = defaultSchema.Database,
        DefaultSchemaName = defaultSchema.Schema,
      };
      if (version.Major < 9 || (version.Major == 9 && version.Minor < 2)) {
        throw new NotSupportedException(Strings.ExOracleBelow9i2IsNotSupported);
      }

      return version.Major switch {
        9 => new v09.Driver(coreServerInfo),
        10 => new v10.Driver(coreServerInfo),
        _ => new v11.Driver(coreServerInfo)
      };
    }

    /// <inheritdoc/>
    protected override DefaultSchemaInfo ReadDefaultSchema(DbConnection connection, DbTransaction transaction) =>
      SqlHelper.ReadDatabaseAndSchema(DatabaseAndSchemaQuery, connection, transaction);

    /// <inheritdoc/>
    protected override Task<DefaultSchemaInfo> ReadDefaultSchemaAsync(
      DbConnection connection, DbTransaction transaction, CancellationToken token) =>
      SqlHelper.ReadDatabaseAndSchemaAsync(DatabaseAndSchemaQuery, connection, transaction, token);
  }
}