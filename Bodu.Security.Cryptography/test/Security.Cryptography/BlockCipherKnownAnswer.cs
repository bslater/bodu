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
/// <see cref="BlockCipherKnownAnswer" /> is the keystone of the cipher test architecture. The same vector
/// data flows through three independent test layers, anchoring observable behaviour at every level of the
/// public surface:
/// </para>
/// <list type="bullet">
/// <item>
/// <description>
/// <b>Block-cipher layer</b> — driven by <see cref="BlockCipherTests{TTest, TCipher, TVariant}" /> and its
/// <c>GetKnownAnswerTests(variant)</c> override. Each per-cipher test class delegates to
/// <c>BlockCipherTests&lt;,,&gt;.AdaptKnownAnswers</c>, which builds runnable
/// <c>KnownAnswerTest</c> rows from a sequence of <see cref="BlockCipherKnownAnswer" /> records and a
/// per-cipher engine factory. Asserts <c>IBlockCipher.Encrypt(plaintext) == ciphertext</c> and the
/// reverse for decrypt.
/// </description>
/// </item>
/// <item>
/// <description>
/// <b>Transform layer</b> — driven by <see cref="BlockCipherTransformTests{TTest, TCryptoTransform}" /> via
/// the <c>GetKnownAnswers</c> + <c>CreateTransformForKnownAnswer</c> hooks. Each per-cipher transform test
/// class returns the same vector list, and the harness runs
/// <c>ICryptoTransform.TransformFinalBlock(plaintext)</c> in raw single-block ECB mode so the result must
/// match the vector's ciphertext byte-for-byte.
/// </description>
/// </item>
/// <item>
/// <description>
/// <b>Algorithm layer</b> — driven by <see cref="SymmetricAlgorithmTests{TTest, TAlgorithm}" /> via the
/// <c>GetKnownAnswers</c> + <c>CreateAlgorithmForKnownAnswer</c> hooks. The harness configures the public
/// <c>SymmetricAlgorithm</c> instance with the vector's <see cref="Key" /> (and <see cref="Tweak" /> for
/// tweakable algorithms), calls <c>CreateEncryptor()</c> / <c>CreateDecryptor()</c>, and asserts the same
/// plaintext-to-ciphertext mapping holds through the consumer-facing API.
/// </description>
/// </item>
/// </list>
/// <para>
/// Vectors are stored as named static fields in a per-cipher static class — for example
/// <c>SkipjackKnownAnswers</c>, <c>CamelliaKnownAnswers</c>, <c>Threefish256KnownAnswers</c> — and exposed
/// via a <c>For(variant)</c> accessor whose <c>variant</c> parameter is one of <c>SingleTestVariant</c>,
/// <see cref="BlockCipherKeyVariant" />, or <see cref="TweakableBlockCipherVariant" /> depending on what
/// configuration shape the family supports. Each layer's KAT-driven test method consumes the same
/// accessor, so a single curated vector list anchors every layer of coverage for that cipher.
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
