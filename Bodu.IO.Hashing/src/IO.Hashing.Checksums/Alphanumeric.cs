// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Alphanumeric.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Resources;
using System.Runtime.CompilerServices;

namespace Bodu.IO.Hashing.Checksums;

/// <summary>
/// Provides shared helpers for expanding and validating the ASCII uppercase alphanumeric alphabet used by ISO 7064
/// MOD 97-10, IBAN, LEI, ISIN, SEDOL, and CUSIP.
/// </summary>
/// <remarks>
/// Letter values follow the convention mandated by ISO 7064 and ISO 13616 where <c>'A'</c> expands to 10,
/// <c>'B'</c> to 11, and so on through <c>'Z'</c> to 35. CUSIP additionally defines sentinels for the punctuation
/// characters <c>'*'</c> (36), <c>'@'</c> (37), and <c>'#'</c> (38).
/// </remarks>
internal static class Alphanumeric
{
    /// <summary>
    /// Expands an ASCII uppercase alphanumeric character to its numeric value. <c>'0'</c>–<c>'9'</c> map to 0–9
    /// and <c>'A'</c>–<c>'Z'</c> map to 10–35.
    /// </summary>
    /// <param name="ch">The character to expand.</param>
    /// <returns>The integer value in the range 0 to 35.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="ch" /> is not an ASCII decimal digit or uppercase letter.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int ExpandLetterDigit(char ch)
    {
        if ((uint)(ch - '0') <= 9u) return ch - '0';
        if ((uint)(ch - 'A') <= 25u) return ch - 'A' + 10;
        ThrowHelper.ThrowIfNotAsciiAlphanumericUppercase(ch);
        return -1;
    }

    /// <summary>
    /// Expands an ASCII uppercase alphanumeric character, or one of the CUSIP punctuation sentinels
    /// (<c>'*'</c>=36, <c>'@'</c>=37, <c>'#'</c>=38), to its numeric value.
    /// </summary>
    /// <param name="ch">The character to expand.</param>
    /// <returns>The integer value in the range 0 to 38.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="ch" /> is not a recognized CUSIP character.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int ExpandCusip(char ch)
    {
        if ((uint)(ch - '0') <= 9u) return ch - '0';
        if ((uint)(ch - 'A') <= 25u) return ch - 'A' + 10;
        if (ch == '*') return 36;
        if (ch == '@') return 37;
        if (ch == '#') return 38;

        throw new ArgumentOutOfRangeException(
            nameof(ch),
            ch,
            FormatInvalidCharacterMessage(
                ch,
                "CUSIP",
                "'0'-'9', 'A'-'Z', '*', '@', or '#'"));
    }

    /// <summary>
    /// Validates that every character in <paramref name="value" /> is an ASCII decimal digit or uppercase letter.
    /// </summary>
    /// <param name="value">The character sequence to validate.</param>
    /// <param name="paramName">The parameter name used in any thrown exception.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when any element of <paramref name="value" /> is not a recognized alphanumeric character.
    /// </exception>
    public static void ValidateAlphanumeric(ReadOnlySpan<char> value, string paramName)
    {
        for (int i = 0; i < value.Length; i++)
        {
            char ch = value[i];
            if ((uint)(ch - '0') > 9u && (uint)(ch - 'A') > 25u)
                throw new ArgumentOutOfRangeException(
                    paramName,
                    ch,
                    FormatInvalidCharacterMessage(
                        ch,
                        "ASCII uppercase alphanumeric",
                        "'0' to '9' or 'A' to 'Z'"));
        }
    }

    /// <summary>
    /// Validates that every character in <paramref name="value" /> is a recognized CUSIP character.
    /// </summary>
    /// <param name="value">The character sequence to validate.</param>
    /// <param name="paramName">The parameter name used in any thrown exception.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when any element of <paramref name="value" /> is not a recognized CUSIP character.
    /// </exception>
    public static void ValidateCusip(ReadOnlySpan<char> value, string paramName)
    {
        for (int i = 0; i < value.Length; i++)
        {
            char ch = value[i];
            if ((uint)(ch - '0') <= 9u) continue;
            if ((uint)(ch - 'A') <= 25u) continue;
            if (ch == '*' || ch == '@' || ch == '#') continue;

            throw new ArgumentOutOfRangeException(
                paramName,
                ch,
                FormatInvalidCharacterMessage(
                    ch,
                    "CUSIP",
                    "'0'-'9', 'A'-'Z', '*', '@', or '#'"));
        }
    }

    /// <summary>
    /// Validates that every character in <paramref name="value" /> is a recognized SEDOL character, i.e. an
    /// ASCII decimal digit or an uppercase consonant (vowels <c>'A'</c>, <c>'E'</c>, <c>'I'</c>, <c>'O'</c>, and
    /// <c>'U'</c> are disallowed).
    /// </summary>
    /// <param name="value">The character sequence to validate.</param>
    /// <param name="paramName">The parameter name used in any thrown exception.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when any element of <paramref name="value" /> is not a recognized SEDOL character.
    /// </exception>
    public static void ValidateSedol(ReadOnlySpan<char> value, string paramName)
    {
        for (int i = 0; i < value.Length; i++)
        {
            char ch = value[i];
            if ((uint)(ch - '0') <= 9u) continue;
            if ((uint)(ch - 'A') <= 25u && !IsVowel(ch)) continue;

            throw new ArgumentOutOfRangeException(
                paramName,
                ch,
                FormatInvalidCharacterMessage(
                    ch,
                    "SEDOL",
                    "'0'-'9' or uppercase consonant; vowels A/E/I/O/U are not permitted"));
        }
    }

    /// <summary>
    /// Determines whether the supplied ASCII character is an uppercase Latin vowel.
    /// </summary>
    /// <param name="ch">The character to test.</param>
    /// <returns><see langword="true" /> if <paramref name="ch" /> is one of <c>'A'</c>, <c>'E'</c>, <c>'I'</c>, <c>'O'</c>, or <c>'U'</c>; otherwise, <see langword="false" />.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsVowel(char ch) =>
        ch == 'A' || ch == 'E' || ch == 'I' || ch == 'O' || ch == 'U';

    private static string FormatInvalidCharacterMessage(
        char ch,
        string characterSetName,
        string validCharacterDescription)
    {
        return string.Format(
            HashingResourceStrings.ArgumentOutOfRange_InvalidCharacterForCharacterSet,
            ch,
            (int)ch,
            characterSetName,
            validCharacterDescription);
    }
}
