// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundThrowHelper.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using Bodu.IO.Compound.Builders;

namespace Bodu.IO.Compound;

/// <summary>
/// Provides shared throw helpers for the compound-file reader and its writable storages, centralizing the construction
/// of <see cref="CompoundFileFormatException" /> so the structural validators stay terse.
/// </summary>
internal static class CompoundThrowHelper
{
    /// <summary>
    /// Throws a <see cref="CompoundFileFormatException" /> with the supplied message and category.
    /// </summary>
    /// <param name="message">A message that describes the structural failure.</param>
    /// <param name="category">The category that classifies the failure.</param>
    /// <exception cref="CompoundFileFormatException">Always thrown.</exception>
    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ThrowFormat(string message, CompoundFileError category) =>
        throw new CompoundFileFormatException(message, category);

    /// <summary>
    /// Throws a <see cref="CompoundFileFormatException" /> with the supplied message and category when
    /// <paramref name="condition" /> is <see langword="true" />.
    /// </summary>
    /// <param name="condition">The failure condition to test.</param>
    /// <param name="message">A message that describes the structural failure.</param>
    /// <param name="category">The category that classifies the failure.</param>
    /// <exception cref="CompoundFileFormatException">
    /// Thrown when <paramref name="condition" /> is <see langword="true" />.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ThrowFormatIf([DoesNotReturnIf(true)] bool condition, string message, CompoundFileError category)
    {
        if (condition)
            throw new CompoundFileFormatException(message, category);
    }

    /// <summary>
    /// Throws an <see cref="ArgumentException" /> when a storage already holds a child with the specified name.
    /// </summary>
    /// <param name="taken">Whether a child of the storage already has <paramref name="name" />.</param>
    /// <param name="name">The name requested for a new or renamed child.</param>
    /// <param name="paramName">The name of the parameter that holds <paramref name="name" />.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="taken" /> is <see langword="true" />.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ThrowIfNameTaken([DoesNotReturnIf(true)] bool taken, string name, [CallerArgumentExpression(nameof(name))] string? paramName = null)
    {
        if (taken)
            throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, CompoundResourceStrings.Arg_Invalid_CompoundChildNameTaken, name), paramName);
    }

    /// <summary>
    /// Throws an <see cref="ArgumentOutOfRangeException" /> when build options hold a version that is neither the
    /// default, which selects version 3, nor a defined <see cref="CompoundFileVersion" />.
    /// </summary>
    /// <param name="options">The build options to validate.</param>
    /// <param name="paramName">
    /// The name of the parameter that holds <paramref name="options" />. Supplied automatically by the compiler.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the <see cref="CompoundBuildOptions.Version" /> of <paramref name="options" /> is neither the
    /// default nor a defined <see cref="CompoundFileVersion" />.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ThrowIfVersionUndefined(CompoundBuildOptions options, [CallerArgumentExpression(nameof(options))] string? paramName = null)
    {
        if (options.Version != default)
            ThrowHelper.ThrowIfEnumValueIsUndefined(options.Version, paramName);
    }
}
