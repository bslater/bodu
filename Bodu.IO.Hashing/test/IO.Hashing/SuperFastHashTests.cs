// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SuperFastHashTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing;

/// <summary>
/// Contains unit tests for the <see cref="SuperFastHash" /> hash algorithm.
/// </summary>
[TestClass]
public sealed partial class SuperFastHashTests
    : NonCryptographicHashAlgorithmTests<SuperFastHashTests, SuperFastHash, SingleTestVariant>
{
    /// <inheritdoc />
    protected override SuperFastHash CreateAlgorithm(SingleTestVariant variant) => new();

    /// <inheritdoc />
    protected override NonCryptographicHashAlgorithmSpecification GetSpecification(SingleTestVariant variant) =>
        new()
        {
            HashLengthInBytes = 4,
            BoundaryLengths = new[] { 1, 2, 3, 4, 5, 7, 8, 16, 64 },
            LongInputLength = 200,
            MinNonZeroBytesForLongInput = 2,
            KnownAnswers = new()
            {
                Empty = "00000000",
                Abc = "39B3AA2F",
                QuickBrownFox = "E37CBF05",
                Zeros16 = "D6705481",
                Sequential0To255 = "73746413",
            },
        };

    /// <inheritdoc />
    /// <remarks>
    /// Entries are the SuperFastHash digests, in little-endian byte order, for incremental inputs
    /// <c>[]</c>, <c>[0x00]</c>, <c>[0x00, 0x01]</c>, … <c>[0x00 .. 0x0E]</c>.
    /// </remarks>
    protected override IEnumerable<string> GetIncrementalHashValue(SingleTestVariant variant) => new[]
    {
        "00000000", "5A595355", "94A0EA00", "7C80828E", "EF71151F", "0A834E96",
        "623C7A3D", "2794B36B", "AA876FB6", "92324598", "5CF6B891", "9F18F1ED",
        "2D50A77C", "0CFAF828", "4D958D23", "AACF3EBF",
    };
}
