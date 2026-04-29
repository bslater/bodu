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
/// layout: little-endian word packing and no external IP/FP permutation.
/// </para>
/// <para>
/// Serpent-128 is non-tweakable and exposes a single configuration, so the accessor uses
/// <see cref="SingleTestVariant" />. The wide-block Serpent variants (256 / 512 / 1024) are tweakable
/// experimental constructions and live in their own KAT files.
/// </para>
/// <para>
/// TODO(gh-145): Extend to the full Bouncy Castle Serpent test set plus chained vectors from the original
/// AES submission. See <see cref="BlockCipherKnownAnswer" /> for the gap policy.
/// </para>
/// </remarks>
internal static class Serpent128KnownAnswers
{
    /// <summary>
    /// Returns the curated KAT vectors for <paramref name="variant" />. Serpent-128 publishes a single
    /// reference vector; the variant parameter is reserved for parity with other non-variant cipher families.
    /// </summary>
    public static IReadOnlyList<BlockCipherKnownAnswer> For(SingleTestVariant variant) => Default;

    private const string ProfileBouncyCastle = "Bouncy Castle SerpentTest.java";

    private static readonly BlockCipherKnownAnswer[] Default =
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
