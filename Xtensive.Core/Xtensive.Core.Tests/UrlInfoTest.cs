// Copyright (C) 2007 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Alexander Ilyin
// Created:    2007.07.18

using NUnit.Framework;
namespace Xtensive.Core.Tests.Utility
{
  [TestFixture]
  public class UrlInfoTest
  {
    [Test]
    public void CombinedTest()
    {
      UrlInfo a1 = new UrlInfo("tcp://user:password@someHost:1000/someUrl/someUrl?someParameter=someValue&someParameter2=someValue2");
      UrlInfo a2 = new UrlInfo("tcp://user:password@someHost:1000/someUrl/someUrl?someParameter=someValue&someParameter2=someValue2");
      UrlInfo aX = new UrlInfo("tcp://user:password@someHost:1000/someUrl/someUrl?someParameter2=someValue2&someParameter=someValue");
      UrlInfo b  = new UrlInfo("tcp://user:password@someHost:1000/someUrl/someUrl");

      Assert.IsTrue(a1.GetHashCode()==a2.GetHashCode());
      Assert.IsTrue(a1.GetHashCode()!=aX.GetHashCode());
      Assert.IsTrue(a1.GetHashCode()!=b.GetHashCode());

      Assert.IsTrue(a1.Equals(a2));
      Assert.IsFalse(a1.Equals(aX));
      Assert.IsFalse(a1.Equals(b));
    }
  }
}