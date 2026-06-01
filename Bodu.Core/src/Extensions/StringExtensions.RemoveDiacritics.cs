// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensions.RemoveDiacritics.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Text;

namespace Bodu.Extensions;

public static partial class StringExtensions
{
    /// <summary>
    /// Returns <paramref name="value" /> with combining diacritic marks removed, normalising accented Latin characters
    /// to their unaccented base form.
    /// </summary>
    /// <param name="value">The string to strip. Must not be <see langword="null" />.</param>
    /// <returns>The string with non-spacing combining marks stripped after Unicode FormD normalisation.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The input is decomposed via <see cref="System.Text.NormalizationForm.FormD" />, which separates precomposed
    /// characters such as <c>'é'</c> into a base character followed by a combining mark. Each resulting character whose
    /// Unicode category is <see cref="UnicodeCategory.NonSpacingMark" /> is then dropped, leaving the base characters
    /// in place. The result is recomposed via <see cref="System.Text.NormalizationForm.FormC" /> for consistent output.
    /// </para>
    /// <para>
    /// This is the canonical pattern for accent-insensitive search keys and is intentionally limited to diacritic
    /// stripping — it does not transliterate non-Latin scripts (e.g. Cyrillic, CJK) and does not case-fold. For full
    /// search normalisation combine this with <c>ToLowerInvariant</c> and <see cref="CollapseWhitespace(string)" />.
    /// </para>
    /// </remarks>
    public static string RemoveDiacritics(this string value)
    {
        ThrowHelper.ThrowIfNull(value);

        if (value.Length == 0) return value;

        var decomposed = value.Normalize(NormalizationForm.FormD);
        StringBuilder builder = new(decomposed.Length);
        foreach (var c in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(c);
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }
}
