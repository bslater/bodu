// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelper.Ascii.CallerExpression.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

#if !NETSTANDARD2_0_OR_GREATER
#pragma warning disable SA1117 // Parameters should be on same line or separate lines
#pragma warning disable IDE0011 // Add braces

using System.Runtime.CompilerServices;

namespace Bodu;

public static partial class ThrowHelper
{
    /// <summary>
    /// Throws an <see cref="ArgumentOutOfRangeException" /> if <paramref name="value" /> is not an ASCII decimal digit
    /// character, that is, a character in the range <c>'0'</c> (U+0030) to <c>'9'</c> (U+0039) inclusive.
    /// </summary>
    /// <param name="value">The character to validate.</param>
    /// <param name="paramName">The name of the parameter. Supplied automatically by the compiler.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="value" /> is outside the inclusive range <c>'0'</c> to <c>'9'</c>.
    /// </exception>
    /// <remarks>
    /// Useful for validating inputs to algorithms that operate on decimal digit strings, such as check-digit
    /// algorithms.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfNotAsciiDecimalDigit(
        char value,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if ((uint)(value - '0') > 9u)
            throw new ArgumentOutOfRangeException(
                paramName,
                value,
                $"Character '{value}' (U+{(int)value:X4}) is not an ASCII decimal digit ('0' to '9').");
    }

    /// <summary>
    /// Throws an <see cref="ArgumentOutOfRangeException" /> if <paramref name="value" /> is not an ASCII uppercase
    /// alphanumeric character, that is, a character in the range <c>'0'</c> (U+0030) to <c>'9'</c> (U+0039) or
    /// <c>'A'</c> (U+0041) to <c>'Z'</c> (U+005A) inclusive.
    /// </summary>
    /// <param name="value">The character to validate.</param>
    /// <param name="paramName">The name of the parameter. Supplied automatically by the compiler.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="value" /> is outside the inclusive ranges <c>'0'</c> to <c>'9'</c> and <c>'A'</c> to
    /// <c>'Z'</c>.
    /// </exception>
    /// <remarks>
    /// Useful for validating inputs to algorithms that operate on uppercase alphanumeric identifiers, such as ISO 7064
    /// MOD 97-10 (IBAN / LEI), ISIN, SEDOL, and CUSIP. Lowercase letters are <b>not</b> accepted — the caller is
    /// expected to normalize before validation.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfNotAsciiAlphanumericUppercase(
        char value,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if ((uint)(value - '0') > 9u && (uint)(value - 'A') > 25u)
            throw new ArgumentOutOfRangeException(
                paramName,
                value,
                $"Character '{value}' (U+{(int)value:X4}) is not an ASCII uppercase alphanumeric character ('0' to '9' or 'A' to 'Z').");
    }

    /// <summary>
    /// Throws an <see cref="ArgumentOutOfRangeException" /> if <paramref name="value" /> is not an ASCII hexadecimal
    /// digit character — that is, a character in the ranges <c>'0'</c>–<c>'9'</c> (U+0030–U+0039), <c>'A'</c>–
    /// <c>'F'</c> (U+0041–U+0046), or <c>'a'</c>–<c>'f'</c> (U+0061–U+0066) inclusive.
    /// </summary>
    /// <param name="value">The character to validate.</param>
    /// <param name="paramName">The name of the parameter. Supplied automatically by the compiler.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="value" /> is not a decimal digit or a letter in <c>'A'</c>–<c>'F'</c> / <c>'a'</c>–
    /// <c>'f'</c>.
    /// </exception>
    /// <remarks>
    /// Both upper- and lowercase hex letters are accepted. Useful for validating hex-encoded cryptographic outputs such
    /// as hash digests, key material, or initialization vectors prior to parsing.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfNotAsciiHexDigit(
        char value,
        [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if ((uint)(value - '0') > 9u &&
            (uint)(value - 'A') > 5u &&
            (uint)(value - 'a') > 5u)
            throw new ArgumentOutOfRangeException(
                paramName,
                value,
                $"Character '{value}' (U+{(int)value:X4}) is not a hex digit ('0'–'9', 'A'–'F', or 'a'–'f').");
    }
}

#endif
