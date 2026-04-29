// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SkipjackKnownAnswers.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Holds the curated <see cref="SkipjackBlockCipher" /> known-answer test vectors used by
/// <see cref="SkipjackBlockCipherTests" />. Each vector encrypts an all-zero 64-bit plaintext under a
/// single-bit-set 80-bit key; the expected ciphertexts are reproduced from the Bouncy Castle
/// <c>SkipjackTest</c> reference, which itself derives from the original NSA Skipjack specification.
/// </summary>
/// <remarks>
/// <para>
/// The corpus combines two well-established sources: the three single-bit-key avalanche cases (NSA Skipjack
/// reference / Bouncy Castle), and the canonical FIPS 185 Section 8 vector reproduced from Bouncy Castle's
/// <c>SkipjackTest.java</c>. Together they exercise both key-schedule sanity (avalanche) and the published
/// reference round-trip (FIPS 185), sufficient to detect any divergence in round-key byte ordering or the
/// Rule&#160;A / Rule&#160;B alternation. See issue #144 for the corpus decision history.
/// </para>
/// </remarks>
internal static class SkipjackKnownAnswers
{
    /// <summary>
    /// Returns the curated KAT vectors for <paramref name="variant" />. Skipjack exposes a single
    /// configuration (80-bit key, 64-bit block) so the variant parameter is reserved for parity with
    /// other cipher families and currently always yields the same set.
    /// </summary>
    public static IReadOnlyList<BlockCipherKnownAnswer> For(SingleTestVariant variant) => Default;

    private const string AllZeroPlaintext = "0000000000000000";

    private const string ProfileNsaReference = "NSA Skipjack reference / Bouncy Castle";

    private const string ProfileFips185 = "FIPS 185 Section 8 / Bouncy Castle SkipjackTest.java";

    private static readonly BlockCipherKnownAnswer[] Default =
    [
        new BlockCipherKnownAnswer
        {
            Name = "AllZeroPlaintext_KeyBit0Set",
            Profile = ProfileNsaReference,
            Plaintext = Convert.FromHexString(AllZeroPlaintext),
            Ciphertext = Convert.FromHexString("E378FE4157A66452"),
            Key = Convert.FromHexString("80000000000000000000"),
        },
        new BlockCipherKnownAnswer
        {
            Name = "AllZeroPlaintext_KeyBit1Set",
            Profile = ProfileNsaReference,
            Plaintext = Convert.FromHexString(AllZeroPlaintext),
            Ciphertext = Convert.FromHexString("61CE4785762E8980"),
            Key = Convert.FromHexString("40000000000000000000"),
        },
        new BlockCipherKnownAnswer
        {
            Name = "AllZeroPlaintext_KeyByte1HighBitSet",
            Profile = ProfileNsaReference,
            Plaintext = Convert.FromHexString(AllZeroPlaintext),
            Ciphertext = Convert.FromHexString("F76307829359FC11"),
            Key = Convert.FromHexString("00800000000000000000"),
        },
        new BlockCipherKnownAnswer
        {
            Name = "Fips185_Section8_CanonicalVector",
            Profile = ProfileFips185,
            Plaintext = Convert.FromHexString("33221100DDCCBBAA"),
            Ciphertext = Convert.FromHexString("2587CAE27A12D300"),
            Key = Convert.FromHexString("00998877665544332211"),
        },
    ];
}
