// Copyright (C) 2011-2020 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.
// Created by: Malisa Ncube
// Created:    2011.04.29

using System;
using System.Data;
using System.Data.Common;
using System.Data.SQLite;
using System.Security;
using Xtensive.Orm;

namespace Xtensive.Sql.Drivers.Sqlite
{
  internal class Connection : SqlConnection
  {
    private SQLiteConnection underlyingConnection;
    private SQLiteTransaction activeTransaction;

    /// <inheritdoc/>
    public override DbConnection UnderlyingConnection { get { return underlyingConnection; } }

    /// <inheritdoc/>
    public override DbTransaction ActiveTransaction { get { return activeTransaction; } }

    /// <inheritdoc/>
    [SecuritySafeCritical]
    public override DbParameter CreateParameter()
    {
      return new SQLiteParameter();
    }

    /// <inheritdoc/>
    [SecuritySafeCritical]
    public override void BeginTransaction()
    {
      EnsureTransactionIsNotActive();
      activeTransaction = underlyingConnection.BeginTransaction();
    }

    /// <inheritdoc/>
    [SecuritySafeCritical]
    public override void BeginTransaction(IsolationLevel isolationLevel)
    {
      EnsureTransactionIsNotActive();
      activeTransaction = underlyingConnection.BeginTransaction(SqlHelper.ReduceIsolationLevel(isolationLevel));
    }

    /// <inheritdoc/>
    public override void MakeSavepoint(string name)
    {
      EnsureTransactionIsActive();
      var commandText = string.Format("SAVEPOINT {0}", name);
      using (var command = CreateCommand(commandText))
        command.ExecuteNonQuery();
    }

    /// <inheritdoc/>
    public override void RollbackToSavepoint(string name)
    {
      EnsureTransactionIsActive();
      var commandText = string.Format("ROLLBACK TO SAVEPOINT {0}; RELEASE SAVEPOINT {0};", name);
      using (var command = CreateCommand(commandText))
        command.ExecuteNonQuery();
    }

    /// <inheritdoc/>
    public override void ReleaseSavepoint(string name)
    {
      EnsureTransactionIsActive();
      var commandText = string.Format("RELEASE SAVEPOINT {0}", name);
      using (var command = CreateCommand(commandText))
        command.ExecuteNonQuery();
    }

    /// <inheritdoc/>
    protected override void ClearActiveTransaction()
    {
      activeTransaction = null;
    }


    // Constructors

    /// <inheritdoc/>
    [SecuritySafeCritical]
    public Connection(SqlDriver driver)
      : base(driver)
    {
      underlyingConnection = new SQLiteConnection();
    }
  }
}