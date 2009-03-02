﻿// Copyright (C) 2009 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Denis Krjuchkov
// Created:    2009.03.02

namespace Xtensive.Sql.Dom.Tests
{
  public static class TestUrl
  {
    public const string MsSql2000 = "mssql://localhost/Northwind";
    public const string MsSql2005 = "mssql2005://localhost/AdventureWorks";

    public const string PgSql80 = "pgsql://do4test:do4testpwd@localhost:8032/do4test?Encoding=ASCII&Pooling=on&MinPoolSize=1&MaxPoolSize=5";
    public const string PgSql81 = "pgsql://do4test:do4testpwd@localhost:8132/do4test?Encoding=ASCII&Pooling=on&MinPoolSize=1&MaxPoolSize=5";
    public const string PgSql82 = "pgsql://do4test:do4testpwd@localhost:8232/do4test?Encoding=UNICODE&Pooling=on&MinPoolSize=1&MaxPoolSize=5";
    public const string PgSql83 = "pgsql://do4test:do4testpwd@localhost:8332/do4test?Encoding=ASCII&Pooling=on&MinPoolSize=1&MaxPoolSize=5";

    public const string VistaDb = "vistadb://localhost/VistaDb/VDBTests.vdb3";
  }
}
