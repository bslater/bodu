// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FletcherTests.32.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing.Checksums;

/// <summary>
/// Contains unit tests for the <see cref="Fletcher32" /> hash algorithm.
/// </summary>
[TestClass]
public sealed partial class Fletcher32Tests
    : FletcherTests<Fletcher32Tests, Fletcher32>
{
    /// <inheritdoc />
    protected override NonCryptographicHashAlgorithmSpecification GetSpecification(SingleTestVariant variant) =>
        new()
        {
            HashLengthInBytes = 4,
            AlgorithmName = "Fletcher-32",
            BlockSizeBytes = 2,
            KnownAnswers = new()
            {
                Empty = "00000000",
                Abc = "84C54284",
                QuickBrownFox = "53CD5B8D",
                Zeros16 = "00000000",
            },
        };

    /// <inheritdoc />
    /// <remarks>
    /// Entries are the documented Fletcher-32 known-answer sequence for incremental inputs
    /// <c>[]</c>, <c>[0x00]</c>, <c>[0x00, 0x01]</c>, … <c>[0x00 .. 0x0E]</c>.
    /// </remarks>
    protected override IEnumerable<string> GetIncrementalHashValue(SingleTestVariant variant) => new[]
    {
        "00000000", "00000000", "01000100", "02020102", "05020402", "09080406",
        "0E080906", "1714090C", "1E14100C", "2E281014", "37281914", "5046191E",
        "5B46241E", "7F70242A", "8C70312A", "BDA83138",
    };
}
