// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SimpleReversingTweakableKnownAnswers.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Holds the curated <see cref="SimpleReversingTweakableSymmetricAlgorithm" /> known-answer test vectors
/// used by the algorithm-tier test harness.
/// </summary>
/// <remarks>
/// <para>
/// These vectors target <see cref="SimpleReversingTweakableSymmetricAlgorithm" /> in ECB mode with no
/// padding, using its default 128-bit (16-byte) block size and 128-bit (16-byte) tweak. The tweak is
/// incorporated as follows:
/// </para>
/// <list type="bullet">
/// <item><description><b>Encrypt:</b> XOR plaintext with tweak (cycling), then reverse the result.</description></item>
/// <item><description><b>Decrypt:</b> Reverse ciphertext, then XOR the result with the tweak (cycling).</description></item>
/// </list>
/// <para>
/// All vectors use a 16-byte key (128 bits) and 16-byte tweak (128 bits) matching the algorithm defaults.
/// The key does not influence the transform output for this diagnostic cipher; it is present solely because
/// the algorithm requires a key at construction time.
/// </para>
/// </remarks>
internal static class SimpleReversingTweakableKnownAnswers
{
    /// <summary>
    /// Returns the curated KAT vectors for <paramref name="variant" />.
    /// </summary>
    /// <param name="variant">The test variant selector. All variants resolve to the same default vector set.</param>
    /// <returns>The known-answer vectors for <see cref="SimpleReversingTweakableSymmetricAlgorithm" />.</returns>
    public static IReadOnlyList<BlockCipherKnownAnswer> For(SingleTestVariant variant) => Default;

    private const string Profile = "SimpleReversingTweakableSymmetricAlgorithm diagnostic vectors — 128-bit block, XOR-tweak + byte-reversal";

    private static readonly BlockCipherKnownAnswer[] Default =
    [
        new BlockCipherKnownAnswer
        {
            // Trivial case: all-zero inputs → zero tweak has no effect → output is reversed zeros.
            Name = "Key_AllZero_Tweak_AllZero_Plaintext_AllZero",
            Profile = Profile,
            Key     = Convert.FromHexString("00000000000000000000000000000000"),
            Tweak   = Convert.FromHexString("00000000000000000000000000000000"),
            Plaintext  = Convert.FromHexString("00000000000000000000000000000000"),
            Ciphertext = Convert.FromHexString("00000000000000000000000000000000"),
        },
        new BlockCipherKnownAnswer
        {
            // Zero plaintext XOR'd with sequential tweak, then reversed.
            // XOR(00×16, 00..0F) = 00..0F; reversed = 0F..00.
            Name = "Key_Sequential_Tweak_Sequential_Plaintext_AllZero",
            Profile = Profile,
            Key     = Convert.FromHexString("000102030405060708090A0B0C0D0E0F"),
            Tweak   = Convert.FromHexString("000102030405060708090A0B0C0D0E0F"),
            Plaintext  = Convert.FromHexString("00000000000000000000000000000000"),
            Ciphertext = Convert.FromHexString("0F0E0D0C0B0A09080706050403020100"),
        },
        new BlockCipherKnownAnswer
        {
            // Non-trivial case: mixed plaintext and tweak.
            // PT  = 00 11 22 33 44 55 66 77 88 99 AA BB CC DD EE FF
            // Twk = DE AD BE EF CA FE BA BE 12 34 56 78 90 AB CD EF
            // XOR = DE BC 9C DC 8E AB DC C9 9A AD FC C3 5C 76 23 10
            // Rev = 10 23 76 5C C3 FC AD 9A C9 DC AB 8E DC 9C BC DE
            Name = "Key_Mixed_Tweak_DEADBEEF_Plaintext_Sequential_Segments",
            Profile = Profile,
            Key     = Convert.FromHexString("0F1E2D3C4B5A69788796A5B4C3D2E1F0"),
            Tweak   = Convert.FromHexString("DEADBEEFCAFEBABE1234567890ABCDEF"),
            Plaintext  = Convert.FromHexString("00112233445566778899AABBCCDDEEFF"),
            Ciphertext = Convert.FromHexString("1023765CC3FCAD9AC9DCAB8EDC9CBCDE"),
        },
        new BlockCipherKnownAnswer
        {
            // All-FF plaintext with alternating 55/AA tweak.
            // XOR(FF×16, 55 AA 55 AA ...) = AA 55 AA 55 ...
            // Reversed = 55 AA 55 AA ...
            Name = "Key_AllFF_Tweak_Alternating55AA_Plaintext_AllFF",
            Profile = Profile,
            Key     = Convert.FromHexString("FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF"),
            Tweak   = Convert.FromHexString("55AA55AA55AA55AA55AA55AA55AA55AA"),
            Plaintext  = Convert.FromHexString("FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF"),
            Ciphertext = Convert.FromHexString("55AA55AA55AA55AA55AA55AA55AA55AA"),
        },
    ];
}
