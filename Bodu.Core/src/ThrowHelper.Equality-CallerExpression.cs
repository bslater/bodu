// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelper.Equality-CallerExpression.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

#pragma warning disable SA1117 // Parameters should be on same line or separate lines
#pragma warning disable IDE0011 // Add braces

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
    /// <param name="paramName">The name of the value parameter. Supplied automatically by the compiler.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="value" /> equals <paramref name="other" />.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfEqual<T>(
        T value, T other,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
        where T : IEquatable<T>
    {
        if (value.Equals(other))
            throw new ArgumentOutOfRangeException(
                paramName,
                string.Format(CultureInfo.CurrentCulture, ResourceStrings.Arg_OutOfRange_ValuesEqual, other));
    }

    /// <summary>
    /// Throws an <see cref="ArgumentException" /> if <paramref name="value" /> does not equal <paramref name="other" />
    /// .
    /// </summary>
    /// <typeparam name="T">A type that implements <see cref="IEquatable{T}" />.</typeparam>
    /// <param name="value">The value to validate.</param>
    /// <param name="other">The value that <paramref name="value" /> must equal.</param>
    /// <param name="paramName">The name of the value parameter. Supplied automatically by the compiler.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="value" /> does not equal <paramref name="other" />.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfNotEqual<T>(
        T value, T other,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
        where T : IEquatable<T>
    {
        if (!value.Equals(other))
            throw new ArgumentException(
                string.Format(CultureInfo.CurrentCulture, ResourceStrings.Arg_Invalid_ValuesNotEqual, other),
                paramName);
    }
}
