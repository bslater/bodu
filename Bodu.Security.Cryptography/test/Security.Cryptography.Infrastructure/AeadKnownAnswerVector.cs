// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AeadKnownAnswerVector.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Kat;

namespace Bodu.Security.Cryptography.Infrastructure;

/// <summary>
/// Represents a single authenticated-encryption known-answer test (KAT) vector — the inputs (key, nonce, associated
/// data, plaintext) and the expected outputs (ciphertext and authentication tag) for one AEAD trial.
/// </summary>
/// <param name="Count">
/// The vector number from the source file (typically 1-based, matching the NIST/LWC convention).
/// </param>
/// <param name="Key">The encryption key.</param>
/// <param name="Nonce">The nonce (also called IV) used for this trial.</param>
/// <param name="AssociatedData">The associated data; empty when the trial has no AAD.</param>
/// <param name="Plaintext">The plaintext input; empty when the trial has no payload.</param>
/// <param name="Ciphertext">The expected ciphertext, excluding the authentication tag.</param>
/// <param name="Tag">
/// The expected authentication tag (typically 16 bytes for the AEAD primitives in this project).
/// </param>
/// <param name="Source">Optional human-readable citation of the source document or file.</param>
public sealed record AeadKnownAnswerVector(
    int Count,
    byte[] Key,
    byte[] Nonce,
    byte[] AssociatedData,
    byte[] Plaintext,
    byte[] Ciphertext,
    byte[] Tag,
    string? Source = null) : IKat
{
    /// <inheritdoc />
    /// <remarks>
    /// AEAD KAT files identify rows by their numeric <see cref="Count" /> rather than a textual label, so the emitted
    /// name embeds <see cref="Count" /> and, when present, the <see cref="Source" /> citation — for example
    /// <c>"NIST LWC #42"</c>.
    /// </remarks>
    string IKat.Name => Source is null
        ? $"Vector {Count}"
        : $"{Source} #{Count}";

    /// <summary>
    /// Constructs an <see cref="AeadKnownAnswerVector" /> from hex-encoded field values, splitting the trailing
    /// <paramref name="tagLength" /> bytes of the combined ciphertext-plus-tag string off as the authentication tag.
    /// </summary>
    /// <param name="count">The vector number from the source file.</param>
    /// <param name="keyHex">The key as a hex string.</param>
    /// <param name="nonceHex">The nonce as a hex string.</param>
    /// <param name="associatedDataHex">The associated data as a hex string; empty when none.</param>
    /// <param name="plaintextHex">The plaintext as a hex string; empty when none.</param>
    /// <param name="ciphertextWithTagHex">
    /// The concatenation of expected ciphertext and tag as a single hex string.
    /// </param>
    /// <param name="tagLength">
    /// The number of trailing bytes in <paramref name="ciphertextWithTagHex" /> that form the tag.
    /// </param>
    /// <param name="source">Optional human-readable citation of the source document or file.</param>
    /// <returns>
    /// A new <see cref="AeadKnownAnswerVector" /> with the trailing bytes of <paramref name="ciphertextWithTagHex" />
    /// split off as the tag.
    /// </returns>
    public static AeadKnownAnswerVector FromHex(
        int count,
        string keyHex,
        string nonceHex,
        string associatedDataHex,
        string plaintextHex,
        string ciphertextWithTagHex,
        int tagLength,
        string? source = null)
    {
        var combined = FromHexBytes(ciphertextWithTagHex);
        var ciphertext = combined.AsSpan(0, combined.Length - tagLength).ToArray();
        var tag = combined.AsSpan(combined.Length - tagLength).ToArray();

        return new AeadKnownAnswerVector(
            count,
            FromHexBytes(keyHex),
            FromHexBytes(nonceHex),
            FromHexBytes(associatedDataHex),
            FromHexBytes(plaintextHex),
            ciphertext,
            tag,
            source);
    }

    /// <inheritdoc />
    public override string ToString() =>
        Source is null ? $"Count={Count}" : $"Count={Count} [{Source}]";

    /// <summary>
    /// Decodes a hex-encoded string into bytes, treating the empty string as a zero-length array.
    /// </summary>
    /// <param name="hex">The hex-encoded string. Must contain an even number of hex characters.</param>
    /// <returns>The decoded byte array.</returns>
    private static byte[] FromHexBytes(string hex) =>
        hex.Length == 0 ? [] : Convert.FromHexString(hex);
}
