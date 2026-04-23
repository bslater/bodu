// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AdlerTests.Adler32Tests.32.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing;

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
    protected override IEnumerable<string> GetIncrementalHashValue(SingleTestVariant variant) => new[]
    {
        "00000001", "00010001", "00030002", "00070004", "000E0007", "0019000B",
        "00290010", "003F0016", "005C001D", "00810025", "00AF002E", "00E70038",
        "012A0043", "0179004F", "01D5005C", "023F006A",
    };
}
