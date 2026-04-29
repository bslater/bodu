// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AdlerTests.Adler32Tests.32.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing.Checksums;

/// <summary>
/// Contains unit tests for the <see cref="Adler32" /> hash algorithm.
/// </summary>
[TestClass]
public sealed partial class Adler32Tests
    : Adler32BaseTests<Adler32Tests, Adler32>
{
    /// <inheritdoc />
    protected override NonCryptographicHashAlgorithmSpecification GetSpecification(SingleTestVariant variant) => new()
    {
        HashLengthInBytes = 4,
        KnownAnswers = new()
        {
            Empty = "00000001",
            Abc = "018D00C7",
            QuickBrownFox = "5BCD0FDA",
            Zeros16 = "00100001",
        },
    };

    /// <inheritdoc />
    /// <remarks>
    /// Entries are the documented Adler-32 known-answer sequence for incremental inputs
    /// <c>[]</c>, <c>[0x00]</c>, <c>[0x00, 0x01]</c>, … <c>[0x00 .. 0x0E]</c>.
    /// </remarks>
    protected override IReadOnlyList<string> GetExpectedHashesForIncrementalInput(SingleTestVariant variant) => new[]
    {
        "00000001", "00010001", "00030002", "00070004",
        "000E0007", "0019000B", "00290010", "003F0016",
        "005C001D", "00810025", "00AF002E", "00E70038",
        "012A0043", "0179004F", "01D5005C", "023F006A",
        "02B80079", "03410089", "03DB009A", "048700AC",
        "054600BF", "061900D3", "070100E8", "07FF00FE",
        "09140115", "0A41012D", "0B870146", "0CE70160",
        "0E62017B", "0FF90197", "11AD01B4", "137F01D2",
        "157001F1", "17810211",
    };
}
