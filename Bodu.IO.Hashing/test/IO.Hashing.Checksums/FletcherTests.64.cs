// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FletcherTests.64.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing.Checksums;

/// <summary>
/// Contains unit tests for the <see cref="Fletcher64" /> hash algorithm.
/// </summary>
[TestClass]
public sealed partial class Fletcher64Tests
    : FletcherTests<Fletcher64Tests, Fletcher64>
{
    /// <inheritdoc />
    protected override NonCryptographicHashAlgorithmSpecification GetSpecification(SingleTestVariant variant) =>
        new()
        {
            HashLengthInBytes = 8,
            AlgorithmName = "Fletcher-64",
            BlockSizeBytes = 4,
            KnownAnswers = new()
            {
                Empty = "0000000000000000",
                Abc = "0000018A000000C6",
                QuickBrownFox = "00015BA200000FD9",
                Zeros16 = "0000000000000000",
                Sequential0To255= "002A2B0000007E81",
            },
        };

    /// <inheritdoc />
    /// <remarks>
    /// Entries are the documented Fletcher-64 known-answer sequence for incremental inputs
    /// <c>[]</c>, <c>[0x00]</c>, <c>[0x00, 0x01]</c>, … <c>[0x00 .. 0x1E]</c>.
    /// </remarks>
    protected override IReadOnlyList<string> GetExpectedHashesForIncrementalInput(SingleTestVariant variant) => new[]
    {
        "0000000000000000", "0000000000000000", "0000000100000001", "0000000400000003",
        "0000000A00000006", "000000140000000A", "000000230000000F", "0000003800000015",
        "000000540000001C", "0000007800000024", "000000A50000002D", "000000DC00000037",
        "0000011E00000042", "0000016C0000004E", "000001C70000005B", "0000023000000069",
        "000002A800000078", "0000033000000088", "000003C900000099", "00000474000000AB",
        "00000532000000BE", "00000604000000D2", "000006EB000000E7", "000007E8000000FD",
        "000008FC00000114", "00000A280000012C", "00000B6D00000145", "00000CCC0000015F",
        "00000E460000017A", "00000FDC00000196", "0000118F000001B3", "00001360000001D1",
        "00001550000001F0", "0000176000000210", "0000199100000231", "00001BE400000253",
        "00001E5A00000276", "000020F40000029A", "000023B3000002BF", "00002698000002E5",
        "000029A40000030C", "00002CD800000334", "000030350000035D", "000033BC00000387",
        "0000376E000003B2", "00003B4C000003DE", "00003F570000040B", "0000439000000439",
        "000047F800000468", "00004C9000000498", "00005159000004C9", "00005654000004FB",
        "00005B820000052E", "000060E400000562", "0000667B00000597", "00006C48000005CD",
        "0000724C00000604", "000078880000063C", "00007EFD00000675", "000085AC000006AF",
        "00008C96000006EA", "000093BC00000726", "00009B1F00000763", "0000A2C0000007A1",
        "0000AAA0000007E0", "0000B2C000000820",
    };
}
