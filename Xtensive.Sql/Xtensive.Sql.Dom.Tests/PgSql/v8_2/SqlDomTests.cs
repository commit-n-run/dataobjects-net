﻿using NUnit.Framework;

namespace Xtensive.Sql.Dom.Tests.PgSql.v8_2
{
  [TestFixture]
  public class SqlDomTests : v8_1.SqlDomTests
  {
    protected override string Url { get { return TestUrl.PgSql82; } }
  }
}