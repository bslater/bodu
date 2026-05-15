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
/// <para>
/// All vectors target single-block ECB encryption with no padding or IV — the raw block primitive contract.
/// The plaintext is identical across all three key sizes (RFC 3713 reuses the same 128-bit input); only the
/// key length and ciphertext differ. The keys themselves share the same 128-bit prefix and append further
/// bytes for 192- and 256-bit variants, exactly as published in the RFC.
/// </para>
/// <para>
/// Additional vectors include RFC 5528 Camellia-CTR keystream rows projected onto the raw block
/// primitive contract: each counter block is used as the plaintext block and the corresponding
/// keystream block is used as the expected ciphertext. These rows exercise repeated use of the
/// same expanded key schedule without depending on locally generated expected values.
/// </para>
/// </remarks>
/// <seealso href="https://datatracker.ietf.org/doc/html/rfc3713#appendix-A">RFC 3713 Appendix A — Camellia test vectors</seealso>
/// <seealso href="https://datatracker.ietf.org/doc/html/rfc5528#section-4.1">RFC 5528 Section 4.1 — Camellia-CTR test vectors</seealso>
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

    private const string ProfileAvalanche = "Camellia single-bit avalanche fixtures (zero / high-bit input)";

    private const string ProfileRfc5528 = "RFC 5528 Section 4.1 Camellia-CTR keystream";

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
        new BlockCipherKnownAnswer
        {
            Name = "Camellia128_ZeroKey_HighBitPlaintext",
            Profile = ProfileAvalanche,
            Plaintext = Convert.FromHexString("80000000000000000000000000000000"),
            Ciphertext = Convert.FromHexString("07923A39EB0A817D1C4D87BDB82D1F1C"),
            Key = Convert.FromHexString("00000000000000000000000000000000"),
        },
        new BlockCipherKnownAnswer
        {
            Name = "Camellia128_HighBitKey_ZeroPlaintext",
            Profile = ProfileAvalanche,
            Plaintext = Convert.FromHexString("00000000000000000000000000000000"),
            Ciphertext = Convert.FromHexString("6C227F749319A3AA7DA235A9BBA05A2C"),
            Key = Convert.FromHexString("80000000000000000000000000000000"),
        },
        new BlockCipherKnownAnswer
        {
            Name = "Camellia128_Rfc5528_Tv3_Block1",
            Profile = ProfileRfc5528,
            Plaintext = Convert.FromHexString("00E0017B27777F3F4A1786F000000001"),
            Ciphertext = Convert.FromHexString("B19C1DCECF70ED8F278D96E94188C17C"),
            Key = Convert.FromHexString("7691BE035E5020A8AC6E618529F9A0DC"),
        },
        new BlockCipherKnownAnswer
        {
            Name = "Camellia128_Rfc5528_Tv3_Block2",
            Profile = ProfileRfc5528,
            Plaintext = Convert.FromHexString("00E0017B27777F3F4A1786F000000002"),
            Ciphertext = Convert.FromHexString("8CF75938488865E657344786D28597D2"),
            Key = Convert.FromHexString("7691BE035E5020A8AC6E618529F9A0DC"),
        },
        new BlockCipherKnownAnswer
        {
            Name = "Camellia128_Rfc5528_Tv3_Block3",
            Profile = ProfileRfc5528,
            Plaintext = Convert.FromHexString("00E0017B27777F3F4A1786F000000003"),
            Ciphertext = Convert.FromHexString("FF71A4B5D88612536A9D10A1130F14F8"),
            Key = Convert.FromHexString("7691BE035E5020A8AC6E618529F9A0DC"),
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
        new BlockCipherKnownAnswer
        {
            Name = "Camellia192_Rfc5528_Tv6_Block1",
            Profile = ProfileRfc5528,
            Plaintext = Convert.FromHexString("0007BDFD5CBD60278DCC091200000001"),
            Ciphertext = Convert.FromHexString("5711E755E54D7C27BDA50478FD934077"),
            Key = Convert.FromHexString("02BF391EE8ECB159B959617B0965279BF59B60A786D3E0FE"),
        },
        new BlockCipherKnownAnswer
        {
            Name = "Camellia192_Rfc5528_Tv6_Block2",
            Profile = ProfileRfc5528,
            Plaintext = Convert.FromHexString("0007BDFD5CBD60278DCC091200000002"),
            Ciphertext = Convert.FromHexString("66E26DCF85A4F95A55B4F2FD7ABB5311"),
            Key = Convert.FromHexString("02BF391EE8ECB159B959617B0965279BF59B60A786D3E0FE"),
        },
        new BlockCipherKnownAnswer
        {
            Name = "Camellia192_Rfc5528_Tv6_Block3",
            Profile = ProfileRfc5528,
            Plaintext = Convert.FromHexString("0007BDFD5CBD60278DCC091200000003"),
            Ciphertext = Convert.FromHexString("F57689746352A8C51E82DE66C39F3834"),
            Key = Convert.FromHexString("02BF391EE8ECB159B959617B0965279BF59B60A786D3E0FE"),
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
        new BlockCipherKnownAnswer
        {
            Name = "Camellia256_Rfc5528_Tv9_Block1",
            Profile = ProfileRfc5528,
            Plaintext = Convert.FromHexString("001CC5B751A51D70A1C1114800000001"),
            Ciphertext = Convert.FromHexString("A4DB21FFE2A0F9AD656DA4910A5FAA23"),
            Key = Convert.FromHexString("FF7A617CE69148E4F1726E2F43581DE2AA62D9F805532EDFF1EED687FB54153D"),
        },
        new BlockCipherKnownAnswer
        {
            Name = "Camellia256_Rfc5528_Tv9_Block2",
            Profile = ProfileRfc5528,
            Plaintext = Convert.FromHexString("001CC5B751A51D70A1C1114800000002"),
            Ciphertext = Convert.FromHexString("C170B15871EC71886DD9050B036C3970"),
            Key = Convert.FromHexString("FF7A617CE69148E4F1726E2F43581DE2AA62D9F805532EDFF1EED687FB54153D"),
        },
        new BlockCipherKnownAnswer
        {
            Name = "Camellia256_Rfc5528_Tv9_Block3",
            Profile = ProfileRfc5528,
            Plaintext = Convert.FromHexString("001CC5B751A51D70A1C1114800000003"),
            Ciphertext = Convert.FromHexString("35CE2FAE9078B372F57612391F8BAFBF"),
            Key = Convert.FromHexString("FF7A617CE69148E4F1726E2F43581DE2AA62D9F805532EDFF1EED687FB54153D"),
        },
    ];
}
