// Copyright (C) 2009-2020 Xtensive LLC.
// This code is distributed under MIT license terms.
// See the License.txt file in the project root for more information.
// Created by: Alexander Nikolaev
// Created:    2009.09.09

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Xtensive.Core;
using Xtensive.Orm.Linq;
using Xtensive.Orm.Model;
using Xtensive.Orm.Rse;
using Xtensive.Orm.Rse.Providers;
using FieldInfo = Xtensive.Orm.Model.FieldInfo;
using Tuple = Xtensive.Tuples.Tuple;

namespace Xtensive.Orm.Internals.Prefetch
{
  [Serializable]
  internal sealed class EntitySetTask : IEquatable<EntitySetTask>
  {
    #region Nested classes

    private struct CacheKey : IEquatable<CacheKey>
    {
      public readonly FieldInfo ReferencingField;
      public readonly int? ItemCountLimit;
      private readonly int cachedHashCode;

      public bool Equals(CacheKey other)
      {
        return (ItemCountLimit == null) == (other.ItemCountLimit == null)
          && Equals(other.ReferencingField, ReferencingField);
      }

      public override bool Equals(object obj)
      {
        if (ReferenceEquals(null, obj)) {
          return false;
        }
        if (obj.GetType() != typeof (CacheKey)) {
          return false;
        }
        return Equals((CacheKey) obj);
      }

      public override int GetHashCode() => cachedHashCode;

      // Constructors

      public CacheKey(FieldInfo referencingField, int? itemCountLimit)
      {
        ReferencingField = referencingField;
        ItemCountLimit = itemCountLimit;
        unchecked {
          cachedHashCode = (ReferencingField.GetHashCode()*397)
                           ^ (ItemCountLimit.HasValue ? 1 : 0);
        }
      }
    }

    #endregion

    private static readonly object itemsQueryCachingRegion = new object();
    private static readonly Parameter<Tuple> ownerParameter = new Parameter<Tuple>(WellKnown.KeyFieldName);
    private static readonly Parameter<int> itemCountLimitParameter = new Parameter<int>("ItemCountLimit");

    private readonly Key ownerKey;
    private readonly bool isOwnerCached;
    private readonly PrefetchManager manager;
    private QueryTask itemsQueryTask;
    private readonly PrefetchFieldDescriptor referencingFieldDescriptor;
    private readonly CacheKey cacheKey;

    public FieldInfo ReferencingField {get { return referencingFieldDescriptor.Field; } }

    public CompilableProvider QueryProvider { get; private set; }

    public int? ItemCountLimit { get; private set; }

    public void RegisterQueryTask()
    {
      if (isOwnerCached && manager.Owner.LookupState(ownerKey, ReferencingField, out var state)) {
        if (state == null || (state.IsFullyLoaded && !state.ShouldUseForcePrefetch(referencingFieldDescriptor.PrefetchOperationId))) {
          return;
        }
      }

      itemsQueryTask = CreateQueryTask();
      manager.Owner.Session.RegisterInternalDelayedQuery(itemsQueryTask);
    }

    public void UpdateCache()
    {
      if (itemsQueryTask == null) {
        return;
      }

      var areToNotifyAboutKeys = !manager.Owner.Session.Domain.Model
        .Types[referencingFieldDescriptor.Field.ItemType].IsLeaf;
      var reader = manager.Owner.Session.Domain.EntityDataReader;
      var records = reader.Read(itemsQueryTask.Result, QueryProvider.Header, manager.Owner.Session);
      var entityKeys = new List<Key>(itemsQueryTask.Result.Count);
      var association = ReferencingField.Associations.Last();
      var auxEntities = (association.AuxiliaryType != null)
        ? new List<Pair<Key, Tuple>>(itemsQueryTask.Result.Count)
        : null;

      foreach (var record in records) {
        for (var i = 0; i < record.Count; i++) {
          var key = record.GetKey(i);
          if (key == null) {
            continue;
          }
          var tuple = record.GetTuple(i);
          if (tuple == null) {
            continue;
          }
          if (association.AuxiliaryType != null) {
            if (i == 0) {
              auxEntities.Add(new Pair<Key, Tuple>(key, tuple));
            }
            else {
              manager.SaveStrongReference(manager.Owner.UpdateState(key, tuple));
              entityKeys.Add(key);
              if (areToNotifyAboutKeys) {
                referencingFieldDescriptor.NotifySubscriber(ownerKey, key);
              }
            }
          }
          else {
            manager.SaveStrongReference(manager.Owner.UpdateState(key, tuple));
            entityKeys.Add(key);
            if (areToNotifyAboutKeys) {
              referencingFieldDescriptor.NotifySubscriber(ownerKey, key);
            }
          }
        }
      }
      var updatedState = manager.Owner.UpdateState(ownerKey, ReferencingField,
        ItemCountLimit == null || entityKeys.Count < ItemCountLimit, entityKeys, auxEntities);
      if (updatedState != null) {
        updatedState.SetLastManualPrefetchId(referencingFieldDescriptor.PrefetchOperationId);
      }
    }

    public bool Equals(EntitySetTask other)
    {
      if (ReferenceEquals(null, other)) {
        return false;
      }
      if (ReferenceEquals(this, other)) {
        return true;
      }
      return other.cacheKey.Equals(cacheKey);
    }

    public override bool Equals(object obj)
    {
      if (ReferenceEquals(null, obj)) {
        return false;
      }
      if (ReferenceEquals(this, obj)) {
        return true;
      }
      if (obj.GetType() != typeof (EntitySetTask)) {
        return false;
      }
      return Equals((EntitySetTask) obj);
    }

    public override int GetHashCode() => cacheKey.GetHashCode();

    #region Private / internal methods

    private QueryTask CreateQueryTask()
    {
      var parameterContext = new ParameterContext();
      parameterContext.SetValue(ownerParameter, ownerKey.Value);
      if (ItemCountLimit != null) {
        parameterContext.SetValue(itemCountLimitParameter, ItemCountLimit.Value);
      }

      object key = new Pair<object, CacheKey>(itemsQueryCachingRegion, cacheKey);
      Func<object, object> generator = CreateRecordSetLoadingItems;
      var session = manager.Owner.Session;
      var scope = new CompiledQueryProcessingScope(null, null, parameterContext, false);
      QueryProvider = (CompilableProvider) session.StorageNode.InternalQueryCache.GetOrAdd(key, generator);
      ExecutableProvider executableProvider;
      using (scope.Enter()) {
        executableProvider = session.Compile(QueryProvider);
      }
      return new QueryTask(executableProvider, session.GetLifetimeToken(), parameterContext);
    }

    private static CompilableProvider CreateRecordSetLoadingItems(object cachingKey)
    {
      var pair = (Pair<object, CacheKey>) cachingKey;
      var association = pair.Second.ReferencingField.Associations.Last();
      var primaryTargetIndex = association.TargetType.Indexes.PrimaryIndex;
      var resultColumns = new List<int>(primaryTargetIndex.Columns.Count);
      var result = association.AuxiliaryType == null
        ? CreateQueryForDirectAssociation(pair, primaryTargetIndex, resultColumns)
        : CreateQueryForAssociationViaAuxType(pair, primaryTargetIndex, resultColumns);
      result = result.Select(resultColumns.ToArray());
      if (pair.Second.ItemCountLimit != null)
        result = result.Take(context => context.GetValue(itemCountLimitParameter));
      return result;
    }

    private static CompilableProvider CreateQueryForAssociationViaAuxType(Pair<object, CacheKey> pair, IndexInfo primaryTargetIndex, List<int> resultColumns)
    {
      var association = pair.Second.ReferencingField.Associations.Last();
      var associationIndex = association.UnderlyingIndex;
      var joiningColumns = GetJoiningColumnIndexes(primaryTargetIndex, associationIndex,
        association.AuxiliaryType != null);
      AddResultColumnIndexes(resultColumns, associationIndex, 0);
      AddResultColumnIndexes(resultColumns, primaryTargetIndex, resultColumns.Count);
      var firstKeyColumnIndex = associationIndex.Columns.IndexOf(associationIndex.KeyColumns[0].Key);
      var keyColumnTypes = associationIndex
        .Columns
        .Skip(firstKeyColumnIndex)
        .Take(associationIndex.KeyColumns.Count)
        .Select(column => column.ValueType)
        .ToList();
      return associationIndex.GetQuery()
        .Filter(QueryHelper.BuildFilterLambda(firstKeyColumnIndex, keyColumnTypes, ownerParameter))
        .Alias("a")
        .Join(primaryTargetIndex.GetQuery(), joiningColumns);
    }

    private static CompilableProvider CreateQueryForDirectAssociation(Pair<object, CacheKey> pair, IndexInfo primaryTargetIndex, List<int> resultColumns)
    {
      AddResultColumnIndexes(resultColumns, primaryTargetIndex, 0);
      var association = pair.Second.ReferencingField.Associations.Last();
      var field = association.Reversed.OwnerField;
      var keyColumnTypes = field.Columns.Select(column => column.ValueType).ToList();
      return primaryTargetIndex
        .GetQuery()
        .Filter(QueryHelper.BuildFilterLambda(field.MappingInfo.Offset, keyColumnTypes, ownerParameter));
    }

    private static void AddResultColumnIndexes(ICollection<int> indexes, IndexInfo index,
      int columnIndexOffset)
    {
      for (var i = 0; i < index.Columns.Count; i++) {
        var column = index.Columns[i];
        if (PrefetchHelper.IsFieldToBeLoadedByDefault(column.Field)) {
          indexes.Add(i + columnIndexOffset);
        }
      }
    }

    private static Pair<int>[] GetJoiningColumnIndexes(IndexInfo primaryIndex, IndexInfo associationIndex, bool hasAuxType)
    {
      var joiningColumns = new Pair<int>[primaryIndex.KeyColumns.Count];
      var firstColumnIndex = primaryIndex.Columns.IndexOf(primaryIndex.KeyColumns[0].Key);
      for (var i = 0; i < joiningColumns.Length; i++) {
        if (hasAuxType) {
          joiningColumns[i] =
            new Pair<int>(associationIndex.Columns.IndexOf(associationIndex.ValueColumns[i]),
              firstColumnIndex + i);
        }
        else {
          joiningColumns[i] =
            new Pair<int>(associationIndex.Columns.IndexOf(primaryIndex.KeyColumns[i].Key),
              firstColumnIndex + i);
        }
      }

      return joiningColumns;
    }

    #endregion


    // Constructors

    public EntitySetTask(Key ownerKey, PrefetchFieldDescriptor referencingFieldDescriptor, bool isOwnerCached,
      PrefetchManager manager)
    {
      ArgumentValidator.EnsureArgumentNotNull(ownerKey, "ownerKey");
      ArgumentValidator.EnsureArgumentNotNull(referencingFieldDescriptor, "referencingFieldDescriptor");
      ArgumentValidator.EnsureArgumentNotNull(manager, "processor");

      this.ownerKey = ownerKey;
      this.referencingFieldDescriptor = referencingFieldDescriptor;
      this.isOwnerCached = isOwnerCached;
      ItemCountLimit = referencingFieldDescriptor.EntitySetItemCountLimit;
      this.manager = manager;
      cacheKey = new CacheKey(ReferencingField, ItemCountLimit);
    }
  }
}
