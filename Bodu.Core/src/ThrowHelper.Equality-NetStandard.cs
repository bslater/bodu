// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelper.Equality-NetStandard.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
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
    /// Throws an <see cref="ArgumentOutOfRangeException" /> if <paramref name="value" /> equals
    /// <paramref name="other" />.
    /// </summary>
    /// <typeparam name="T">A type that implements <see cref="IEquatable{T}" />.</typeparam>
    /// <param name="value">The value to validate.</param>
    /// <param name="other">The value that <paramref name="value" /> must not equal.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="value" /> equals <paramref name="other" />.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfEqual<T>(T value, T other)
        where T : IEquatable<T>
    {
        if (value.Equals(other))
            throw new ArgumentOutOfRangeException(
                nameof(value),
                string.Format(CultureInfo.CurrentCulture, ResourceStrings.Arg_OutOfRange_ValuesEqual, other));
    }

    /// <summary>
    /// Throws an <see cref="ArgumentException" /> if <paramref name="value" /> does not equal
    /// <paramref name="other" />.
    /// </summary>
    /// <typeparam name="T">A type that implements <see cref="IEquatable{T}" />.</typeparam>
    /// <param name="value">The value to validate.</param>
    /// <param name="other">The value that <paramref name="value" /> must equal.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="value" /> does not equal <paramref name="other" />.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfNotEqual<T>(T value, T other)
        where T : IEquatable<T>
    {
        if (!value.Equals(other))
            throw new ArgumentException(
                string.Format(CultureInfo.CurrentCulture, ResourceStrings.Arg_Invalid_ValuesNotEqual, other),
                nameof(value));
    }
}

#endif
