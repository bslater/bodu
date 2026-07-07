// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThreefishCipherTests{T,T}.256.KnownAnswers.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Security.Cryptography.Infrastructure;
using Bodu.Test;
namespace Bodu.Security.Cryptography;

/// <summary>
/// Curated <see cref="Threefish256Cipher" /> known-answer test vectors. Each row pins a (key, tweak, plaintext,
/// ciphertext) tuple so the vectors remain self-contained: subsequent changes to the variant specification cannot
/// silently invalidate the captured ciphertext.
/// </summary>
/// <remarks>
/// The two vectors mirror <see cref="TweakableBlockCipherVariant.ZeroedKeyAndTweak" /> — an all-zero
/// (key, tweak, plaintext) baseline — and <see cref="TweakableBlockCipherVariant.DefaultKeyAndTweak" /> — the
/// harness's incremental-byte default (key bytes 0x10..0x2F, tweak bytes 0x00..0x0F, descending plaintext
/// FF..E0). The non-zero row is confirmed byte-for-byte against the Skein/Threefish golden KAT as mirrored in
/// Crypto++ <c>threefish.txt</c> (Test Vector 6): each field is the little-endian byte serialisation of the
/// published 64-bit word values, matching Threefish's byte/word convention. The all-zero baseline row is an
/// in-tree regression capture (not present in the external mirror).
/// </remarks>
internal sealed partial class Threefish256CipherTests
{
    private static readonly KatProvenance ProfileInTreeRegression = KatProvenance.InternalRegression("Skein 1.3 / NIST SHA-3 reference (verified equivalent)");
    private static readonly KatProvenance ProfileCryptoPpMirror = KatProvenance.ReferenceImplementation("Crypto++ threefish.txt (Skein golden KAT, Test Vector 6); little-endian word64 decode, verified byte-for-byte");

    private static readonly BlockCipherKnownAnswer[] ZeroedKeyAndTweakKnownAnswers =
    [
        new BlockCipherKnownAnswer
        {
            Name = "Threefish256_ZeroKeyZeroTweak_ZeroPlaintext",
            Provenance = ProfileInTreeRegression,
            Plaintext = new byte[32],
            Ciphertext = Convert.FromHexString("84DA2A1F8BEAEE947066AE3E3103F1AD536DB1F4A1192495116B9F3CE6133FD8"),
            Key = new byte[32],
            Tweak = new byte[16],
        },
    ];

    private static readonly BlockCipherKnownAnswer[] DefaultKeyAndTweakKnownAnswers =
    [
        new BlockCipherKnownAnswer
        {
            Name = "Threefish256_IncrementalKey_IncrementalTweak_DescendingPlaintext",
            Provenance = ProfileCryptoPpMirror,
            Plaintext = Convert.FromHexString(
                "FFFEFDFCFBFAF9F8F7F6F5F4F3F2F1F0EFEEEDECEBEAE9E8E7E6E5E4E3E2E1E0"),
            Ciphertext = Convert.FromHexString(
                "E0D091FF0EEA8FDFC98192E62ED80AD59D865D08588DF476657056B5955E97DF"),
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
