// Copyright (C) 2009 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Alex Yakunin
// Created:    2009.05.01

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xtensive.Core;
using Xtensive.Storage.Building;
using Xtensive.Storage.Resources;
using Xtensive.Storage.Upgrade.Hints;
using M=Xtensive.Storage.Metadata;

namespace Xtensive.Storage.Upgrade
{
  /// <summary>
  /// <see cref="UpgradeHandler"/> implementation 
  /// for <see cref="Xtensive.Storage"/> assembly.
  /// </summary>
  public sealed class SystemUpgradeHandler : UpgradeHandler
  {
    /// <inheritdoc/>
    protected override void CheckMetadata()
    {
      var context = UpgradeContext.Current;
      if (Assembly==Assembly.GetExecutingAssembly()) {
        // We're in Xtensive.Storage upgrade handler
        CheckAssemblies();
      }
    }

    /// <exception cref="DomainBuilderException">Impossible to upgrade all assemblies.</exception>
    private void CheckAssemblies()
    {
      foreach (var pair in GetAssemblies()) {
        var h = pair.First;
        var a = pair.Second;
        if (h==null)
          throw new DomainBuilderException(string.Format(
            Strings.ExNoUpgradeHandlerIsFoundForAssemblyXVersionY,
            a.Name, a.Version));
        if (a==null) {
          if (!h.CanUpgradeFrom(null))
            throw new DomainBuilderException(string.Format(
              Strings.ExUpgradeOfAssemblyXFromVersionYToZIsNotSupported,
              h.AssemblyName, Strings.ZeroAssemblyVersion, h.AssemblyVersion));
          else
            continue;
        }
        if (!h.CanUpgradeFrom(a.Version))
          throw new DomainBuilderException(string.Format(
            Strings.ExUpgradeOfAssemblyXFromVersionYToZIsNotSupported,
            a.Name, a.Version, h.AssemblyVersion));
      }
    }

    /// <summary>
    /// Upgrades the metadata (see <see cref="Xtensive.Storage.Metadata"/>).
    /// </summary>
    protected override void UpdateMetadata()
    {
      var context = UpgradeContext.Current;
      if (Assembly==Assembly.GetExecutingAssembly()) {
        // We're in Xtensive.Storage upgrade handler
        UpdateAssemblies();
        UpdateTypes();
      }
    }

    /// <exception cref="DomainBuilderException">Impossible to upgrade all assemblies.</exception>
    private void UpdateAssemblies()
    {
      foreach (var pair in GetAssemblies()) {
        var h = pair.First;
        var a = pair.Second;
        if (h==null)
          throw new DomainBuilderException(string.Format(
            Strings.ExNoUpgradeHandlerIsFoundForAssemblyXVersionY,
            a.Name, a.Version));
        if (a==null) {
          a = new M.Assembly(h.AssemblyName) {
            Version = h.AssemblyVersion
          };
          Log.Info(Strings.LogMetadataAssemblyCreatedX, a);
        }
        else {
          var oldVersion = a.Version;
          a.Version = h.AssemblyVersion;
          Log.Info(Strings.LogMetadataAssemblyUpdatedXFromVersionYToZ, a, oldVersion, a.Version);
        }
      }
    }

    /// <exception cref="DomainBuilderException">Something went wrong.</exception>
    private void UpdateTypes()
    {
      var context = UpgradeContext.Current;

      var types = Query<M.Type>.All.ToArray();
      var typeByName = new Dictionary<string, M.Type>();
      foreach (var type in types)
        typeByName.Add(type.Name, type);

      foreach (var hint in context.Hints) {
        var trh = hint as RenameTypeHint;
        if (trh!=null) {
          if (!typeByName.ContainsKey(trh.OldName))
            throw new DomainBuilderException(string.Format(
              Strings.ExTypeWithNameXIsNotFoundInMetadata, trh.OldName));
          var newName = TypeIdBuilder.GetTypeName(trh.TargetType);
          typeByName[trh.OldName].Name = newName;
          // Session.Current.Persist();
          Log.Info(Strings.LogMetadataTypeRenamedXToY, trh.OldName, newName);
        }
      }
    }

    private Pair<IUpgradeHandler, M.Assembly>[] GetAssemblies()
    {
      var context = UpgradeContext.Current;

      var oldAssemblies = Query<M.Assembly>.All.ToArray();
      var oldAssemblyByName = new Dictionary<string, M.Assembly>();
      foreach (var oldAssembly in oldAssemblies)
        oldAssemblyByName.Add(oldAssembly.Name, oldAssembly);
      var oldNames = oldAssemblies.Select(a => a.Name);
      
      var handlers = context.UpgradeHandlers.Values.ToArray();
      var handlerByName = new Dictionary<string, IUpgradeHandler>();
      foreach (var handler in handlers)
        handlerByName.Add(handler.AssemblyName, handler);
      var names = handlers.Select(a => a.AssemblyName);
      
      var commonNames = names.Intersect(oldNames);
      var addedNames = names.Except(commonNames);
      var removedNames = oldNames.Except(commonNames);

      return 
        addedNames.Select(n => new Pair<IUpgradeHandler, M.Assembly>(handlerByName[n], null))
          .Concat(commonNames.Select(n => new Pair<IUpgradeHandler, M.Assembly>(handlerByName[n], oldAssemblyByName[n])))
          .Concat(removedNames.Select(n => new Pair<IUpgradeHandler, M.Assembly>(null, oldAssemblyByName[n])))
          .ToArray();
    }
  }
}