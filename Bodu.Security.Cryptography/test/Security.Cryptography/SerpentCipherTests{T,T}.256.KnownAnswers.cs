// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SerpentCipherTests{T,T}.256.KnownAnswers.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Curated <see cref="Serpent256Cipher" /> known-answer test vectors. Each row pins a (key, tweak, plaintext,
/// ciphertext) tuple so the vectors remain self-contained: subsequent changes to the wide-block construction
/// cannot silently invalidate the captured ciphertext.
/// </summary>
/// <remarks>
/// <para>
/// Serpent-256 is a non-standard tweakable Serpent construction developed for this library — it has no externally
/// published reference vectors. The captured ciphertexts were cross-validated against an independent Python port
/// of the wide-block round function (see <c>tools/cipher-vectors/wide_serpent.py</c>), which is hand-translated
/// from the C# source and exercises the same Serpent S-boxes, bitsliced linear transform, prekey recurrence,
/// cross-lane rotation, and five-word tweak schedule. Both implementations agree on the rows below.
/// </para>
/// <para>
/// The two vectors mirror <see cref="TweakableBlockCipherVariant.ZeroedKeyAndTweak" /> — an all-zero
/// (key, tweak, plaintext) baseline — and <see cref="TweakableBlockCipherVariant.DefaultKeyAndTweak" /> — the
/// harness's incremental-byte default (key bytes 0x10..0x2F, tweak bytes 0x00..0x0F, descending plaintext
/// FF..E0).
/// </para>
/// </remarks>
internal sealed partial class Serpent256CipherTests
{
    private const string ProfileCrossValidated =
        "Cross-validated against tools/cipher-vectors/wide_serpent.py (independent Python port)";

    private static readonly BlockCipherKnownAnswer[] ZeroedKeyAndTweakKnownAnswers =
    [
        new BlockCipherKnownAnswer
        {
            Name = "Serpent256_ZeroKeyZeroTweak_ZeroPlaintext",
            Profile = ProfileCrossValidated,
            Plaintext = new byte[32],
            Ciphertext = Convert.FromHexString(
                "79A23C5889F070C99DEDC6CC9806A29A98A3F2B854B61D719C1FA832ADF900D0"),
            Key = new byte[32],
            Tweak = new byte[16],
        },
    ];

    private static readonly BlockCipherKnownAnswer[] DefaultKeyAndTweakKnownAnswers =
    [
        new BlockCipherKnownAnswer
        {
            Name = "Serpent256_IncrementalKey_IncrementalTweak_DescendingPlaintext",
            Profile = ProfileCrossValidated,
            Plaintext = Convert.FromHexString(
                "FFFEFDFCFBFAF9F8F7F6F5F4F3F2F1F0EFEEEDECEBEAE9E8E7E6E5E4E3E2E1E0"),
            Ciphertext = Convert.FromHexString(
                "E51B93C7DD7D8AB64FEF7AE1A231296A55DA11463B91C4110E2E243D705A4517"),
            Key = TestHelpers.GenerateIncrementalByteSequence(0x10, 32),
            Tweak = TestHelpers.GenerateIncrementalByteSequence(0x00, 16),
        },
    ];

    /// <summary>
    /// Returns the curated KAT vector for <paramref name="variant" />.
    /// </summary>
    private static IReadOnlyList<BlockCipherKnownAnswer> KnownAnswersFor(TweakableBlockCipherVariant variant) => variant switch
    {
        TweakableBlockCipherVariant.ZeroedKeyAndTweak => ZeroedKeyAndTweakKnownAnswers,
        TweakableBlockCipherVariant.DefaultKeyAndTweak => DefaultKeyAndTweakKnownAnswers,
        _ => throw new ArgumentOutOfRangeException(nameof(variant), variant, null),
    };
}
