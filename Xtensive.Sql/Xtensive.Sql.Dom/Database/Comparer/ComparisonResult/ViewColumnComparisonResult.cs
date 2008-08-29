// Copyright (C) 2008 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Aleksey Gamzov
// Created:    2008.08.26

using System;
using Xtensive.Core.Internals.DocTemplates;

namespace Xtensive.Sql.Dom.Database.Comparer
{
  [Serializable]
  public class ViewColumnComparisonResult : DataTableColumnComparisonResult, 
    IComparisonResult<ViewColumn>
  {
    /// <inheritdoc/>
    public new ViewColumn NewValue
    {
      get { return (ViewColumn) base.NewValue; }
    }

    /// <inheritdoc/>
    public new ViewColumn OriginalValue
    {
      get { return (ViewColumn) base.OriginalValue; }
    }

    /// <summary>
    /// <see cref="ClassDocTemplate.Ctor" copy="true"/>
    /// </summary>
    public ViewColumnComparisonResult(DataTableColumn originalValue, DataTableColumn newValue)
      : base(originalValue, newValue)
    {
    }
  }
}