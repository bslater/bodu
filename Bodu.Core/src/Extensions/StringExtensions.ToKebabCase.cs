// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensions.ToKebabCase.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public static partial class StringExtensions
{
    /// <summary>
    /// Converts <paramref name="value" /> to <c>kebab-case</c>: lower-cased words joined by hyphens.
    /// </summary>
    /// <param name="value">The string to convert. Must not be <see langword="null" />.</param>
    /// <returns>The <c>kebab-case</c> form of <paramref name="value" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// Word boundaries follow <see cref="EnumerateWords(string, WordCasingOptions)" /> using
    /// <see cref="WordCasingOptions.Default" />. Every word, including acronyms, is lower-cased.
    /// </remarks>
    public static string ToKebabCase(this string value) =>
        ToKebabCase(value, WordCasingOptions.Default);

    /// <summary>
    /// Converts <paramref name="value" /> to <c>kebab-case</c> under the supplied <paramref name="options" />.
    /// </summary>
    /// <param name="value">The string to convert. Must not be <see langword="null" />.</param>
    /// <param name="options">
    /// The acronym, mixed-case, and culture configuration. Must not be <see langword="null" />.
    /// </param>
    /// <returns>The <c>kebab-case</c> form of <paramref name="value" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value" /> or <paramref name="options" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// Every word, including acronyms, is lower-cased before joining.
    /// </remarks>
    public static string ToKebabCase(this string value, WordCasingOptions options) =>
        JoinLowerWords(value, options, '-');
}
