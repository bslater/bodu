// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SignatureValue.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Represents a digital-signature value together with its wire encoding.
/// </summary>
/// <remarks>
/// <para>
/// A signature is a byte sequence whose interpretation depends on its encoding (see <see cref="SignatureFormat" />).
/// Carrying the <see cref="Format" /> with the bytes prevents the most common signature interoperability failure:
/// passing an ASN.1 DER signature to a verifier expecting the fixed-width IEEE P1363 form, or vice versa.
/// </para>
/// <para>
/// <see cref="SignatureValue" /> carries its bytes by defensive copy, and the default instance (
/// <c>default(SignatureValue)</c>) is the empty value with <see cref="SignatureFormat.Unknown" />:
/// <see cref="Length" /> is <c>0</c> and <see cref="IsEmpty" /> is <see langword="true" />.
/// </para>
/// <para>
/// <see cref="Equals(SignatureValue)" /> compares both the format and the bytes;
/// <see cref="FixedTimeEquals(SignatureValue)" /> compares the bytes only, in fixed time.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// // Record a signature together with the wire encoding its producer emitted.
/// SignatureValue signature = SignatureValue.FromBytes(signatureBytes, SignatureFormat.P1363);
///
/// // Downstream code branches on the recorded format instead of guessing, avoiding the classic
/// // failure of feeding a fixed-width P1363 signature to a verifier that expects ASN.1 DER.
/// ReadOnlySpan<byte> bytes = signature.AsSpan();
/// bool valid = signature.Format == SignatureFormat.Der
///     ? VerifyDer(bytes)
///     : VerifyP1363(bytes);
///]]>
/// </code>
/// </example>
public readonly struct SignatureValue
    : IEquatable<SignatureValue>
{
    /// <summary>The signature bytes, or <see langword="null" /> for the default (empty) instance. Never exposed directly; all accessors normalize <see langword="null" /> to an empty value.</summary>
    private readonly byte[]? _value;

    /// <summary>The wire encoding of the signature bytes.</summary>
    private readonly SignatureFormat _format;

    /// <summary>
    /// Initializes a new instance of the <see cref="SignatureValue" /> struct that takes ownership of the supplied
    /// array.
    /// </summary>
    /// <param name="value">The signature bytes. Callers must pass a buffer that is not retained elsewhere.</param>
    /// <param name="format">The wire encoding of <paramref name="value" />.</param>
    private SignatureValue(byte[] value, SignatureFormat format)
    {
        _value = value;
        _format = format;
    }

    /// <summary>
    /// Gets the wire encoding of the signature bytes.
    /// </summary>
    /// <returns>
    /// The <see cref="SignatureFormat" /> recorded when the value was created; <see cref="SignatureFormat.Unknown" />
    /// for the default instance.
    /// </returns>
    public SignatureFormat Format =>
        _format;

    /// <summary>
    /// Gets a value indicating whether the signature is empty.
    /// </summary>
    /// <returns>
    /// <see langword="true" /> if the signature contains no bytes; otherwise, <see langword="false" />.
    /// </returns>
    public bool IsEmpty =>
        Length == 0;

    /// <summary>
    /// Gets the number of bytes in the signature.
    /// </summary>
    /// <returns>The signature length in bytes, or <c>0</c> for the empty value.</returns>
    public int Length =>
        _value?.Length ?? 0;

    /// <summary>
    /// Determines whether two signatures are equal.
    /// </summary>
    /// <param name="left">The first signature.</param>
    /// <param name="right">The second signature.</param>
    /// <returns>
    /// <see langword="true" /> if the values record the same format and contain identical bytes; otherwise,
    /// <see langword="false" />.
    /// </returns>
    public static bool operator ==(SignatureValue left, SignatureValue right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Determines whether two signatures are not equal.
    /// </summary>
    /// <param name="left">The first signature.</param>
    /// <param name="right">The second signature.</param>
    /// <returns>
    /// <see langword="true" /> if the values differ in format or bytes; otherwise, <see langword="false" />.
    /// </returns>
    public static bool operator !=(SignatureValue left, SignatureValue right)
    {
        return !left.Equals(right);
    }

    /// <summary>
    /// Creates a <see cref="SignatureValue" /> from the provided bytes and encoding.
    /// </summary>
    /// <param name="value">
    /// The signature bytes to copy. An empty span yields an empty value carrying <paramref name="format" />.
    /// </param>
    /// <param name="format">The wire encoding of <paramref name="value" />.</param>
    /// <returns>A new <see cref="SignatureValue" /> containing a defensive copy of <paramref name="value" />.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="format" /> is not a defined <see cref="SignatureFormat" /> value.
    /// </exception>
    /// <remarks>
    /// The structural validity of <paramref name="value" /> against <paramref name="format" /> is not verified; the
    /// format is metadata recorded by the producer.
    /// </remarks>
    public static SignatureValue FromBytes(ReadOnlySpan<byte> value, SignatureFormat format)
    {
        ThrowHelper.ThrowIfEnumValueIsUndefined(format);

        return new SignatureValue(value.ToArray(), format);
    }

    /// <summary>
    /// Returns a read-only view over the signature bytes.
    /// </summary>
    /// <returns>A read-only span over the value; empty for the empty value.</returns>
    public ReadOnlySpan<byte> AsSpan() =>
        _value;

    /// <summary>
    /// Determines whether this signature equals another, comparing both format and bytes.
    /// </summary>
    /// <param name="other">The signature to compare against.</param>
    /// <returns>
    /// <see langword="true" /> if both values record the same <see cref="Format" /> and contain identical bytes;
    /// otherwise, <see langword="false" />.
    /// </returns>
    /// <remarks>
    /// This is an ordinary, short-circuiting comparison; use <see cref="FixedTimeEquals(SignatureValue)" /> when the
    /// comparison is security-relevant.
    /// </remarks>
    public bool Equals(SignatureValue other) =>
        _format == other._format && AsSpan().SequenceEqual(other.AsSpan());

    /// <summary>
    /// Determines whether this signature equals the specified object.
    /// </summary>
    /// <param name="obj">The object to compare against.</param>
    /// <returns>
    /// <see langword="true" /> if <paramref name="obj" /> is a <see cref="SignatureValue" /> with the same format and
    /// identical bytes; otherwise, <see langword="false" />.
    /// </returns>
    public override bool Equals(object? obj) =>
        obj is SignatureValue other && Equals(other);

    /// <summary>
    /// Compares the bytes of this signature to another in fixed time, ignoring <see cref="Format" />.
    /// </summary>
    /// <param name="other">The signature to compare against.</param>
    /// <returns>
    /// <see langword="true" /> if both signatures have the same length and identical bytes; otherwise,
    /// <see langword="false" />.
    /// </returns>
    /// <remarks>
    /// Built on <see cref="CryptographicOperations.FixedTimeEquals(ReadOnlySpan{byte}, ReadOnlySpan{byte})" />: the
    /// comparison time depends only on the length of the operands, not on their content. A length mismatch returns
    /// <see langword="false" /> immediately, so the lengths themselves are not concealed. The recorded format is
    /// intentionally excluded; compare <see cref="Format" /> separately when the encoding matters.
    /// </remarks>
    public bool FixedTimeEquals(SignatureValue other) =>
        CryptographicOperations.FixedTimeEquals(AsSpan(), other.AsSpan());

    /// <summary>
    /// Returns a hash code computed over the format and the signature bytes.
    /// </summary>
    /// <returns>A hash code consistent with <see cref="Equals(SignatureValue)" />.</returns>
    public override int GetHashCode()
    {
        HashCode hashCode = default;
        hashCode.Add(_format);
        hashCode.AddBytes(AsSpan());
        return hashCode.ToHashCode();
    }

    /// <summary>
    /// Copies the signature bytes into a new array.
    /// </summary>
    /// <returns>A new array containing the signature bytes; an empty array for the empty value.</returns>
    public byte[] ToArray() =>
        _value is null ? [] : (byte[])_value.Clone();

    /// <summary>
    /// Formats the signature as a Base64 string.
    /// </summary>
    /// <returns>The Base64 representation, or <see cref="string.Empty" /> for the empty value.</returns>
    public string ToBase64String() =>
        _value is null ? string.Empty : Convert.ToBase64String(_value);

    /// <summary>
    /// Formats the signature as a lowercase hexadecimal string.
    /// </summary>
    /// <returns>The lowercase hexadecimal representation, or <see cref="string.Empty" /> for the empty value.</returns>
    public string ToHexString() =>
        CryptographyHelper.ToLowercaseHexString(_value);

    /// <summary>
    /// Returns the lowercase hexadecimal representation of the signature bytes.
    /// </summary>
    /// <returns>The same text as <see cref="ToHexString" />.</returns>
    /// <remarks>
    /// Signatures are not secrets, so the content is intentionally included in the string representation. The recorded
    /// format is not part of the text; read <see cref="Format" /> directly when it is needed.
    /// </remarks>
    public override string ToString() =>
        ToHexString();
}
