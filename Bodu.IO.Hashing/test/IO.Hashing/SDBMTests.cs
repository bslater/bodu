// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SDBMTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing;

/// <summary>
/// Contains unit tests for the <see cref="SDBM" /> hash algorithm.
/// </summary>
[TestClass]
public sealed partial class SDBMTests
    : NonCryptographicHashAlgorithmTests<SDBMTests, SDBM, SingleTestVariant>
{
    /// <inheritdoc />
    protected override SDBM CreateAlgorithm(SingleTestVariant variant) => new();

    /// <inheritdoc />
    protected override NonCryptographicHashAlgorithmSpecification GetSpecification(SingleTestVariant variant) =>
        new()
        {
            HashLengthInBytes = 4,
            BoundaryLengths = new[] { 1, 8, 16, 64 },
            LongInputLength = 200,
            MinNonZeroBytesForLongInput = 2,
            KnownAnswers = new()
            {
                Empty = "00000000",
                Abc = "20440042",
                QuickBrownFox = "8CA77173",
                Zeros16 = "00000000",
                Sequential0To255 = "DC07103F",
            },
        };

    /// <inheritdoc />
    /// <remarks>
    /// Entries are the documented SDBM known-answer sequence for incremental inputs
    /// <c>[]</c>, <c>[0x00]</c>, <c>[0x00, 0x01]</c>, … <c>[0x00 .. 0x0E]</c>.
    /// </remarks>
    protected override IReadOnlyList<string> GetExpectedHashesForIncrementalInput(SingleTestVariant variant) => new[]
    {
        "00000000", "00000000", "00000001", "00010041",
        "00801002", "2F85F082", "A2783003", "2B96D0C3",
        "8AE06004", "8D3BA104", "62B0A005", "E97C6145",
        "D6E0F006", "D1611186", "98695007", "D1F1B1C7",
        "5C41C008", "74364208", "DB624009", "3D36C249",
        "D2C2D00A", "ADFB328A", "035B700B", "438B92CB",
        "3224200C", "76EFE30C", "2814E00D", "BD30234D",
        "B225B00E", "8754538E", "A14E900F", "426473CF",
        "CA878010", "57688410",
    };
}
