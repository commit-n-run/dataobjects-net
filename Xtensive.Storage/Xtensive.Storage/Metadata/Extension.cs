// Copyright (C) 2008 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Alex Kofman
// Created:    2008.12.24

using System;
using Xtensive.Core.Internals.DocTemplates;

namespace Xtensive.Storage.Metadata
{
  /// <summary>
  /// Persistent value of any kind indentified by its <see cref="Name"/>.
  /// </summary>
  [Serializable]
  [SystemType(3)]
  [HierarchyRoot]
  [KeyGenerator(KeyGeneratorKind.None)]
  [TableMapping("Metadata.Extension")]
  public class Extension : MetadataBase
  {
    /// <summary>
    /// Gets or sets the name of the extension.
    /// </summary>
    [Key]
    [Field(Length = 1024)]
    public string Name { get; private set; }

    /// <summary>
    /// Gets or sets the text data.
    /// </summary>
    [Field(Length = int.MaxValue)]
    public string Text { get; set; }

    /// <summary>
    /// Gets or sets the binary data.
    /// </summary>
    [Field(Length = int.MaxValue)]
    public byte[] Data { get; set; }

    /// <inheritdoc/>
    public override string ToString()
    {
      return Name;
    }


    // Constructors

    /// <summary>
    /// <see cref="ClassDocTemplate.Ctor" copy="true"/>
    /// </summary>
    /// <param name="name">A value for <see cref="Name"/>.</param>
    /// <exception cref="Exception">Object is read-only.</exception>
    public Extension(string name) 
      : base(name)
    {
    }
  }
}