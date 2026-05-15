// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodedString.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Formats;

/// <summary>
/// Represents a bencoded raw byte string.
/// </summary>
/// <remarks>
/// Bencoded strings are byte strings. They should only be interpreted as text when the consuming format explicitly
/// defines the field as UTF-8 text.
/// </remarks>
public sealed class BencodedString
    : BencodedValue
    , IEquatable<BencodedString>
{

    private readonly byte[] bytes;

    /// <summary>
    /// Initializes a new instance of the <see cref="BencodedString" /> class.
    /// </summary>
    /// <param name="bytes">The raw byte content of the string.</param>
    public BencodedString(ReadOnlySpan<byte> bytes)
    {
        this.bytes = bytes.ToArray();
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

        this.bytes = bytes.ToArray();
    }

    /// <summary>
    /// Gets the raw byte content.
    /// </summary>
    public ReadOnlyMemory<byte> Bytes => bytes;

    /// <inheritdoc />
    public override BencodedValueKind Kind => BencodedValueKind.String;

    /// <summary>
    /// Gets the number of bytes in the string.
    /// </summary>
    public int Length => bytes.Length;

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
    /// <returns><see langword="true" /> when both byte sequences are equal; otherwise, <see langword="false" />.</returns>
    public bool Equals(BencodedString? other) =>
        other is not null && bytes.AsSpan().SequenceEqual(other.bytes);

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
        System.Text.Encoding.UTF8.GetString(bytes);

    /// <inheritdoc />
    public override string ToString() =>
        GetUtf8String();

}
