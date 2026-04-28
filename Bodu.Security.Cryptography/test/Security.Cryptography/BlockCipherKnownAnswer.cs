// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BlockCipherKnownAnswer.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Represents a single known-answer test vector for a symmetric block cipher — a named plaintext / ciphertext
/// pair together with the per-row key (and, for tweakable ciphers, the per-row tweak) that produced the
/// expected output. Mirrors the role of <see cref="KeyedHashAlgorithmKnownAnswer" /> on the keyed-hash side
/// so cipher and hash families share the same data-driven KAT idiom.
/// </summary>
/// <remarks>
/// <para>
/// Vectors are typically stored as named static fields in a per-cipher static class (for example
/// <c>SkipjackKnownAnswers</c>) and exposed via a <c>For(variant)</c> accessor. The cipher test base class
/// adapts each entry into a runnable test row, supplying a factory that constructs the cipher under test
/// from <see cref="Key" /> and (when applicable) <see cref="Tweak" />.
/// </para>
/// <para>
/// When <see cref="Key" /> is <see langword="null" /> the harness falls back to the variant's default
/// <c>TestKey</c> from <see cref="BlockCipherSpecification" />. <see cref="Tweak" /> is ignored by
/// non-tweakable cipher families and should be left <see langword="null" /> for them.
/// </para>
/// </remarks>
public sealed record BlockCipherKnownAnswer
{
    /// <summary>Gets the semantic name of this test vector used in diagnostic messages.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the plaintext input bytes — the value fed to <c>Encrypt</c> and the value
    /// expected from <c>Decrypt</c> when applied to <see cref="Ciphertext" />.</summary>
    public required byte[] Plaintext { get; init; }

    /// <summary>Gets the ciphertext expected from <c>Encrypt</c>(<see cref="Plaintext" />), which is
    /// also the value fed to <c>Decrypt</c> during the round-trip assertion.</summary>
    public required byte[] Ciphertext { get; init; }

    /// <summary>
    /// Gets the per-row key applied to the cipher before encrypting <see cref="Plaintext" />, or
    /// <see langword="null" /> when the variant's default <c>TestKey</c> from
    /// <see cref="BlockCipherSpecification" /> should be used.
    /// </summary>
    /// <value>The key bytes, or <see langword="null" /> to defer to the variant default.</value>
    public byte[]? Key { get; init; }

    /// <summary>
    /// Gets the per-row tweak applied to the cipher before encrypting <see cref="Plaintext" />, or
    /// <see langword="null" /> for non-tweakable ciphers and for tweakable ciphers that should fall
    /// back to the variant's default <c>TestTweak</c>.
    /// </summary>
    public byte[]? Tweak { get; init; }

    /// <summary>
    /// Gets an optional free-form profile tag describing the source or category of this vector — for
    /// example <c>"RFC 3713 Appendix A"</c>, <c>"NIST AES KAT"</c>, or <c>"Bouncy Castle reference"</c>.
    /// Surfaced in failure diagnostics so a test report makes the provenance obvious.
    /// </summary>
    public string? Profile { get; init; }
}
