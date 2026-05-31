// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensions.ToSlug.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

namespace Bodu.Extensions;

public static partial class StringExtensions
{
    /// <summary>
    /// Converts <paramref name="value" /> to a URL-friendly slug: lower-cased words joined by hyphens with diacritics
    /// normalised and punctuation removed.
    /// </summary>
    /// <param name="value">The string to convert. Must not be <see langword="null" />.</param>
    /// <returns>The slug form of <paramref name="value" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// Equivalent to calling <see cref="ToSlug(string, SlugOptions)" /> with <see cref="SlugOptions.Default" />.
    /// </remarks>
    public static string ToSlug(this string value) =>
        ToSlug(value, SlugOptions.Default);

    /// <summary>
    /// Converts <paramref name="value" /> to a URL-friendly slug under the supplied <paramref name="options" />.
    /// </summary>
    /// <param name="value">The string to convert. Must not be <see langword="null" />.</param>
    /// <param name="options">
    /// The separator, casing, diacritic, and length configuration. Must not be <see langword="null" />.
    /// </param>
    /// <returns>The slug form of <paramref name="value" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value" /> or <paramref name="options" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// <para>
    /// When <see cref="SlugOptions.NormalizeDiacritics" /> is set the input is first transliterated: the German sharp-s
    /// (<c>ß</c>) becomes <c>ss</c> and combining diacritic marks are stripped via
    /// <see cref="RemoveDiacritics(string)" />. The result is then tokenised, optionally lower-cased, and joined with
    /// <see cref="SlugOptions.Separator" />.
    /// </para>
    /// <para>
    /// When <see cref="SlugOptions.MaxLength" /> is greater than zero the slug is truncated at a separator boundary so
    /// the result never exceeds the limit and never ends with a dangling separator.
    /// </para>
    /// </remarks>
    public static string ToSlug(this string value, SlugOptions options)
    {
        ThrowHelper.ThrowIfNull(value);
        ThrowHelper.ThrowIfNull(options);

        if (value.Length == 0) return string.Empty;

        var prepared = options.NormalizeDiacritics ? TransliterateForSlug(value) : value;

        List<string> words = EnumerateWords(prepared, WordCasingOptions.Default);
        if (words.Count == 0) return string.Empty;

        if (options.Lowercase)
        {
            for (var i = 0; i < words.Count; i++) words[i] = words[i].ToLowerInvariant();
        }

        var slug = string.Join(options.Separator, words);
        return options.MaxLength > 0 ? TruncateSlug(slug, options.Separator, options.MaxLength) : slug;
    }

    /// <summary>
    /// Transliterates characters that Unicode FormD decomposition cannot fold, then strips the remaining combining
    /// diacritic marks.
    /// </summary>
    /// <param name="value">The string to transliterate.</param>
    /// <returns>The transliterated, diacritic-free string.</returns>
    /// <remarks>
    /// The German sharp-s (<c>ß</c>, U+00DF) has no decomposition mapping, so it is expanded to <c>ss</c> explicitly
    /// before <see cref="RemoveDiacritics(string)" /> removes the remaining combining marks.
    /// </remarks>
    private static string TransliterateForSlug(string value)
    {
        StringBuilder builder = new(value.Length);
        foreach (var c in value)
        {
            switch (c)
            {
                case 'ß':
                    builder.Append("ss");
                    break;
                case 'ẞ':
                    builder.Append("SS");
                    break;
                default:
                    builder.Append(c);
                    break;
            }
        }

        return builder.ToString().RemoveDiacritics();
    }

    /// <summary>
    /// Truncates <paramref name="slug" /> at a separator boundary so the result does not exceed
    /// <paramref name="maxLength" /> characters and does not end with a separator.
    /// </summary>
    /// <param name="slug">The slug to truncate.</param>
    /// <param name="separator">The separator character used between words.</param>
    /// <param name="maxLength">The maximum permitted length.</param>
    /// <returns>The truncated slug.</returns>
    private static string TruncateSlug(string slug, char separator, int maxLength)
    {
        if (slug.Length <= maxLength) return slug;

        var cut = maxLength;
        while (cut > 0 && slug[cut - 1] != separator) cut--;

        // Drop the trailing separator; when no boundary was found, fall back to a hard cut.
        return cut > 0 ? slug[..(cut - 1)] : slug[..maxLength];
    }
}
