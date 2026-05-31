// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Serpent128CipherTests.KnownAnswers.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Curated <see cref="Serpent128Cipher" /> known-answer test vectors. Transcribed verbatim from the Serpent NESSIE
/// submission by Ross Anderson, Eli Biham, and Lars Knudsen — the same vectors that accompanied the AES candidate
/// submission and are echoed across every interoperable Serpent implementation.
/// </summary>
/// <remarks>
/// <para>
/// All vectors pin the byte-order and key-schedule conventions to the canonical Serpent layout from the reference
/// submission: little-endian word packing and no external IP/FP permutation.
/// </para>
/// <para>
/// Serpent-128 is non-tweakable and exposes a single configuration, so the accessor uses
/// <see cref="SingleTestVariant" />. The wide-block Serpent variants (256 / 512 / 1024) are tweakable experimental
/// constructions and live in their own partials.
/// </para>
/// <para>
/// The corpus combines five 128-bit-key entries from the NESSIE submission's <c>BlockCipherVectorTest</c> fixture
/// (tests 0, 1, 2, 9, and 10 — the entries whose key length is 16 bytes) with one additional row capturing the
/// encryption of the AES standard plaintext under the incrementing-byte key. Together they exercise the all-zero
/// baseline, the high-bit avalanche case, a self-similar repeating-byte key/plaintext, and the two AES-standard
/// reverse-engineered vectors that round-trip the canonical AES test pattern.
/// </para>
/// </remarks>
/// <seealso href="https://www.cl.cam.ac.uk/~rja14/serpent.html">Serpent home page (Anderson / Biham / Knudsen)</seealso>
internal sealed partial class Serpent128CipherTests
{
    private const string ProfileNessie = "Serpent NESSIE submission (Anderson / Biham / Knudsen)";

    private static readonly BlockCipherKnownAnswer[] DefaultKnownAnswers =
    [
        new BlockCipherKnownAnswer
        {
            Name = "Serpent128_Nessie_Test0_ZeroKey_ZeroPlain",
            Profile = ProfileNessie,
            Plaintext = Convert.FromHexString("00000000000000000000000000000000"),
            Ciphertext = Convert.FromHexString("3620B17AE6A993D09618B8768266BAE9"),
            Key = Convert.FromHexString("00000000000000000000000000000000"),
        },
        new BlockCipherKnownAnswer
        {
            Name = "Serpent128_Nessie_Test1_HighBitKey_ZeroPlain",
            Profile = ProfileNessie,
            Plaintext = Convert.FromHexString("00000000000000000000000000000000"),
            Ciphertext = Convert.FromHexString("264E5481EFF42A4606ABDA06C0BFDA3D"),
            Key = Convert.FromHexString("80000000000000000000000000000000"),
        },
        new BlockCipherKnownAnswer
        {
            Name = "Serpent128_Nessie_Test2_RepeatingD9",
            Profile = ProfileNessie,
            Plaintext = Convert.FromHexString("D9D9D9D9D9D9D9D9D9D9D9D9D9D9D9D9"),
            Ciphertext = Convert.FromHexString("20EA07F19C8E93FDA30F6B822AD5D486"),
            Key = Convert.FromHexString("D9D9D9D9D9D9D9D9D9D9D9D9D9D9D9D9"),
        },
        new BlockCipherKnownAnswer
        {
            Name = "Serpent128_Nessie_Test9_IncrementingKey_AesStdPlain",
            Profile = ProfileNessie,
            Plaintext = Convert.FromHexString("33B3DC87EDDD9B0F6A1F407D14919365"),
            Ciphertext = Convert.FromHexString("00112233445566778899AABBCCDDEEFF"),
            Key = Convert.FromHexString("000102030405060708090A0B0C0D0E0F"),
        },
        new BlockCipherKnownAnswer
        {
            Name = "Serpent128_Nessie_Test10_AesStdCt",
            Profile = ProfileNessie,
            Plaintext = Convert.FromHexString("BEB6C069393822D3BE73FF30525EC43E"),
            Ciphertext = Convert.FromHexString("EA024714AD5C4D84EA024714AD5C4D84"),
            Key = Convert.FromHexString("2BD6459F82C5B300952C49104881FF48"),
        },
        new BlockCipherKnownAnswer
        {
            Name = "Serpent128_IncrementingKey_AesStdPlaintext",
            Profile = ProfileNessie,
            Plaintext = Convert.FromHexString("00112233445566778899AABBCCDDEEFF"),
            Ciphertext = Convert.FromHexString("563E2CF8740A27C164804560391E9B27"),
            Key = Convert.FromHexString("000102030405060708090A0B0C0D0E0F"),
        },
    ];

    /// <summary>
    /// Returns the curated KAT vectors for <paramref name="variant" />. Serpent-128 publishes a single
    /// configuration; the variant parameter is reserved for parity with other non-variant cipher families.
    /// </summary>
    private static IReadOnlyList<BlockCipherKnownAnswer> KnownAnswersFor(SingleTestVariant variant) => DefaultKnownAnswers;
}
