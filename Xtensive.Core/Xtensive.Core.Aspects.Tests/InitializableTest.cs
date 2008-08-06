// Copyright (C) 2008 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Alex Yakunin
// Created:    2008.08.01

using System;
using NUnit.Framework;
using Xtensive.Core.Aspects;
using Xtensive.Core.Reflection;

[assembly:Initializable(AttributeTargetTypes = "*")]

namespace Xtensive.Core.Aspects.Tests
{
  public class WrongInitializableBase
  {
    public bool IsInitialized { get; private set; }

    protected void Initialize(Type ctorType)
    {
      IsInitialized = true;
    }
  }

  public class InitializableBase: IInitializable
  {
    public bool IsInitialized { get; private set; }

    protected void Initialize(Type ctorType)
    {
      if (ctorType!=GetType())
        return;
      IsInitialized = true;
      Log.Info("Initialized: type {0}", ctorType.GetShortName());
    }
  }

  [Initializable]
  public class InitializableSample : InitializableBase
  {
    // Constructors

    public InitializableSample()
    {
    }

    public InitializableSample(object arg)
    {
    }
  }

  [TestFixture]
  public class InitializableTest
  {
    [Test]
    public void CombinedTest()
    {
      var wi = new WrongInitializableBase(); 
      Assert.IsFalse(wi.IsInitialized);

      var i = new InitializableBase(); 
      Assert.IsTrue(i.IsInitialized);
      i = new InitializableSample();
      Assert.IsTrue(i.IsInitialized);
    }
  }
}