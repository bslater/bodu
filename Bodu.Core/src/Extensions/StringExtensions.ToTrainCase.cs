// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensions.ToTrainCase.cs" company="PlaceholderCompany">
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
    /// Converts <paramref name="value" /> to <c>Train-Case</c>: each word has its first character upper-cased
    /// and the rest lower-cased, joined by hyphens.
    /// </summary>
    /// <param name="value">The string to convert. Must not be <see langword="null" />.</param>
    /// <returns>The <c>Train-Case</c> form of <paramref name="value" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// Word boundaries follow <see cref="EnumerateWords(string)" />. Casing changes use
    /// <see cref="CultureInfo.InvariantCulture" /> for deterministic output across locales.
    /// </remarks>
    public static string ToTrainCase(this string value)
    {
        ThrowHelper.ThrowIfNull(value);

        List<string> words = EnumerateWords(value);
        if (words.Count == 0) return string.Empty;

        StringBuilder builder = new(value.Length);
        for (int i = 0; i < words.Count; i++)
        {
            if (i > 0) builder.Append('-');
            builder.Append(char.ToUpperInvariant(words[i][0]));
            if (words[i].Length > 1) builder.Append(words[i].AsSpan(1).ToString().ToLowerInvariant());
        }
        return builder.ToString();
    }
}
