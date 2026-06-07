// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelper.Ascii.NetStandard.cs" company="Bodu Pty. Ltd.">
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
    /// Throws an <see cref="ArgumentOutOfRangeException" /> if <paramref name="value" /> is not an ASCII decimal
    /// digit character, that is, a character in the range <c>'0'</c> (U+0030) to <c>'9'</c> (U+0039) inclusive.
    /// </summary>
    /// <param name="value">The character to validate.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="value" /> is outside the inclusive range <c>'0'</c> to <c>'9'</c>.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfNotAsciiDecimalDigit(char value)
    {
        if ((uint)(value - '0') > 9u)
            throw new ArgumentOutOfRangeException(
                nameof(value),
                value,
                string.Format(CultureInfo.CurrentCulture, ResourceStrings.Arg_OutOfRange_NotAsciiDecimalDigit, value, (int)value));
    }

    /// <summary>
    /// Throws an <see cref="ArgumentOutOfRangeException" /> if <paramref name="value" /> is not an ASCII uppercase
    /// alphanumeric character, that is, a character in the range <c>'0'</c> (U+0030) to <c>'9'</c> (U+0039) or
    /// <c>'A'</c> (U+0041) to <c>'Z'</c> (U+005A) inclusive.
    /// </summary>
    /// <param name="value">The character to validate.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="value" /> is outside the inclusive ranges <c>'0'</c> to <c>'9'</c> and
    /// <c>'A'</c> to <c>'Z'</c>.
    /// </exception>
    /// <remarks>
    /// Useful for validating inputs to algorithms that operate on uppercase alphanumeric identifiers, such as
    /// ISO 7064 MOD 97-10 (IBAN / LEI), ISIN, SEDOL, and CUSIP. Lowercase letters are <b>not</b> accepted — the
    /// caller is expected to normalize before validation.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfNotAsciiAlphanumericUppercase(char value)
    {
        if ((uint)(value - '0') > 9u && (uint)(value - 'A') > 25u)
            throw new ArgumentOutOfRangeException(
                nameof(value),
                value,
                string.Format(CultureInfo.CurrentCulture, ResourceStrings.Arg_OutOfRange_NotAsciiAlphanumericUppercase, value, (int)value));
    }

    /// <summary>
    /// Throws an <see cref="ArgumentOutOfRangeException" /> if <paramref name="value" /> is not an ASCII
    /// hexadecimal digit character — that is, a character in the ranges <c>'0'</c>–<c>'9'</c>, <c>'A'</c>–<c>'F'</c>,
    /// or <c>'a'</c>–<c>'f'</c> inclusive.
    /// </summary>
    /// <param name="value">The character to validate.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="value" /> is not a decimal digit or a letter in <c>'A'</c>–<c>'F'</c> /
    /// <c>'a'</c>–<c>'f'</c>.
    /// </exception>
    /// <remarks>Both upper- and lowercase hex letters are accepted.</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfNotAsciiHexDigit(char value)
    {
        if ((uint)(value - '0') > 9u &&
            (uint)(value - 'A') > 5u &&
            (uint)(value - 'a') > 5u)
            throw new ArgumentOutOfRangeException(
                nameof(value),
                value,
                string.Format(CultureInfo.CurrentCulture, ResourceStrings.Arg_OutOfRange_NotAsciiHexDigit, value, (int)value));
    }
}

#endif
