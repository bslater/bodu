// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelper.Null.NetStandard.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

#if NETSTANDARD2_0_OR_GREATER
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Bodu;

public static partial class ThrowHelper
{
    /// <summary>
    /// Throws an <see cref="ArgumentNullException" /> if <paramref name="value" /> is <see langword="null" />.
    /// </summary>
    /// <typeparam name="T">The type of the object.</typeparam>
    /// <param name="value">The value to check. Must not be <see langword="null" />.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value" /> is <see langword="null" />.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfNull<T>(T value)
    {
        if (value is null)
            throw new ArgumentNullException(nameof(value));
    }

    /// <summary>
    /// Throws an <see cref="ArgumentNullException" /> if <paramref name="value" /> is <see langword="null" />,
    /// using <paramref name="message" /> as the exception message.
    /// </summary>
    /// <typeparam name="T">The type of the object.</typeparam>
    /// <param name="value">The value to check. Must not be <see langword="null" />.</param>
    /// <param name="message">The message to include in the exception.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value" /> is <see langword="null" />.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfNull<T>(T value, string message)
    {
        if (value is null)
            throw new ArgumentNullException(nameof(value), message);
    }

    /// <summary>
    /// Throws an <see cref="ObjectDisposedException" /> if <paramref name="disposed" /> is
    /// <see langword="true" />.
    /// </summary>
    /// <param name="disposed">The disposal flag to evaluate.</param>
    /// <param name="objectName">The name of the disposed object included in the exception message.</param>
    /// <exception cref="ObjectDisposedException">
    /// Thrown when <paramref name="disposed" /> is <see langword="true" />.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfDisposed(bool disposed, string? objectName = null)
    {
        if (disposed)
            throw new ObjectDisposedException(objectName);
    }
}

#endif
