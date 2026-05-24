// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensions.ToConstantCase.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Extensions;

public static partial class StringExtensions
{
    /// <summary>
    /// Converts <paramref name="value" /> to <c>CONSTANT_CASE</c>: upper-cased words joined by underscores.
    /// </summary>
    /// <param name="value">The string to convert. Must not be <see langword="null" />.</param>
    /// <returns>The <c>CONSTANT_CASE</c> form of <paramref name="value" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// Word boundaries follow <see cref="EnumerateWords(string, WordCasingOptions)" /> using
    /// <see cref="WordCasingOptions.Default" />. Every word is upper-cased.
    /// </remarks>
    public static string ToConstantCase(this string value) =>
        ToConstantCase(value, WordCasingOptions.Default);

    /// <summary>
    /// Converts <paramref name="value" /> to <c>CONSTANT_CASE</c> under the supplied <paramref name="options" />.
    /// </summary>
    /// <param name="value">The string to convert. Must not be <see langword="null" />.</param>
    /// <param name="options">
    /// The acronym, mixed-case, and culture configuration. Must not be <see langword="null" />.
    /// </param>
    /// <returns>The <c>CONSTANT_CASE</c> form of <paramref name="value" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value" /> or <paramref name="options" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// Every word, including acronyms, is upper-cased before joining.
    /// </remarks>
    public static string ToConstantCase(this string value, WordCasingOptions options)
    {
        ThrowHelper.ThrowIfNull(value);
        ThrowHelper.ThrowIfNull(options);

        List<string> words = EnumerateWords(value, options);
        if (words.Count == 0) return string.Empty;

        CultureInfo culture = options.Culture;
        for (var i = 0; i < words.Count; i++) words[i] = words[i].ToUpper(culture);
        return string.Join('_', words);
    }
}
