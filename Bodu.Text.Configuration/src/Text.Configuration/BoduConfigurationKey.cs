// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoduConfigurationKey.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Immutable;
using System.Diagnostics;

namespace Bodu.Text.Configuration;

/// <summary>
/// Represents a configuration key in both its raw, file-level form and its colon-delimited logical form used by the
/// resolved view and by <c>Microsoft.Extensions.Configuration</c>.
/// </summary>
/// <remarks>
/// <para>
/// The struct stores the input <see cref="RawKey" /> verbatim. <see cref="ConfigurationKey" /> is the canonical
/// colon-joined form derived from <see cref="Segments" />, applying the mapping policy from
/// <see cref="BoduConfigurationKeyOptions" />.
/// </para>
/// <para>
/// Equality compares the segment sequence under the configured comparer; the raw form is informational only.
/// </para>
/// </remarks>
[DebuggerDisplay("{RawKey,nq} -> {ConfigurationKey,nq}")]
public readonly partial struct BoduConfigurationKey : IEquatable<BoduConfigurationKey>
{
    private readonly ImmutableArray<string> _segments;
    private readonly string? _rawKey;
    private readonly string? _configurationKey;
    private readonly bool _caseSensitive;

    /// <summary>
    /// Initializes a new <see cref="BoduConfigurationKey" /> from a raw key string using the supplied options.
    /// </summary>
    /// <param name="rawKey">The raw key as authored in the configuration source.</param>
    /// <param name="options">The key options to apply, or <see langword="null" /> for the defaults.</param>
    /// <exception cref="ArgumentException">
    /// <paramref name="rawKey" /> is <see langword="null" />, empty, or contains only whitespace; or a segment was
    /// empty when empty segments are not permitted.
    /// </exception>
    public BoduConfigurationKey(string rawKey, BoduConfigurationKeyOptions? options = null)
    {
        ThrowHelper.ThrowIfNullOrWhiteSpace(rawKey);
        RejectControlCharacters(rawKey);

        BoduConfigurationKeyOptions effective = options ?? BoduConfigurationKeyOptions.Default;
        ImmutableArray<string> segments = SplitSegments(rawKey, effective);

        this._rawKey = rawKey;
        this._segments = segments;
        this._configurationKey = ComposeConfigurationKey(segments, effective);
        this._caseSensitive = effective.CaseSensitive;
    }

    private BoduConfigurationKey(string rawKey, ImmutableArray<string> segments, string configurationKey, bool caseSensitive)
    {
        this._rawKey = rawKey;
        this._segments = segments;
        this._configurationKey = configurationKey;
        this._caseSensitive = caseSensitive;
    }

    /// <summary>
    /// Gets the raw key string exactly as it appeared in the source document.
    /// </summary>
    /// <returns>The original key text, or the empty string for a default instance.</returns>
    public string RawKey => this._rawKey ?? string.Empty;

    /// <summary>
    /// Gets the canonical colon-delimited logical key derived from <see cref="Segments" />.
    /// </summary>
    /// <returns>The configuration key in colon-delimited form, or the empty string for a default instance.</returns>
    public string ConfigurationKey => this._configurationKey ?? string.Empty;

    /// <summary>
    /// Gets the segments produced by splitting <see cref="RawKey" /> on the configured separators.
    /// </summary>
    /// <returns>An immutable array of segment strings.</returns>
    public ImmutableArray<string> Segments => this._segments.IsDefault ? ImmutableArray<string>.Empty : this._segments;

    /// <summary>
    /// Gets a value indicating whether equality and hashing for this key are case-sensitive.
    /// </summary>
    /// <returns>
    /// <see langword="true" /> when ordinal case-sensitive comparison is used; otherwise, <see langword="false" /> for
    /// ordinal-ignore-case.
    /// </returns>
    public bool CaseSensitive => this._caseSensitive;

    /// <summary>
    /// Determines whether this key has the same segment sequence as <paramref name="other" /> under the configured
    /// comparer.
    /// </summary>
    /// <param name="other">The other key to compare with.</param>
    /// <returns>
    /// <see langword="true" /> when the segment sequences are equal under the chosen comparer; otherwise,
    /// <see langword="false" />.
    /// </returns>
    public bool Equals(BoduConfigurationKey other)
    {
        ImmutableArray<string> a = this.Segments;
        ImmutableArray<string> b = other.Segments;

        if (a.Length != b.Length)
            return false;

        StringComparer comparer = this._caseSensitive
            ? StringComparer.Ordinal
            : StringComparer.OrdinalIgnoreCase;

        for (int i = 0; i < a.Length; i++)
        {
            if (!comparer.Equals(a[i], b[i]))
                return false;
        }

        return true;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) =>
        obj is BoduConfigurationKey other && this.Equals(other);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        ImmutableArray<string> segments = this.Segments;
        StringComparer comparer = this._caseSensitive
            ? StringComparer.Ordinal
            : StringComparer.OrdinalIgnoreCase;

        HashCode hash = default;
        for (int i = 0; i < segments.Length; i++)
            hash.Add(segments[i], comparer);
        return hash.ToHashCode();
    }

    /// <inheritdoc />
    public override string ToString() => this.ConfigurationKey;

    /// <summary>
    /// Determines whether two keys are equal.
    /// </summary>
    /// <param name="left">The first key to compare.</param>
    /// <param name="right">The second key to compare.</param>
    /// <returns><see langword="true" /> when the keys are equal; otherwise, <see langword="false" />.</returns>
    public static bool operator ==(BoduConfigurationKey left, BoduConfigurationKey right) =>
        left.Equals(right);

    /// <summary>
    /// Determines whether two keys are not equal.
    /// </summary>
    /// <param name="left">The first key to compare.</param>
    /// <param name="right">The second key to compare.</param>
    /// <returns><see langword="true" /> when the keys differ; otherwise, <see langword="false" />.</returns>
    public static bool operator !=(BoduConfigurationKey left, BoduConfigurationKey right) =>
        !left.Equals(right);

    private static ImmutableArray<string> SplitSegments(string rawKey, BoduConfigurationKeyOptions options)
    {
        IReadOnlyList<char> separators = options.SegmentSeparators;
        ImmutableArray<string>.Builder builder = ImmutableArray.CreateBuilder<string>();

        int start = 0;
        for (int i = 0; i < rawKey.Length; i++)
        {
            char c = rawKey[i];
            if (IsSeparator(c, separators))
            {
                AddSegment(builder, rawKey.AsSpan(start, i - start), options);
                start = i + 1;
            }
        }

        AddSegment(builder, rawKey.AsSpan(start, rawKey.Length - start), options);
        return builder.ToImmutable();
    }

    private static void AddSegment(ImmutableArray<string>.Builder builder, ReadOnlySpan<char> segment, BoduConfigurationKeyOptions options)
    {
        if (segment.IsEmpty)
        {
            if (!options.AllowEmptySegments)
                throw new ArgumentException(ConfigurationResourceStrings.Arg_Invalid_ConfigKeyEmptySegment, nameof(segment));

            builder.Add(string.Empty);
            return;
        }

        builder.Add(segment.ToString());
    }

    private static void RejectControlCharacters(string rawKey)
    {
        for (int i = 0; i < rawKey.Length; i++)
        {
            char c = rawKey[i];
            if (char.IsControl(c))
                throw new ArgumentException(string.Format(System.Globalization.CultureInfo.InvariantCulture, ConfigurationResourceStrings.Arg_Invalid_ConfigKeyControlChar, i), nameof(rawKey));
        }
    }

    private static bool IsSeparator(char c, IReadOnlyList<char> separators)
    {
        for (int i = 0; i < separators.Count; i++)
        {
            if (separators[i] == c)
                return true;
        }

        return false;
    }

    private static string ComposeConfigurationKey(ImmutableArray<string> segments, BoduConfigurationKeyOptions options) =>
        options.Mapping switch
        {
            BoduConfigurationKeyMapping.Identity => string.Join(GetFirstSeparator(options), segments),
            BoduConfigurationKeyMapping.Colon => string.Join(':', segments),
            BoduConfigurationKeyMapping.DotToColon => string.Join(':', segments),
            _ => string.Join(':', segments),
        };

    private static char GetFirstSeparator(BoduConfigurationKeyOptions options) =>
        options.SegmentSeparators.Count > 0 ? options.SegmentSeparators[0] : '.';
}
