// Copyright (C) 2009 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Alexey Gamzov
// Created:    2009.09.03

using System;
using NUnit.Framework;
using Xtensive.Storage.Configuration;
using Xtensive.Storage.Tests.Issues.Issue0372_SelfReferenceWithInheritance_Model;

namespace Xtensive.Storage.Tests.Issues.Issue0372_SelfReferenceWithInheritance_Model
{
  [HierarchyRoot]
  public class MyEntity
  {
    [Field, Key]
    public int Id { get; private set; }

    [Field]
    private MyEntity MyEntityField { get; set; }

    public MyEntity()
    {
      MyEntityField = this;
    }
  }

  [Serializable]
  [HierarchyRoot]
  public class Item
    : Entity
  {
    [Field, Key]
    public int Id { get; private set; }

    [Field]
    public WebSite WebSite { get; set; }

    [Field]
    public WebSite WebSite2 { get; set; }
  }

  [Serializable]
  public class WebSite
    : Item
  {
    [Field]
    public string DomainName { get; set; }
  }
}

namespace Xtensive.Storage.Tests.Issues
{
  public class Issue0372_SelfReferenceWithInheritance : AutoBuildTest
  {
    protected override DomainConfiguration BuildConfiguration()
    {
      var config = base.BuildConfiguration();
      config.Types.Register(typeof (WebSite).Assembly, typeof (WebSite).Namespace);
      return config;
    }

    [Test]
    public void MainTest()
    {
      using (Session.Open(Domain)) {
        using (var t = Transaction.Open()) {
          var webSite = new WebSite();
          webSite.WebSite = webSite; // self-refernece
          Session.Current.Persist();
          // Rollback
        }
      }
    }

    [Test]
    public void DualSelfreferenceTest()
    {
      using (Session.Open(Domain)) {
        using (var t = Transaction.Open()) {
          var webSite = new WebSite();
          webSite.WebSite = webSite; // self-refernece 1
          webSite.WebSite2 = webSite; // self-refernece 2
          Session.Current.Persist();
          // Rollback
        }
      }
    }

    [Test]
    public void SelfreferenceTest()
    {
      using (Session.Open(Domain)) {
        using (var t = Transaction.Open()) {
          var myEntity = new MyEntity();
          Session.Current.Persist();
          // Rollback
        }
      }
    }
  }
}