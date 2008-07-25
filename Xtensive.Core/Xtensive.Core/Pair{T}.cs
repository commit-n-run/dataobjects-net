// Copyright (C) 2007 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Alex Ustinov
// Created:    2007.06.01

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Xtensive.Core.Comparison;
using Xtensive.Core.Internals.DocTemplates;
using Xtensive.Core.Resources;

namespace Xtensive.Core
{
  /// <summary>
  /// A pair of two values of the same type.
  /// </summary>
  /// <typeparam name="T">The type of both stored values.</typeparam>
  [Serializable]
  [DebuggerDisplay("{First}, {Second}")]
  public struct Pair<T> : 
    IEquatable<Pair<T>>,
    IComparable<Pair<T>>
  {
    /// <summary>
    /// The first value.
    /// </summary>
    public readonly T First;
    
    /// <summary>
    /// The second value.
    /// </summary>
    public readonly T Second;

    #region IComparable<...>, IEquatable<...> methods

    /// <inheritdoc/>
    public bool Equals(Pair<T> other)
    {
      if (!AdvancedComparerStruct<T>.System.Equals(First, other.First))
        return false;
      return AdvancedComparerStruct<T>.System.Equals(Second, other.Second);
    }

    /// <inheritdoc/>
    public int CompareTo(Pair<T> other)
    {
      int result = AdvancedComparerStruct<T>.System.Compare(First, other.First);
      if (result!=0)
        return result;
      return AdvancedComparerStruct<T>.System.Compare(Second, other.Second);
    }

    #endregion

    #region Equals, GetHashCode, ==, !=

    /// <inheritdoc/>
    public override bool Equals(object obj)
    {
      if (obj.GetType()!=typeof (Pair<T>))
        return false;
      return Equals((Pair<T>) obj);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
      unchecked {
        int result = (First!=null ? First.GetHashCode() : 0);
        result = (result * 397) ^ (Second!=null ? Second.GetHashCode() : 0);
        return result;
      }
    }

    /// <see cref="ClassDocTemplate.OperatorEq"/>
    public static bool operator ==(Pair<T> left, Pair<T> right)
    {
      return left.Equals(right);
    }

    /// <see cref="ClassDocTemplate.OperatorNeq"/>
    public static bool operator !=(Pair<T> left, Pair<T> right)
    {
      return !left.Equals(right);
    }

    #endregion

    /// <inheritdoc/>
    public override string ToString()
    {
      return String.Format(Strings.PairFormat, First, Second);
    }


    // Constructors

    /// <summary>
    /// <see cref="ClassDocTemplate.Ctor" copy="true" />
    /// </summary>
    /// <param name="first">The first value in the pair.</param>
    /// <param name="second">The second value in the pair.</param>
    public Pair(T first, T second)
    {
      First = first;
      Second = second;
    }
  }
}