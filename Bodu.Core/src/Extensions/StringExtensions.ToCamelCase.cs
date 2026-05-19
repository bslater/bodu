// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensions.ToCamelCase.cs" company="PlaceholderCompany">
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
    /// Converts <paramref name="value" /> to <c>camelCase</c>: the first word is lower-cased and each
    /// subsequent word has its first character upper-cased, with separators removed.
    /// </summary>
    /// <param name="value">The string to convert. Must not be <see langword="null" />.</param>
    /// <returns>The <c>camelCase</c> form of <paramref name="value" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// Word boundaries follow <see cref="EnumerateWords(string)" />. Casing changes use
    /// <see cref="CultureInfo.InvariantCulture" /> for deterministic output across locales.
    /// </remarks>
    public static string ToCamelCase(this string value)
    {
        ThrowHelper.ThrowIfNull(value);

        List<string> words = EnumerateWords(value);
        if (words.Count == 0) return string.Empty;

        StringBuilder builder = new(value.Length);
        builder.Append(words[0].ToLowerInvariant());
        for (int i = 1; i < words.Count; i++)
        {
            builder.Append(char.ToUpperInvariant(words[i][0]));
            if (words[i].Length > 1) builder.Append(words[i].AsSpan(1).ToString().ToLowerInvariant());
        }
        return builder.ToString();
    }
}
