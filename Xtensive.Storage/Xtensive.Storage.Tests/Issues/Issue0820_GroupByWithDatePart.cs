﻿using System;
using System.Linq;
using NUnit.Framework;
using Xtensive.Storage.Configuration;
using Xtensive.Storage.Tests.Issues_Issue0820_GroupByWithDatePart;

namespace Xtensive.Storage.Tests.Issues_Issue0820_GroupByWithDatePart
{
  [HierarchyRoot]
  public class Item : Entity
  {
    [Field, Key]
    public long Id { get; private set; }

    [Field]
    public DateTime? Date { get; set; }
  }
}

namespace Xtensive.Storage.Tests.Issues
{
  [TestFixture]
  public class Issue0820_GroupByWithDatePart : AutoBuildTest
  {
    protected override DomainConfiguration BuildConfiguration()
    {
      var config = base.BuildConfiguration();
      config.Types.Register(typeof (Item).Assembly, typeof (Item).Namespace);
      return config;
    }

    [Test]
    public void OriginalTest()
    {
      using (Domain.OpenSession()) {
        using (var t = Transaction.Open()) {
          var dateTime = DateTime.Now;
          var item1 = new Item {Date = dateTime};
          var item2 = new Item {Date = dateTime.AddSeconds(1)};
          var query = Query.All<Item>().Select(c => new PipeAbonentDocumentReportItemDto {
            Year = (c.Date!=((Nullable<DateTime>) null))
              ? ((Nullable<Int32>) c.Date.Value.Year)
              : null
          }).GroupBy(
            g => new PipeAbonentDocumentReportItemDto {
              Year = g.Year
            },
            (e, g) => new PipeAbonentDocumentReportItemDto {
              Year = e.Year,
              LineCount = g.Count()
            }
            );

          var result = query.ToList();
          Assert.AreEqual(1, result.Count);
          Assert.AreEqual(2, result[0].LineCount);
        }
      }
    }
    
    [Test]
    public void SimplifiedTest()
    {
      using (Domain.OpenSession()) {
        using (var t = Transaction.Open()) {
          var dateTime = DateTime.Now;
          var item1 = new Item {Date = dateTime};
          var item2 = new Item {Date = dateTime.AddSeconds(1)};
          var query = Query.All<Item>().Select(c => new PipeAbonentDocumentReportItemDto {
            Year = (c.Date!=((DateTime?) null))
              ? ((int?) c.Date.Value.Year)
              : null
          }).GroupBy(
            g => new PipeAbonentDocumentReportItemDto {
              Year = g.Year
            });

          var result = query.ToList();
          Assert.AreEqual(1, result.Count);
        }
      }
    }

    [Test]
    public void AnonimousTest()
    {
      using (Domain.OpenSession()) {
        using (var t = Transaction.Open()) {
          var dateTime = DateTime.Now;
          var item1 = new Item {Date = dateTime};
          var item2 = new Item {Date = dateTime.AddSeconds(1)};
          var query = Query.All<Item>().Select(c => new PipeAbonentDocumentReportItemDto {
            Year = (c.Date!=((DateTime?) null))
              ? ((int?) c.Date.Value.Year)
              : null
          }).GroupBy(
            g => new {
              Year = g.Year
            });

          var result = query.ToList();
          Assert.AreEqual(1, result.Count);
        }
      }
    }
  }

  public class PipeAbonentDocumentReportItemDto
  {
    public int? Year;
    public int LineCount;
  }
}