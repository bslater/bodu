// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Skein256KnownAnswers.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Holds the curated <see cref="Skein256" /> known-answer test vectors, transcribed verbatim from the Skein 1.3 /
/// NIST CD <c>skein_golden_kat.txt</c> reference distribution. Each per-variant collection is keyed on a
/// <see cref="Skein256TestVariant" /> value so the test fixture can attach the correct rows to that variant's
/// specification.
/// </summary>
/// <remarks>
/// The non-canonical truncations (Skein-256-160, -224) ship with a single 1024-bit message in the reference KAT;
/// Skein-256-256 ships with a richer set spanning empty, single-byte, one-block and two-block messages. Skein-256
/// with a 128-bit output has no published KAT vectors in the NIST CD distribution and so contributes no rows.
/// </remarks>
internal static class Skein256KnownAnswers
{
    private static readonly KeyedHashAlgorithmKnownAnswer[] Empty = Array.Empty<KeyedHashAlgorithmKnownAnswer>();

    /// <summary>
    /// Returns the curated KAT vectors for <paramref name="variant" />, or an empty array when the NIST CD KAT
    /// publishes no vectors for that (output, mode) combination.
    /// </summary>
    public static IReadOnlyList<KeyedHashAlgorithmKnownAnswer> For(Skein256TestVariant variant) => variant switch
    {
        Skein256TestVariant.Hash_160 => Hash160,
        Skein256TestVariant.Mac_160 => Mac160,
        Skein256TestVariant.Hash_224 => Hash224,
        Skein256TestVariant.Mac_224 => Mac224,
        Skein256TestVariant.Hash_256 => Hash256,
        Skein256TestVariant.Mac_256 => Mac256,
        _ => Empty,
    };

    private const string IncrementingOneBlockSkein256 =
        "FFFEFDFCFBFAF9F8F7F6F5F4F3F2F1F0EFEEEDECEBEAE9E8E7E6E5E4E3E2E1E0";

    private const string IncrementingTwoBlocksSkein256 =
        "FFFEFDFCFBFAF9F8F7F6F5F4F3F2F1F0EFEEEDECEBEAE9E8E7E6E5E4E3E2E1E0" +
        "DFDEDDDCDBDAD9D8D7D6D5D4D3D2D1D0CFCECDCCCBCAC9C8C7C6C5C4C3C2C1C0";

    private const string Incrementing1024Bits =
        "FFFEFDFCFBFAF9F8F7F6F5F4F3F2F1F0EFEEEDECEBEAE9E8E7E6E5E4E3E2E1E0" +
        "DFDEDDDCDBDAD9D8D7D6D5D4D3D2D1D0CFCECDCCCBCAC9C8C7C6C5C4C3C2C1C0" +
        "BFBEBDBCBBBAB9B8B7B6B5B4B3B2B1B0AFAEADACABAAA9A8A7A6A5A4A3A2A1A0" +
        "9F9E9D9C9B9A999897969594939291908F8E8D8C8B8A89888786858483828180";

    private const string MacOneBlockSkein256 =
        "D3090C72167517F7C7AD82A70C2FD3F6443F608301591E598EADB195E8357135";

    private const string MacTwoBlocksSkein256 =
        "D3090C72167517F7C7AD82A70C2FD3F6443F608301591E598EADB195E8357135" +
        "BA26FEDE2EE187417F816048D00FC23512737A2113709A77E4170C49A94B7FDF";

    // 1024-bit MAC payload for Skein-256 = 128 bytes = four Skein-256 blocks. Same byte sequence shape as the
    // canonical Skein-256 random+MAC corpus, just longer than the two-block Mac vectors above.
    private const string Mac1024BitsSkein256 =
        "D3090C72167517F7C7AD82A70C2FD3F6443F608301591E598EADB195E8357135" +
        "BA26FEDE2EE187417F816048D00FC23512737A2113709A77E4170C49A94B7FDF" +
        "F45FF579A72287743102E7766C35CA5ABC5DFE2F63A1E726CE5FBD2926DB03A2" +
        "DD18B03FC1508A9AAC45EB362440203A323E09EDEE6324EE2E37B4432C1867ED";

    private static readonly KeyedHashAlgorithmKnownAnswer[] Hash160 =
    [
        new KeyedHashAlgorithmKnownAnswer
        {
            Name = "Hash_NistKat1024",
            Profile = "NIST CD KAT",
            Input = Convert.FromHexString(Incrementing1024Bits),
            ExpectedHex = "1FD30886A2C315DE86F67FFE66EDDDCF73BE4FE4",
        },
    ];

    private static readonly KeyedHashAlgorithmKnownAnswer[] Mac160 =
    [
        new KeyedHashAlgorithmKnownAnswer
        {
            Name = "Mac_NistKat1024",
            Profile = "NIST CD KAT",
            Input = Convert.FromHexString(Mac1024BitsSkein256),
            Key = Array.Empty<byte>(),
            ExpectedHex = "4982E9E281C13F1117134816A7B858E8F12FB729",
        },
    ];

    private static readonly KeyedHashAlgorithmKnownAnswer[] Hash224 =
    [
        new KeyedHashAlgorithmKnownAnswer
        {
            Name = "Hash_NistKat1024",
            Profile = "NIST CD KAT",
            Input = Convert.FromHexString(Incrementing1024Bits),
            ExpectedHex = "FAE243AB76B414FC4883EE73102FDCF51C2D74B98DF185A0BE9045F6",
        },
    ];

    private static readonly KeyedHashAlgorithmKnownAnswer[] Mac224 =
    [
        new KeyedHashAlgorithmKnownAnswer
        {
            Name = "Mac_NistKat1024",
            Profile = "NIST CD KAT",
            Input = Convert.FromHexString(Mac1024BitsSkein256),
            Key = Convert.FromHexString("CB41F1706CDE09651203C2D0EFBADDF8"),
            ExpectedHex = "A097340709B443ED2C0A921F5DCEFEF3EAD65C4F0BCD5F13DA54D7ED",
        },
    ];

    private static readonly KeyedHashAlgorithmKnownAnswer[] Hash256 =
    [
        new KeyedHashAlgorithmKnownAnswer
        {
            Name = "Hash_Empty",
            Profile = "NIST CD KAT",
            Input = Array.Empty<byte>(),
            ExpectedHex = "C8877087DA56E072870DAA843F176E9453115929094C3A40C463A196C29BF7BA",
        },
        new KeyedHashAlgorithmKnownAnswer
        {
            Name = "Hash_OneByte",
            Profile = "NIST CD KAT",
            Input = Convert.FromHexString("FF"),
            ExpectedHex = "0B98DCD198EA0E50A7A244C444E25C23DA30C10FC9A1F270A6637F1F34E67ED2",
        },
        new KeyedHashAlgorithmKnownAnswer
        {
            Name = "Hash_OneBlock",
            Profile = "NIST CD KAT",
            Input = Convert.FromHexString(IncrementingOneBlockSkein256),
            ExpectedHex = "8D0FA4EF777FD759DFD4044E6F6A5AC3C774AEC943DCFC07927B723B5DBF408B",
        },
        new KeyedHashAlgorithmKnownAnswer
        {
            Name = "Hash_TwoBlocks",
            Profile = "NIST CD KAT",
            Input = Convert.FromHexString(IncrementingTwoBlocksSkein256),
            ExpectedHex = "DF28E916630D0B44C4A849DC9A02F07A07CB30F732318256B15D865AC4AE162F",
        },
    ];

    private static readonly KeyedHashAlgorithmKnownAnswer[] Mac256 =
    [
        new KeyedHashAlgorithmKnownAnswer
        {
            Name = "Mac_OneBlock",
            Profile = "NIST CD KAT",
            Input = Convert.FromHexString(MacOneBlockSkein256),
            Key = Convert.FromHexString("CB41F1706CDE09651203C2D0EFBADDF847A0D315CB2E53FF8BAC41DA0002672E"),
            ExpectedHex = "9E9980FCC16EE082CF164A5147D0E0692AEFFE3DCB8D620E2BB542091162E2E9",
        },
        new KeyedHashAlgorithmKnownAnswer
        {
            Name = "Mac_TwoBlocks",
            Profile = "NIST CD KAT",
            Input = Convert.FromHexString(MacTwoBlocksSkein256),
            Key = Convert.FromHexString("CB41F1706CDE09651203C2D0EFBADDF8"),
            ExpectedHex = "B1B8C18188E69A6ECAE0B6018E6B638C6A91E6DE6881E32A60858468C17B520D",
        },
    ];
}
