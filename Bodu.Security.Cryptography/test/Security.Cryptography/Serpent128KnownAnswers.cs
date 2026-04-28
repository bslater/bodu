// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Serpent128KnownAnswers.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Holds the curated <see cref="Serpent128Cipher" /> known-answer test vectors used by
/// <see cref="Serpent128CipherTests" />. The single canonical vector is transcribed from the Bouncy Castle
/// (bc-java) <c>SerpentTest.java</c> reference, derived from the Anderson / Biham / Knudsen AES submission.
/// </summary>
/// <remarks>
/// <para>
/// This vector pins the byte-order and key-schedule conventions to the standard Linux kernel / Bouncy Castle
/// layout: little-endian word packing and no external IP/FP permutation. The
/// <see cref="SerpentCipherTestVariant.ZeroedKeyAndTweak" /> variant ships no published KAT and contributes
/// only to inherited round-trip and determinism coverage.
/// </para>
/// <para>
/// The wide-block variants (Serpent-256, Serpent-512, Serpent-1024) are non-standard constructions with no
/// published reference vectors and intentionally do not have their own <c>KnownAnswers</c> data files. They
/// rely on self-referential regression vectors generated at test-discovery time, which cannot be expressed
/// as static hex literals without first running the cipher engine being tested.
/// </para>
/// </remarks>
internal static class Serpent128KnownAnswers
{
    /// <summary>
    /// Returns the curated KAT vectors for <paramref name="variant" />. Only
    /// <see cref="SerpentCipherTestVariant.DefaultKeyAndTweak" /> carries a published vector; the zeroed
    /// variant returns an empty list.
    /// </summary>
    public static IReadOnlyList<BlockCipherKnownAnswer> For(SerpentCipherTestVariant variant) => variant switch
    {
        SerpentCipherTestVariant.ZeroedKeyAndTweak => Array.Empty<BlockCipherKnownAnswer>(),
        SerpentCipherTestVariant.DefaultKeyAndTweak => DefaultKeyAndTweak,
        _ => throw new ArgumentOutOfRangeException(nameof(variant), variant, null),
    };

    private const string ProfileBouncyCastle = "Bouncy Castle SerpentTest.java";

    private static readonly BlockCipherKnownAnswer[] DefaultKeyAndTweak =
    [
        new BlockCipherKnownAnswer
        {
            Name = "Serpent128_BouncyCastle_IncrementingKey",
            Profile = ProfileBouncyCastle,
            Plaintext = Convert.FromHexString("00112233445566778899AABBCCDDEEFF"),
            Ciphertext = Convert.FromHexString("563E2CF8740A27C164804560391E9B27"),
            Key = Convert.FromHexString("000102030405060708090A0B0C0D0E0F"),
        },
    ];
}
