// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundThrowHelper.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Bodu.IO.Compound;

/// <summary>
/// Provides shared throw helpers for the compound-file reader, centralizing the construction of
/// <see cref="CompoundFileFormatException" /> so the structural validators stay terse.
/// </summary>
internal static class CompoundThrowHelper
{
    /// <summary>
    /// Throws a <see cref="CompoundFileFormatException" /> with the supplied message.
    /// </summary>
    /// <param name="message">A message that describes the structural failure.</param>
    /// <exception cref="CompoundFileFormatException">Always thrown.</exception>
    [DoesNotReturn]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ThrowFormat(string message) =>
        throw new CompoundFileFormatException(message);

    /// <summary>
    /// Throws a <see cref="CompoundFileFormatException" /> with the supplied message when <paramref name="condition" />
    /// is <see langword="true" />.
    /// </summary>
    /// <param name="condition">The failure condition to test.</param>
    /// <param name="message">A message that describes the structural failure.</param>
    /// <exception cref="CompoundFileFormatException">
    /// Thrown when <paramref name="condition" /> is <see langword="true" />.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void ThrowFormatIf([DoesNotReturnIf(true)] bool condition, string message)
    {
        if (condition)
            throw new CompoundFileFormatException(message);
    }
}
