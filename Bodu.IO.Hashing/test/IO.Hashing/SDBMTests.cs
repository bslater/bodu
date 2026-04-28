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
    protected override IEnumerable<string> GetIncrementalHashValue(SingleTestVariant variant) => new[]
    {
        "00000000", "00000000", "00000001", "00010041", "00801002", "2F85F082",
        "A2783003", "2B96D0C3", "8AE06004", "8D3BA104", "62B0A005", "E97C6145",
        "D6E0F006", "D1611186", "98695007", "D1F1B1C7",
    };
}
