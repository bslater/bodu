// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoduConfigurationKeyOptions.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Configuration;

/// <summary>
/// Controls how raw configuration keys are split into segments and mapped to the colon-delimited logical key shape used
/// by the resolved view and the Microsoft.Extensions.Configuration bridge.
/// </summary>
/// <remarks>
/// <para>
/// Two questions need consistent answers for a configuration host: how are the dotted, colon-delimited, or mixed key
/// forms in a source document split into segments, and under which comparer are the resulting keys looked up.
/// <see cref="BoduConfigurationKeyOptions" /> answers both — <see cref="SegmentSeparators" /> drives splitting,
/// <see cref="Mapping" /> drives the canonical join, and <see cref="CaseSensitive" /> drives the comparer exposed via
/// <see cref="KeyComparer" /> and used for equality on every <see cref="BoduConfigurationKey" /> it produces.
/// </para>
/// <para>
/// The same instance is consumed by <see cref="BoduConfigurationParseOptions.KeyOptions" /> and
/// <see cref="BoduConfigurationResolveOptions.KeyOptions" />; sharing one configured value across both keeps the parsed
/// model and the resolved view's lookups consistent. The default <see cref="Default" /> mirrors
/// <c>Microsoft.Extensions.Configuration</c> — case-insensitive ordinal comparison, dot-to-colon mapping, and
/// <c>{ '.', ':' }</c> as recognised separators.
/// </para>
/// <para>
/// Instances are immutable once constructed; <c>init</c>-only setters allow object-initializer syntax for callers that
/// need to deviate from the defaults. Reuse a single configured instance across calls when consistency matters.
/// </para>
/// </remarks>
public sealed class BoduConfigurationKeyOptions
{
    private static readonly char[] s_defaultSeparators = ['.', ':'];

    /// <summary>
    /// Gets the default key options: <see cref="BoduConfigurationKeyMapping.DotToColon" /> mapping, case-insensitive
    /// comparison, and <c>.</c> / <c>:</c> separators recognised by parser input.
    /// </summary>
    /// <returns>A cached default options instance.</returns>
    public static BoduConfigurationKeyOptions Default { get; } = new BoduConfigurationKeyOptions();

    /// <summary>
    /// Gets the segment-separator characters recognised in a raw key when splitting into segments.
    /// </summary>
    /// <returns>A non-empty set of separator characters. The default is <c>{ '.', ':' }</c>.</returns>
    public IReadOnlyList<char> SegmentSeparators { get; init; } = s_defaultSeparators;

    /// <summary>
    /// Gets the mapping that converts the raw key to a colon-delimited configuration key.
    /// </summary>
    /// <returns>The selected <see cref="BoduConfigurationKeyMapping" /> value.</returns>
    public BoduConfigurationKeyMapping Mapping { get; init; } = BoduConfigurationKeyMapping.DotToColon;

    /// <summary>
    /// Gets a value indicating whether logical key comparison is case-sensitive.
    /// </summary>
    /// <returns>
    /// <see langword="true" /> when keys are compared with ordinal case sensitivity; otherwise,
    /// <see langword="false" />. The default is <see langword="false" />, mirroring
    /// <c>Microsoft.Extensions.Configuration</c>.
    /// </returns>
    public bool CaseSensitive { get; init; }

    /// <summary>
    /// Gets a value indicating whether the parser permits empty segments in a raw key (for example <c>a..b</c>). The
    /// default rejects empty segments.
    /// </summary>
    /// <returns><see langword="true" /> when empty segments are allowed; otherwise, <see langword="false" />.</returns>
    public bool AllowEmptySegments { get; init; }

    /// <summary>
    /// Gets the <see cref="StringComparer" /> implied by <see cref="CaseSensitive" />.
    /// </summary>
    /// <returns>
    /// <see cref="StringComparer.Ordinal" /> when case-sensitive; otherwise,
    /// <see cref="StringComparer.OrdinalIgnoreCase" />.
    /// </returns>
    public StringComparer KeyComparer =>
        CaseSensitive ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;
}
