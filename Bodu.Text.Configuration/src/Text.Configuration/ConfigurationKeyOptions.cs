// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigurationKeyOptions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Configuration;

/// <summary>
/// Controls how raw configuration keys are split into segments and mapped to the logical keys used by the resolved view
/// and the Microsoft.Extensions.Configuration bridge.
/// </summary>
/// <remarks>
/// <para>
/// Two questions need consistent answers for a configuration host: how are the dotted, colon-delimited, or mixed key
/// forms in a source document split into segments, and under which comparer are the resulting keys looked up.
/// <see cref="ConfigurationKeyOptions" /> answers both - <see cref="SegmentSeparators" /> drives splitting,
/// <see cref="Mapping" /> drives the canonical join, or keeps a key whole under
/// <see cref="ConfigurationKeyMapping.Identity" />, and <see cref="CaseSensitive" /> drives the comparer exposed via
/// <see cref="KeyComparer" /> and used for equality on every <see cref="ConfigurationKey" /> it produces.
/// </para>
/// <para>
/// The same instance is consumed by <see cref="ConfigurationParseOptions.KeyOptions" /> and
/// <see cref="ConfigurationResolveOptions.KeyOptions" />; sharing one configured value across both keeps the parsed
/// model and the resolved view's lookups consistent. The default <see cref="Default" /> mirrors
/// <c>Microsoft.Extensions.Configuration</c> - case-insensitive ordinal comparison, dot-to-colon mapping, and
/// <c>{ '.', ':' }</c> as recognised separators.
/// </para>
/// <para>
/// Instances are immutable once constructed; <c>init</c>-only setters allow object-initializer syntax for callers that
/// need to deviate from the defaults. Reuse a single configured instance across calls when consistency matters.
/// </para>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// // Dotted keys normalize to colon-separated configuration paths by default;
/// // Identity keeps authored keys verbatim.
/// var options = new ConfigurationResolveOptions
/// {
///     KeyOptions = new ConfigurationKeyOptions { Mapping = ConfigurationKeyMapping.Identity },
/// };
///]]>
/// </code>
/// </example>
/// </remarks>
public sealed class ConfigurationKeyOptions
{
    /// <summary>The separator characters recognised by default when splitting a raw key into segments.</summary>
    private static readonly char[] s_defaultSeparators = ['.', ':'];

    /// <summary>
    /// Gets the default key options: <see cref="ConfigurationKeyMapping.DotToColon" /> mapping, case-insensitive
    /// comparison, and <c>.</c> / <c>:</c> separators recognised by parser input.
    /// </summary>
    /// <value>A cached default options instance.</value>
    public static ConfigurationKeyOptions Default { get; } = new ConfigurationKeyOptions();

    /// <summary>
    /// Gets the key options of the EditorConfig-compatible profile: <see cref="ConfigurationKeyMapping.Identity" />
    /// mapping, so a key is kept as written, its dots and colons included, and keys lowercased as EditorConfig
    /// requires, with the default separators and case-insensitive comparison.
    /// </summary>
    /// <value>A cached options instance shared by the profile's parse and resolve presets.</value>
    internal static ConfigurationKeyOptions EditorConfigCompatible { get; } =
        new ConfigurationKeyOptions { Mapping = ConfigurationKeyMapping.Identity, LowercaseKeys = true };

    /// <summary>
    /// Gets the segment-separator characters recognised in a raw key when splitting into segments.
    /// <see cref="ConfigurationKeyMapping.Identity" /> does not split a key, so it ignores them.
    /// </summary>
    /// <value>A non-empty set of separator characters. The default is <c>{ '.', ':' }</c>.</value>
    public IReadOnlyList<char> SegmentSeparators { get; init; } = s_defaultSeparators;

    /// <summary>
    /// Gets the mapping that converts the raw key to its configuration key: colon-delimited, or kept as written under
    /// <see cref="ConfigurationKeyMapping.Identity" />.
    /// </summary>
    /// <value>The selected <see cref="ConfigurationKeyMapping" /> value.</value>
    public ConfigurationKeyMapping Mapping { get; init; } = ConfigurationKeyMapping.DotToColon;

    /// <summary>
    /// Gets a value indicating whether logical key comparison is case-sensitive.
    /// </summary>
    /// <value>
    /// <see langword="true" /> when keys are compared with ordinal case sensitivity; otherwise,
    /// <see langword="false" />. The default is <see langword="false" />, mirroring
    /// <c>Microsoft.Extensions.Configuration</c>.
    /// </value>
    public bool CaseSensitive { get; init; }

    /// <summary>
    /// Gets a value indicating whether the logical key is lowercased with the casing rules of the invariant culture, as
    /// EditorConfig lowercases every key after parsing. The default keeps the case each key was written in.
    /// </summary>
    /// <value>
    /// <see langword="true" /> when <see cref="ConfigurationKey.Segments" /> and <see cref="ConfigurationKey.Path" />
    /// are lowercased; otherwise, <see langword="false" />. <see cref="ConfigurationKey.RawKey" /> keeps the case it
    /// was written in either way. The <see cref="ConfigurationProfile.EditorConfigCompatible" /> presets set it.
    /// </value>
    public bool LowercaseKeys { get; init; }

    /// <summary>
    /// Gets a value indicating whether the parser permits empty segments in a raw key (for example <c>a..b</c>). The
    /// default rejects empty segments. <see cref="ConfigurationKeyMapping.Identity" /> does not split a key, so it has
    /// no segment to reject.
    /// </summary>
    /// <value><see langword="true" /> when empty segments are allowed; otherwise, <see langword="false" />.</value>
    public bool AllowEmptySegments { get; init; }

    /// <summary>
    /// Gets the <see cref="StringComparer" /> implied by <see cref="CaseSensitive" />.
    /// </summary>
    /// <value>
    /// <see cref="StringComparer.Ordinal" /> when case-sensitive; otherwise,
    /// <see cref="StringComparer.OrdinalIgnoreCase" />.
    /// </value>
    public StringComparer KeyComparer =>
        CaseSensitive ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase;
}
