// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensions.ToTitleCase.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Bodu.Extensions;

public static partial class StringExtensions
{
    /// <summary>
    /// The connective words lower-cased when <see cref="TitleCaseOptions.LowerCaseSmallWords" /> is set.
    /// </summary>
    private static readonly HashSet<string> s_titleCaseSmallWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "a", "an", "and", "as", "at", "but", "by", "for", "in", "nor",
        "of", "on", "or", "per", "so", "the", "to", "up", "via", "vs", "yet",
    };

    /// <summary>
    /// Returns <paramref name="value" /> with the first character of every word capitalised and the rest
    /// lower-cased, using <see cref="CultureInfo.InvariantCulture" />.
    /// </summary>
    /// <param name="value">The string to convert. Must not be <see langword="null" />.</param>
    /// <returns>The title-case form of <paramref name="value" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value" /> is <see langword="null" />.
    /// </exception>
    public static string ToTitleCase(this string value) =>
        ToTitleCase(value, TitleCaseOptions.None);

    /// <summary>
    /// Returns <paramref name="value" /> in title case under the supplied <paramref name="options" />.
    /// </summary>
    /// <param name="value">The string to convert. Must not be <see langword="null" />.</param>
    /// <param name="options">Flags that adjust acronym preservation and small-word handling.</param>
    /// <returns>The title-case form of <paramref name="value" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value" /> is <see langword="null" />.
    /// </exception>
    public static string ToTitleCase(this string value, TitleCaseOptions options)
    {
        ThrowHelper.ThrowIfNull(value);

        List<string> words = EnumerateWords(value);
        if (words.Count == 0) return string.Empty;

        bool preserveAcronyms = (options & TitleCaseOptions.PreserveAcronyms) == TitleCaseOptions.PreserveAcronyms;
        bool lowerSmall = (options & TitleCaseOptions.LowerCaseSmallWords) == TitleCaseOptions.LowerCaseSmallWords;

        StringBuilder builder = new(value.Length);
        for (int i = 0; i < words.Count; i++)
        {
            if (i > 0) builder.Append(' ');
            string word = words[i];

            if (preserveAcronyms && IsAllUpper(word))
            {
                builder.Append(word);
                continue;
            }

            bool isInterior = i > 0 && i < words.Count - 1;
            if (lowerSmall && isInterior && s_titleCaseSmallWords.Contains(word))
            {
                builder.Append(word.ToLowerInvariant());
                continue;
            }

            builder.Append(char.ToUpperInvariant(word[0]));
            if (word.Length > 1) builder.Append(word.AsSpan(1).ToString().ToLowerInvariant());
        }

        return builder.ToString();
    }

    /// <summary>
    /// Returns <see langword="true" /> when every letter in <paramref name="word" /> is upper case.
    /// </summary>
    /// <param name="word">The word to inspect.</param>
    /// <returns><see langword="true" /> for all-uppercase words; otherwise <see langword="false" />.</returns>
    private static bool IsAllUpper(string word)
    {
        if (word.Length == 0) return false;
        for (int i = 0; i < word.Length; i++)
        {
            char c = word[i];
            if (char.IsLetter(c) && !char.IsUpper(c)) return false;
        }
        return true;
    }
}
