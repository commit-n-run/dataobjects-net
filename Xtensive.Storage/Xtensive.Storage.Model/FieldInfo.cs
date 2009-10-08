// Copyright (C) 2007 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Dmitri Maximov
// Created:    2007.09.10

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Diagnostics;
using System.Reflection;
using Xtensive.Core;
using Xtensive.Core.Helpers;
using Xtensive.Core.Internals.DocTemplates;
using Xtensive.Core.Threading;
using Xtensive.Core.Tuples;
using Xtensive.Core.Tuples.Transform;

namespace Xtensive.Storage.Model
{
  [DebuggerDisplay("{Name}; Attributes = {Attributes}")]
  [Serializable]
  public sealed class FieldInfo : MappingNode,
    ICloneable
  {
    private PropertyInfo                  underlyingProperty;
    private Type                          valueType;
    private int?                          length;
    private int?                          scale;
    private int?                          precision;
    private TypeInfo                      reflectedType;
    private TypeInfo                      declaringType;
    private FieldInfo                     parent;
    private ColumnInfo                    column;
    private AssociationInfo               association;
    private CultureInfo                   cultureInfo = CultureInfo.InvariantCulture;
    private ThreadSafeCached<int>         cachedHashCode = ThreadSafeCached<int>.Create(new object());
    private Type                          itemType;
    private string                        originalName;
    internal SegmentTransform             valueExtractorTransform;
    private int                           adapterIndex = -1;
    private ColumnInfoCollection          columns;
    
    #region IsXxx properties

    /// <summary>
    /// Gets a value indicating whether this property is system.
    /// </summary>
    public bool IsSystem {
      [DebuggerStepThrough]
      get { return (Attributes & FieldAttributes.System) != 0; }
      [DebuggerStepThrough]
      set {
        this.EnsureNotLocked();
        Attributes = value
          ? (Attributes | FieldAttributes.System)
          : (Attributes & ~FieldAttributes.System);
      }
    }

    /// <summary>
    /// Gets a value indicating whether this property contains Type identifier.
    /// </summary>
    public bool IsTypeId {
      [DebuggerStepThrough]
      get { return (Attributes & FieldAttributes.TypeId) != 0; }
    }

    /// <summary>
    /// Gets or sets a value indicating whether this instance is declared in <see cref="TypeInfo"/> instance.
    /// </summary>
    public bool IsDeclared {
      [DebuggerStepThrough]
      get { return (Attributes & FieldAttributes.Declared) > 0; }
      [DebuggerStepThrough]
      set {
        this.EnsureNotLocked();
        Attributes = value
          ? (Attributes | FieldAttributes.Declared) & ~FieldAttributes.Inherited
          : (Attributes & ~FieldAttributes.Declared) | FieldAttributes.Inherited;
      }
    }

    /// <summary>
    /// Gets a value indicating whether this property is enum.
    /// </summary>
    public bool IsEnum { 
      [DebuggerStepThrough]
      get { return (Attributes & FieldAttributes.Enum) > 0; }
      private set {
        this.EnsureNotLocked();
        Attributes = value
          ? (Attributes | FieldAttributes.Enum)
          : (Attributes & ~FieldAttributes.Enum);
      }
    }

    /// <summary>
    /// Gets or sets a value indicating whether this instance is inherited from parent <see cref="TypeInfo"/> instance.
    /// </summary>
    public bool IsInherited {
      [DebuggerStepThrough]
      get { return (Attributes & FieldAttributes.Inherited) > 0; }
      [DebuggerStepThrough]
      set {
        this.EnsureNotLocked();
        Attributes = value
          ? (Attributes | FieldAttributes.Inherited) & ~FieldAttributes.Declared
          : (Attributes & ~FieldAttributes.Inherited) | FieldAttributes.Declared;
      }
    }

    /// <summary>
    /// Gets a value indicating whether this property is contained by primary key.
    /// </summary>
    public bool IsPrimaryKey {
      [DebuggerStepThrough]
      get { return (Attributes & FieldAttributes.PrimaryKey) != 0; }
      [DebuggerStepThrough]
      set {
        this.EnsureNotLocked();
        Attributes = value ? Attributes | FieldAttributes.PrimaryKey : Attributes & ~FieldAttributes.PrimaryKey;
        if (column != null)
          column.IsPrimaryKey = true;
        else
          foreach (FieldInfo childField in Fields)
            childField.IsPrimaryKey = true;
      }
    }

    /// <summary>
    /// Gets a value indicating whether this property is nested.
    /// </summary>
    public bool IsNested {
      [DebuggerStepThrough]
      get { return Parent != null; }
    }

    /// <summary>
    /// Gets a value indicating whether this property explicitly implemented.
    /// </summary>
    public bool IsExplicit {
      [DebuggerStepThrough]
      get { return (Attributes & FieldAttributes.Explicit) != 0; }
      [DebuggerStepThrough]
      set {
        this.EnsureNotLocked();
        Attributes = value ? Attributes | FieldAttributes.Explicit : Attributes & ~FieldAttributes.Explicit;
      }
    }

    /// <summary>
    /// Gets a value indicating whether this property implements property of one or more interfaces.
    /// </summary>
    public bool IsInterfaceImplementation {
      [DebuggerStepThrough]
      get { return (Attributes & FieldAttributes.InterfaceImplementation) != 0; }
      [DebuggerStepThrough]
      set {
        this.EnsureNotLocked();
        Attributes = value ? Attributes | FieldAttributes.InterfaceImplementation : Attributes & ~FieldAttributes.InterfaceImplementation;
      }
    }

    /// <summary>
    /// Gets a value indicating whether this property is primitive field.
    /// </summary>
    public bool IsPrimitive {
      [DebuggerStepThrough]
      get { return !IsStructure && !IsEntity && !IsEntitySet; }
    }

    /// <summary>
    /// Gets a value indicating whether this property is reference to Entity.
    /// </summary>
    public bool IsEntity {
      [DebuggerStepThrough]
      get { return (Attributes & FieldAttributes.Entity) != 0; }
    }

    /// <summary>
    /// Gets a value indicating whether this property is structure field.
    /// </summary>
    public bool IsStructure {
      [DebuggerStepThrough]
      get { return (Attributes & FieldAttributes.Structure) != 0; }
    }

    /// <summary>
    /// Gets a value indicating whether this property is reference to EntitySet.
    /// </summary>
    public bool IsEntitySet {
      [DebuggerStepThrough]
      get { return (Attributes & FieldAttributes.EntitySet) != 0; }
    }

    /// <summary>
    /// Gets or sets a value indicating whether property is nullable.
    /// </summary>
    public bool IsNullable
    {
      [DebuggerStepThrough]
      get { return (Attributes & FieldAttributes.Nullable) != 0; }
      [DebuggerStepThrough]
      private set {
        this.EnsureNotLocked();
        Attributes = value ? Attributes | FieldAttributes.Nullable : Attributes & ~FieldAttributes.Nullable;
      }
    }

    /// <summary>
    /// Gets or sets a value indicating whether property will be loaded on demand.
    /// </summary>
    public bool IsLazyLoad
    {
      [DebuggerStepThrough]
      get { return (Attributes & FieldAttributes.LazyLoad) != 0; }
      [DebuggerStepThrough]
      set {
        this.EnsureNotLocked();
        Attributes = value ? Attributes | FieldAttributes.LazyLoad : Attributes & ~FieldAttributes.LazyLoad;
      }
    }

    /// <summary>
    /// Gets or sets a value indicating whether property is part of version.
    /// </summary>
    public bool IsVersion
    {
      [DebuggerStepThrough]
      get { return (Attributes & FieldAttributes.Version) != 0; }
      [DebuggerStepThrough]
      set {
        this.EnsureNotLocked();
        Attributes = value ? Attributes | FieldAttributes.Version : Attributes & ~FieldAttributes.Version;
      }
    }

    #endregion

    /// <summary>
    /// Gets or sets original name of this instance.
    /// </summary>
    public string OriginalName
    {
      get { return originalName; }
      set
      {
        this.EnsureNotLocked();
        ValidateName(value);
        originalName = value;
      }
    }

    /// <summary>
    /// Gets or sets the type of the value of this instance.
    /// </summary>
    public Type ValueType {
      [DebuggerStepThrough]
      get { return valueType; }
      [DebuggerStepThrough]
      set {
        this.EnsureNotLocked();
        valueType = value;
        if (valueType.IsGenericType) {
          if (valueType.GetGenericTypeDefinition() == typeof (Nullable<>))
            IsEnum = Nullable.GetUnderlyingType(valueType).IsEnum;
        }
        else
          IsEnum = value.IsEnum;
      }
    }

    /// <summary>
    /// Gets or sets the item type for field that describes the EntitySet.
    /// </summary>
    public Type ItemType {
      [DebuggerStepThrough]
      get { return itemType; }
      [DebuggerStepThrough]
      set {
        this.EnsureNotLocked();
        itemType = value;
      }
    }

    /// <summary>
    /// Gets or sets the maximal length of the field.
    /// </summary>
    public int? Length {
      [DebuggerStepThrough]
      get { return length; }
      [DebuggerStepThrough]
      set {
        this.EnsureNotLocked();
        length = value;
      }
    }

    /// <summary>
    /// Gets or sets the scale of the field.
    /// </summary>
    public int? Scale {
      [DebuggerStepThrough]
      get { return scale; }
      [DebuggerStepThrough]
      set {
        this.EnsureNotLocked();
        scale = value;
      }
    }

    /// <summary>
    /// Gets or sets the precision of the field.
    /// </summary>
    public int? Precision {
      [DebuggerStepThrough]
      get { return precision; }
      [DebuggerStepThrough]
      set {
        this.EnsureNotLocked();
        precision = value;
      }
    }

    /// <summary>
    /// Gets the attributes.
    /// </summary>
    public FieldAttributes Attributes { get; private set; }

    /// <summary>
    /// Gets <see cref="MappingInfo"/> for current field.
    /// </summary>
    public Segment<int> MappingInfo { get; private set; }

    /// <summary>
    /// Gets the underlying system property.
    /// </summary>
    public PropertyInfo UnderlyingProperty {
      [DebuggerStepThrough]
      get { return underlyingProperty; }
      [DebuggerStepThrough]
      set {
        this.EnsureNotLocked();
        underlyingProperty = value;
      }
    }

    /// <summary>
    /// Gets or sets the parent field for nested fields.
    /// </summary>
    /// <remarks>
    /// For not nested fields return value is <see langword="null"/>.
    /// </remarks>
    public FieldInfo Parent {
      [DebuggerStepThrough]
      get { return parent; }
      [DebuggerStepThrough]
      set {
        this.EnsureNotLocked();
        ArgumentValidator.EnsureArgumentNotNull(value, "Parent");
        parent = value;
        parent.Fields.Add(this);
        reflectedType = value.ReflectedType;
        declaringType = value.DeclaringType;
        IsDeclared = value.IsDeclared;
        IsPrimaryKey = value.IsPrimaryKey;
        if (value.IsEntity)
          IsNullable = value.IsNullable;
        association = value.association;
        itemType = value.itemType;
      }
    }

    /// <summary>
    /// Gets the type that was used to obtain this instance.
    /// </summary>
    public TypeInfo ReflectedType {
      [DebuggerStepThrough]
      get { return reflectedType; }
      [DebuggerStepThrough]
      set {
        this.EnsureNotLocked();
        reflectedType = value;
      }
    }

    /// <summary>
    /// Gets the type where the field is declared.
    /// </summary>
    public TypeInfo DeclaringType {
      [DebuggerStepThrough]
      get { return declaringType; }
      [DebuggerStepThrough]
      set {
        this.EnsureNotLocked();
        declaringType = value;
      }
    }

    /// <summary>
    /// Gets the nested fields.
    /// </summary>
    public FieldInfoCollection Fields { get; private set; }

    /// <summary>
    /// Gets or sets the column associated with this instance.
    /// </summary>
    public ColumnInfo Column {
      [DebuggerStepThrough]
      get { return column; }
      [DebuggerStepThrough]
      set {
        this.EnsureNotLocked();
        column = value;
      }
    }

    /// <summary>
    /// Gets or sets the field association.
    /// </summary>
    public AssociationInfo Association {
      [DebuggerStepThrough]
      get { return association; }
      [DebuggerStepThrough]
      set {
        this.EnsureNotLocked();
        association = value;
      }
    }

    /// <summary>
    /// Gets or sets field <see cref="CultureInfo"/> info.
    /// </summary>
    public CultureInfo CultureInfo {
      [DebuggerStepThrough]
      get { return cultureInfo; }
      [DebuggerStepThrough]
      set {
        this.EnsureNotLocked();
        cultureInfo = value;
      }
    }

    /// <summary>
    /// Gets or sets field's adapter index.
    /// </summary>
    public int AdapterIndex {
      [DebuggerStepThrough]
      get { return adapterIndex; }
      [DebuggerStepThrough]
      set {
        this.EnsureNotLocked();
        adapterIndex = value;
      }
    }

    /// <summary>
    /// Extracts the field value from the specified <see cref="Tuple"/>.
    /// </summary>
    /// <param name="tuple">The tuple to extract value from.</param>
    /// <returns><see cref="Tuple"/> instance with the extracted value.</returns>
    public Tuple ExtractValue (Tuple tuple)
    {
      return valueExtractorTransform.Apply(TupleTransformType.TransformedTuple, tuple);
    }

    /// <summary>
    /// Gets field columns.
    /// </summary>
    public ColumnInfoCollection Columns
    {
      get
      {
        if (columns != null)
          return columns;

        var result = new ColumnInfoCollection();
        GetColumns(result);
        return result;
      }
    }

    private void GetColumns(ColumnInfoCollection result)
    {
      if (Column != null)
        result.Add(Column);
      else
        if (!IsPrimitive)
          foreach (var item in Fields.Where(f => f.Column!=null).Select(f => f.Column))
            result.Add(item);
    }

    /// <inheritdoc/>
    public override void UpdateState(bool recursive)
    {
      base.UpdateState(recursive);
      if (!recursive)
        return;
      Fields.UpdateState(true);
      if (column!=null) 
        column.UpdateState(true);
      columns = new ColumnInfoCollection();
      GetColumns(columns);

      CreateMappingInfo();
    }

    /// <inheritdoc/>
    public override void Lock(bool recursive)
    {
      base.Lock(recursive);
      if (!recursive)
        return;
      Fields.Lock(true);
      if (column!=null) 
        column.Lock(true);
    }

    private void CreateMappingInfo()
    {
      if (column!=null) {
        if (reflectedType.IsStructure)
          MappingInfo = new Segment<int>(reflectedType.Columns.IndexOf(column), 1);
        else {
          var primaryIndex = reflectedType.Indexes.PrimaryIndex;
          var indexColumn = primaryIndex.Columns.Where(c => c.Name==column.Name).FirstOrDefault();
          if (indexColumn == null)
            throw new InvalidOperationException();
          MappingInfo = new Segment<int>(primaryIndex.Columns.IndexOf(indexColumn), 1);
        }
      }
      else 
        if (Fields.Count > 0)
          MappingInfo = new Segment<int>(
            Fields.First().MappingInfo.Offset, Fields.Sum(f => f.IsPrimitive ? f.MappingInfo.Length : 0));

      if (IsEntity || IsStructure) {
        valueExtractorTransform = new SegmentTransform(
          false, reflectedType.TupleDescriptor, new Segment<int>(MappingInfo.Offset, MappingInfo.Length));
      }
    }

    #region Equals, GetHashCode methods

    /// <inheritdoc/>
    public bool Equals(FieldInfo obj)
    {
      if (ReferenceEquals(null, obj))
        return false;
      if (ReferenceEquals(this, obj))
        return true;
      return 
        obj.declaringType==declaringType && 
          obj.valueType==valueType && 
            obj.Name==Name;
    }

    /// <inheritdoc/>
    public override bool Equals(object obj)
    {
      if (ReferenceEquals(this, obj))
        return true;
      if (obj.GetType()!=typeof (FieldInfo))
        return false;
      return Equals((FieldInfo) obj);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
      unchecked {
        return cachedHashCode.GetValue(
          _this =>
            ((_this.declaringType.GetHashCode() * 397) ^ 
              _this.valueType.GetHashCode() * 631) ^ 
                _this.Name.GetHashCode(),
          this);
      }
    }

    #endregion

    #region ICloneable methods

    /// <inheritdoc/>
    object ICloneable.Clone()
    {
      return Clone();
    }

    /// <summary>
    /// Clones this instance.
    /// </summary>
    public FieldInfo Clone()
    {
      var clone= new FieldInfo(declaringType, reflectedType, Attributes)
        {
          Name = Name, 
          OriginalName = OriginalName,
          MappingName = MappingName, 
          underlyingProperty = underlyingProperty, 
          valueType = valueType, 
          itemType = itemType,
          length = length, 
          scale = scale,
          precision = precision,
          association = association
        };
      return clone;
    }

    #endregion


    // Constructors

    /// <summary>
    /// <see cref="ClassDocTemplate.Ctor" copy="true"/>
    /// </summary>
    /// <param name="type">The type.</param>
    /// <param name="attributes">The attributes.</param>
    public FieldInfo(TypeInfo type, FieldAttributes attributes) : this(type, type, attributes)
    {
      Attributes |= FieldAttributes.Declared;
    }

    private FieldInfo(TypeInfo declaringType, TypeInfo reflectedType, FieldAttributes attributes)
    {
      Attributes = attributes;
      this.declaringType = declaringType;
      this.reflectedType = reflectedType;
      Fields = IsEntity || IsStructure ? new FieldInfoCollection() : FieldInfoCollection.Empty;
    }
  }
}