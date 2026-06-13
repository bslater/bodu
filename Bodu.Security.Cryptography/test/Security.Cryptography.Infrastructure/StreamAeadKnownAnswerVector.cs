// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StreamAeadKnownAnswerVector.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Kat;

namespace Bodu.Security.Cryptography.Infrastructure;

/// <summary>
/// Represents a provenance-aware known-answer-test vector for the stream-cipher AEAD constructions (XChaCha20-Poly1305
/// and the XSalsa20-Poly1305 family). Vectors are stored with their ciphertext and authentication tag detached; the
/// <see cref="SourceKind" /> records how authoritative the vector is so test labels do not overclaim.
/// </summary>
/// <param name="Id">A stable identifier for the vector.</param>
/// <param name="Algorithm">The construction name the vector exercises; selects the implementation under test.</param>
/// <param name="DisplayName">A human-readable label surfaced in test output.</param>
/// <param name="SourceKind">The provenance category for the vector.</param>
/// <param name="Source">A precise citation of the source document, implementation, or derivation.</param>
/// <param name="KeyHex">The key as lowercase hexadecimal.</param>
/// <param name="NonceHex">The nonce as lowercase hexadecimal.</param>
/// <param name="AadHex">The associated data as lowercase hexadecimal; empty when there is no AAD.</param>
/// <param name="PlaintextHex">The plaintext as lowercase hexadecimal.</param>
/// <param name="CiphertextHex">The expected ciphertext as lowercase hexadecimal, excluding the tag.</param>
/// <param name="TagHex">The expected authentication tag as lowercase hexadecimal.</param>
/// <param name="SourceOutputLayout">The combined-output layout used by the vector's source.</param>
/// <param name="Notes">Optional additional guidance about the vector.</param>
public sealed record StreamAeadKnownAnswerVector(
    string Id,
    string Algorithm,
    string DisplayName,
    AeadKatSourceKind SourceKind,
    string Source,
    string KeyHex,
    string NonceHex,
    string AadHex,
    string PlaintextHex,
    string CiphertextHex,
    string TagHex,
    AeadKatOutputLayout SourceOutputLayout = AeadKatOutputLayout.DetachedTag,
    string? Notes = null) : IKat
{
    /// <inheritdoc />
    string IKat.Name => DisplayName;

    /// <summary>
    /// Gets the decoded key bytes.
    /// </summary>
    /// <returns>The key.</returns>
    public byte[] Key => Convert.FromHexString(KeyHex);

    /// <summary>
    /// Gets the decoded nonce bytes.
    /// </summary>
    /// <returns>The nonce.</returns>
    public byte[] Nonce => Convert.FromHexString(NonceHex);

    /// <summary>
    /// Gets the decoded associated-data bytes.
    /// </summary>
    /// <returns>The associated data; an empty array when the vector has no AAD.</returns>
    public byte[] AssociatedData => AadHex.Length == 0 ? [] : Convert.FromHexString(AadHex);

    /// <summary>
    /// Gets the decoded plaintext bytes.
    /// </summary>
    /// <returns>The plaintext.</returns>
    public byte[] Plaintext => Convert.FromHexString(PlaintextHex);

    /// <summary>
    /// Gets the decoded ciphertext bytes, excluding the tag.
    /// </summary>
    /// <returns>The ciphertext.</returns>
    public byte[] Ciphertext => Convert.FromHexString(CiphertextHex);

    /// <summary>
    /// Gets the decoded authentication-tag bytes.
    /// </summary>
    /// <returns>The tag.</returns>
    public byte[] Tag => Convert.FromHexString(TagHex);

    /// <summary>
    /// Gets the ciphertext followed by the tag, matching the wire format emitted by the Bodu AEAD transforms.
    /// </summary>
    /// <returns>The concatenation of <see cref="Ciphertext" /> and <see cref="Tag" />.</returns>
    public byte[] CiphertextWithTag => [.. Ciphertext, .. Tag];

    /// <inheritdoc />
    public override string ToString() => DisplayName;
}
