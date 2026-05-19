// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensions.ToSnakeCase.cs" company="PlaceholderCompany">
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
    /// Converts <paramref name="value" /> to <c>snake_case</c>: lower-cased words joined by underscores.
    /// </summary>
    /// <param name="value">The string to convert. Must not be <see langword="null" />.</param>
    /// <returns>The <c>snake_case</c> form of <paramref name="value" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// Word boundaries follow <see cref="EnumerateWords(string)" />. Casing changes use
    /// <see cref="CultureInfo.InvariantCulture" /> for deterministic output across locales.
    /// </remarks>
    public static string ToSnakeCase(this string value)
    {
        ThrowHelper.ThrowIfNull(value);

        List<string> words = EnumerateWords(value);
        if (words.Count == 0) return string.Empty;

        for (int i = 0; i < words.Count; i++) words[i] = words[i].ToLowerInvariant();
        return string.Join('_', words);
    }
}
