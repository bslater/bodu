// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AesKnownAnswers.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Holds the curated <see cref="AesBlockCipher" /> known-answer test vectors used by
/// <see cref="AesBlockCipherTests" />, transcribed verbatim from FIPS-197 Appendix C — the canonical NIST
/// reference for the three AES key sizes (128, 192, 256 bits). All three vectors share the same 128-bit
/// plaintext and incremental-byte key prefix; only the key length and ciphertext differ.
/// </summary>
/// <remarks>
/// All vectors target single-block ECB encryption with no padding or IV — the raw block primitive contract.
/// AES is implemented in-tree as <see cref="AesBlockCipher" />, a thin adapter over the BCL <c>Aes</c>
/// primitive; these KATs anchor the adapter against the FIPS reference outputs without depending on the
/// BCL's own KAT coverage.
/// </remarks>
/// <seealso href="https://csrc.nist.gov/publications/detail/fips/197/final">FIPS 197 — Advanced Encryption Standard</seealso>
internal static class AesKnownAnswers
{
    /// <summary>
    /// Returns the curated KAT vector for <paramref name="variant" />. FIPS-197 Appendix C publishes one
    /// vector per key size; future expansions may add additional rows without changing the calling shape.
    /// </summary>
    public static IReadOnlyList<BlockCipherKnownAnswer> For(AesTestVariant variant) => variant switch
    {
        AesTestVariant.Key128 => Key128,
        AesTestVariant.Key192 => Key192,
        AesTestVariant.Key256 => Key256,
        _ => throw new ArgumentOutOfRangeException(nameof(variant), variant, null),
    };

    private const string Plaintext = "00112233445566778899AABBCCDDEEFF";

    private const string Key128Hex = "000102030405060708090A0B0C0D0E0F";

    private const string Key192Hex = Key128Hex + "1011121314151617";

    private const string Key256Hex = Key192Hex + "18191A1B1C1D1E1F";

    private const string ProfileFips197 = "FIPS-197 Appendix C";

    private static readonly BlockCipherKnownAnswer[] Key128 =
    [
        new BlockCipherKnownAnswer
        {
            Name = "Aes128_Fips197_C1",
            Profile = ProfileFips197,
            Plaintext = Convert.FromHexString(Plaintext),
            Ciphertext = Convert.FromHexString("69C4E0D86A7B0430D8CDB78070B4C55A"),
            Key = Convert.FromHexString(Key128Hex),
        },
    ];

    private static readonly BlockCipherKnownAnswer[] Key192 =
    [
        new BlockCipherKnownAnswer
        {
            Name = "Aes192_Fips197_C2",
            Profile = ProfileFips197,
            Plaintext = Convert.FromHexString(Plaintext),
            Ciphertext = Convert.FromHexString("DDA97CA4864CDFE06EAF70A0EC0D7191"),
            Key = Convert.FromHexString(Key192Hex),
        },
    ];

    private static readonly BlockCipherKnownAnswer[] Key256 =
    [
        new BlockCipherKnownAnswer
        {
            Name = "Aes256_Fips197_C3",
            Profile = ProfileFips197,
            Plaintext = Convert.FromHexString(Plaintext),
            Ciphertext = Convert.FromHexString("8EA2B7CA516745BFEAFC49904B496089"),
            Key = Convert.FromHexString(Key256Hex),
        },
    ];
}
