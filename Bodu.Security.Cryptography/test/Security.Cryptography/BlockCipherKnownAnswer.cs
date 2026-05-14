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
/// <b>Block-cipher layer</b> — driven by <see cref="BlockCipherTests{TTest, TCipher, TVariant}" />. Each
/// per-cipher test class overrides two simple hooks: <c>GetKnownAnswers(variant)</c> returns the curated
/// vector list, and <c>CreateBlockCipherForAnswer(answer)</c> constructs an engine that applies the row's
/// key and (where applicable) tweak. The base class wires those hooks together via
/// <c>AdaptKnownAnswers</c> to drive the encrypt and decrypt assertions; concrete classes need only
/// override <c>GetKnownAnswerTests</c> directly when their vectors are runtime-generated rather than
/// static (for example the wide-block Serpent self-referential regression rows).
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
/// <para>
/// <b>Minimum vector set.</b> Every cipher's <c>&lt;Cipher&gt;KnownAnswers</c> file should contain at least:
/// </para>
/// <list type="bullet">
/// <item><description>An all-zero key, all-zero plaintext row — basic regression sanity.</description></item>
/// <item><description>An all-ones (or all-FF) key, all-ones plaintext row — regression sanity at the
/// opposite extreme.</description></item>
/// <item><description>One row sourced from a published reference (RFC, NIST, AES submission, vendor) where
/// vectors are available. Set <see cref="Profile" /> to identify the source.</description></item>
/// <item><description>(Recommended) at least one chained or multi-vector sequence to exercise key-schedule
/// stability across iterations.</description></item>
/// </list>
/// <para>
/// <b>Gap policy.</b> When a cipher has no published reference vectors, do <i>not</i> fabricate in-tree
/// regression baselines as a substitute. Instead, file a GitHub tracking issue and reference it from a
/// <c>TODO(gh-NNN):</c> comment in the per-cipher <c>&lt;Cipher&gt;KnownAnswers</c> file's
/// <c>&lt;remarks&gt;</c>. This keeps the gap visible and the test data honest about its provenance.
/// In-tree regression baselines that pre-date this policy should be tagged with their tracking issue and
/// kept until authoritative vectors are sourced.
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
    /// example <c>"RFC 3713 Appendix A"</c>, <c>"NIST AES KAT"</c>, or <c>"NESSIE submission"</c>.
    /// Surfaced in failure diagnostics so a test report makes the provenance obvious.
    /// </summary>
    public string? Profile { get; init; }
}
