// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensions.WordCasing.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Text;

namespace Bodu.Extensions;

public static partial class StringExtensions
{
    /// <summary>
    /// Capitalises a single word: the first letter is upper-cased, the first letter after any apostrophe is
    /// upper-cased, and every other letter is lower-cased.
    /// </summary>
    /// <param name="word">The word to capitalise. Must not be empty.</param>
    /// <param name="culture">The culture whose casing rules are applied.</param>
    /// <returns>The capitalised word.</returns>
    /// <remarks>
    /// Capitalising after an apostrophe handles personal names such as <c>o'connor</c> becoming <c>O'Connor</c>.
    /// </remarks>
    private static string CapitalizeWord(string word, CultureInfo culture)
    {
        if (word.Length == 0) return word;

        StringBuilder builder = new(word.Length);
        var capitaliseNext = true;
        for (var i = 0; i < word.Length; i++)
        {
            var c = word[i];
            if (char.IsLetter(c))
            {
                builder.Append(capitaliseNext ? ToUpper(c, culture) : ToLower(c, culture));
                capitaliseNext = false;
            }
            else
            {
                builder.Append(c);
                if (c == '\'') capitaliseNext = true;
            }
        }

        return builder.ToString();
    }

    /// <summary>
    /// Determines whether <paramref name="word" /> is a mixed-case word that must survive casing changes verbatim —
    /// that is, it carries an upper-case letter beyond the first position and at least one lower-case letter.
    /// </summary>
    /// <param name="word">The word to inspect.</param>
    /// <returns>
    /// <see langword="true" /> when the word is a preserved mixed-case word; otherwise <see langword="false" />.
    /// </returns>
    private static bool IsPreservedMixedCaseWord(string word)
    {
        if (word.Length < 2) return false;

        var hasInteriorUpper = false;
        var hasLower = false;
        for (var i = 0; i < word.Length; i++)
        {
            var c = word[i];
            if (char.IsUpper(c) && i > 0) hasInteriorUpper = true;
            else if (char.IsLower(c)) hasLower = true;
        }

        return hasInteriorUpper && hasLower;
    }

    /// <summary>
    /// Upper-cases a single character using the casing rules of <paramref name="culture" />.
    /// </summary>
    /// <param name="c">The character to convert.</param>
    /// <param name="culture">The culture whose casing rules are applied.</param>
    /// <returns>The upper-case form of <paramref name="c" />.</returns>
    private static char ToUpper(char c, CultureInfo culture) =>
        culture.TextInfo.ToUpper(c);

    /// <summary>
    /// Lower-cases a single character using the casing rules of <paramref name="culture" />.
    /// </summary>
    /// <param name="c">The character to convert.</param>
    /// <param name="culture">The culture whose casing rules are applied.</param>
    /// <returns>The lower-case form of <paramref name="c" />.</returns>
    private static char ToLower(char c, CultureInfo culture) =>
        culture.TextInfo.ToLower(c);
}
