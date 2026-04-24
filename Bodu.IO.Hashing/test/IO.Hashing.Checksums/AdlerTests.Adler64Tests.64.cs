// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AdlerTests.Adler64Tests.64.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing.Checksums;

/// <summary>
/// Contains unit tests for the <see cref="Adler64" /> hash algorithm.
/// </summary>
[TestClass]
public sealed partial class Adler64Tests
    : Adler64BaseTests<Adler64Tests, Adler64>
{
    /// <inheritdoc />
    protected override NonCryptographicHashAlgorithmSpecification GetSpecification(SingleTestVariant variant) => new()
    {
        HashLengthInBytes = 8,
        KnownAnswers = new()
        {
            Empty = "0000000000000001",
            Abc = "0000018D000000C7",
            QuickBrownFox = "00015BCD00000FDA",
            Zeros16 = "0000001000000001",
        },
    };

    /// <inheritdoc />
    /// <remarks>
    /// Entries are the documented Adler-64 known-answer sequence for incremental inputs
    /// <c>[]</c>, <c>[0x00]</c>, <c>[0x00, 0x01]</c>, … <c>[0x00 .. 0x0E]</c>.
    /// </remarks>
    protected override IEnumerable<string> GetIncrementalHashValue(SingleTestVariant variant) => new[]
    {
        "0000000000000001", "0000000100000001", "0000000300000002", "0000000700000004",
        "0000000E00000007", "000000190000000B", "0000002900000010", "0000003F00000016",
        "0000005C0000001D", "0000008100000025", "000000AF0000002E", "000000E700000038",
        "0000012A00000043", "000001790000004F", "000001D50000005C", "0000023F0000006A",
        "000002B800000079", "0000034100000089", "000003DB0000009A", "00000487000000AC",
        "00000546000000BF", "00000619000000D3", "00000701000000E8", "000007FF000000FE",
        "0000091400000115", "00000A410000012D", "00000B8700000146", "00000CE700000160",
        "00000E620000017B", "00000FF900000197", "000011AD000001B4", "0000137F000001D2",
    };
}
