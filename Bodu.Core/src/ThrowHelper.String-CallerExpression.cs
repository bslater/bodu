// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelper.String-CallerExpression.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd.. All rights reserved.
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
    /// Cached parsed format for <see cref="ResourceStrings.Arg_Invalid_StringLength" />.
    /// </summary>
    private static readonly CompositeFormat s_argInvalidStringLength =
        CompositeFormat.Parse(ResourceStrings.Arg_Invalid_StringLength);

    /// <summary>
    /// Cached parsed format for <see cref="ResourceStrings.Arg_Invalid_StringLengthRange" />.
    /// </summary>
    private static readonly CompositeFormat s_argInvalidStringLengthRange =
        CompositeFormat.Parse(ResourceStrings.Arg_Invalid_StringLengthRange);

    /// <summary>
    /// Cached parsed format for <see cref="ResourceStrings.Arg_Invalid_StringTooLong" />.
    /// </summary>
    private static readonly CompositeFormat s_argInvalidStringTooLong =
        CompositeFormat.Parse(ResourceStrings.Arg_Invalid_StringTooLong);

    /// <summary>
    /// Throws an <see cref="ArgumentNullException" /> if <paramref name="value" /> is <see langword="null" />, or an
    /// <see cref="ArgumentException" /> if it is an empty string.
    /// </summary>
    /// <param name="value">The string value to validate.</param>
    /// <param name="paramName">The name of the parameter. Supplied automatically by the compiler.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="value" /> is an empty string.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfNullOrEmpty(
        string value,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (value is null)
            throw new ArgumentNullException(paramName);

        if (value.Length == 0)
            throw new ArgumentException(ResourceStrings.Arg_Invalid_StringNullOrEmpty, paramName);
    }

    /// <summary>
    /// Throws an <see cref="ArgumentNullException" /> if <paramref name="value" /> is <see langword="null" />, or an
    /// <see cref="ArgumentException" /> if it is empty or contains only whitespace.
    /// </summary>
    /// <param name="value">The string value to validate.</param>
    /// <param name="paramName">The name of the parameter. Supplied automatically by the compiler.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="value" /> is empty or contains only whitespace characters.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfNullOrWhiteSpace(
        string value,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (value is null)
            throw new ArgumentNullException(paramName);

        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException(ResourceStrings.Arg_Invalid_StringEmptyOrWhitespace, paramName);
    }

    /// <summary>
    /// Throws an <see cref="ArgumentNullException" /> if <paramref name="value" /> is <see langword="null" />, or an
    /// <see cref="ArgumentOutOfRangeException" /> if its length exceeds <paramref name="maxLength" /> characters.
    /// </summary>
    /// <param name="value">The string to validate. Must not be <see langword="null" />.</param>
    /// <param name="maxLength">The maximum permitted length, inclusive.</param>
    /// <param name="paramName">The name of the parameter. Supplied automatically by the compiler.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <c>value.Length &gt; maxLength</c>.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfStringTooLong(
        string value, int maxLength,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (value is null)
            throw new ArgumentNullException(paramName);

        if (value.Length > maxLength)
            throw new ArgumentOutOfRangeException(
                paramName,
                string.Format(CultureInfo.CurrentCulture, s_argInvalidStringTooLong, maxLength));
    }

    /// <summary>
    /// Throws an <see cref="ArgumentNullException" /> if <paramref name="value" /> is <see langword="null" />, or an
    /// <see cref="ArgumentException" /> if its length does not equal <paramref name="expectedLength" /> characters.
    /// </summary>
    /// <param name="value">The string to validate. Must not be <see langword="null" />.</param>
    /// <param name="expectedLength">The exact required length in characters.</param>
    /// <param name="paramName">The name of the parameter. Supplied automatically by the compiler.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">Thrown when <c>value.Length != expectedLength</c>.</exception>
    /// <remarks>
    /// Useful for validating fixed-length identifiers such as ISIN (12 characters), CUSIP (9 characters), or IBAN
    /// country codes (2 characters).
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfStringLengthIsNotEqualTo(
        string value, int expectedLength,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (value is null)
            throw new ArgumentNullException(paramName);

        if (value.Length != expectedLength)
            throw new ArgumentException(
                string.Format(CultureInfo.CurrentCulture, s_argInvalidStringLength, expectedLength),
                paramName);
    }

    /// <summary>
    /// Throws an <see cref="ArgumentNullException" /> if <paramref name="value" /> is <see langword="null" />, or an
    /// <see cref="ArgumentOutOfRangeException" /> if its length is not between <paramref name="minLength" /> and
    /// <paramref name="maxLength" /> (inclusive).
    /// </summary>
    /// <param name="value">The string to validate. Must not be <see langword="null" />.</param>
    /// <param name="minLength">The minimum permitted length, inclusive.</param>
    /// <param name="maxLength">
    /// The maximum permitted length, inclusive. Must be &gt;= <paramref name="minLength" />.
    /// </param>
    /// <param name="paramName">The name of the parameter. Supplied automatically by the compiler.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <c>value.Length &lt; minLength</c> or <c>value.Length &gt; maxLength</c>.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfStringLengthOutOfRange(
        string value, int minLength, int maxLength,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (value is null)
            throw new ArgumentNullException(paramName);

        if (value.Length < minLength || value.Length > maxLength)
            throw new ArgumentOutOfRangeException(
                paramName,
                string.Format(CultureInfo.CurrentCulture, s_argInvalidStringLengthRange, minLength, maxLength));
    }
}

#endif
