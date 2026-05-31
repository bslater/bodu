// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelper.Equality-CallerExpression.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

#if !NETSTANDARD2_0_OR_GREATER
#pragma warning disable SA1117 // Parameters should be on same line or separate lines
#pragma warning disable IDE0011 // Add braces

using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;

namespace Bodu;

public static partial class ThrowHelper
{
    /// <summary>
    /// Cached parsed format for <see cref="ResourceStrings.Arg_Invalid_ValuesNotEqual" />.
    /// </summary>
    private static readonly CompositeFormat s_argInvalidValuesNotEqual =
        CompositeFormat.Parse(ResourceStrings.Arg_Invalid_ValuesNotEqual);

    /// <summary>
    /// Cached parsed format for <see cref="ResourceStrings.Arg_OutOfRange_ValuesEqual" />.
    /// </summary>
    private static readonly CompositeFormat s_argOutOfRangeValuesEqual =
        CompositeFormat.Parse(ResourceStrings.Arg_OutOfRange_ValuesEqual);

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
                string.Format(CultureInfo.CurrentCulture, s_argOutOfRangeValuesEqual, other));
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
                string.Format(CultureInfo.CurrentCulture, s_argInvalidValuesNotEqual, other),
                paramName);
    }
}

#endif
