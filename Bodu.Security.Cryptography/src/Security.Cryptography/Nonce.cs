// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Nonce.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Represents a number-used-once supplied to an authenticated-encryption or stream-cipher operation.
/// </summary>
/// <remarks>
/// <para>
/// A nonce is unique per operation under a given key but is not secret; it typically travels alongside the ciphertext.
/// Using a dedicated type keeps nonces from being confused with keys, salts, or authentication tags in APIs that would
/// otherwise accept several look-alike byte buffers.
/// </para>
/// <para>
/// <see cref="Nonce" /> carries its bytes by defensive copy, and the default instance (<c>default(Nonce)</c>) is the
/// empty value: <see cref="Length" /> is <c>0</c> and <see cref="IsEmpty" /> is <see langword="true" />.
/// </para>
/// <para>
/// This type does not enforce uniqueness; callers remain responsible for never reusing a nonce with the same key.
/// <see cref="Random(int)" /> draws from a cryptographically secure generator, which is appropriate for nonce sizes
/// where random collision is negligible (for example the 24-byte XChaCha20 nonce).
/// </para>
/// </remarks>
public readonly struct Nonce : IEquatable<Nonce>
{
    /// <summary>
    /// The nonce bytes, or <see langword="null" /> for the default (empty) instance. Never exposed directly; all
    /// accessors normalize <see langword="null" /> to an empty value.
    /// </summary>
    private readonly byte[]? _value;

    /// <summary>
    /// Initializes a new instance of the <see cref="Nonce" /> struct that takes ownership of the supplied array.
    /// </summary>
    /// <param name="value">The nonce bytes. Callers must pass a buffer that is not retained elsewhere.</param>
    private Nonce(byte[] value)
    {
        _value = value;
    }

    /// <summary>
    /// Gets the number of bytes in the nonce.
    /// </summary>
    /// <returns>The nonce length in bytes, or <c>0</c> for the empty value.</returns>
    public int Length =>
        _value?.Length ?? 0;

    /// <summary>
    /// Gets a value indicating whether the nonce is empty.
    /// </summary>
    /// <returns><see langword="true" /> if the nonce contains no bytes; otherwise, <see langword="false" />.</returns>
    public bool IsEmpty =>
        Length == 0;

    /// <summary>
    /// Creates a <see cref="Nonce" /> from the provided bytes.
    /// </summary>
    /// <param name="value">The nonce bytes to copy. An empty span yields the empty value.</param>
    /// <returns>A new <see cref="Nonce" /> containing a defensive copy of <paramref name="value" />.</returns>
    public static Nonce FromBytes(ReadOnlySpan<byte> value) =>
        value.IsEmpty ? default : new Nonce(value.ToArray());

    /// <summary>
    /// Creates a <see cref="Nonce" /> of the specified length filled with cryptographically secure random bytes.
    /// </summary>
    /// <param name="length">The number of random bytes to generate.</param>
    /// <returns>A new <see cref="Nonce" /> of <paramref name="length" /> random bytes.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="length" /> ≤ 0.</exception>
    public static Nonce Random(int length)
    {
        ThrowHelper.ThrowIfZeroOrNegative(length);

        return new Nonce(CryptographyHelper.GetRandomBytes(length));
    }

    /// <summary>
    /// Returns a read-only view over the nonce bytes.
    /// </summary>
    /// <returns>A read-only span over the value; empty for the empty value.</returns>
    public ReadOnlySpan<byte> AsSpan() =>
        _value;

    /// <summary>
    /// Copies the nonce bytes into a new array.
    /// </summary>
    /// <returns>A new array containing the nonce bytes; an empty array for the empty value.</returns>
    public byte[] ToArray() =>
        _value is null ? [] : (byte[])_value.Clone();

    /// <summary>
    /// Determines whether this nonce equals another.
    /// </summary>
    /// <param name="other">The nonce to compare against.</param>
    /// <returns>
    /// <see langword="true" /> if both values contain identical bytes; otherwise, <see langword="false" />.
    /// </returns>
    public bool Equals(Nonce other) =>
        AsSpan().SequenceEqual(other.AsSpan());

    /// <summary>
    /// Determines whether this nonce equals the specified object.
    /// </summary>
    /// <param name="obj">The object to compare against.</param>
    /// <returns>
    /// <see langword="true" /> if <paramref name="obj" /> is a <see cref="Nonce" /> with identical bytes; otherwise,
    /// <see langword="false" />.
    /// </returns>
    public override bool Equals(object? obj) =>
        obj is Nonce other && Equals(other);

    /// <summary>
    /// Returns a hash code computed over the nonce bytes.
    /// </summary>
    /// <returns>A hash code consistent with <see cref="Equals(Nonce)" />.</returns>
    public override int GetHashCode()
    {
        HashCode hashCode = default;
        hashCode.AddBytes(AsSpan());
        return hashCode.ToHashCode();
    }

    /// <summary>
    /// Determines whether two nonces are equal.
    /// </summary>
    /// <param name="left">The first nonce.</param>
    /// <param name="right">The second nonce.</param>
    /// <returns>
    /// <see langword="true" /> if the values contain identical bytes; otherwise, <see langword="false" />.
    /// </returns>
    public static bool operator ==(Nonce left, Nonce right) =>
        left.Equals(right);

    /// <summary>
    /// Determines whether two nonces are not equal.
    /// </summary>
    /// <param name="left">The first nonce.</param>
    /// <param name="right">The second nonce.</param>
    /// <returns><see langword="true" /> if the values differ; otherwise, <see langword="false" />.</returns>
    public static bool operator !=(Nonce left, Nonce right) =>
        !left.Equals(right);

    /// <summary>
    /// Returns the lowercase hexadecimal representation of the nonce.
    /// </summary>
    /// <returns>
    /// The nonce bytes as lowercase hexadecimal text; <see cref="string.Empty" /> for the empty value.
    /// </returns>
    /// <remarks>
    /// Nonces are not secrets, so the content is intentionally included in the string representation.
    /// </remarks>
    public override string ToString() =>
        CryptographyHelper.ToLowercaseHexString(_value);
}
