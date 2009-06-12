// Copyright (C) 2009 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Dmitri Maximov
// Created:    2009.05.19

using System;
using System.Text;
using NUnit.Framework;
using Xtensive.Core.Reflection;
using Xtensive.Storage.Configuration;

namespace Xtensive.Storage.Tests
{
  [TestFixture]
  public class DomainConfigurationFactoryTest : AutoBuildTest
  {
    private DomainConfiguration config;

    [TestFixtureSetUp]
    public override void TestFixtureSetUp()
    {
      config = BuildConfiguration();
    }

    [Test]
    public void MainTest()
    {
      var result = new StringBuilder();
      result.Append("ConnectionString: ").AppendLine(config.ConnectionInfo.Url);
      result.Append("ForeignKeyMode: ").AppendLine(config.ForeignKeyMode.ToString());
      if (config.Builders.Count > 0) {
        result.AppendLine("CustomBuilders:");
        foreach (var builder in config.Builders) {
          result.Append("\t- ");
          result.AppendLine(builder.GetShortName());
        }
      }
      Log.Error(result.ToString());
    }
  }
}