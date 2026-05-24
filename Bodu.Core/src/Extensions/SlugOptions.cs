// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SlugOptions.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

/// <summary>
/// Supplies separator, casing, diacritic, and length configuration for
/// <see cref="StringExtensions.ToSlug(string, SlugOptions)" />.
/// </summary>
/// <remarks>
/// Instances are immutable once constructed: every property is <c>init</c>-only. Construct a customised instance with
/// an object initialiser, or use <see cref="Default" /> for the standard configuration.
/// </remarks>
public sealed class SlugOptions
{
    /// <summary>
    /// The shared default instance returned by <see cref="Default" />.
    /// </summary>
    private static readonly SlugOptions s_default = new();

    /// <summary>
    /// Gets a shared <see cref="SlugOptions" /> instance whose every property holds its default value.
    /// </summary>
    /// <returns>The shared default-configured options instance.</returns>
    /// <value>An immutable, reusable options object suitable as the implicit default for slug generation.</value>
    public static SlugOptions Default => s_default;

    /// <summary>
    /// Gets the character inserted between adjacent words in the slug.
    /// </summary>
    /// <returns>The word-separator character.</returns>
    /// <value>Defaults to the hyphen <c>'-'</c>.</value>
    public char Separator { get; init; } = '-';

    /// <summary>
    /// Gets a value indicating whether the slug is lower-cased.
    /// </summary>
    /// <returns>
    /// <see langword="true" /> when the slug is emitted in lower case; otherwise <see langword="false" />.
    /// </returns>
    /// <value>Defaults to <see langword="true" />.</value>
    public bool Lowercase { get; init; } = true;

    /// <summary>
    /// Gets a value indicating whether diacritical marks are stripped and transliterated before slugging.
    /// </summary>
    /// <returns>
    /// <see langword="true" /> when accented characters are normalised to their base form; otherwise
    /// <see langword="false" />.
    /// </returns>
    /// <value>
    /// Defaults to <see langword="true" />. When set, combining marks are removed and the German sharp-s (<c>ß</c>) is
    /// transliterated to <c>ss</c>.
    /// </value>
    public bool NormalizeDiacritics { get; init; } = true;

    /// <summary>
    /// Gets the maximum slug length, or zero for an unbounded slug.
    /// </summary>
    /// <returns>The maximum character count of the produced slug, or <c>0</c> when unbounded.</returns>
    /// <value>
    /// Defaults to <c>0</c> (unlimited). When greater than zero, the slug is truncated at a separator boundary so the
    /// result never exceeds the limit and never ends with a dangling separator.
    /// </value>
    public int MaxLength { get; init; }
}
