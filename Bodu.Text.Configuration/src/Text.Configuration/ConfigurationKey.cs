// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigurationKey.cs" company="PlaceholderCompany">
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
/// The struct stores the input <see cref="RawKey" /> verbatim. <see cref="Path" /> is the canonical
/// colon-joined form derived from <see cref="Segments" />, applying the mapping policy from
/// <see cref="ConfigurationKeyOptions" />.
/// </para>
/// <para>
/// Equality compares the segment sequence under the configured comparer; the raw form is informational only.
/// </para>
/// <para>
/// Both dotted (<c>logging.level.default</c>) and colon-delimited (<c>logging:level:default</c>) source forms produce
/// the same canonical <see cref="Path" />, so consumers can mix the two notations in a document without
/// disturbing the resolved view. Whitespace in segments is trimmed; control characters are rejected at construction
/// time.
/// </para>
/// </remarks>
/// <example>
///<![CDATA[
/// // Default mapping — split on '.' and ':', case-insensitive comparison.
/// var k1 = new ConfigurationKey("Logging.Level.Default");
/// var k2 = new ConfigurationKey("logging:level:default");
/// Console.WriteLine(k1 == k2);              // True — same segment sequence under the default comparer
/// Console.WriteLine(k1.Path);               // "Logging:Level:Default"
/// Console.WriteLine(string.Join(",", k1.Segments)); // "Logging,Level,Default"
///
/// // Case-sensitive parsing for hosts that distinguish 'Foo' from 'foo'.
/// var opts = new ConfigurationKeyOptions { CaseSensitive = true };
/// var k3   = new ConfigurationKey("Foo:Bar", opts);
/// var k4   = new ConfigurationKey("foo:bar", opts);
/// Console.WriteLine(k3 == k4);              // False
///]]>
/// </example>
[DebuggerDisplay("{RawKey,nq} -> {Path,nq}")]
public readonly partial struct ConfigurationKey : IEquatable<ConfigurationKey>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationKey" /> struct from a raw key string using the
    /// supplied options.
    /// </summary>
    /// <param name="rawKey">The raw key as authored in the configuration source.</param>
    /// <param name="options">The key options to apply, or <see langword="null" /> for the defaults.</param>
    /// <exception cref="ArgumentException">
    /// <paramref name="rawKey" /> is <see langword="null" />, empty, or contains only whitespace; or a segment was
    /// empty when empty segments are not permitted.
    /// </exception>
    public ConfigurationKey(string rawKey, ConfigurationKeyOptions? options = null)
    {
        ThrowHelper.ThrowIfNullOrWhiteSpace(rawKey);
        RejectControlCharacters(rawKey);

        ConfigurationKeyOptions effective = options ?? ConfigurationKeyOptions.Default;
        ImmutableArray<string> segments = SplitSegments(rawKey, effective);

        RawKey = rawKey;
        Segments = segments;
        Path = ComposePath(segments, effective);
        CaseSensitive = effective.CaseSensitive;
    }

    private ConfigurationKey(string rawKey, ImmutableArray<string> segments, string path, bool caseSensitive)
    {
        RawKey = rawKey;
        Segments = segments;
        Path = path;
        CaseSensitive = caseSensitive;
    }

    /// <summary>
    /// Gets the raw key string exactly as it appeared in the source document.
    /// </summary>
    /// <returns>The original key text, or the empty string for a default instance.</returns>
    public string RawKey => field ?? string.Empty;

    /// <summary>
    /// Gets the canonical colon-delimited logical key path derived from <see cref="Segments" />.
    /// </summary>
    /// <returns>The configuration key in colon-delimited form, or the empty string for a default instance.</returns>
    public string Path => field ?? string.Empty;

    /// <summary>
    /// Gets the segments produced by splitting <see cref="RawKey" /> on the configured separators.
    /// </summary>
    /// <returns>An immutable array of segment strings.</returns>
    public ImmutableArray<string> Segments => field.IsDefault ? [] : field;

    /// <summary>
    /// Gets a value indicating whether equality and hashing for this key are case-sensitive.
    /// </summary>
    /// <returns>
    /// <see langword="true" /> when ordinal case-sensitive comparison is used; otherwise, <see langword="false" /> for
    /// ordinal-ignore-case.
    /// </returns>
    public bool CaseSensitive { get; }

    /// <summary>
    /// Determines whether two keys are equal.
    /// </summary>
    /// <param name="left">The first key to compare.</param>
    /// <param name="right">The second key to compare.</param>
    /// <returns><see langword="true" /> when the keys are equal; otherwise, <see langword="false" />.</returns>
    public static bool operator ==(ConfigurationKey left, ConfigurationKey right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Determines whether two keys are not equal.
    /// </summary>
    /// <param name="left">The first key to compare.</param>
    /// <param name="right">The second key to compare.</param>
    /// <returns><see langword="true" /> when the keys differ; otherwise, <see langword="false" />.</returns>
    public static bool operator !=(ConfigurationKey left, ConfigurationKey right)
    {
        return !left.Equals(right);
    }

    /// <summary>
    /// Determines whether this key has the same segment sequence as <paramref name="other" /> under the configured
    /// comparer.
    /// </summary>
    /// <param name="other">The other key to compare with.</param>
    /// <returns>
    /// <see langword="true" /> when the segment sequences are equal under the chosen comparer; otherwise,
    /// <see langword="false" />.
    /// </returns>
    public bool Equals(ConfigurationKey other)
    {
        ImmutableArray<string> a = Segments;
        ImmutableArray<string> b = other.Segments;

        if (a.Length != b.Length)
            return false;

        StringComparer comparer = CaseSensitive
            ? StringComparer.Ordinal
            : StringComparer.OrdinalIgnoreCase;

        for (var i = 0; i < a.Length; i++)
        {
            if (!comparer.Equals(a[i], b[i])) return false;
        }

        return true;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) =>
        obj is ConfigurationKey other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        ImmutableArray<string> segments = Segments;
        StringComparer comparer = CaseSensitive
            ? StringComparer.Ordinal
            : StringComparer.OrdinalIgnoreCase;

        HashCode hash = default;
        for (var i = 0; i < segments.Length; i++)
            hash.Add(segments[i], comparer);

        return hash.ToHashCode();
    }

    /// <inheritdoc />
    public override string ToString() => Path;

    private static ImmutableArray<string> SplitSegments(string rawKey, ConfigurationKeyOptions options)
    {
        IReadOnlyList<char> separators = options.SegmentSeparators;
        ImmutableArray<string>.Builder builder = ImmutableArray.CreateBuilder<string>();

        var start = 0;
        for (var i = 0; i < rawKey.Length; i++)
        {
            var c = rawKey[i];
            if (IsSeparator(c, separators))
            {
                AddSegment(builder, rawKey.AsSpan(start, i - start), options);
                start = i + 1;
            }
        }

        AddSegment(builder, rawKey.AsSpan(start, rawKey.Length - start), options);

        return builder.ToImmutable();
    }

    private static void AddSegment(ImmutableArray<string>.Builder builder, ReadOnlySpan<char> segment, ConfigurationKeyOptions options)
    {
        if (segment.IsEmpty)
        {
            if (!options.AllowEmptySegments) throw new ArgumentException(ConfigurationResourceStrings.Arg_Invalid_ConfigKeyEmptySegment, nameof(segment));

            builder.Add(string.Empty);
            return;
        }

        builder.Add(segment.ToString());
    }

    private static void RejectControlCharacters(string rawKey) =>
        ConfigurationThrowHelper.ThrowIfConfigKeyContainsControlChar(rawKey);

    private static bool IsSeparator(char c, IReadOnlyList<char> separators)
    {
        for (var i = 0; i < separators.Count; i++)
        {
            if (separators[i] == c)
                return true;
        }

        return false;
    }

    private static string ComposePath(ImmutableArray<string> segments, ConfigurationKeyOptions options) =>
        options.Mapping switch
        {
            ConfigurationKeyMapping.Identity => string.Join(GetFirstSeparator(options), segments),
            ConfigurationKeyMapping.Colon => string.Join(':', segments),
            ConfigurationKeyMapping.DotToColon => string.Join(':', segments),
            _ => string.Join(':', segments),
        };

    private static char GetFirstSeparator(ConfigurationKeyOptions options) =>
        options.SegmentSeparators.Count > 0 ? options.SegmentSeparators[0] : '.';
}
