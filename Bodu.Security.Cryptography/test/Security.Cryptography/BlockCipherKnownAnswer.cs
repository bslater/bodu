// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BlockCipherKnownAnswer.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Kat;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Represents a single known-answer test vector for a symmetric block cipher — a named plaintext / ciphertext pair
/// together with the per-row key (and, for tweakable ciphers, the per-row tweak) that produced the expected output.
/// Mirrors the role of <see cref="KeyedHashAlgorithmKnownAnswer" /> on the keyed-hash side so cipher and hash families
/// share the same data-driven KAT idiom.
/// </summary>
/// <remarks>
/// <para>
/// KAT byte-level assertion lives at a single tier: <see cref="BlockCipherTests{TTest, TCipher, TVariant}" />. Each
/// per-cipher test class overrides <c>GetKnownAnswers(variant)</c> to surface the curated vectors and
/// <c>CreateBlockCipherForAnswer(answer)</c> to construct an engine seeded with the row's key (and tweak, where
/// applicable); the base class drives encrypt / decrypt assertions against the row's expected ciphertext. The
/// transform-tier and algorithm-tier suites instead exercise their public contracts plus a generic round-trip — cipher
/// correctness flows up through delegation rather than through duplicate KAT byte-equality checks.
/// </para>
/// <para>
/// Vectors are colocated with their consumer test class as a <c>*.KnownAnswers.cs</c> partial — for example
/// <see cref="SkipjackBlockCipherTests" />, <see cref="CamelliaBlockCipherTests" />,
/// <see cref="Threefish256CipherTests" /> — and exposed through a private <c>KnownAnswersFor(variant)</c> accessor
/// whose <c>variant</c> parameter is one of <see cref="SingleTestVariant" />, <see cref="BlockCipherKeyVariant" />, or
/// <see cref="TweakableBlockCipherVariant" /> depending on what configuration shape the family supports.
/// </para>
/// <para>
/// When <see cref="Key" /> is <see langword="null" /> the harness falls back to the variant's default <c>TestKey</c>
/// from <see cref="BlockCipherSpecification" />. <see cref="Tweak" /> is ignored by non-tweakable cipher families and
/// should be left <see langword="null" /> for them.
/// </para>
/// <para>
/// <b>Minimum vector set.</b> Every cipher's <c>*BlockCipherTests.KnownAnswers.cs</c> (or
/// <c>*CipherTests.KnownAnswers.cs</c>) partial should contain at least:
/// </para>
/// <list type="bullet">
/// <item>
/// <description>
/// An all-zero key, all-zero plaintext row — basic regression sanity.
/// </description>
/// </item>
/// <item>
/// <description>
/// An all-ones (or all-FF) key, all-ones plaintext row — regression sanity at the opposite extreme.
/// </description>
/// </item>
/// <item>
/// <description>
/// One row sourced from a published reference (RFC, NIST, AES submission, vendor) where vectors are available. Set
/// <see cref="Profile" /> to identify the source.
/// </description>
/// </item>
/// <item>
/// <description>
/// (Recommended) at least one chained or multi-vector sequence to exercise key-schedule stability across iterations.
/// </description>
/// </item>
/// </list>
/// <para>
/// <b>Gap policy.</b> When a cipher has no published reference vectors, do <i>not</i> fabricate in-tree regression
/// baselines as a substitute. Instead, file a GitHub tracking issue and reference it from a <c>TODO(gh-NNN):</c>
/// comment in the per-cipher partial's <c>&lt;remarks&gt;</c>. This keeps the gap visible and the test data honest
/// about its provenance. In-tree regression baselines that pre-date this policy should be tagged with their tracking
/// issue and kept until authoritative vectors are sourced.
/// </para>
/// </remarks>
public sealed record BlockCipherKnownAnswer : IKat
{
    /// <summary>
    /// Gets the semantic name of this test vector used in diagnostic messages.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the plaintext input bytes — the value fed to <c>Encrypt</c> and the value expected from <c>Decrypt</c> when
    /// applied to <see cref="Ciphertext" />.
    /// </summary>
    public required byte[] Plaintext { get; init; }

    /// <summary>
    /// Gets the ciphertext expected from <c>Encrypt</c>(<see cref="Plaintext" />), which is also the value fed to
    /// <c>Decrypt</c> during the round-trip assertion.
    /// </summary>
    public required byte[] Ciphertext { get; init; }

    /// <summary>
    /// Gets the per-row key applied to the cipher before encrypting <see cref="Plaintext" />, or
    /// <see langword="null" /> when the variant's default <c>TestKey</c> from <see cref="BlockCipherSpecification" />
    /// should be used.
    /// </summary>
    /// <value>The key bytes, or <see langword="null" /> to defer to the variant default.</value>
    public byte[]? Key { get; init; }

    /// <summary>
    /// Gets the per-row tweak applied to the cipher before encrypting <see cref="Plaintext" />, or
    /// <see langword="null" /> for non-tweakable ciphers and for tweakable ciphers that should fall back to the
    /// variant's default <c>TestTweak</c>.
    /// </summary>
    public byte[]? Tweak { get; init; }

    /// <summary>
    /// Gets an optional free-form profile tag describing the source or category of this vector — for example
    /// <c>"RFC 3713 Appendix A"</c>, <c>"NIST AES KAT"</c>, or <c>"NESSIE submission"</c>. Surfaced in failure
    /// diagnostics so a test report makes the provenance obvious.
    /// </summary>
    public string? Profile { get; init; }
}
