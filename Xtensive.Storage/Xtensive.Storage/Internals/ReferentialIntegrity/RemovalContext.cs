// Copyright (C) 2008 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Dmitri Maximov
// Created:    2008.07.02

using System;
using System.Collections.Generic;
using Xtensive.Core.Disposing;
using Xtensive.Storage.Model;
using System.Linq;

namespace Xtensive.Storage.ReferentialIntegrity
{
  internal class RemovalContext : IDisposable
  {
    private readonly RemovalProcessor processor;
    private readonly HashSet<Entity> processedEntities = new HashSet<Entity>();
    private readonly Queue<TypeInfo> types = new Queue<TypeInfo>();
    private readonly Dictionary<TypeInfo, List<Entity>> queue = new Dictionary<TypeInfo, List<Entity>>();
    private readonly RemovalContext parent;

    public bool QueueIsEmpty
    {
      get { return types.Count == 0; }
    }

    public bool Contains(Entity entity)
    {
      if (processedEntities.Contains(entity))
        return true;
      return parent != null && parent.Contains(entity);
    }

    public IEnumerable<Entity> GetProcessedEntities()
    {
      return processedEntities.Where(e => e.PersistenceState != PersistenceState.Removed);
    }

    public List<Entity> GatherEntitiesForProcessing()
    {
      while (types.Count > 0) {
        var type = types.Dequeue();
        var list = queue[type];
        if (list.Count == 0) 
          continue;
        var result = list.Where(e => !processedEntities.Contains(e)).ToList();
        foreach (var entity in result)
          processedEntities.Add(entity);
        list.Clear();
        return result;
      }
      return new List<Entity>(0);
    }

    public void Enqueue(Entity entity)
    {
      var type = entity.Type;
      if (types.Count > 0) {
        if (types.Peek() != type)
          types.Enqueue(type);
      }
      else
        types.Enqueue(type);
      List<Entity> list;
      if (queue.TryGetValue(type, out list))
        list.Add(entity);
      else {
        list = new List<Entity> {entity};
        queue.Add(type, list);
      }
    }

    public void Enqueue(IEnumerable<Entity> entities)
    {
      foreach (var group in entities.GroupBy(e => e.Type)) {
        var type = group.Key;
        if (types.Count > 0) {
          if (types.Peek() != type)
            types.Enqueue(type);
        }
        else
          types.Enqueue(type);
        List<Entity> list;
        if (queue.TryGetValue(type, out list))
          list.AddRange(group);
        else {
          list = new List<Entity>(group);
          queue.Add(type, list);
        }
      }
    }

    public void Dispose()
    {
      if (parent != null)
        foreach (var entity in processedEntities)
          parent.processedEntities.Add(entity);
      processedEntities.Clear();
      types.Clear();
      queue.Clear();
      processor.Context = parent;
    }

    
    // Constructors

    public RemovalContext(RemovalProcessor processor)
    {
      this.processor = processor;
      parent = processor.Context;
    }
  }
}