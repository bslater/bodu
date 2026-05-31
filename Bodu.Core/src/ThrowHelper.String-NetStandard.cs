// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelper.String.NetStandard.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
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
    /// Throws an <see cref="ArgumentNullException" /> if <paramref name="value" /> is <see langword="null" />, or
    /// an <see cref="ArgumentException" /> if it is an empty string.
    /// </summary>
    /// <param name="value">The string value to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value" /> is an empty string.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfNullOrEmpty(string value)
    {
        if (value is null)
            throw new ArgumentNullException(nameof(value));

        if (value.Length == 0)
            throw new ArgumentException(ResourceStrings.Arg_Invalid_StringNullOrEmpty, nameof(value));
    }

    /// <summary>
    /// Throws an <see cref="ArgumentNullException" /> if <paramref name="value" /> is <see langword="null" />, or
    /// an <see cref="ArgumentException" /> if it is empty or contains only whitespace.
    /// </summary>
    /// <param name="value">The string value to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="value" /> is empty or contains only whitespace characters.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfNullOrWhiteSpace(string value)
    {
        if (value is null)
            throw new ArgumentNullException(nameof(value));

        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException(ResourceStrings.Arg_Invalid_StringEmptyOrWhitespace, nameof(value));
    }

    /// <summary>
    /// Throws an <see cref="ArgumentNullException" /> if <paramref name="value" /> is <see langword="null" />, or an
    /// <see cref="ArgumentOutOfRangeException" /> if its length exceeds <paramref name="maxLength" /> characters.
    /// </summary>
    /// <param name="value">The string to validate. Must not be <see langword="null" />.</param>
    /// <param name="maxLength">The maximum permitted length, inclusive.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <c>value.Length &gt; maxLength</c>.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfStringTooLong(string value, int maxLength)
    {
        if (value is null)
            throw new ArgumentNullException(nameof(value));

        if (value.Length > maxLength)
            throw new ArgumentOutOfRangeException(
                nameof(value),
                string.Format(CultureInfo.CurrentCulture, ResourceStrings.Arg_Invalid_StringTooLong, maxLength));
    }

    /// <summary>
    /// Throws an <see cref="ArgumentNullException" /> if <paramref name="value" /> is <see langword="null" />, or an
    /// <see cref="ArgumentException" /> if its length does not equal <paramref name="expectedLength" /> characters.
    /// </summary>
    /// <param name="value">The string to validate. Must not be <see langword="null" />.</param>
    /// <param name="expectedLength">The exact required length in characters.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <c>value.Length != expectedLength</c>.
    /// </exception>
    /// <remarks>
    /// Useful for validating fixed-length identifiers such as ISIN (12 characters), CUSIP (9 characters),
    /// or IBAN country codes (2 characters).
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfStringLengthIsNotEqualTo(string value, int expectedLength)
    {
        if (value is null)
            throw new ArgumentNullException(nameof(value));

        if (value.Length != expectedLength)
            throw new ArgumentException(
                string.Format(CultureInfo.CurrentCulture, ResourceStrings.Arg_Invalid_StringLength, expectedLength),
                nameof(value));
    }

    /// <summary>
    /// Throws an <see cref="ArgumentNullException" /> if <paramref name="value" /> is <see langword="null" />, or an
    /// <see cref="ArgumentOutOfRangeException" /> if its length is not between <paramref name="minLength" /> and
    /// <paramref name="maxLength" /> (inclusive).
    /// </summary>
    /// <param name="value">The string to validate. Must not be <see langword="null" />.</param>
    /// <param name="minLength">The minimum permitted length, inclusive.</param>
    /// <param name="maxLength">The maximum permitted length, inclusive. Must be &gt;= <paramref name="minLength" />.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <c>value.Length &lt; minLength</c> or <c>value.Length &gt; maxLength</c>.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfStringLengthOutOfRange(string value, int minLength, int maxLength)
    {
        if (value is null)
            throw new ArgumentNullException(nameof(value));

        if (value.Length < minLength || value.Length > maxLength)
            throw new ArgumentOutOfRangeException(
                nameof(value),
                string.Format(CultureInfo.CurrentCulture, ResourceStrings.Arg_Invalid_StringLengthRange, minLength, maxLength));
    }
}

#endif
