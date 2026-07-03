// ---------------------------------------------------------------------------------------------------------------
// <copyright file="HashValue.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Represents the immutable output of a hash operation, with strict hexadecimal parsing, common text formattings, and
/// explicit fixed-time comparison.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="HashValue" /> carries the digest bytes by defensive copy: the value is unaffected by later mutation of
/// the source buffer, and no member exposes the internal storage as a mutable array.
/// </para>
/// <para>
/// The default instance (<c>default(HashValue)</c>) is the empty value: <see cref="Length" /> is <c>0</c>,
/// <see cref="IsEmpty" /> is <see langword="true" />, and it compares equal to
/// <see cref="FromBytes(ReadOnlySpan{byte})" /> over an empty span.
/// </para>
/// <para>
/// <see cref="Equals(HashValue)" /> performs an ordinary, short-circuiting comparison suitable for collections and
/// general-purpose code. When the comparison itself is security-relevant — for example validating a received digest
/// against a locally computed one — use <see cref="FixedTimeEquals(HashValue)" />.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// // Wrap a freshly computed digest, then format it for logging or storage.
/// HashValue digest = HashValue.FromBytes(SHA256.HashData(payload));
/// string hex = digest.ToHexString();   // lowercase, e.g. "e3b0c442..."
///
/// // Compare the digest against an expected value in fixed time before trusting the payload.
/// HashValue expected = HashValue.ParseHex("e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855");
/// if (!digest.FixedTimeEquals(expected))
///     throw new CryptographicException("Digest mismatch.");
///]]>
/// </code>
/// </example>
public readonly struct HashValue
    : IEquatable<HashValue>
{
    /// <summary>The digest bytes, or <see langword="null" /> for the default (empty) instance. Never exposed directly; all accessors normalize <see langword="null" /> to an empty value.</summary>
    private readonly byte[]? _value;

    /// <summary>
    /// Initializes a new instance of the <see cref="HashValue" /> struct that takes ownership of the supplied array.
    /// </summary>
    /// <param name="value">The digest bytes. Callers must pass a buffer that is not retained elsewhere.</param>
    private HashValue(byte[] value)
    {
        _value = value;
    }

    /// <summary>
    /// Gets a value indicating whether the hash value is empty.
    /// </summary>
    /// <value><see langword="true" /> if the value contains no bytes; otherwise, <see langword="false" />.</value>
    public bool IsEmpty =>
        Length == 0;

    /// <summary>
    /// Gets the number of bytes in the hash value.
    /// </summary>
    /// <value>The digest length in bytes, or <c>0</c> for the empty value.</value>
    public int Length =>
        _value?.Length ?? 0;

    /// <summary>
    /// Determines whether two hash values are equal.
    /// </summary>
    /// <param name="left">The first hash value.</param>
    /// <param name="right">The second hash value.</param>
    /// <returns>
    /// <see langword="true" /> if the values contain identical bytes; otherwise, <see langword="false" />.
    /// </returns>
    public static bool operator ==(HashValue left, HashValue right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Determines whether two hash values are not equal.
    /// </summary>
    /// <param name="left">The first hash value.</param>
    /// <param name="right">The second hash value.</param>
    /// <returns><see langword="true" /> if the values differ; otherwise, <see langword="false" />.</returns>
    public static bool operator !=(HashValue left, HashValue right)
    {
        return !left.Equals(right);
    }

    /// <summary>
    /// Creates a <see cref="HashValue" /> from the provided bytes.
    /// </summary>
    /// <param name="value">The digest bytes to copy. An empty span yields the empty value.</param>
    /// <returns>A new <see cref="HashValue" /> containing a defensive copy of <paramref name="value" />.</returns>
    public static HashValue FromBytes(ReadOnlySpan<byte> value) =>
        value.IsEmpty ? default : new HashValue(value.ToArray());

    /// <summary>
    /// Parses a strict hexadecimal string into a <see cref="HashValue" />.
    /// </summary>
    /// <param name="text">The hexadecimal text. Both uppercase and lowercase digits are accepted.</param>
    /// <returns>A <see cref="HashValue" /> containing the decoded bytes.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="text" /> is <see langword="null" />.</exception>
    /// <exception cref="FormatException">
    /// <paramref name="text" /> has odd length or contains a character that is not an ASCII hexadecimal digit.
    /// </exception>
    /// <remarks>
    /// Parsing is strict: whitespace, separators, and <c>0x</c> prefixes are rejected. An empty string parses to the
    /// empty value.
    /// </remarks>
    public static HashValue ParseHex(string text)
    {
        ThrowHelper.ThrowIfNull(text);
        if (!CryptographyHelper.TryFromHexString(text, out byte[]? bytes)) throw new FormatException(CryptoResourceStrings.Format_Invalid_HexString);

        return bytes.Length == 0 ? default : new HashValue(bytes);
    }

    /// <summary>
    /// Attempts to parse a strict hexadecimal string into a <see cref="HashValue" />.
    /// </summary>
    /// <param name="text">The hexadecimal text, or <see langword="null" />.</param>
    /// <param name="result">
    /// When this method returns <see langword="true" />, the parsed value; otherwise the empty value.
    /// </param>
    /// <returns>
    /// <see langword="true" /> if <paramref name="text" /> is a well-formed hexadecimal string; otherwise,
    /// <see langword="false" />.
    /// </returns>
    public static bool TryParseHex(string? text, out HashValue result)
    {
        result = default;

        if (text is null || !CryptographyHelper.TryFromHexString(text, out byte[]? bytes))
            return false;

        result = bytes.Length == 0 ? default : new HashValue(bytes);
        return true;
    }

    /// <summary>
    /// Returns a read-only view over the digest bytes.
    /// </summary>
    /// <returns>A read-only span over the value; empty for the empty value.</returns>
    public ReadOnlySpan<byte> AsSpan() =>
        _value;

    /// <summary>
    /// Determines whether this hash value equals another.
    /// </summary>
    /// <param name="other">The hash value to compare against.</param>
    /// <returns>
    /// <see langword="true" /> if both values contain identical bytes; otherwise, <see langword="false" />.
    /// </returns>
    /// <remarks>
    /// The byte comparison is constant-time in content — it runs through
    /// <see cref="CryptographicOperations.FixedTimeEquals(ReadOnlySpan{byte}, ReadOnlySpan{byte})" />, so its
    /// duration depends only on the operand length, not on where the bytes first differ. A length mismatch returns
    /// <see langword="false" /> immediately (the length is not secret). <see cref="FixedTimeEquals(HashValue)" />
    /// remains available and behaves identically.
    /// </remarks>
    public bool Equals(HashValue other) =>
        CryptographicOperations.FixedTimeEquals(AsSpan(), other.AsSpan());

    /// <summary>
    /// Determines whether this hash value equals the specified object.
    /// </summary>
    /// <param name="obj">The object to compare against.</param>
    /// <returns>
    /// <see langword="true" /> if <paramref name="obj" /> is a <see cref="HashValue" /> with identical bytes;
    /// otherwise, <see langword="false" />.
    /// </returns>
    public override bool Equals(object? obj) =>
        obj is HashValue other && Equals(other);

    /// <summary>
    /// Compares this hash value to another in fixed time.
    /// </summary>
    /// <param name="other">The hash value to compare against.</param>
    /// <returns>
    /// <see langword="true" /> if both values have the same length and identical bytes; otherwise,
    /// <see langword="false" />.
    /// </returns>
    /// <remarks>
    /// Built on <see cref="CryptographicOperations.FixedTimeEquals(ReadOnlySpan{byte}, ReadOnlySpan{byte})" />: the
    /// comparison time depends only on the length of the operands, not on their content. A length mismatch returns
    /// <see langword="false" /> immediately, so the lengths themselves are not concealed.
    /// </remarks>
    public bool FixedTimeEquals(HashValue other) =>
        CryptographicOperations.FixedTimeEquals(AsSpan(), other.AsSpan());

    /// <summary>
    /// Returns a hash code computed over the digest bytes.
    /// </summary>
    /// <returns>A hash code consistent with <see cref="Equals(HashValue)" />.</returns>
    public override int GetHashCode()
    {
        HashCode hashCode = default;
        hashCode.AddBytes(AsSpan());
        return hashCode.ToHashCode();
    }

    /// <summary>
    /// Copies the digest bytes into a new array.
    /// </summary>
    /// <returns>A new array containing the digest bytes; an empty array for the empty value.</returns>
    public byte[] ToArray() =>
        _value is null ? [] : (byte[])_value.Clone();

    /// <summary>
    /// Formats the hash value as a Base64 string.
    /// </summary>
    /// <returns>The Base64 representation, or <see cref="string.Empty" /> for the empty value.</returns>
    public string ToBase64String() =>
        _value is null ? string.Empty : Convert.ToBase64String(_value);

    /// <summary>
    /// Formats the hash value as a lowercase hexadecimal string.
    /// </summary>
    /// <returns>The lowercase hexadecimal representation, or <see cref="string.Empty" /> for the empty value.</returns>
    public string ToHexString() =>
        CryptographyHelper.ToLowercaseHexString(_value);

    /// <summary>
    /// Returns the lowercase hexadecimal representation of the hash value.
    /// </summary>
    /// <returns>The same text as <see cref="ToHexString" />.</returns>
    /// <remarks>
    /// Hash values are not secrets, so the digest content is intentionally included in the string representation.
    /// </remarks>
    public override string ToString() =>
        ToHexString();
}
