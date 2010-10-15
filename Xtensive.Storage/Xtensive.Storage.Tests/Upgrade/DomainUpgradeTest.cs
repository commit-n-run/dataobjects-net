// Copyright (C) 2003-2010 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Ivan Galkin
// Created:    2009.05.20

using System;
using System.Linq;
using NUnit.Framework;
using System.Reflection;
using Xtensive.Core.Disposing;
using Xtensive.Core;
using Xtensive.Core.Testing;
using M1 = Xtensive.Storage.Tests.Upgrade.Model.Version1;
using M2 = Xtensive.Storage.Tests.Upgrade.Model.Version2;

namespace Xtensive.Storage.Tests.Upgrade
{
  [TestFixture, Category("Upgrade")]
  public class DomainUpgradeTest
  {
    private Domain domain;

    [TestFixtureSetUp]
    public void TestSetUp()
    {
      Require.ProviderIsNot(StorageProvider.Memory);
    }

    [SetUp]
    public void SetUp()
    {
      BuildDomain("1", DomainUpgradeMode.Recreate);
      FillData();
    }
    
    [Test]
    public void UpgradeModeTest()
    {
      BuildDomain("1", DomainUpgradeMode.Recreate, null, typeof (M1.Address), typeof (M1.Person));

      BuildDomain("1", DomainUpgradeMode.PerformSafely, null, typeof (M1.Address), typeof (M1.Person), typeof (M1.BusinessContact));
      AssertEx.Throws<SchemaSynchronizationException>(() =>
        BuildDomain("1", DomainUpgradeMode.PerformSafely, null, typeof (M1.Address), typeof (M1.Person)));

      BuildDomain("1", DomainUpgradeMode.Validate, null, typeof (M1.Address), typeof (M1.Person), typeof (M1.BusinessContact));
      AssertEx.Throws<SchemaSynchronizationException>(() =>
        BuildDomain("1", DomainUpgradeMode.Validate, null, typeof (M1.Address), typeof (M1.Person)));
      AssertEx.Throws<SchemaSynchronizationException>(() =>
        BuildDomain("1", DomainUpgradeMode.Validate, null, typeof (M1.Address), typeof (M1.Person), 
        typeof (M1.BusinessContact), typeof (M1.Employee), typeof (M1.Order)));
    }

    [Test]
    public void UpgradeGeneratorsTest()
    {
      var generatorCacheSize = 3;
      BuildDomain("1", DomainUpgradeMode.Recreate, generatorCacheSize, typeof (M1.Address), typeof (M1.Person));
      using (var session = domain.OpenSession()) {
        using (var t = session.OpenTransaction()) {
          for (int i = 0; i < generatorCacheSize; i++)
            new M1.Person {
              Address = new M1.Address {City = "City", Country = "Country"}
            };
          Assert.AreEqual(3, session.Query.All<M1.Person>().Max(p => p.Id));
          t.Complete();
        }
      }
      BuildDomain("1", DomainUpgradeMode.Perform, generatorCacheSize, typeof (M1.Address), typeof (M1.Person));
      using (var session = domain.OpenSession()) {
        using (var t = session.OpenTransaction()) {
          for (int i = 0; i < generatorCacheSize; i++)
            new M1.Person {
              Address = new M1.Address {City = "City", Country = "Country"}
            };
          Assert.AreEqual(6, session.Query.All<M1.Person>().Max(p => p.Id));
          t.Complete();
        }
      }
      
      generatorCacheSize = 2;
      BuildDomain("1", DomainUpgradeMode.Perform, generatorCacheSize, typeof (M1.Address), typeof (M1.Person));
      using (var session = domain.OpenSession()) {
        using (var t = session.OpenTransaction()) {
          new M1.Person {Address = new M1.Address {City = "City", Country = "Country"}};
          new M1.Person {Address = new M1.Address {City = "City", Country = "Country"}};
          new M1.Person {Address = new M1.Address {City = "City", Country = "Country"}};
          Assert.AreEqual(12, session.Query.All<M1.Person>().Max(p => p.Id));
          t.Complete();
        }
      }
    }

    [Test]
    public void UpdateTypeIdTest()
    {
      int personTypeId;
      int maxTypeId;

      BuildDomain("1", DomainUpgradeMode.Recreate, null, typeof (M1.Address), typeof (M1.Person));
      using (var session = domain.OpenSession()) {
        using (var t = session.OpenTransaction()) {
          personTypeId = session.Query.All<Metadata.Type>()
            .First(type => type.Name=="Xtensive.Storage.Tests.Upgrade.Model.Version1.Person").Id;
          maxTypeId = session.Query.All<Metadata.Type>().Max(type => type.Id);
        }
      }

      BuildDomain("1", DomainUpgradeMode.PerformSafely, null, typeof (M1.Address), typeof (M1.Person), typeof (M1.BusinessContact));
      using (var session = domain.OpenSession()) {
        using (var t = session.OpenTransaction()) {
          var businessContactTypeId = session.Query.All<Metadata.Type>()
            .First(type => type.Name=="Xtensive.Storage.Tests.Upgrade.Model.Version1.BusinessContact").Id;
          var newPersonTypeId = session.Query.All<Metadata.Type>()
            .First(type => type.Name=="Xtensive.Storage.Tests.Upgrade.Model.Version1.Person").Id;

          Assert.AreEqual(personTypeId, newPersonTypeId);
          Assert.AreEqual(maxTypeId + 1, businessContactTypeId);
        }
      }
    }

    [Test]
    public void UpdateTypeIdWithMutualRenameTest()
    {
      int personTypeId;
      int businessContactTypeId;
      
      using (var session = domain.OpenSession()) {
        using (var t = session.OpenTransaction()) {
          personTypeId = session.Query.All<Metadata.Type>()
            .First(type => type.Name=="Xtensive.Storage.Tests.Upgrade.Model.Version1.Person").Id;
          businessContactTypeId = session.Query.All<Metadata.Type>()
            .First(type => type.Name=="Xtensive.Storage.Tests.Upgrade.Model.Version1.BusinessContact").Id;
        }
      }

      BuildDomain("2", DomainUpgradeMode.Perform);
      using (var session = domain.OpenSession()) {
        using (var t = session.OpenTransaction()) {
          var newBusinessContactTypeId = session.Query.All<Metadata.Type>()
            .First(type => type.Name=="Xtensive.Storage.Tests.Upgrade.Model.Version2.BusinessContact").Id;
          var newPersonTypeId = session.Query.All<Metadata.Type>()
            .First(type => type.Name=="Xtensive.Storage.Tests.Upgrade.Model.Version2.Person").Id;

          Assert.AreEqual(personTypeId, newBusinessContactTypeId);
          Assert.AreEqual(businessContactTypeId, newPersonTypeId);
        }
      }
    }

    [Test]
    public void UpgradeToVersion2Test()
    {
      BuildDomain("2", DomainUpgradeMode.Perform);
      using (var session = domain.OpenSession()) {
        using (session.OpenTransaction()) {
          Assert.AreEqual(2, session.Query.All<M2.Person>().Count());
          Assert.AreEqual("Island Trading", session.Query.All<M2.Person>()
            .First(person => person.ContactName=="Helen Bennett").CompanyName);
          Assert.AreEqual(5, session.Query.All<M2.BusinessContact>().Count());
          Assert.AreEqual("Suyama", session.Query.All<M2.BusinessContact>()
            .First(contact => contact.FirstName=="Michael").LastName);
          Assert.AreEqual("Fuller", session.Query.All<M2.Employee>()
            .First(employee => employee.FirstName=="Nancy").ReportsTo.LastName);
          Assert.AreEqual(123, session.Query.All<M2.Person>()
            .First(person => person.ContactName=="Helen Bennett").PassportNumber);
          Assert.AreEqual(1, session.Query.All<M2.Order>()
            .First(order => order.ProductName=="Maxilaku").Number);

          session.Query.All<M2.Product>().Single(product => product.Title=="DataObjects.NET");
          session.Query.All<M2.Product>().Single(product => product.Title=="HelpServer");
          Assert.AreEqual(2, session.Query.All<M2.Product>().Count());
          var webApps = session.Query.All<M2.ProductGroup>().Single(group => group.Name=="Web applications");
          var frameworks = session.Query.All<M2.ProductGroup>().Single(group => group.Name=="Frameworks");
          Assert.AreEqual(1, webApps.Products.Count);
          Assert.AreEqual(1, frameworks.Products.Count);

          var allBoys = session.Query.All<M2.Boy>().ToArray();
          var allGirls = session.Query.All<M2.Girl>().ToArray();
          Assert.AreEqual(2, allBoys.Length);
          Assert.AreEqual(2, allGirls.Length);
          var alex = allBoys.Single(boy => boy.Name=="Alex");
          foreach (var girl in allGirls)
            Assert.IsTrue(alex.MeetWith.Contains(girl));

          var e1 = session.Query.All<M2.Entity1>().Single();
          var e2 = session.Query.All<M2.Entity2>().Single();
          var e3 = session.Query.All<M2.Entity3>().Single();
          var e4 = session.Query.All<M2.Entity4>().Single();
          var se1 = session.Query.All<M2.StructureContainer1>().Single();
          var se2 = session.Query.All<M2.StructureContainer2>().Single();
          var se3 = session.Query.All<M2.StructureContainer3>().Single();
          var se4 = session.Query.All<M2.StructureContainer4>().Single();

          Assert.AreEqual(1, e1.Code);
          Assert.AreEqual(2, e2.Code);
          Assert.AreEqual(3, e3.Code);
          Assert.AreEqual(4, e4.Code);

          Assert.AreEqual(e1, e2.E1);
          Assert.AreEqual(e2, e3.E2);
          Assert.AreEqual(e3, e4.E3);

          Assert.AreEqual(se1.S1.MyE1, e1);

          Assert.AreEqual(se2.S2.MyE2, e2);
          Assert.AreEqual(se2.S2.S1.MyE1, e1);

          Assert.AreEqual(se3.S3.MyE3, e3);
          Assert.AreEqual(se3.S3.S2.MyE2, e2);
          Assert.AreEqual(se3.S3.S2.S1.MyE1, e1);

          Assert.AreEqual(se4.S4.MyE4, e4);
          Assert.AreEqual(se4.S4.S3.MyE3, e3);
          Assert.AreEqual(se4.S4.S3.S2.MyE2, e2);
          Assert.AreEqual(se4.S4.S3.S2.S1.MyE1, e1);

          var so1 = session.Query.All<M2.MyStructureOwner>().Single(e => e.Id==0);
          var so2 = session.Query.All<M2.MyStructureOwner>().Single(e => e.Id==1);
          var re1 = session.Query.All<M2.ReferencedEntity>().Single(e => e.A==1 && e.B==2);
          var re2 = session.Query.All<M2.ReferencedEntity>().Single(e => e.A==2 && e.B==3);
          if (!IncludeTypeIdModifier.IsEnabled) {
            Assert.AreEqual(so1.Reference, re1);
            Assert.AreEqual(so2.Reference, re2);
          }

          Assert.AreEqual(2, session.Query.All<M2.NewSync<M2.BusinessContact>>().Count());
          Assert.AreEqual("Alex", session.Query.All<M2.NewSync<M2.Boy>>().First().NewRoot.Name);
        }
      }
    }

    private void BuildDomain(string version, DomainUpgradeMode upgradeMode)
    {
      if (domain!=null)
        domain.DisposeSafely();

      var configuration = DomainConfigurationFactory.Create();
      configuration.UpgradeMode = upgradeMode;
      configuration.Types.Register(Assembly.GetExecutingAssembly(),
        "Xtensive.Storage.Tests.Upgrade.Model.Version" + version);
      configuration.Types.Register(typeof (Upgrader));
      using (Upgrader.Enable(version)) {
        domain = Domain.Build(configuration);
      }
    }

    private void BuildDomain(string version, DomainUpgradeMode upgradeMode, int? keyCacheSize, params Type[] types)
    {
      if (domain != null)
        domain.DisposeSafely();

      var configuration = DomainConfigurationFactory.Create();
      configuration.UpgradeMode = upgradeMode;
      foreach (var type in types)
        configuration.Types.Register(type);
      if (keyCacheSize.HasValue)
        configuration.KeyGeneratorCacheSize = keyCacheSize.Value;
      configuration.Types.Register(typeof (Upgrader));
      using (Upgrader.Enable(version)) {
        domain = Domain.Build(configuration);
      }
    }

    #region Data filler

    private void FillData()
    {
      using (var session = domain.OpenSession()) {
        using (var transactionScope = session.OpenTransaction()) {
          // BusinessContacts
          var helen = new M1.BusinessContact {
            Address = new M1.Address {
              City = "Cowes",
              Country = "UK"
            },
            CompanyName = "Island Trading",
            ContactName = "Helen Bennett",
            PassportNumber = "123"
          };
          var philip = new M1.BusinessContact {
            Address = new M1.Address {
              City = "Brandenburg",
              Country = "Germany"
            },
            CompanyName = "Koniglich Essen",
            ContactName = "Philip Cramer",
            PassportNumber = "321"
          };
          
          // Employies
          var director = new M1.Employee {
            Address = new M1.Address {
              City = "Tacoma",
              Country = "USA"
            },
            FirstName = "Andrew",
            LastName = "Fuller",
            HireDate = new DateTime(1992, 8, 13)
          };
          var nancy = new M1.Employee {
            Address = new M1.Address {
              City = "Seattle",
              Country = "USA"
            },
            FirstName = "Nancy",
            LastName = "Davolio",
            HireDate = new DateTime(1992, 5, 1),
            ReportsTo = director
          };
          var michael = new M1.Employee {
            Address = new M1.Address {
              City = "London",
              Country = "UK"
            },
            FirstName = "Michael",
            LastName = "Suyama",
            HireDate = new DateTime(1993, 10, 17),
            ReportsTo = director
          };

          // Orders
          new M1.Order {
            OrderNumber = "1",
            Customer = helen,
            Employee = michael,
            Freight = 12,
            OrderDate = new DateTime(1996, 7, 4),
            ProductName = "Maxilaku"
          };
          new M1.Order {
            OrderNumber = "2",
            Customer = helen,
            Employee = nancy,
            Freight = 12,
            OrderDate = new DateTime(1996, 7, 4),
            ProductName = "Filo Mix"
          };
          new M1.Order {
            OrderNumber = "3",
            Customer = philip,
            Employee = michael,
            Freight = 12,
            OrderDate = new DateTime(1996, 7, 4),
            ProductName = "Tourtiere"
          };
          new M1.Order {
            OrderNumber = "4",
            Customer = philip,
            Employee = nancy,
            Freight = 12,
            OrderDate = new DateTime(1996, 7, 4),
            ProductName = "Pate chinois"
          };

          // Products & catgories
          new M1.Category {
            Name = "Web applications",
            Products = {new M1.Product {Name = "HelpServer", IsActive = true}}
          };

          new M1.Category {
            Name = "Frameworks",
            Products = {new M1.Product {Name = "DataObjects.NET", IsActive = true}}
          };

          // Boys & girls
          var alex = new M1.Boy("Alex");
          var dmitry = new M1.Boy("Dmitry");
          var elena = new M1.Girl("Elena");
          var tanya = new M1.Girl("Tanya");
          alex.FriendlyGirls.Add(elena);
          alex.FriendlyGirls.Add(tanya);
          elena.FriendlyBoys.Add(dmitry);

          // EntityX
          var e1 = new M1.Entity1(1);
          var e2 = new M1.Entity2(2, e1);
          var e3 = new M1.Entity3(3, e2);
          var e4 = new M1.Entity4(4, e3);

          // StructureContainerX
          var se1 = new M1.StructureContainer1 {S1 = new M1.Structure1 {E1 = e1}};
          var se2 = new M1.StructureContainer2 {S2 = new M1.Structure2 {E2 = e2, S1 = se1.S1}};
          var se3 = new M1.StructureContainer3 {S3 = new M1.Structure3 {E3 = e3, S2 = se2.S2}};
          var se4 = new M1.StructureContainer4 {S4 = new M1.Structure4 {E4 = e4, S3 = se3.S3}};

          // MyStructureOwner, ReferencedEntity
          new M1.MyStructureOwner(0) {Structure = new M1.MyStructure {A = 1, B = 2}};
          new M1.MyStructureOwner(1) {Structure = new M1.MyStructure {A = 2, B = 3}};
          new M1.ReferencedEntity(1, 2);
          new M1.ReferencedEntity(2, 3);

          // Generic types
          new M1.Sync<M1.Person> {Root = helen};
          new M1.Sync<M1.Person> {Root = director};
          new M1.Sync<M1.Boy> {Root = alex};
          
          // Commiting changes
          transactionScope.Complete();
        }
      }
    }

    #endregion
  }
}