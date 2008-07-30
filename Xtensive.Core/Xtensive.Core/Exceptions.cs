// Copyright (C) 2007 Xtensive LLC.
// All rights reserved.
// For conditions of distribution and use, see license.
// Created by: Alex Yakunin
// Created:    2007.10.03

using System;
using Xtensive.Core.Diagnostics;
using Xtensive.Core.Resources;
using Xtensive.Core.Reflection;

namespace Xtensive.Core
{
  /// <summary>
  /// Most common <see cref="Exception"/> factory.
  /// </summary>
  public static class Exceptions
  {
    /// <summary>
    /// Returns an exception informing internal error has occurred.
    /// </summary>
    /// <param name="description">Error description.</param>
    /// <param name="log"><see cref="ILog"/> instance to log the problem;
    /// <see langword="null"/> means logging is not necessary.</param>
    /// <returns>Newly created exception.</returns>
    public static Exception InternalError(string description, ILog log)
    {
      return log.Error(new InvalidOperationException(String.Format(Strings.ExInternalError, description)));
    }

    /// <summary>
    /// Returns an exception informing that URL is invalid.
    /// </summary>
    /// <param name="url">Invalid URL.</param>
    /// <param name="parameterName">Name of method parameter where URL was passed (<see langword="null"/> if none).</param>
    /// <returns>Newly created exception.</returns>
    public static Exception InvalidUrl(string url, string parameterName)
    {
      if (String.IsNullOrEmpty(parameterName))
        return new InvalidOperationException(String.Format(Strings.ExInvalidUrl, url));
      else
        return new ArgumentException(String.Format(Strings.ExInvalidUrl, url), parameterName);
    }

    /// <summary>
    /// Returns an exception informing that object or property is already initialized.
    /// </summary>
    /// <param name="propertyName">Name of the property; <see langword="null"/>, if none.</param>
    /// <returns>Newly created exception.</returns>
    public static Exception AlreadyInitialized(string propertyName)
    {
      if (String.IsNullOrEmpty(propertyName))
        return new NotSupportedException(Strings.ExAlreadyInitialized);
      else
        return new NotSupportedException(String.Format(Strings.ExPropertyIsAlreadyInitialized, propertyName));
    }

    /// <summary>
    /// Returns an exception informing that object or property is not initialized,
    /// or not initialized properly.
    /// </summary>
    /// <param name="propertyName">Name of the property; <see langword="null"/>, if none.</param>
    /// <returns>Newly created exception.</returns>
    public static Exception NotInitialized(string propertyName)
    {
      if (String.IsNullOrEmpty(propertyName))
        return new InvalidOperationException(Strings.ExNotInitialized);
      else
        return new InvalidOperationException(String.Format(Strings.ExPropertyIsNotInitialized, propertyName));
    }

    /// <summary>
    /// Returns an exception informing that specified argument
    /// value is not allowed or invalid.
    /// </summary>
    /// <param name="value">Actual parameter value.</param>
    /// <param name="parameterName">Name of the method parameter (<see langword="null"/> if none).</param>
    /// <returns>Newly created exception.</returns>
    /// <typeparam name="T">The type of the value.</typeparam>
    public static Exception InvalidArgument<T>(T value, string parameterName)
    {
      if (String.IsNullOrEmpty(parameterName))
        return new InvalidOperationException(String.Format(
          Strings.ExValueXIsNotAllowedHere, value));
      else
        return new ArgumentOutOfRangeException(parameterName, value, String.Format(
          Strings.ExValueXIsNotAllowedHere, value));
    }

    /// <summary>
    /// Returns an exception informing that object is read-only.
    /// </summary>
    /// <param name="parameterName">Name of the method parameter (<see langword="null"/> if none).</param>
    /// <returns>Newly created exception.</returns>
    public static Exception ObjectIsReadOnly(string parameterName)
    {
      if (String.IsNullOrEmpty(parameterName))
        return new NotSupportedException(Strings.ExObjectIsReadOnly);
      else
        return new ArgumentException(Strings.ExObjectIsReadOnly, parameterName);
    }

    /// <summary>
    /// Returns an exception informing that collection is empty.
    /// </summary>
    /// <param name="parameterName">Name of the method parameter (<see langword="null"/> if none).</param>
    /// <returns>Newly created exception.</returns>
    public static Exception CollectionIsEmpty(string parameterName)
    {
      if (String.IsNullOrEmpty(parameterName))
        return new InvalidOperationException(Strings.ExCollectionIsEmpty);
      else
        return new ArgumentException(Strings.ExCollectionIsEmpty, parameterName);
    }

    /// <summary>
    /// Returns an exception informing that collection is read-only.
    /// </summary>
    /// <param name="parameterName">Name of the method parameter (<see langword="null"/> if none).</param>
    /// <returns>Newly created exception.</returns>
    public static Exception CollectionIsReadOnly(string parameterName)
    {
      if (String.IsNullOrEmpty(parameterName))
        return new NotSupportedException(Strings.ExCollectionIsReadOnly);
      else
        return new ArgumentException(Strings.ExCollectionIsReadOnly, parameterName);
    }

    /// <summary>
    /// Returns an exception informing that collection has been changed during the enumeration.
    /// </summary>
    /// <param name="parameterName">Name of the method parameter (<see langword="null"/> if none).</param>
    /// <returns>Newly created exception.</returns>
    public static Exception CollectionHasBeenChanged(string parameterName)
    {
      if (String.IsNullOrEmpty(parameterName))
        return new InvalidOperationException(Strings.ExCollectionHasBeenChanged);
      else
        return new ArgumentException(Strings.ExCollectionHasBeenChanged, parameterName);
    }

    /// <summary>
    /// Returns an exception informing that context is required.
    /// </summary>
    /// <param name="contextType">Type of required context.</param>
    /// <param name="scopeType">Type of <see cref="Scope{TContext}"/> used to set the context.</param>
    /// <returns>Newly created exception.</returns>
    public static Exception ContextRequired(Type contextType, Type scopeType)
    {
      ArgumentValidator.EnsureArgumentNotNull(contextType, "contextType");
      ArgumentValidator.EnsureArgumentNotNull(scopeType, "scopeType");
      return new Exception(String.Format(Strings.ExContextRequired, contextType.GetShortName(), scopeType.GetShortName()));
    }
  }
}