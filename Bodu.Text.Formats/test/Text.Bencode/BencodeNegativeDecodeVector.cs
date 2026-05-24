// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeNegativeDecodeVector.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Encoding;

namespace Bodu.Text.Bencode;

/// <summary>
/// Represents a negative Known Answer Test vector — a malformed bencoded input that the decoder must reject with
/// a specific exception type. Negative vectors capture the contract that <see cref="Bencode" /> makes about which
/// inputs are invalid: leading zeros, missing separators, unsorted dictionary keys, etc.
/// </summary>
/// <param name="Description">A short human-readable label identifying the negative scenario.</param>
/// <param name="EncodedBytes">The malformed encoded bytes.</param>
/// <param name="ExpectedExceptionType">The exception type the decoder must throw on this input.</param>
/// <param name="ExpectedResourceKey">
/// Optional name of the <see cref="FormatsResourceStrings" /> resource that backs the expected exception message,
/// for tests that want to assert message stability.
/// </param>
/// <param name="Source">Optional citation describing the rule the input violates.</param>
public sealed record BencodeNegativeDecodeVector(
    string Description,
    byte[] EncodedBytes,
    Type ExpectedExceptionType,
    string? ExpectedResourceKey = null,
    string? Source = null)
{

    /// <summary>
    /// Builds a negative vector from an ASCII string input expected to fail with
    /// <see cref="BencodeFormatException" />.
    /// </summary>
    /// <param name="description">A short label.</param>
    /// <param name="ascii">The malformed ASCII input.</param>
    /// <param name="expectedResourceKey">The resource key whose value should appear in the thrown exception message.</param>
    /// <param name="source">Optional citation.</param>
    /// <returns>A negative KAT vector.</returns>
    public static BencodeNegativeDecodeVector Ascii(string description, string ascii, string? expectedResourceKey = null, string? source = null) =>
        new(description, System.Text.Encoding.ASCII.GetBytes(ascii), typeof(BencodeFormatException), expectedResourceKey, source);

    /// <summary>
    /// Builds a negative vector from hex-encoded bytes — useful for malformed inputs that contain non-printable
    /// bytes. Reuses <see cref="Base16.Decode(string)" />.
    /// </summary>
    /// <param name="description">A short label.</param>
    /// <param name="hex">The malformed encoded bytes expressed as hex.</param>
    /// <param name="expectedResourceKey">The resource key whose value should appear in the thrown exception message.</param>
    /// <param name="source">Optional citation.</param>
    /// <returns>A negative KAT vector.</returns>
    public static BencodeNegativeDecodeVector FromHex(string description, string hex, string? expectedResourceKey = null, string? source = null) =>
        new(description, Base16.Decode(hex), typeof(BencodeFormatException), expectedResourceKey, source);

    /// <summary>
    /// Returns the human-readable label, used by MSTest's <c>[DynamicData]</c> infrastructure when generating test
    /// names.
    /// </summary>
    /// <returns>The description, optionally suffixed with the citation in square brackets.</returns>
    public override string ToString() =>
        Source is null ? Description : $"{Description} [{Source}]";

}
