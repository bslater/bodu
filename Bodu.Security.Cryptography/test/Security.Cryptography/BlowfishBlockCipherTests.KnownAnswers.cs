// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BlowfishBlockCipherTests.KnownAnswers.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Security.Cryptography.Infrastructure;
namespace Bodu.Security.Cryptography;

/// <summary>
/// Curated <see cref="BlowfishBlockCipher" /> known-answer test vectors. Transcribed from Eric Young's published
/// Blowfish test set hosted on Bruce Schneier's site, which is the canonical reference for the 16-round Blowfish
/// ECB cipher with 64-bit keys.
/// </summary>
/// <remarks>
/// All vectors target standard 16-round Blowfish in raw single-block ECB mode with a 64-bit key. They cover the
/// all-zero key/plaintext, the all-ones key/plaintext, and a small set of representative key/plaintext pairings
/// published in the reference document.
/// </remarks>
/// <seealso href="https://www.schneier.com/wp-content/uploads/2015/12/vectors-2.txt">vectors-2.txt — Eric Young Blowfish vectors</seealso>
internal sealed partial class BlowfishBlockCipherTests
{
    private static readonly KatProvenance ProfileSchneierVectors = KatProvenance.ReferenceImplementation("Eric Young / Schneier vectors-2.txt");

    private static readonly BlockCipherKnownAnswer[] DefaultKnownAnswers =
    [
        new BlockCipherKnownAnswer
        {
            Name = "Key_AllZero_Plaintext_AllZero",
            Provenance = ProfileSchneierVectors,
            Plaintext = Convert.FromHexString("0000000000000000"),
            Ciphertext = Convert.FromHexString("4EF997456198DD78"),
            Key = Convert.FromHexString("0000000000000000"),
        },
        new BlockCipherKnownAnswer
        {
            Name = "Key_AllOnes_Plaintext_AllOnes",
            Provenance = ProfileSchneierVectors,
            Plaintext = Convert.FromHexString("FFFFFFFFFFFFFFFF"),
            Ciphertext = Convert.FromHexString("51866FD5B85ECB8A"),
            Key = Convert.FromHexString("FFFFFFFFFFFFFFFF"),
        },
        new BlockCipherKnownAnswer
        {
            Name = "Key_0123456789ABCDEF_Plaintext_AllOnesNibbles",
            Provenance = ProfileSchneierVectors,
            Plaintext = Convert.FromHexString("1111111111111111"),
            Ciphertext = Convert.FromHexString("61F9C3802281B096"),
            Key = Convert.FromHexString("0123456789ABCDEF"),
        },
        new BlockCipherKnownAnswer
        {
            Name = "Key_AllOnesNibbles_Plaintext_0123456789ABCDEF",
            Provenance = ProfileSchneierVectors,
            Plaintext = Convert.FromHexString("0123456789ABCDEF"),
            Ciphertext = Convert.FromHexString("7D0CC630AFDA1EC7"),
            Key = Convert.FromHexString("1111111111111111"),
        },
        new BlockCipherKnownAnswer
        {
            Name = "Key_FEDCBA9876543210_Plaintext_0123456789ABCDEF",
            Provenance = ProfileSchneierVectors,
            Plaintext = Convert.FromHexString("0123456789ABCDEF"),
            Ciphertext = Convert.FromHexString("0ACEAB0FC6A0A28D"),
            Key = Convert.FromHexString("FEDCBA9876543210"),
        },
        new BlockCipherKnownAnswer
        {
            Name = "Key_7CA110454A1A6E57_Plaintext_01A1D6D039776742",
            Provenance = ProfileSchneierVectors,
            Plaintext = Convert.FromHexString("01A1D6D039776742"),
            Ciphertext = Convert.FromHexString("59C68245EB05282B"),
            Key = Convert.FromHexString("7CA110454A1A6E57"),
        },
    ];

    /// <summary>
    /// Returns the curated KAT vectors for <paramref name="variant" />. Blowfish exposes a single configuration in
    /// this test suite (64-bit key, 64-bit block) so the variant parameter is reserved for parity with other cipher
    /// families and currently always yields the same set.
    /// </summary>
    private static IReadOnlyList<BlockCipherKnownAnswer> KnownAnswersFor(SingleTestVariant variant) => DefaultKnownAnswers;
}
