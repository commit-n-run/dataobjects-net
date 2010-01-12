// Copyright (C) 2009 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Alex Gamzov
// Created:    2009.09.28

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Xtensive.Storage.Configuration;
using Xtensive.Storage.Tests.Linq.LocalCollectionsComplexTestModel;

namespace Xtensive.Storage.Tests.Linq.LocalCollectionsComplexTestModel
{
  public class Poco1
  {
    public EntityA A { get; set; }

    public EntityB B { get; set; }

    public EntityStructure EntityStructure { get; set; }

    public ComplexStructure ComplexStructure { get; set; }
  }

  public class Poco2
  {
    public Poco1 Poco1 { get; set; }

    public EntityA A { get; set; }

    public EntityB B { get; set; }

    public EntityStructure EntityStructure { get; set; }

    public ComplexStructure ComplexStructure { get; set; }
  }

  [Serializable]
  public class EntityStructure : Structure
  {
    [Field]
    public EntityA A { get; set; }

    [Field]
    public string StructureName { get; set; }

    [Field]
    public DateTime StructureAge { get; set; }
  }

  [Serializable]
  public class ComplexStructure : Structure
  {
    [Field]
    public EntityA A { get; set; }

    [Field]
    public string StructureName { get; set; }

    [Field]
    public DateTime StructureAge { get; set; }

    [Field]
    public EntityStructure EntityStructure { get; set; }
  }

  [Serializable]
  [HierarchyRoot]
  public class EntityA : Entity
  {
    [Field]
    [Key]
    public int Id { get; private set; }

    [Field]
    public string Name { get; set; }

    [Field]
    public DateTime Age { get; set; }
  }

  [Serializable]
  [HierarchyRoot]
  public class EntityB : Entity
  {
    [Field]
    [Key]
    public int Id { get; private set; }

    [Field]
    public string Name { get; set; }

    [Field]
    public DateTime Age { get; set; }

    [Field]
    public ComplexStructure AdditionalInfo { get; set; }
  }
}

namespace Xtensive.Storage.Tests.Linq
{
  public class LocalCollectionsComplexTest : AutoBuildTest
  {
    private const int count = 10;

    protected override DomainConfiguration BuildConfiguration()
    {
      DomainConfiguration config = base.BuildConfiguration();
      config.Types.Register(typeof (EntityA).Assembly, typeof (EntityA).Namespace);
      config.UpgradeMode = DomainUpgradeMode.Recreate;
      return config;
    }

    public override void TestFixtureSetUp()
    {
      base.TestFixtureSetUp();
      using (Session session = Session.Open(Domain)) {
        using (TransactionScope t = Transaction.Open()) {
          var entitiesB = Enumerable
            .Range(0, count)
            .Select(i => new EntityB {
              Age = new DateTime(2000 + i, 2, 2),
              Name = "NameB_" + i,
              AdditionalInfo = new ComplexStructure {
                A = new EntityA {
                  Age = new DateTime(2000 + i, 3, 3),
                  Name = "NameA_1_" + i
                },
                StructureAge = new DateTime(2000 + i, 7, 7),
                StructureName = "StructureName_1_" + i,
                EntityStructure = new EntityStructure {
                  A = new EntityA {
                    Age = new DateTime(2000 + i, 1, 1),
                    Name = "NameA_2_" + i
                  },
                  StructureAge = new DateTime(2000 + i, 10, 10),
                  StructureName = "StructureName_2_" + i,
                }
              }
            })
            .ToList();
          t.Complete();
        }
      }
    }

    [Test]
    public void UnionEntityTest()
    {
      using (Session session = Session.Open(Domain)) {
        using (TransactionScope t = Transaction.Open()) {
          session.Persist();
          var localItems = Query.All<EntityB>().Take(count / 2).ToArray();
          var union = Query.All<EntityB>().Union(localItems);
          var expected = Query.All<EntityB>().AsEnumerable().Union(localItems);
          Assert.AreEqual(0, expected.Except(union).Count());
        }
      }
    }

    [Test]
    public void UnionStructureTest()
    {
      using (Session session = Session.Open(Domain)) {
        using (TransactionScope t = Transaction.Open()) {
          session.Persist();
          var localItems = Query.All<EntityB>().Select(b => b.AdditionalInfo).Take(count / 2).ToArray();
          var union = Query.All<EntityB>().Select(b => b.AdditionalInfo).Union(localItems);
          var expected = Query.All<EntityB>().AsEnumerable().Select(b => b.AdditionalInfo).Union(localItems);
          Assert.AreEqual(0, expected.Except(union).Count());
        }
      }
    }

    [Test]
    public void UnionFieldTest()
    {
      using (Session session = Session.Open(Domain)) {
        using (TransactionScope t = Transaction.Open()) {
          session.Persist();
          var localItems = Query.All<EntityB>().Select(b => b.Name).Take(count / 2).ToArray();
          var union = Query.All<EntityB>().Select(b => b.Name).Union(localItems);
          var expected = Query.All<EntityB>().AsEnumerable().Select(b => b.Name).Union(localItems);
          Assert.AreEqual(0, expected.Except(union).Count());
        }
      }
    }

    [Test]
    public void JoinEntityDirectTest()
    {
      using (Session session = Session.Open(Domain)) {
        using (TransactionScope t = Transaction.Open()) {
          session.Persist();
          var localItems = Query.All<EntityB>().Take(count / 2).ToArray();
          var join = Query.All<EntityB>().Join(localItems, b => b, l => l, (b, l) => new {b, l});
          var expected = Query.All<EntityB>().AsEnumerable().Join(localItems, b => b, l => l, (b, l) => new {b, l});
          Assert.AreEqual(0, expected.Except(join).Count());
        }
      }
    }


    [Test]
    public void JoinEntityIndirect2Test()
    {
      using (Session session = Session.Open(Domain)) {
        using (TransactionScope t = Transaction.Open()) {
          session.Persist();
          var localItems = Query.All<EntityB>().Take(count / 2).ToArray();
          var join = Query.All<EntityB>().Join(localItems, b => b.AdditionalInfo.A, l => l.AdditionalInfo.A, (b, l) => new {b, l});
          var expected = Query.All<EntityB>().AsEnumerable().Join(localItems, b => b.AdditionalInfo.A, l => l.AdditionalInfo.A, (b, l) => new {b, l});
          Assert.AreEqual(0, expected.Except(join).Count());
        }
      }
    }

    [Test]
    public void JoinEntityIndirect3Test()
    {
      using (Session session = Session.Open(Domain)) {
        using (TransactionScope t = Transaction.Open()) {
          session.Persist();
          var localItems = Query.All<EntityB>().Take(count / 2).ToArray();
          var join = Query.All<EntityA>().Join(localItems, a => a, l => l.AdditionalInfo.A, (a, l) => new {a, l});
          var expected = Query.All<EntityA>().AsEnumerable().Join(localItems, a => a, l => l.AdditionalInfo.A, (a, l) => new {a, l});
          Assert.AreEqual(0, expected.Except(join).Count());
        }
      }
    }

    [Test]
    public void JoinStructureDirectTest()
    {
      using (Session session = Session.Open(Domain)) {
        using (TransactionScope t = Transaction.Open()) {
          session.Persist();
          var localStructure = Query.All<EntityB>().Select(b => b.AdditionalInfo).First();
          var array = Query.All<EntityB>().Where(b => b.AdditionalInfo==localStructure).ToArray();
          var localItems = Query.All<EntityB>().Select(b => b.AdditionalInfo).ToArray();
          var join = Query.All<EntityB>().Select(b => b.AdditionalInfo).Join(localItems, b => b, l => l, (b, l) => new {b, l});
          var expected = Query.All<EntityB>().AsEnumerable().Select(b => b.AdditionalInfo).Join(localItems, b => b, l => l, (b, l) => new {b, l});
          var except = expected.Except(join);
          var joinArray = join.ToArray();
          var expectedArray = expected.ToArray();
          Assert.AreEqual(0, except.Count());
        }
      }
    }

    [Test]
    public void JoinStructureIndirectTest()
    {
      using (Session session = Session.Open(Domain)) {
        using (TransactionScope t = Transaction.Open()) {
          session.Persist();
          var localItems = Query.All<EntityB>().Take(count / 2).ToArray();
          var join = Query.All<EntityB>().Join(localItems, b => b.AdditionalInfo, l => l.AdditionalInfo, (b, l) => new {b, l});
          var expected = Query.All<EntityB>().AsEnumerable().Join(localItems, b => b.AdditionalInfo, l => l.AdditionalInfo, (b, l) => new {b, l});
          Assert.AreEqual(0, expected.Except(join).Count());
        }
      }
    }

    [Test]
    public void JoinEntityPocoTest()
    {
      using (Session session = Session.Open(Domain)) {
        using (TransactionScope t = Transaction.Open()) {
          session.Persist();
          Poco2[] localItems = GetPocoCollection();
          var join = Query.All<EntityB>().Join(localItems, b=>b, p=>p.B, (b,p) => new{b, p.A});
          var count = join.Count();
          Assert.IsTrue(count>0);
          var expected = Query.All<EntityB>().AsEnumerable().Join(localItems, b=>b, p=>p.B, (b,p) => new{b, p.A});
          var except = expected.Except(join).ToArray();
          Assert.AreEqual(0, except.Length);
        }
      }
    }

    private Poco2[] GetPocoCollection()
    {
      return Query.All<EntityB>()
        .Join(Query.All<EntityA>(), b => 1, a => 1, (b, a) => new {a, b})
        .Take(count)
        .AsEnumerable()
        .Select((ab, i) => new Poco2 {
          A = (i % 2==0) ? null : ab.a,
          B = (i % 3==0) ? null : ab.b,
          ComplexStructure = (i % 5==0) ? null : new ComplexStructure {
            A = (i % 13==0) ? null : ab.a,
            EntityStructure = new EntityStructure {
              A = (i % 21==0) ? null : ab.a,
            }
          },
          EntityStructure = (i % 7==0) ? null : new EntityStructure {
            A = (i % 17==0) ? null : ab.a,
          },
          Poco1 = (i % 11==0) ? null : new Poco1 {
            A = (i % 23==0) ? null : ab.a,
            B = (i % 29==0) ? null : ab.b,
            ComplexStructure = (i % 41==0) ? null : new ComplexStructure {
              A = (i % 31==0) ? null : ab.a,
              EntityStructure = new EntityStructure {
                A = (i % 37==0) ? null : ab.a,
              }
            },
            EntityStructure = (i % 19==0) ? null : new EntityStructure {
              A = (i % 51==0) ? null : ab.a,
            },
          }
        })
        .ToArray();
    }
  }
}