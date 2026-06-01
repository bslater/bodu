// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeParseOptions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Bencode;

/// <summary>
/// Controls parser policy for <see cref="Bencode.Decode(System.ReadOnlySpan{byte}, BencodeParseOptions)" /> and related
/// decoding entry points.
/// </summary>
/// <remarks>
/// <para>
/// The defaults match the strict, BEP 3-aligned behaviour applied when the parameterless
/// <see cref="Bencode.Decode(System.ReadOnlySpan{byte})" /> overload is used: complete-document consumption, dictionary
/// keys in strictly ascending bytewise order, and a recursion-depth ceiling that defends against pathologically nested
/// input.
/// </para>
/// <para>
/// Instances are immutable and safe to share across threads.
/// </para>
/// </remarks>
public readonly struct BencodeParseOptions
{
    /// <summary>
    /// The default maximum recursion depth applied when callers do not override <see cref="MaxDepth" />.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The value is chosen to comfortably accommodate every realistic torrent metainfo file (which rarely nest beyond a
    /// depth of ten) while keeping the recursive parser well clear of the platform's per-thread stack budget on 64-bit
    /// runtimes.
    /// </para>
    /// </remarks>
    public const int DefaultMaxDepth = 512;

    /// <summary>
    /// Gets a <see cref="BencodeParseOptions" /> instance initialised with the default policy.
    /// </summary>
    /// <returns>A default <see cref="BencodeParseOptions" /> value.</returns>
    public static readonly BencodeParseOptions Default = new();

    private readonly int _maxDepth;

    /// <summary>
    /// Initializes a new instance of the <see cref="BencodeParseOptions" /> struct with all properties at their
    /// defaults.
    /// </summary>
    public BencodeParseOptions()
    {
        _maxDepth = DefaultMaxDepth;
    }

    /// <summary>
    /// Gets the maximum permitted depth of nested lists and dictionaries that the parser will accept before throwing a
    /// <see cref="BencodeFormatException" />.
    /// </summary>
    /// <returns>A positive integer. The default is <see cref="DefaultMaxDepth" />.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// Thrown when assigned a value less than or equal to zero.
    /// </exception>
    public int MaxDepth
    {
        get => _maxDepth == 0 ? DefaultMaxDepth : _maxDepth;

        init
        {
            ThrowHelper.ThrowIfLessThanOrEqual(value, 0);
            _maxDepth = value;
        }
    }

    /// <summary>
    /// Gets a value indicating whether
    /// <see cref="Bencode.TryDecode(System.ReadOnlySpan{byte}, BencodeParseOptions, out BencodedValue?, out int)" />
    /// must consume the entire input.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The default is <see langword="false" />, which preserves the lenient TryDecode contract: a prefix that
    /// successfully decodes returns <see langword="true" /> with <c>bytesConsumed</c> set to the prefix length, and
    /// trailing bytes are silently ignored. Set to <see langword="true" /> to require that the entire source is
    /// consumed by a single value, mirroring the strict behaviour of
    /// <see cref="Bencode.Decode(System.ReadOnlySpan{byte})" />.
    /// </para>
    /// </remarks>
    /// <returns>
    /// <see langword="true" /> if TryDecode must consume the full input; otherwise, <see langword="false" />.
    /// </returns>
    public bool RequireCompleteDocument { get; init; }
}
