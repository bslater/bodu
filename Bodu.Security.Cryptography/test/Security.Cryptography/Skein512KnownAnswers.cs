// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Skein512KnownAnswers.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Holds the curated <see cref="Skein512" /> known-answer test vectors, transcribed verbatim from the Skein 1.3 /
/// NIST CD <c>skein_golden_kat.txt</c> reference distribution.
/// </summary>
/// <remarks>
/// Skein-512-512 ships with a richer message-length set covering empty, one-byte, one-block, and two-block messages.
/// The non-canonical truncations (160, 224, 256, 384) carry a single 1024-bit incrementing-message vector each.
/// Skein-512 with a 128-bit output has no published KAT vectors in the NIST CD distribution.
/// </remarks>
internal static class Skein512KnownAnswers
{
    private static readonly KeyedHashAlgorithmKnownAnswer[] Empty = Array.Empty<KeyedHashAlgorithmKnownAnswer>();

    /// <summary>
    /// Returns the curated KAT vectors for <paramref name="variant" />, or an empty array when the NIST CD KAT
    /// publishes no vectors for that (output, mode) combination.
    /// </summary>
    public static IReadOnlyList<KeyedHashAlgorithmKnownAnswer> For(Skein512TestVariant variant) => variant switch
    {
        Skein512TestVariant.Hash_160 => Hash160,
        Skein512TestVariant.Mac_160 => Mac160,
        Skein512TestVariant.Hash_224 => Hash224,
        Skein512TestVariant.Mac_224 => Mac224,
        Skein512TestVariant.Hash_256 => Hash256,
        Skein512TestVariant.Mac_256 => Mac256,
        Skein512TestVariant.Hash_384 => Hash384,
        Skein512TestVariant.Mac_384 => Mac384,
        Skein512TestVariant.Hash_512 => Hash512,
        Skein512TestVariant.Mac_512 => Mac512,
        _ => Empty,
    };

    private const string IncrementingOneBlockSkein512 =
        "FFFEFDFCFBFAF9F8F7F6F5F4F3F2F1F0EFEEEDECEBEAE9E8E7E6E5E4E3E2E1E0" +
        "DFDEDDDCDBDAD9D8D7D6D5D4D3D2D1D0CFCECDCCCBCAC9C8C7C6C5C4C3C2C1C0";

    private const string IncrementingTwoBlocksSkein512 =
        "FFFEFDFCFBFAF9F8F7F6F5F4F3F2F1F0EFEEEDECEBEAE9E8E7E6E5E4E3E2E1E0" +
        "DFDEDDDCDBDAD9D8D7D6D5D4D3D2D1D0CFCECDCCCBCAC9C8C7C6C5C4C3C2C1C0" +
        "BFBEBDBCBBBAB9B8B7B6B5B4B3B2B1B0AFAEADACABAAA9A8A7A6A5A4A3A2A1A0" +
        "9F9E9D9C9B9A999897969594939291908F8E8D8C8B8A89888786858483828180";

    // Same byte sequence as IncrementingTwoBlocksSkein512 — the 1024-bit truncation vectors reuse this message.
    private const string Incrementing1024Bits = IncrementingTwoBlocksSkein512;

    private const string MacOneBlockSkein512 =
        "D3090C72167517F7C7AD82A70C2FD3F6443F608301591E598EADB195E8357135" +
        "BA26FEDE2EE187417F816048D00FC23512737A2113709A77E4170C49A94B7FDF";

    private const string MacTwoBlocksSkein512 =
        "D3090C72167517F7C7AD82A70C2FD3F6443F608301591E598EADB195E8357135" +
        "BA26FEDE2EE187417F816048D00FC23512737A2113709A77E4170C49A94B7FDF" +
        "F45FF579A72287743102E7766C35CA5ABC5DFE2F63A1E726CE5FBD2926DB03A2" +
        "DD18B03FC1508A9AAC45EB362440203A323E09EDEE6324EE2E37B4432C1867ED";

    // 1024-bit MAC payload reused by the truncation vectors.
    private const string Mac1024BitsSkein512 = MacTwoBlocksSkein512;

    private static readonly KeyedHashAlgorithmKnownAnswer[] Hash160 =
    [
        new KeyedHashAlgorithmKnownAnswer
        {
            Name = "Hash_NistKat1024",
            Profile = "NIST CD KAT",
            Input = Convert.FromHexString(Incrementing1024Bits),
            ExpectedHex = "7D59D23FCF38FF54710F0D38D3A0ACCE7B8D64F6",
        },
    ];

    private static readonly KeyedHashAlgorithmKnownAnswer[] Mac160 =
    [
        new KeyedHashAlgorithmKnownAnswer
        {
            Name = "Mac_NistKat1024",
            Profile = "NIST CD KAT",
            Input = Convert.FromHexString(Mac1024BitsSkein512),
            Key = Convert.FromHexString(
                "CB41F1706CDE09651203C2D0EFBADDF847A0D315CB2E53FF8BAC41DA0002672E" +
                "920244C66E02D5F0DAD3E94C42BB65F0D14157DECF4105EF5609D5B0984457C193"),
            ExpectedHex = "5670B226156570DFF3EFE16661AB86EB24982CDF",
        },
    ];

    private static readonly KeyedHashAlgorithmKnownAnswer[] Hash224 =
    [
        new KeyedHashAlgorithmKnownAnswer
        {
            Name = "Hash_NistKat1024",
            Profile = "NIST CD KAT",
            Input = Convert.FromHexString(Incrementing1024Bits),
            ExpectedHex = "21521B15C8A9F05D5958F997008E95C50C4EEE35FB30BA81D5831856",
        },
    ];

    private static readonly KeyedHashAlgorithmKnownAnswer[] Mac224 =
    [
        new KeyedHashAlgorithmKnownAnswer
        {
            Name = "Mac_NistKat1024",
            Profile = "NIST CD KAT",
            Input = Convert.FromHexString(Mac1024BitsSkein512),
            Key = Convert.FromHexString(
                "CB41F1706CDE09651203C2D0EFBADDF847A0D315CB2E53FF8BAC41DA0002672E" +
                "920244C66E02D5F0DAD3E94C42BB65F0D14157DECF4105EF5609D5B0984457C1" +
                "935DF3061FF06E9F204192BA11E5BB2CAC0430C1C370CB3D113FEA5EC1021EB8" +
                "75E5946D7A96AC69A1626C6206B7252736F24253C9EE9B85EB852DFC814631346C"),
            ExpectedHex = "C41B9FF9753E6C0F8ED88866E320535E927FE4DA552C289841A920DB",
        },
    ];

    private static readonly KeyedHashAlgorithmKnownAnswer[] Hash256 =
    [
        new KeyedHashAlgorithmKnownAnswer
        {
            Name = "Hash_NistKat1024",
            Profile = "NIST CD KAT",
            Input = Convert.FromHexString(Incrementing1024Bits),
            ExpectedHex = "1A6A5BA08E74A864B5CB052CFB9B2FA128203230A4D9923A329F5427C477A4DB",
        },
    ];

    private static readonly KeyedHashAlgorithmKnownAnswer[] Mac256 =
    [
        new KeyedHashAlgorithmKnownAnswer
        {
            Name = "Mac_NistKat1024",
            Profile = "NIST CD KAT",
            Input = Convert.FromHexString(Mac1024BitsSkein512),
            Key = Array.Empty<byte>(),
            ExpectedHex = "AA703B798B6F472BAA9D1E1689FA0F70F8DCA25A6046BB2C8FB7F34407934AE4",
        },
    ];

    private static readonly KeyedHashAlgorithmKnownAnswer[] Hash384 =
    [
        new KeyedHashAlgorithmKnownAnswer
        {
            Name = "Hash_NistKat1024",
            Profile = "NIST CD KAT",
            Input = Convert.FromHexString(Incrementing1024Bits),
            ExpectedHex =
                "EEAF4DC9B668C2A270B90CBD2E986C857E464B08903E5B6DDA1F15736F50D1BF" +
                "2B6C40A398B79C67533592EFD96BD8DC",
        },
    ];

    private static readonly KeyedHashAlgorithmKnownAnswer[] Mac384 =
    [
        new KeyedHashAlgorithmKnownAnswer
        {
            Name = "Mac_NistKat1024",
            Profile = "NIST CD KAT",
            Input = Convert.FromHexString(Mac1024BitsSkein512),
            Key = Convert.FromHexString("CB41F1706CDE09651203C2D0EFBADDF847A0D315CB2E53FF8BAC41DA0002672E"),
            ExpectedHex =
                "DFBF5C1319A1D9D70EFB2F1600FBCF694F935907F31D24A16D6CD2FB2D7855A7" +
                "69681766C0A29DA778EED346CD1D740F",
        },
    ];

    private static readonly KeyedHashAlgorithmKnownAnswer[] Hash512 =
    [
        new KeyedHashAlgorithmKnownAnswer
        {
            Name = "Hash_Empty",
            Profile = "NIST CD KAT",
            Input = Array.Empty<byte>(),
            ExpectedHex =
                "BC5B4C50925519C290CC634277AE3D6257212395CBA733BBAD37A4AF0FA06AF4" +
                "1FCA7903D06564FEA7A2D3730DBDB80C1F85562DFCC070334EA4D1D9E72CBA7A",
        },
        new KeyedHashAlgorithmKnownAnswer
        {
            Name = "Hash_OneByte",
            Profile = "NIST CD KAT",
            Input = Convert.FromHexString("FF"),
            ExpectedHex =
                "71B7BCE6FE6452227B9CED6014249E5BF9A9754C3AD618CCC4E0AAE16B316CC8" +
                "CA698D864307ED3E80B6EF1570812AC5272DC409B5A012DF2A579102F340617A",
        },
        new KeyedHashAlgorithmKnownAnswer
        {
            Name = "Hash_OneBlock",
            Profile = "NIST CD KAT",
            Input = Convert.FromHexString(IncrementingOneBlockSkein512),
            ExpectedHex =
                "45863BA3BE0C4DFC27E75D358496F4AC9A736A505D9313B42B2F5EADA79FC17F" +
                "63861E947AFB1D056AA199575AD3F8C9A3CC1780B5E5FA4CAE050E989876625B",
        },
        new KeyedHashAlgorithmKnownAnswer
        {
            Name = "Hash_TwoBlocks",
            Profile = "NIST CD KAT",
            Input = Convert.FromHexString(IncrementingTwoBlocksSkein512),
            ExpectedHex =
                "91CCA510C263C4DDD010530A33073309628631F308747E1BCBAA90E451CAB92E" +
                "5188087AF4188773A332303E6667A7A210856F742139000071F48E8BA2A5ADB7",
        },
    ];

    private static readonly KeyedHashAlgorithmKnownAnswer[] Mac512 =
    [
        new KeyedHashAlgorithmKnownAnswer
        {
            Name = "Mac_OneBlock",
            Profile = "NIST CD KAT",
            Input = Convert.FromHexString(MacOneBlockSkein512),
            Key = Convert.FromHexString(
                "CB41F1706CDE09651203C2D0EFBADDF847A0D315CB2E53FF8BAC41DA0002672E" +
                "920244C66E02D5F0DAD3E94C42BB65F0D14157DECF4105EF5609D5B0984457C1" +
                "935DF3061FF06E9F204192BA11E5BB2CAC0430C1C370CB3D113FEA5EC1021EB8" +
                "75E5946D7A96AC69A1626C6206B7252736F24253C9EE9B85EB852DFC814631346C"),
            ExpectedHex =
                "7690BA61F10E0BBA312980B0212E6A9A51B0E9AADFDE7CA535754A706E042335" +
                "B29172AAE29D8BAD18EFAF92D43E6406F3098E253F41F2931EDA5911DC740352",
        },
        new KeyedHashAlgorithmKnownAnswer
        {
            Name = "Mac_TwoBlocks",
            Profile = "NIST CD KAT",
            Input = Convert.FromHexString(MacTwoBlocksSkein512),
            Key = Convert.FromHexString(
                "CB41F1706CDE09651203C2D0EFBADDF847A0D315CB2E53FF8BAC41DA0002672E" +
                "920244C66E02D5F0DAD3E94C42BB65F0D14157DECF4105EF5609D5B0984457C1"),
            ExpectedHex =
                "04D8CDDB0AD931D54D195899A094684344E902286037272890BCE98A41813EDC" +
                "37A3CEE190A693FCCA613EE30049CE7EC2BDFF9613F56778A13F8C28A21D167A",
        },
    ];
}
