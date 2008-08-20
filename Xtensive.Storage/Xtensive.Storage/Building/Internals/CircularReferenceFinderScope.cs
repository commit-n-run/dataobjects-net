// Copyright (C) 2008 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Dmitri Maximov
// Created:    2008.07.15

using System;
using Xtensive.Core.Internals.DocTemplates;

namespace Xtensive.Storage.Building.Internals
{
  internal class CircularReferenceFinderScope<TNode> : IDisposable
    where TNode : class 
  {
    public TNode Node { get; private set; }
    public CircularReferenceFinder<TNode> Finder { get; private set; }


    // Constructors

    /// <summary>
    /// <see cref="ClassDocTemplate.Ctor" copy="true"/>
    /// </summary>
    /// <param name="finder">The circular reference finder.</param>
    /// <param name="node">The node.</param>
    public CircularReferenceFinderScope(CircularReferenceFinder<TNode> finder, TNode node)
    {
      Finder = finder;
      Node = node;
    }

    // Destructor

    /// <summary>
    /// <see cref="ClassDocTemplate.Dispose" copy="true"/>
    /// </summary>
    public void Dispose()
    {
      if (Finder.Path.Peek() == Node)
        Finder.Path.Pop();
    }
  }
}