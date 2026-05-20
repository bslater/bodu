// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensions.ToDotCase.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;

namespace Bodu.Extensions;

public static partial class StringExtensions
{
    /// <summary>
    /// Converts <paramref name="value" /> to <c>dot.case</c>: lower-cased words joined by periods.
    /// </summary>
    /// <param name="value">The string to convert. Must not be <see langword="null" />.</param>
    /// <returns>The <c>dot.case</c> form of <paramref name="value" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// Word boundaries follow <see cref="EnumerateWords(string, WordCasingOptions)" /> using
    /// <see cref="WordCasingOptions.Default" />. Every word, including acronyms, is lower-cased.
    /// </remarks>
    public static string ToDotCase(this string value) =>
        ToDotCase(value, WordCasingOptions.Default);

    /// <summary>
    /// Converts <paramref name="value" /> to <c>dot.case</c> under the supplied <paramref name="options" />.
    /// </summary>
    /// <param name="value">The string to convert. Must not be <see langword="null" />.</param>
    /// <param name="options">The acronym, mixed-case, and culture configuration. Must not be <see langword="null" />.</param>
    /// <returns>The <c>dot.case</c> form of <paramref name="value" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value" /> or <paramref name="options" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>Every word, including acronyms, is lower-cased before joining.</remarks>
    public static string ToDotCase(this string value, WordCasingOptions options) =>
        JoinLowerWords(value, options, '.');

    /// <summary>
    /// Tokenises <paramref name="value" />, lower-cases every word using the culture from
    /// <paramref name="options" />, and joins the words with <paramref name="separator" />.
    /// </summary>
    /// <param name="value">The string to tokenise and join.</param>
    /// <param name="options">The acronym, mixed-case, and culture configuration.</param>
    /// <param name="separator">The character placed between adjacent words.</param>
    /// <returns>The lower-cased, separator-joined form of <paramref name="value" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value" /> or <paramref name="options" /> is <see langword="null" />.
    /// </exception>
    private static string JoinLowerWords(string value, WordCasingOptions options, char separator)
    {
        ThrowHelper.ThrowIfNull(value);
        ThrowHelper.ThrowIfNull(options);

        List<string> words = EnumerateWords(value, options);
        if (words.Count == 0) return string.Empty;

        CultureInfo culture = options.Culture;
        for (int i = 0; i < words.Count; i++) words[i] = words[i].ToLower(culture);
        return string.Join(separator, words);
    }
}
