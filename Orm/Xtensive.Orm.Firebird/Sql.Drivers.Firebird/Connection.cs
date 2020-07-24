// Copyright (C) 2011-2020 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.
// Created by: Csaba Beer
// Created:    2011.01.08

using System;
using System.Data;
using System.Data.Common;
using FirebirdSql.Data.FirebirdClient;
using Xtensive.Orm;

namespace Xtensive.Sql.Drivers.Firebird
{
  internal class Connection : SqlConnection
  {
    private FbConnection underlyingConnection;
    private FbTransaction activeTransaction;

    /// <inheritdoc/>
    public override DbConnection UnderlyingConnection
    {
      get { return underlyingConnection; }
    }

    /// <inheritdoc/>
    public override DbTransaction ActiveTransaction
    {
      get { return activeTransaction; }
    }

    /// <inheritdoc/>
    public override DbParameter CreateParameter()
    {
      return new FbParameter();
    }

    /// <inheritdoc/>
    public override void BeginTransaction()
    {
      BeginTransaction(IsolationLevel.Serializable);
    }

    /// <inheritdoc/>
    public override void BeginTransaction(IsolationLevel isolationLevel)
    {
      EnsureTransactionIsNotActive();
      var transactionOptions = new FbTransactionOptions {WaitTimeout = TimeSpan.FromSeconds(10)};
      switch (SqlHelper.ReduceIsolationLevel(isolationLevel)) {
      case IsolationLevel.ReadCommitted:
        transactionOptions.TransactionBehavior = FbTransactionBehavior.ReadCommitted
          | FbTransactionBehavior.NoRecVersion
          | FbTransactionBehavior.Write
          | FbTransactionBehavior.NoWait;
        break;
      case IsolationLevel.Serializable:
        transactionOptions.TransactionBehavior = FbTransactionBehavior.Concurrency
          | FbTransactionBehavior.Write
          | FbTransactionBehavior.Wait;
        break;
      }
      activeTransaction = underlyingConnection.BeginTransaction(transactionOptions);
    }

    /// <inheritdoc/>
    protected override void ClearActiveTransaction()
    {
      activeTransaction = null;
    }

    /// <inheritdoc/>
    public override void MakeSavepoint(string name)
    {
      EnsureTransactionIsActive();
      activeTransaction.Save(name);
    }

    /// <inheritdoc/>
    public override void RollbackToSavepoint(string name)
    {
      EnsureTransactionIsActive();
      activeTransaction.Rollback(name);
    }

    // Constructors
    public Connection(SqlDriver driver)
      : base(driver)
    {
      underlyingConnection = new FbConnection();
    }
  }
} ;