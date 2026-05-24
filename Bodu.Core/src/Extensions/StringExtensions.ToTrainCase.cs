// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensions.ToTrainCase.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Text;

namespace Bodu.Extensions;

public static partial class StringExtensions
{
    /// <summary>
    /// Converts <paramref name="value" /> to <c>Train-Case</c>: each word has its first character upper-cased and the
    /// rest lower-cased, joined by hyphens.
    /// </summary>
    /// <param name="value">The string to convert. Must not be <see langword="null" />.</param>
    /// <returns>The <c>Train-Case</c> form of <paramref name="value" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// Word boundaries follow <see cref="EnumerateWords(string, WordCasingOptions)" /> using
    /// <see cref="WordCasingOptions.Default" />. Casing changes use the configured culture.
    /// </remarks>
    public static string ToTrainCase(this string value) =>
        ToTrainCase(value, WordCasingOptions.Default);

    /// <summary>
    /// Converts <paramref name="value" /> to <c>Train-Case</c> under the supplied <paramref name="options" />.
    /// </summary>
    /// <param name="value">The string to convert. Must not be <see langword="null" />.</param>
    /// <param name="options">
    /// The acronym, mixed-case, and culture configuration. Must not be <see langword="null" />.
    /// </param>
    /// <returns>The <c>Train-Case</c> form of <paramref name="value" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value" /> or <paramref name="options" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// Every word is capitalised unless it is a recognised mixed-case word, which is emitted verbatim.
    /// </remarks>
    public static string ToTrainCase(this string value, WordCasingOptions options)
    {
        ThrowHelper.ThrowIfNull(value);
        ThrowHelper.ThrowIfNull(options);

        List<string> words = EnumerateWords(value, options);
        if (words.Count == 0) return string.Empty;

        CultureInfo culture = options.Culture;
        StringBuilder builder = new(value.Length);
        for (var i = 0; i < words.Count; i++)
        {
            if (i > 0) builder.Append('-');
            builder.Append(IsPreservedMixedCaseWord(words[i]) ? words[i] : CapitalizeWord(words[i], culture));
        }

        return builder.ToString();
    }
}
