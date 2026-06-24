// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AuthenticationTag.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Represents the authentication tag produced by an authenticated-encryption (AEAD) operation.
/// </summary>
/// <remarks>
/// <para>
/// An authentication tag proves that ciphertext and associated data were produced under a specific key and have not
/// been modified. Using a dedicated type keeps tags from being confused with nonces, keys, or digests in APIs that
/// would otherwise accept several look-alike byte buffers.
/// </para>
/// <para>
/// <see cref="AuthenticationTag" /> carries its bytes by defensive copy, and the default instance (
/// <c>default(AuthenticationTag)</c>) is the empty value: <see cref="Length" /> is <c>0</c> and <see cref="IsEmpty" />
/// is <see langword="true" />.
/// </para>
/// <para>
/// Tag verification is security-critical: always compare tags with <see cref="FixedTimeEquals(AuthenticationTag)" />
/// (or the span overload), never with <see cref="Equals(AuthenticationTag)" />, so an attacker cannot learn the
/// position of the first mismatching byte from the comparison time.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// // Capture the tag emitted by an AEAD encryption and store or transmit it with the ciphertext.
/// AuthenticationTag tag = AuthenticationTag.FromBytes(producedTag);
///
/// // On the decrypt side, verify the recomputed tag in fixed time before trusting the plaintext.
/// if (!tag.FixedTimeEquals(recomputedTag))
///     throw new CryptographicException("Authentication failed; the message was altered.");
///]]>
/// </code>
/// </example>
public readonly struct AuthenticationTag
    : IEquatable<AuthenticationTag>
{
    /// <summary>The tag bytes, or <see langword="null" /> for the default (empty) instance. Never exposed directly; all accessors normalize <see langword="null" /> to an empty value.</summary>
    private readonly byte[]? _value;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthenticationTag" /> struct that takes ownership of the supplied
    /// array.
    /// </summary>
    /// <param name="value">The tag bytes. Callers must pass a buffer that is not retained elsewhere.</param>
    private AuthenticationTag(byte[] value)
    {
        _value = value;
    }

    /// <summary>
    /// Gets a value indicating whether the tag is empty.
    /// </summary>
    /// <value><see langword="true" /> if the tag contains no bytes; otherwise, <see langword="false" />.</value>
    public bool IsEmpty =>
        Length == 0;

    /// <summary>
    /// Gets the number of bytes in the tag.
    /// </summary>
    /// <value>The tag length in bytes, or <c>0</c> for the empty value.</value>
    public int Length =>
        _value?.Length ?? 0;

    /// <summary>
    /// Determines whether two tags are equal.
    /// </summary>
    /// <param name="left">The first tag.</param>
    /// <param name="right">The second tag.</param>
    /// <returns>
    /// <see langword="true" /> if the tags contain identical bytes; otherwise, <see langword="false" />.
    /// </returns>
    public static bool operator ==(AuthenticationTag left, AuthenticationTag right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Determines whether two tags are not equal.
    /// </summary>
    /// <param name="left">The first tag.</param>
    /// <param name="right">The second tag.</param>
    /// <returns><see langword="true" /> if the tags differ; otherwise, <see langword="false" />.</returns>
    public static bool operator !=(AuthenticationTag left, AuthenticationTag right)
    {
        return !left.Equals(right);
    }

    /// <summary>
    /// Creates an <see cref="AuthenticationTag" /> from the provided bytes.
    /// </summary>
    /// <param name="value">The tag bytes to copy. An empty span yields the empty value.</param>
    /// <returns>
    /// A new <see cref="AuthenticationTag" /> containing a defensive copy of <paramref name="value" />.
    /// </returns>
    public static AuthenticationTag FromBytes(ReadOnlySpan<byte> value) =>
        value.IsEmpty ? default : new AuthenticationTag(value.ToArray());

    /// <summary>
    /// Returns a read-only view over the tag bytes.
    /// </summary>
    /// <returns>A read-only span over the value; empty for the empty value.</returns>
    public ReadOnlySpan<byte> AsSpan() =>
        _value;

    /// <summary>
    /// Determines whether this tag equals another.
    /// </summary>
    /// <param name="other">The tag to compare against.</param>
    /// <returns>
    /// <see langword="true" /> if both tags contain identical bytes; otherwise, <see langword="false" />.
    /// </returns>
    /// <remarks>
    /// This is an ordinary, short-circuiting comparison; tag verification must use
    /// <see cref="FixedTimeEquals(AuthenticationTag)" /> instead.
    /// </remarks>
    public bool Equals(AuthenticationTag other) =>
        AsSpan().SequenceEqual(other.AsSpan());

    /// <summary>
    /// Determines whether this tag equals the specified object.
    /// </summary>
    /// <param name="obj">The object to compare against.</param>
    /// <returns>
    /// <see langword="true" /> if <paramref name="obj" /> is an <see cref="AuthenticationTag" /> with identical bytes;
    /// otherwise, <see langword="false" />.
    /// </returns>
    public override bool Equals(object? obj) =>
        obj is AuthenticationTag other && Equals(other);

    /// <summary>
    /// Compares this tag to another in fixed time.
    /// </summary>
    /// <param name="other">The tag to compare against.</param>
    /// <returns>
    /// <see langword="true" /> if both tags have the same length and identical bytes; otherwise,
    /// <see langword="false" />.
    /// </returns>
    /// <remarks>
    /// Built on <see cref="CryptographicOperations.FixedTimeEquals(ReadOnlySpan{byte}, ReadOnlySpan{byte})" />: the
    /// comparison time depends only on the length of the operands, not on their content. A length mismatch returns
    /// <see langword="false" /> immediately, so the lengths themselves are not concealed.
    /// </remarks>
    public bool FixedTimeEquals(AuthenticationTag other) =>
        CryptographicOperations.FixedTimeEquals(AsSpan(), other.AsSpan());

    /// <summary>
    /// Compares this tag to a raw byte sequence in fixed time.
    /// </summary>
    /// <param name="other">
    /// The bytes to compare against, typically the tag region of a transform's output buffer.
    /// </param>
    /// <returns>
    /// <see langword="true" /> if <paramref name="other" /> has the same length and identical bytes; otherwise,
    /// <see langword="false" />.
    /// </returns>
    public bool FixedTimeEquals(ReadOnlySpan<byte> other) =>
        CryptographicOperations.FixedTimeEquals(AsSpan(), other);

    /// <summary>
    /// Returns a hash code computed over the tag bytes.
    /// </summary>
    /// <returns>A hash code consistent with <see cref="Equals(AuthenticationTag)" />.</returns>
    public override int GetHashCode()
    {
        HashCode hashCode = default;
        hashCode.AddBytes(AsSpan());
        return hashCode.ToHashCode();
    }

    /// <summary>
    /// Copies the tag bytes into a new array.
    /// </summary>
    /// <returns>A new array containing the tag bytes; an empty array for the empty value.</returns>
    public byte[] ToArray() =>
        _value is null ? [] : (byte[])_value.Clone();

    /// <summary>
    /// Returns the lowercase hexadecimal representation of the tag.
    /// </summary>
    /// <returns>The tag bytes as lowercase hexadecimal text; <see cref="string.Empty" /> for the empty value.</returns>
    /// <remarks>
    /// Authentication tags travel with the ciphertext and are not secrets, so the content is intentionally included in
    /// the string representation.
    /// </remarks>
    public override string ToString() =>
        CryptographyHelper.ToLowercaseHexString(_value);
}
