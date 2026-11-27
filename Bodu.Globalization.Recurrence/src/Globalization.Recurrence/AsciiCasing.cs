// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsciiCasing.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Recurrence;

/// <summary>
/// Raises the ASCII letters of the recurrence grammars to upper case, leaving every other character unchanged.
/// </summary>
/// <remarks>
/// The cron, RFC 5545 rule and duration grammars are defined over US-ASCII and match their letters case-insensitively.
/// Invariant uppercasing would also map U+017F LATIN SMALL LETTER LONG S to <c>S</c>, so a character outside US-ASCII
/// could stand for a letter of the grammar. These members raise <c>a</c>-<c>z</c> alone, so such a character stays as
/// it is and is rejected, as any other character outside the grammar is.
/// </remarks>
internal static class AsciiCasing
{
    /// <summary>
    /// Returns an ASCII lower-case letter in upper case, and every other character unchanged.
    /// </summary>
    /// <param name="value">The character to raise.</param>
    /// <returns><paramref name="value" />, with <c>a</c>-<c>z</c> raised to <c>A</c>-<c>Z</c>.</returns>
    internal static char ToUpper(char value) =>
        char.IsAsciiLetterLower(value) ? (char)(value - ('a' - 'A')) : value;

    /// <summary>
    /// Returns <paramref name="value" /> with its ASCII lower-case letters in upper case, and every other character
    /// unchanged.
    /// </summary>
    /// <param name="value">The text to raise.</param>
    /// <returns>
    /// The raised text, or <paramref name="value" /> itself when it holds no ASCII lower-case letter.
    /// </returns>
    internal static string ToUpper(string value)
    {
        if (!value.AsSpan().ContainsAnyInRange('a', 'z'))
            return value;

        return string.Create(value.Length, value, static (destination, source) =>
        {
            for (int i = 0; i < source.Length; i++)
                destination[i] = ToUpper(source[i]);
        });
    }
}
