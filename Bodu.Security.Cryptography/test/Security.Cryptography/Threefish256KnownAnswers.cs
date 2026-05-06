// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Threefish256KnownAnswers.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Holds the curated <see cref="Threefish256Cipher" /> known-answer test vectors used by
/// <see cref="Threefish256CipherTests" />. Each row pins a (key, tweak, plaintext, ciphertext) tuple so the
/// vectors remain self-contained: subsequent changes to the variant specification cannot silently
/// invalidate the captured ciphertext.
/// </summary>
/// <remarks>
/// <para>
/// The two vectors mirror <see cref="TweakableBlockCipherVariant.ZeroedKeyAndTweak" /> — an all-zero
/// (key, tweak, plaintext) baseline — and <see cref="TweakableBlockCipherVariant.DefaultKeyAndTweak" /> —
/// the harness's incremental-byte default (key bytes 0x10..0x2F, tweak bytes 0x00..0x0F, descending
/// plaintext FF..E0). Both rows have been independently verified against the Skein 1.3 / NIST SHA-3
/// submission reference (<c>wernerd/Skein3Fish</c> <c>ThreefishTest.java</c>): each captured
/// little-endian byte stream equals the byte serialisation of the reference's published 64-bit word
/// values, and the non-zero row matches the reference's Matyas-Meyer-Oseas feed-forward
/// (<c>Encrypt(plaintext) ⊕ plaintext</c>) byte-for-byte across all four words.
/// </para>
/// </remarks>
internal static class Threefish256KnownAnswers
{
    /// <summary>
    /// Returns the curated KAT vector for <paramref name="variant" />.
    /// </summary>
    public static IReadOnlyList<BlockCipherKnownAnswer> For(TweakableBlockCipherVariant variant) => variant switch
    {
        TweakableBlockCipherVariant.ZeroedKeyAndTweak => ZeroedKeyAndTweak,
        TweakableBlockCipherVariant.DefaultKeyAndTweak => DefaultKeyAndTweak,
        _ => throw new ArgumentOutOfRangeException(nameof(variant), variant, null),
    };

    private const string ProfileInTreeRegression = "Skein 1.3 / NIST SHA-3 reference (verified equivalent)";

    private static readonly BlockCipherKnownAnswer[] ZeroedKeyAndTweak =
    [
        new BlockCipherKnownAnswer
        {
            Name = "Threefish256_ZeroKeyZeroTweak_ZeroPlaintext",
            Profile = ProfileInTreeRegression,
            Plaintext = new byte[32],
            Ciphertext = Convert.FromHexString("84DA2A1F8BEAEE947066AE3E3103F1AD536DB1F4A1192495116B9F3CE6133FD8"),
            Key = new byte[32],
            Tweak = new byte[16],
        },
    ];

    private static readonly BlockCipherKnownAnswer[] DefaultKeyAndTweak =
    [
        new BlockCipherKnownAnswer
        {
            Name = "Threefish256_IncrementalKey_IncrementalTweak_DescendingPlaintext",
            Profile = ProfileInTreeRegression,
            Plaintext = Convert.FromHexString(
                "FFFEFDFCFBFAF9F8F7F6F5F4F3F2F1F0EFEEEDECEBEAE9E8E7E6E5E4E3E2E1E0"),
            Ciphertext = Convert.FromHexString(
                "E0D091FF0EEA8FDFC98192E62ED80AD59D865D08588DF476657056B5955E97DF"),
            Key = TestHelpers.GenerateIncrementalByteSequence(0x10, 32),
            Tweak = TestHelpers.GenerateIncrementalByteSequence(0x00, 16),
        },
    ];
}
