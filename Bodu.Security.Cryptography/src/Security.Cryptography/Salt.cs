// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Salt.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Represents a salt supplied to a key-derivation or password-hashing operation.
/// </summary>
/// <remarks>
/// <para>
/// A salt diversifies derivation output so identical inputs do not produce identical results; it is unique per
/// derivation but not secret, and is typically stored alongside the derived value. Using a dedicated type keeps salts
/// from being confused with keys, nonces, or derived output in APIs that would otherwise accept several look-alike byte
/// buffers.
/// </para>
/// <para>
/// <see cref="Salt" /> carries its bytes by defensive copy, and the default instance (<c>default(Salt)</c>) is the
/// empty value: <see cref="Length" /> is <c>0</c> and <see cref="IsEmpty" /> is <see langword="true" />.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// // Generate a 16-byte salt for password hashing and persist it next to the derived value.
/// Salt salt = Salt.Random(16);
/// byte[] derived = Rfc2898DeriveBytes.Pbkdf2(
///     password, salt.AsSpan(), iterations: 100_000, HashAlgorithmName.SHA256, outputLength: 32);
///
/// // The salt is not secret: reload it alongside the stored hash to recompute the value later.
/// Salt reloaded = Salt.FromBytes(storedSaltBytes);
///]]>
/// </code>
/// </example>
public readonly struct Salt
    : IEquatable<Salt>
{
    /// <summary>The salt bytes, or <see langword="null" /> for the default (empty) instance. Never exposed directly; all accessors normalize <see langword="null" /> to an empty value.</summary>
    private readonly byte[]? _value;

    /// <summary>
    /// Initializes a new instance of the <see cref="Salt" /> struct that takes ownership of the supplied array.
    /// </summary>
    /// <param name="value">The salt bytes. Callers must pass a buffer that is not retained elsewhere.</param>
    private Salt(byte[] value)
    {
        _value = value;
    }

    /// <summary>
    /// Gets a value indicating whether the salt is empty.
    /// </summary>
    /// <returns><see langword="true" /> if the salt contains no bytes; otherwise, <see langword="false" />.</returns>
    public bool IsEmpty =>
        Length == 0;

    /// <summary>
    /// Gets the number of bytes in the salt.
    /// </summary>
    /// <returns>The salt length in bytes, or <c>0</c> for the empty value.</returns>
    public int Length =>
        _value?.Length ?? 0;

    /// <summary>
    /// Determines whether two salts are equal.
    /// </summary>
    /// <param name="left">The first salt.</param>
    /// <param name="right">The second salt.</param>
    /// <returns>
    /// <see langword="true" /> if the values contain identical bytes; otherwise, <see langword="false" />.
    /// </returns>
    public static bool operator ==(Salt left, Salt right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Determines whether two salts are not equal.
    /// </summary>
    /// <param name="left">The first salt.</param>
    /// <param name="right">The second salt.</param>
    /// <returns><see langword="true" /> if the values differ; otherwise, <see langword="false" />.</returns>
    public static bool operator !=(Salt left, Salt right)
    {
        return !left.Equals(right);
    }

    /// <summary>
    /// Creates a <see cref="Salt" /> from the provided bytes.
    /// </summary>
    /// <param name="value">The salt bytes to copy. An empty span yields the empty value.</param>
    /// <returns>A new <see cref="Salt" /> containing a defensive copy of <paramref name="value" />.</returns>
    public static Salt FromBytes(ReadOnlySpan<byte> value) =>
        value.IsEmpty ? default : new Salt(value.ToArray());

    /// <summary>
    /// Creates a <see cref="Salt" /> of the specified length filled with cryptographically secure random bytes.
    /// </summary>
    /// <param name="length">The number of random bytes to generate.</param>
    /// <returns>A new <see cref="Salt" /> of <paramref name="length" /> random bytes.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="length" /> ≤ 0.</exception>
    public static Salt Random(int length)
    {
        ThrowHelper.ThrowIfZeroOrNegative(length);

        return new Salt(CryptographyHelper.GetRandomBytes(length));
    }

    /// <summary>
    /// Returns a read-only view over the salt bytes.
    /// </summary>
    /// <returns>A read-only span over the value; empty for the empty value.</returns>
    public ReadOnlySpan<byte> AsSpan() =>
        _value;

    /// <summary>
    /// Determines whether this salt equals another.
    /// </summary>
    /// <param name="other">The salt to compare against.</param>
    /// <returns>
    /// <see langword="true" /> if both values contain identical bytes; otherwise, <see langword="false" />.
    /// </returns>
    public bool Equals(Salt other) =>
        AsSpan().SequenceEqual(other.AsSpan());

    /// <summary>
    /// Determines whether this salt equals the specified object.
    /// </summary>
    /// <param name="obj">The object to compare against.</param>
    /// <returns>
    /// <see langword="true" /> if <paramref name="obj" /> is a <see cref="Salt" /> with identical bytes; otherwise,
    /// <see langword="false" />.
    /// </returns>
    public override bool Equals(object? obj) =>
        obj is Salt other && Equals(other);

    /// <summary>
    /// Returns a hash code computed over the salt bytes.
    /// </summary>
    /// <returns>A hash code consistent with <see cref="Equals(Salt)" />.</returns>
    public override int GetHashCode()
    {
        HashCode hashCode = default;
        hashCode.AddBytes(AsSpan());
        return hashCode.ToHashCode();
    }

    /// <summary>
    /// Copies the salt bytes into a new array.
    /// </summary>
    /// <returns>A new array containing the salt bytes; an empty array for the empty value.</returns>
    public byte[] ToArray() =>
        _value is null ? [] : (byte[])_value.Clone();

    /// <summary>
    /// Returns the lowercase hexadecimal representation of the salt.
    /// </summary>
    /// <returns>
    /// The salt bytes as lowercase hexadecimal text; <see cref="string.Empty" /> for the empty value.
    /// </returns>
    /// <remarks>
    /// Salts are not secrets, so the content is intentionally included in the string representation.
    /// </remarks>
    public override string ToString() =>
        CryptographyHelper.ToLowercaseHexString(_value);
}
