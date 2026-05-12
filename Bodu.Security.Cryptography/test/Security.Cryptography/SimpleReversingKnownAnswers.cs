// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SimpleReversingKnownAnswers.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Holds the curated <see cref="SimpleReversingBlockCipher" /> known-answer test vectors used by
/// the block-cipher test harness.
/// </summary>
/// <remarks>
/// <para>
/// These vectors target <see cref="SimpleReversingBlockCipher" /> in its default no-tweak configuration.
/// In this mode encryption and decryption are identical operations: each block is copied and then byte-reversed.
/// </para>
/// <para>
/// The key is retained in each vector because the cipher requires a key at construction time, but the no-tweak
/// transform output is intentionally independent of the key value.
/// </para>
/// </remarks>
internal static class SimpleReversingKnownAnswers
{
    /// <summary>
    /// Returns the curated KAT vectors for <paramref name="variant" />.
    /// </summary>
    /// <param name="variant">
    /// The test variant. The current vectors target the default 8-byte block configuration used by the
    /// test harness.
    /// </param>
    /// <returns>
    /// The known-answer vectors for <see cref="SimpleReversingBlockCipher" />.
    /// </returns>
    public static IReadOnlyList<BlockCipherKnownAnswer> For(SingleTestVariant variant) => Default;

    private const string ProfileSimpleReversingVectors = "SimpleReversingBlockCipher diagnostic vectors";

    private static readonly BlockCipherKnownAnswer[] Default =
    [
        new BlockCipherKnownAnswer
        {
            Name = "Key_AllZero_Plaintext_AllZero",
            Profile = ProfileSimpleReversingVectors,
            Plaintext = Convert.FromHexString("0000000000000000"),
            Ciphertext = Convert.FromHexString("0000000000000000"),
            Key = Convert.FromHexString("0000000000000000"),
        },
        new BlockCipherKnownAnswer
        {
            Name = "Key_AllOnes_Plaintext_AllOnes",
            Profile = ProfileSimpleReversingVectors,
            Plaintext = Convert.FromHexString("FFFFFFFFFFFFFFFF"),
            Ciphertext = Convert.FromHexString("FFFFFFFFFFFFFFFF"),
            Key = Convert.FromHexString("FFFFFFFFFFFFFFFF"),
        },
        new BlockCipherKnownAnswer
        {
            Name = "Key_0123456789ABCDEF_Plaintext_0011223344556677",
            Profile = ProfileSimpleReversingVectors,
            Plaintext = Convert.FromHexString("0011223344556677"),
            Ciphertext = Convert.FromHexString("7766554433221100"),
            Key = Convert.FromHexString("0123456789ABCDEF"),
        },
        new BlockCipherKnownAnswer
        {
            Name = "Key_FEDCBA9876543210_Plaintext_89ABCDEF01234567",
            Profile = ProfileSimpleReversingVectors,
            Plaintext = Convert.FromHexString("89ABCDEF01234567"),
            Ciphertext = Convert.FromHexString("67452301EFCDAB89"),
            Key = Convert.FromHexString("FEDCBA9876543210"),
        },
        new BlockCipherKnownAnswer
        {
            Name = "Key_0F1E2D3C4B5A6978_Plaintext_0102030405060708",
            Profile = ProfileSimpleReversingVectors,
            Plaintext = Convert.FromHexString("0102030405060708"),
            Ciphertext = Convert.FromHexString("0807060504030201"),
            Key = Convert.FromHexString("0F1E2D3C4B5A6978"),
        },
        new BlockCipherKnownAnswer
        {
            Name = "Key_55AA55AA55AA55AA_Plaintext_A5C33C5A0FF0AA55",
            Profile = ProfileSimpleReversingVectors,
            Plaintext = Convert.FromHexString("A5C33C5A0FF0AA55"),
            Ciphertext = Convert.FromHexString("55AAF00F5A3CC3A5"),
            Key = Convert.FromHexString("55AA55AA55AA55AA"),
        },
    ];
}
