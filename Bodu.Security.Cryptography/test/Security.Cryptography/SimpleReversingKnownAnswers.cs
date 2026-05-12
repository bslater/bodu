// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SimpleReversingKnownAnswers.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Holds the curated <see cref="SimpleReversingSymmetricAlgorithm" /> known-answer test vectors used by
/// the algorithm-tier test harness.
/// </summary>
/// <remarks>
/// <para>
/// These vectors target <see cref="SimpleReversingSymmetricAlgorithm" /> in ECB mode with no padding,
/// using its default 128-bit (16-byte) block size. The cipher reverses the byte order of each block;
/// encryption and decryption are identical operations — reversing a reversed block restores the original
/// input.
/// </para>
/// <para>
/// The key is included in each vector because the algorithm requires a key at construction time, but the
/// no-tweak transform output is intentionally independent of the key value.
/// </para>
/// </remarks>
internal static class SimpleReversingKnownAnswers
{
    /// <summary>
    /// Returns the curated KAT vectors for <paramref name="variant" />.
    /// </summary>
    /// <param name="variant">The test variant selector. All variants resolve to the same default vector set.</param>
    /// <returns>The known-answer vectors for <see cref="SimpleReversingSymmetricAlgorithm" />.</returns>
    public static IReadOnlyList<BlockCipherKnownAnswer> For(SingleTestVariant variant) => Default;

    private const string Profile = "SimpleReversingSymmetricAlgorithm diagnostic vectors — 128-bit block, byte-reversal";

    private static readonly BlockCipherKnownAnswer[] Default =
    [
        new BlockCipherKnownAnswer
        {
            Name = "Key_AllZero_Plaintext_AllZero",
            Profile = Profile,
            Key = Convert.FromHexString("00000000000000000000000000000000"),
            Plaintext  = Convert.FromHexString("00000000000000000000000000000000"),
            Ciphertext = Convert.FromHexString("00000000000000000000000000000000"),
        },
        new BlockCipherKnownAnswer
        {
            Name = "Key_AllFF_Plaintext_AllFF",
            Profile = Profile,
            Key = Convert.FromHexString("FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF"),
            Plaintext  = Convert.FromHexString("FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF"),
            Ciphertext = Convert.FromHexString("FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF"),
        },
        new BlockCipherKnownAnswer
        {
            Name = "Key_Sequential_Plaintext_Sequential",
            Profile = Profile,
            Key = Convert.FromHexString("000102030405060708090A0B0C0D0E0F"),
            Plaintext  = Convert.FromHexString("000102030405060708090A0B0C0D0E0F"),
            Ciphertext = Convert.FromHexString("0F0E0D0C0B0A09080706050403020100"),
        },
        new BlockCipherKnownAnswer
        {
            Name = "Key_FEDCBA_Plaintext_AlternatingSegments",
            Profile = Profile,
            Key = Convert.FromHexString("FEDCBA9876543210FEDCBA9876543210"),
            Plaintext  = Convert.FromHexString("00112233445566778899AABBCCDDEEFF"),
            Ciphertext = Convert.FromHexString("FFEEDDCCBBAA99887766554433221100"),
        },
        new BlockCipherKnownAnswer
        {
            Name = "Key_Mixed_Plaintext_Interleaved",
            Profile = Profile,
            Key = Convert.FromHexString("0F1E2D3C4B5A69788796A5B4C3D2E1F0"),
            Plaintext  = Convert.FromHexString("A5C33C5A0FF0AA555A3CC3A5F00F5AA5"),
            Ciphertext = Convert.FromHexString("A55A0FF0A5C33C5A55AAF00F5A3CC3A5"),
        },
    ];
}
