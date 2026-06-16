// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensions.IsValidIdentifier.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Extensions;

public static partial class StringExtensions
{
    /// <summary>
    /// Returns a value indicating whether <paramref name="value" /> is a syntactically valid C# identifier (a letter or
    /// underscore followed by zero or more letters, digits, underscores, or connector punctuation).
    /// </summary>
    /// <param name="value">The string to test.</param>
    /// <returns>
    /// <see langword="true" /> when <paramref name="value" /> is non-empty and every character is a permitted
    /// identifier character; otherwise <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The rules follow the C# 5 ECMA-334 identifier grammar at a character level — the first character must be a
    /// letter (any <see cref="UnicodeCategory" /> in the letter family) or an underscore, and subsequent characters
    /// must additionally permit decimal digits, connector punctuation, and combining or formatting marks. Reserved
    /// keywords (<c>if</c>, <c>class</c>, etc.) are not rejected because the keyword set changes with the C# language
    /// version; callers needing keyword validation should compose this method with a custom keyword check.
    /// </para>
    /// <para>
    /// The empty string returns <see langword="false" /> because an identifier must contain at least one character.
    /// </para>
    /// </remarks>
    public static bool IsValidIdentifier(this string value)
    {
        ThrowHelper.ThrowIfNull(value);

        if (value.Length == 0) return false;
        if (!IsIdentifierStart(value[0])) return false;

        for (int i = 1; i < value.Length; i++)
        {
            if (!IsIdentifierPart(value[i])) return false;
        }

        return true;
    }

    /// <summary>
    /// Returns <see langword="true" /> when <paramref name="c" /> is permitted as the first character of an identifier
    /// (letter or underscore).
    /// </summary>
    /// <param name="c">The candidate character.</param>
    /// <returns>Whether the character may begin an identifier.</returns>
    internal static bool IsIdentifierStart(char c) =>
        c == '_' || char.IsLetter(c);

    /// <summary>
    /// Returns <see langword="true" /> when <paramref name="c" /> is permitted as a non-leading character of an
    /// identifier (letter, decimal digit, connector punctuation, combining mark, or formatting mark).
    /// </summary>
    /// <param name="c">The candidate character.</param>
    /// <returns>Whether the character may appear in an identifier after the first position.</returns>
    internal static bool IsIdentifierPart(char c)
    {
        if (c == '_') return true;
        if (char.IsLetterOrDigit(c)) return true;
        UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(c);
        return category is UnicodeCategory.ConnectorPunctuation
            or UnicodeCategory.NonSpacingMark
            or UnicodeCategory.SpacingCombiningMark
            or UnicodeCategory.Format;
    }
}
