﻿using System;
using System.Collections.Generic;
using Xtensive.Orm.Building.Definitions;

namespace Xtensive.Orm.Building
{
  /// <summary>
  /// Default implementation of <see cref="IModule2"/>.
  /// </summary>
  public abstract class Module : IModule2
  {
    /// <inheritdoc />
    public virtual void OnDefinitionsBuilt(BuildingContext context, DomainModelDef model)
    {
    }

    /// <inheritdoc />
    public virtual void OnAutoGenericsBuilt(BuildingContext context, ICollection<Type> autoGenerics)
    {
    }

    /// <inheritdoc />
    public virtual void OnBuilt(Domain domain)
    {
    }
  }
}