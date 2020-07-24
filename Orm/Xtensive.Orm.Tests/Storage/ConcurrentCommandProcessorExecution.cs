﻿// Copyright (C) 2019-2020 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Xtensive.Core;
using Xtensive.Orm.Configuration;
using Xtensive.Orm.Internals;
using Xtensive.Orm.Tests.Storage.ConcurrentCommandProcessorExecutionModel;

namespace Xtensive.Orm.Tests.Storage.ConcurrentCommandProcessorExecutionModel
{
  [HierarchyRoot]
  public class TestEntity : Entity
  {
    [Field, Key]
    public int Id { get; private set; }

    [Field]
    public string Name { get; set; }

    [Field]
    public DateTime CreationDate { get; set; }

    [Field]
    public long Value { get; set; }

    public TestEntity(Session session)
      : base(session)
    {
    }
  }

  [HierarchyRoot]
  public class EntitySetContainer : Entity
  {
    [Field, Key]
    public int Id { get; private set; }

    [Field]
    public EntitySet<TestEntity> EntitySet { get; set; }

    public EntitySetContainer(Session session)
      : base(session)
    {
    }
  }
}

namespace Xtensive.Orm.Tests.Storage
{
  public class ConcurrentCommandProcessorExecution : AutoBuildTest
  {
    private const int OneBatchSizedTaskCollectionCount = SessionConfiguration.DefaultBatchSize;
    private const int TwoBatchSizedTaskCollectionCount = SessionConfiguration.DefaultBatchSize * 2;
    private const int MoreThatTwoBatchSizedTaskCollectionCount = SessionConfiguration.DefaultBatchSize * 2 + 2;

    protected override DomainConfiguration BuildConfiguration()
    {
      var configuration = base.BuildConfiguration();
      configuration.Types.Register(typeof (TestEntity).Assembly, typeof (TestEntity).Namespace);
      configuration.UpgradeMode = DomainUpgradeMode.Recreate;
      return configuration;
    }

    protected override void PopulateData()
    {
      using (var session = Domain.OpenSession())
      using (var transaction = session.OpenTransaction()) {
        var entityCount = 1000;
        for (int i = 0; i < entityCount; i++) {
          new TestEntity(session) { Name = "A", Value = i, CreationDate = DateTime.Now.AddSeconds(i) };
          if (i % 10==0)
            session.SaveChanges();
        }
        transaction.Complete();
      }
    }

    [Test]
    public async Task InseparableBatchTest()
    {
      await using (var session = await Domain.OpenSessionAsync())
      await using (var transaction = session.OpenTransaction()) {
        var longListOfQueries = new List<DelayedQuery<TestEntity>>(OneBatchSizedTaskCollectionCount);
        var value = 1;
        while (longListOfQueries.Count < OneBatchSizedTaskCollectionCount) {
          var closureValue = value;
          longListOfQueries.Add(
            session.Query.CreateDelayedQuery((q) => q.All<TestEntity>().Where(e => e.Value < closureValue)));
          value += 1;
        }

        var task = longListOfQueries.First().ExecuteAsync();
        var fastBatch = await session.Query.CreateDelayedQuery(q => q.All<TestEntity>().Where(e => e.Value > 10)).ExecuteAsync();
        await task;

        int countBefore = 1;
        foreach (var query in longListOfQueries) {
          var actualCount = query.Count();
          var expectedCount = countBefore;
          Console.WriteLine(actualCount);
          Assert.That(actualCount, Is.EqualTo(expectedCount));
          countBefore = countBefore + 1;
        }
      }
    }

    [Test]
    public async Task SeparableBatchTest01()
    {
      await using (var session = await Domain.OpenSessionAsync())
      await using (var transaction = session.OpenTransaction()) {
        var longListOfQueries = new List<DelayedQuery<TestEntity>>(TwoBatchSizedTaskCollectionCount);
        int value = 1;
        while (longListOfQueries.Count < TwoBatchSizedTaskCollectionCount) {
          var closureValue = value;
          longListOfQueries.Add(session.Query.CreateDelayedQuery((q) => q.All<TestEntity>().Where(e => e.Value < closureValue)));
          value += 1;
        }

        var task = longListOfQueries.First().ExecuteAsync();
        var fastBatch = await session.Query.CreateDelayedQuery((q) => q.All<TestEntity>().Where(e => e.Value > 10)).ExecuteAsync();
        await task;

        int countBefore = 1;
        foreach (var query in longListOfQueries) {
          var actualCount = query.Count();
          var expectedCount = countBefore;
          Console.WriteLine(actualCount);
          Assert.That(actualCount, Is.EqualTo(expectedCount));
          countBefore = countBefore + 1;
        }
      }
    }

    [Test]
    public async Task SeparableBatchTest02()
    {
      using (var session = await Domain.OpenSessionAsync())
      using (var transaction = session.OpenTransaction()) {
        var longListOfQueries = new List<DelayedQuery<TestEntity>>(MoreThatTwoBatchSizedTaskCollectionCount);
        int value = 1;
        while (longListOfQueries.Count < MoreThatTwoBatchSizedTaskCollectionCount) {
          var closureValue = value;
          longListOfQueries.Add(
            session.Query.CreateDelayedQuery((q) => q.All<TestEntity>().Where(e => e.Value < closureValue)));
          value += 1;
        }

        var task = longListOfQueries.First().ExecuteAsync();
        var fastBatch = await
          session.Query.CreateDelayedQuery((q) => q.All<TestEntity>().Where(e => e.Value > 10)).ExecuteAsync();
        await task;

        int countBefore = 1;
        foreach (var query in longListOfQueries) {
          var actualCount = query.Count();
          var expectedCount = countBefore;
          Console.WriteLine(actualCount);
          Assert.That(actualCount, Is.EqualTo(expectedCount));
          countBefore = countBefore + 1;
        }
      }
    }

    [Test]
    public async Task ActualPersistAfterBatchFinishedTest01()
    {
      await using (var session = await Domain.OpenSessionAsync())
      await using (var transaction = session.OpenTransaction()) {
        var looooongListOfQueries = new List<DelayedQuery<TestEntity>>(OneBatchSizedTaskCollectionCount);
        int value = 1;
        while (looooongListOfQueries.Count < OneBatchSizedTaskCollectionCount) {
          var closureValue = value;
          looooongListOfQueries.Add(
            session.Query.CreateDelayedQuery((q) => q.All<TestEntity>().Where(e => e.Value < closureValue)));
          value++;
        }

        var task = looooongListOfQueries.First().ExecuteAsync();
        new TestEntity(session) {Value = 0};
        new TestEntity(session) {Value = 0};
        var result1 = await task;
        var anotherTask =
          session.Query.CreateDelayedQuery((q) => q.All<TestEntity>().Where(e => e.Value > 10)).ExecuteAsync();
        var result2 = await anotherTask;

        int countBefore = 1;
        foreach (var query in looooongListOfQueries) {
          var actualCount = query.Count();
          var expectedCount = countBefore;
          Console.WriteLine(actualCount);
          Assert.That(actualCount, Is.EqualTo(expectedCount));
          countBefore = countBefore + 1;
        }
      }
    }

    [Test]
    public async Task ActualPersistAfterBatchFinishedTest02()
    {
      await using (var session = await Domain.OpenSessionAsync())
      await using (var transaction = session.OpenTransaction()) {
        var looooongListOfQueries = new List<DelayedQuery<TestEntity>>(MoreThatTwoBatchSizedTaskCollectionCount);
        int value = 1;
        while (looooongListOfQueries.Count < MoreThatTwoBatchSizedTaskCollectionCount) {
          var closureValue = value;
          looooongListOfQueries.Add(
            session.Query.CreateDelayedQuery((q) => q.All<TestEntity>().Where(e => e.Value < closureValue)));
          value++;
        }

        var task = looooongListOfQueries.First().ExecuteAsync();
        new TestEntity(session) {Value = 0};
        new TestEntity(session) {Value = 0};
        var result1 = await task;
        var anotherTask =
          session.Query.CreateDelayedQuery((q) => q.All<TestEntity>().Where(e => e.Value > 10)).ExecuteAsync();

        var result2 = await anotherTask;

        int countBefore = 1;
        foreach (var query in looooongListOfQueries) {
          var actualCount = query.Count();
          var expectedCount = countBefore;
          Console.WriteLine(actualCount);
          Assert.That(actualCount, Is.EqualTo(expectedCount));
          countBefore = countBefore + 1;
        }
      }
    }

    [Test]
    public async Task PersistDuringNonSeparableBatchExecutionTest01()
    {
      await using (var session = await Domain.OpenSessionAsync())
      await using (var transaction = session.OpenTransaction()) {
        var longListOfQueries = new List<DelayedQuery<TestEntity>>(OneBatchSizedTaskCollectionCount);
        int value = 1;
        while (longListOfQueries.Count < OneBatchSizedTaskCollectionCount){
          var closureValue = value;
          longListOfQueries.Add(
            session.Query.CreateDelayedQuery((q) => q.All<TestEntity>().Where(e => e.Value < closureValue)));
          value += 1;
        }

        var task = longListOfQueries.First().ExecuteAsync();
        new TestEntity(session) {Value = 0};
        new TestEntity(session) {Value = 0};
        var anotherTask =
          session.Query.CreateDelayedQuery((q) => q.All<TestEntity>().Where(e => e.Value > 10)).ExecuteAsync();
        var result1 = await task;
        Assert.ThrowsAsync<InvalidOperationException>(async () => await anotherTask);
      }
    }

    [Test]
    public async Task PersistDuringNonSeparableBatchExecutionTest02()
    {
      await using (var session = await Domain.OpenSessionAsync())
      await using (var transaction = session.OpenTransaction()) {
        var longListOfQueries = new List<DelayedQuery<TestEntity>>(OneBatchSizedTaskCollectionCount);
        int value = 1;
        while (longListOfQueries.Count < OneBatchSizedTaskCollectionCount) {
          var closureValue = value;
          longListOfQueries.Add(
            session.Query.CreateDelayedQuery((q) => q.All<TestEntity>().Where(e => e.Value < closureValue)));
          value += 1;
        }

        var task = longListOfQueries.First().ExecuteAsync();
        new TestEntity(session) {Value = 0};
        new TestEntity(session) {Value = 0};
        var anotherTask =
          session.Query.CreateDelayedQuery((q) => q.All<TestEntity>().Where(e => e.Value > 10)).ExecuteAsync();
        try {
          Assert.ThrowsAsync<InvalidOperationException>(async () => await anotherTask);
        }
        finally {
          var result2 = await task;
        }
      }
    }

    [Test]
    public async Task PersistDuringSeparableBatchExecutionTest01()
    {
      using (var session = await Domain.OpenSessionAsync())
      using (var transaction = session.OpenTransaction()) {
        var longListOfQueries = new List<DelayedQuery<TestEntity>>(MoreThatTwoBatchSizedTaskCollectionCount);
        int value = 1;
        while (longListOfQueries.Count < MoreThatTwoBatchSizedTaskCollectionCount) {
          var closureValue = value;
          longListOfQueries.Add(
            session.Query.CreateDelayedQuery((q) => q.All<TestEntity>().Where(e => e.Value < closureValue)));
          value += 1;
        }

        var task = longListOfQueries.First().ExecuteAsync();
        new TestEntity(session) { Value = 0 };
        new TestEntity(session) { Value = 0 };
        var anotherTask =
          session.Query.CreateDelayedQuery((q) => q.All<TestEntity>().Where(e => e.Value > 10)).ExecuteAsync();
        var result1 = await task;
        Assert.ThrowsAsync<InvalidOperationException>(async () => await anotherTask);
      }
    }

    [Test]
    public async Task PersistDuringSeparableBatchExecutionTest02()
    {
      await using (var session = await Domain.OpenSessionAsync())
      await using (var transaction = session.OpenTransaction()) {
        var longListOfQueries = new List<DelayedQuery<TestEntity>>(MoreThatTwoBatchSizedTaskCollectionCount);
        int value = 1;
        while (longListOfQueries.Count < MoreThatTwoBatchSizedTaskCollectionCount) {
          var closureValue = value;
          longListOfQueries.Add(
            session.Query.CreateDelayedQuery((q) => q.All<TestEntity>().Where(e => e.Value < closureValue)));
          value += 1;
        }

        var task = longListOfQueries.First().ExecuteAsync();
        new TestEntity(session) {Value = 0};
        new TestEntity(session) {Value = 0};
        var anotherTask =
          session.Query.CreateDelayedQuery((q) => q.All<TestEntity>().Where(e => e.Value > 10)).ExecuteAsync();
        try {
          Assert.ThrowsAsync<InvalidOperationException>(async () => await anotherTask);
        }
        finally {
          var result2 = await task;
        }
      }
    }
  }
}