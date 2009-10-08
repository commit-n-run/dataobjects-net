// Copyright (C) 2007 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Dmitri Maximov
// Created:    2007.10.03

using System;

namespace Xtensive.Storage.Model
{
  [Flags]
  public enum ColumnAttributes
  {
    None = 0,
    Nullable = 0x1,
    Collatable = 0x2,
    Declared = 0x4,
    Inherited = 0x8,
    PrimaryKey = 0x10,
    System = 0x40,
    LazyLoad = 0x80,
    Stub = 0x100
  }
}