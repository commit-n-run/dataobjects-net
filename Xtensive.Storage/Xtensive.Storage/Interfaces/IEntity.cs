// Copyright (C) 2007 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Alex Yakunin
// Created:    2007.05.25

using System;
using Xtensive.Core;

namespace Xtensive.Storage
{
  /// <summary>
  /// Should be implemented by any persistent entity.
  /// </summary>
  public interface IEntity: IIdentified<Key>
  {
    /// <summary>
    /// Gets persistence state of the entity.
    /// </summary>
    PersistenceState PersistenceState { get; }

    /// <summary>
    /// Raised when <see cref="PersistenceState"/> of persistent object is changed.
    /// </summary>
    event EventHandler PersistenceStateChanged;
  }
}