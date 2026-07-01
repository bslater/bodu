// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SerpentCipherTests{T,T}.512.KnownAnswers.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Security.Cryptography.Infrastructure;
using Bodu.Test;
namespace Bodu.Security.Cryptography;

/// <summary>
/// Curated <see cref="Serpent512Cipher" /> known-answer test vectors. Each row pins a (key, tweak, plaintext,
/// ciphertext) tuple so the vectors remain self-contained: subsequent changes to the wide-block construction
/// cannot silently invalidate the captured ciphertext.
/// </summary>
/// <remarks>
/// <para>
/// Serpent-512 is a non-standard tweakable Serpent construction developed for this library — it has no externally
/// published reference vectors. The captured ciphertexts were cross-validated against an independent Python port
/// of the wide-block round function (see <c>tools/cipher-vectors/wide_serpent.py</c>), which is hand-translated
/// from the C# source and exercises the same Serpent S-boxes, bitsliced linear transform, prekey recurrence,
/// cross-lane rotation, and five-word tweak schedule. Both implementations agree on the rows below.
/// </para>
/// <para>
/// The two vectors mirror <see cref="TweakableBlockCipherVariant.ZeroedKeyAndTweak" /> — an all-zero
/// (key, tweak, plaintext) baseline — and <see cref="TweakableBlockCipherVariant.DefaultKeyAndTweak" /> — the
/// harness's incremental-byte default (key bytes 0x10..0x4F, tweak bytes 0x00..0x0F, descending plaintext
/// FF..C0).
/// </para>
/// </remarks>
internal sealed partial class Serpent512CipherTests
{
    private static readonly KatProvenance ProfileCrossValidated =
        KatProvenance.DerivedOracle("Cross-validated against tools/cipher-vectors/wide_serpent.py (independent Python port)");

    private static readonly BlockCipherKnownAnswer[] ZeroedKeyAndTweakKnownAnswers =
    [
        new BlockCipherKnownAnswer
        {
            Name = "Serpent512_ZeroKeyZeroTweak_ZeroPlaintext",
            Provenance = ProfileCrossValidated,
            Plaintext = new byte[64],
            Ciphertext = Convert.FromHexString(
                "90A72CF7757CC9C74F00EB0CAFE661DF2315012465594D1393BC780B83CD91AD" +
                "AC8F7F7A427226B78BE412CC7AFB4DF9519935B75F0E4B429204F64806A86EA5"),
            Key = new byte[64],
            Tweak = new byte[16],
        },
    ];

    private static readonly BlockCipherKnownAnswer[] DefaultKeyAndTweakKnownAnswers =
    [
        new BlockCipherKnownAnswer
        {
            Name = "Serpent512_IncrementalKey_IncrementalTweak_DescendingPlaintext",
            Provenance = ProfileCrossValidated,
            Plaintext = Convert.FromHexString(
                "FFFEFDFCFBFAF9F8F7F6F5F4F3F2F1F0EFEEEDECEBEAE9E8E7E6E5E4E3E2E1E0" +
                "DFDEDDDCDBDAD9D8D7D6D5D4D3D2D1D0CFCECDCCCBCAC9C8C7C6C5C4C3C2C1C0"),
            Ciphertext = Convert.FromHexString(
                "08DF808DFAA6F470D3E91BBAA92E3B8E2B54179419F7C629B7FB623B710B155F" +
                "10F6702CFF09D0306C21B7BC8A8402B65606B734CFBA00696870C5204C62372F"),
            Key = TestHelpers.GenerateIncrementalByteSequence(0x10, 64),
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
