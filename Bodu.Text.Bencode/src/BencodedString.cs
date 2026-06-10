// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodedString.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Bencode;

/// <summary>
/// Represents a bencoded raw byte string.
/// </summary>
/// <remarks>
/// <para>
/// Bencoded strings are byte strings: a length-prefixed run of arbitrary bytes that the Bencode format does not assign
/// a character encoding to. They should only be interpreted as text when the consuming format explicitly defines the
/// field as UTF-8 text — the conventional encoding for modern peers but never a guarantee. The companion
/// <see cref="FromUtf8(string)" /> factory and <see cref="GetUtf8String" /> accessor make the encoding decision
/// explicit; for non-text payloads (info-hashes, peer-id bytes, raw binary blobs) work directly with
/// <see cref="Bytes" />.
/// </para>
/// <para>
/// Instances are immutable and implement value equality through <see cref="IEquatable{T}" /> so they compose cleanly
/// with hash sets, dictionaries, and <see cref="BencodedStringComparer.Ordinal" />.
/// </para>
/// </remarks>
/// <example>
///<![CDATA[
/// // Construct from raw bytes or from a UTF-8 string.
/// var hash  = new BencodedString(infoHashBytes);
/// var label = BencodedString.FromUtf8("comment");
///
/// // Inspect the payload — Bytes for raw, GetUtf8String only when text is expected.
/// ReadOnlySpan<byte> raw  = hash.Bytes.Span;
/// string             text = label.GetUtf8String();   // "comment"
///]]>
/// </example>
public sealed class BencodedString
    : BencodedValue,
    IEquatable<BencodedString>
{
    private readonly byte[] _bytes;

    /// <summary>
    /// Initializes a new instance of the <see cref="BencodedString" /> class.
    /// </summary>
    /// <param name="bytes">The raw byte content of the string.</param>
    public BencodedString(ReadOnlySpan<byte> bytes)
    {
        this._bytes = bytes.ToArray();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BencodedString" /> class.
    /// </summary>
    /// <param name="bytes">The raw byte content of the string.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="bytes" /> is <see langword="null" />.
    /// </exception>
    public BencodedString(byte[] bytes)
    {
        ThrowHelper.ThrowIfNull(bytes);

        this._bytes = [.. bytes];
    }

    /// <summary>
    /// Gets the raw byte content.
    /// </summary>
    public ReadOnlyMemory<byte> Bytes => _bytes;

    /// <inheritdoc />
    public override BencodedValueKind Kind => BencodedValueKind.String;

    /// <summary>
    /// Gets the number of bytes in the string.
    /// </summary>
    public int Length => _bytes.Length;

    /// <summary>
    /// Creates a bencoded byte string from UTF-8 text.
    /// </summary>
    /// <param name="value">The text value to encode as UTF-8.</param>
    /// <returns>A bencoded string containing the UTF-8 encoded bytes.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value" /> is <see langword="null" />.
    /// </exception>
    public static BencodedString FromUtf8(string value)
    {
        ThrowHelper.ThrowIfNull(value);

        return new BencodedString(System.Text.Encoding.UTF8.GetBytes(value));
    }

    /// <summary>
    /// Determines whether this instance and another <see cref="BencodedString" /> contain the same bytes.
    /// </summary>
    /// <param name="other">The value to compare with this instance.</param>
    /// <returns>
    /// <see langword="true" /> when both byte sequences are equal; otherwise, <see langword="false" />.
    /// </returns>
    public bool Equals(BencodedString? other) =>
        other is not null && _bytes.AsSpan().SequenceEqual(other._bytes);

    /// <inheritdoc />
    public override bool Equals(object? obj) =>
        obj is BencodedString other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() =>
        BencodedStringComparer.Ordinal.GetHashCode(this);

    /// <summary>
    /// Decodes the raw byte content as UTF-8 text.
    /// </summary>
    /// <returns>The decoded UTF-8 string.</returns>
    public string GetUtf8String() =>
        System.Text.Encoding.UTF8.GetString(_bytes);

    /// <inheritdoc />
    public override string ToString() =>
        GetUtf8String();
}
