// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MessageDigestKnownAnswer.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using static Bodu.Security.Cryptography.Infrastructure.KatBytes;

namespace Bodu.Security.Cryptography.Infrastructure;

/// <summary>
/// Represents a single known-answer test vector for a message-digest algorithm — a named message paired with the
/// expected digest, plus an optional per-row key. Covers plain hashes, keyed hashes and MACs, and the hashing extension
/// surface in one record.
/// </summary>
/// <remarks>
/// <para>
/// When <see cref="KeyedKnownAnswer.Key" /> is <see langword="null" /> the digest is produced with whatever key (if
/// any) the variant's algorithm instance is constructed with; a populated key applies that key per row (for example the
/// Skein 1.3 / NIST CD <c>random+MAC</c> vectors, where every row carries its own MAC key), and an empty array selects
/// the canonical unkeyed mode for algorithms that accept one.
/// </para>
/// <para>
/// Use the <see cref="Empty(string)" />, <see cref="Abc(string)" />, <see cref="QuickBrownFox(string)" />,
/// <see cref="Zeros16(string)" />, and <see cref="Sequential0To255(string)" /> factories to declare the canonical
/// shared-input vectors tersely; construct the record directly for algorithm-specific messages.
/// </para>
/// </remarks>
public sealed record MessageDigestKnownAnswer
    : KeyedKnownAnswer
{
    /// <summary>
    /// Gets the raw message bytes fed to the algorithm.
    /// </summary>
    public required byte[] Message { get; init; }

    /// <summary>
    /// Gets the expected digest produced by hashing <see cref="Message" /> (under <see cref="KeyedKnownAnswer.Key" />
    /// when one is supplied).
    /// </summary>
    public required byte[] Digest { get; init; }

    /// <summary>
    /// Gets the async/streaming buffer size used when exercising the streamed hashing extension methods. Defaults to
    /// the .NET <c>Stream.CopyToAsync</c> default of 81920 bytes.
    /// </summary>
    public int BufferSize { get; init; } = 81920;

    /// <summary>
    /// Creates a vector for the empty input.
    /// </summary>
    /// <param name="digestHex">The expected digest as a hex string.</param>
    /// <returns>A vector named <c>"Empty"</c> over the empty message.</returns>
    public static MessageDigestKnownAnswer Empty(string digestHex) =>
        Canonical(nameof(Empty), CryptoKatInputs.Empty, digestHex);

    /// <summary>
    /// Creates a vector for the ASCII input <c>"ABC"</c>.
    /// </summary>
    /// <param name="digestHex">The expected digest as a hex string.</param>
    /// <returns>A vector named <c>"Abc"</c> over the <c>"ABC"</c> message.</returns>
    public static MessageDigestKnownAnswer Abc(string digestHex) =>
        Canonical(nameof(Abc), CryptoKatInputs.Abc, digestHex);

    /// <summary>
    /// Creates a vector for the quick-brown-fox pangram.
    /// </summary>
    /// <param name="digestHex">The expected digest as a hex string.</param>
    /// <returns>A vector named <c>"QuickBrownFox"</c> over the pangram message.</returns>
    public static MessageDigestKnownAnswer QuickBrownFox(string digestHex) =>
        Canonical(nameof(QuickBrownFox), CryptoKatInputs.QuickBrownFox, digestHex);

    /// <summary>
    /// Creates a vector for sixteen zero bytes.
    /// </summary>
    /// <param name="digestHex">The expected digest as a hex string.</param>
    /// <returns>A vector named <c>"Zeros16"</c> over sixteen zero bytes.</returns>
    public static MessageDigestKnownAnswer Zeros16(string digestHex) =>
        Canonical(nameof(Zeros16), CryptoKatInputs.Zeros16, digestHex);

    /// <summary>
    /// Creates a vector for the 255-byte sequence <c>0x00, 0x01, …, 0xFE</c>.
    /// </summary>
    /// <param name="digestHex">The expected digest as a hex string.</param>
    /// <returns>A vector named <c>"Sequential0To255"</c> over the byte sequence.</returns>
    public static MessageDigestKnownAnswer Sequential0To255(string digestHex) =>
        Canonical(nameof(Sequential0To255), CryptoKatInputs.Sequential0To255, digestHex);

    /// <summary>
    /// Creates a canonical shared-input vector with the given slot name, message, and hex digest.
    /// </summary>
    /// <param name="name">The slot name used as the vector's display label.</param>
    /// <param name="message">The shared input message.</param>
    /// <param name="digestHex">The expected digest as a hex string.</param>
    /// <returns>A populated <see cref="MessageDigestKnownAnswer" />.</returns>
    private static MessageDigestKnownAnswer Canonical(string name, byte[] message, string digestHex) =>
        new() { Name = name, Message = message, Digest = Hex(digestHex) };
}
