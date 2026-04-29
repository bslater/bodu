// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CamelliaKnownAnswers.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Holds the curated <see cref="CamelliaBlockCipher" /> known-answer test vectors used by
/// <see cref="CamelliaBlockCipherTests" />, transcribed verbatim from RFC 3713 Appendix A. Each per-variant
/// collection is keyed on a <see cref="BlockCipherKeyVariant" /> value so the test fixture can attach the
/// correct row to that variant's specification.
/// </summary>
/// <remarks>
/// All vectors target single-block ECB encryption with no padding or IV — the raw block primitive contract.
/// The plaintext is identical across all three key sizes (RFC 3713 reuses the same 128-bit input); only the
/// key length and ciphertext differ. The keys themselves share the same 128-bit prefix and append further
/// bytes for 192- and 256-bit variants, exactly as published in the RFC.
/// </remarks>
/// <seealso href="https://datatracker.ietf.org/doc/html/rfc3713#appendix-A">RFC 3713 Appendix A — Camellia test vectors</seealso>
internal static class CamelliaKnownAnswers
{
    /// <summary>
    /// Returns the curated KAT vectors for <paramref name="variant" />. RFC 3713 publishes one vector per
    /// key size; future expansions may add additional rows without changing the calling shape.
    /// </summary>
    public static IReadOnlyList<BlockCipherKnownAnswer> For(BlockCipherKeyVariant variant) => variant switch
    {
        BlockCipherKeyVariant.Key128 => Key128,
        BlockCipherKeyVariant.Key192 => Key192,
        BlockCipherKeyVariant.Key256 => Key256,
        _ => throw new ArgumentOutOfRangeException(nameof(variant), variant, null),
    };

    private const string Plaintext = "0123456789ABCDEFFEDCBA9876543210";

    private const string Key128Hex = "0123456789ABCDEFFEDCBA9876543210";

    private const string Key192Hex = Key128Hex + "0011223344556677";

    private const string Key256Hex = Key192Hex + "8899AABBCCDDEEFF";

    private const string ProfileRfc3713 = "RFC 3713 Appendix A";

    private static readonly BlockCipherKnownAnswer[] Key128 =
    [
        new BlockCipherKnownAnswer
        {
            Name = "Camellia128_Rfc3713_A1",
            Profile = ProfileRfc3713,
            Plaintext = Convert.FromHexString(Plaintext),
            Ciphertext = Convert.FromHexString("67673138549669730857065648EABE43"),
            Key = Convert.FromHexString(Key128Hex),
        },
    ];

    private static readonly BlockCipherKnownAnswer[] Key192 =
    [
        new BlockCipherKnownAnswer
        {
            Name = "Camellia192_Rfc3713_A2",
            Profile = ProfileRfc3713,
            Plaintext = Convert.FromHexString(Plaintext),
            Ciphertext = Convert.FromHexString("B4993401B3E996F84EE5CEE7D79B09B9"),
            Key = Convert.FromHexString(Key192Hex),
        },
    ];

    private static readonly BlockCipherKnownAnswer[] Key256 =
    [
        new BlockCipherKnownAnswer
        {
            Name = "Camellia256_Rfc3713_A3",
            Profile = ProfileRfc3713,
            Plaintext = Convert.FromHexString(Plaintext),
            Ciphertext = Convert.FromHexString("9ACC237DFF16D76C20EF7C919E3A7509"),
            Key = Convert.FromHexString(Key256Hex),
        },
    ];
}
