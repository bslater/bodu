// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ApHashTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing;

/// <summary>
/// Contains unit tests for the <see cref="ApHash" /> hash algorithm.
/// </summary>
[TestClass]
public sealed partial class ApHashTests
    : NonCryptographicHashAlgorithmTests<ApHashTests, ApHash, SingleTestVariant>
{
    /// <inheritdoc />
    protected override ApHash CreateAlgorithm(SingleTestVariant variant) => new();

    /// <inheritdoc />
    protected override NonCryptographicHashAlgorithmSpecification GetSpecification(SingleTestVariant variant) =>
        new()
        {
            HashLengthInBytes = 4,
            KnownAnswers = new()
            {
                Empty = "AAAAAAAA",
                Abc = "1F6547E5",
                QuickBrownFox = "4724B335",
                Zeros16 = "8D323DA9",
                Sequential0To255 = "8EF230D2",
            },
        };

    /// <inheritdoc />
    /// <remarks>
    /// Entries are the documented APHash known-answer sequence for incremental inputs
    /// <c>[]</c>, <c>[0x00]</c>, <c>[0x00, 0x01]</c>, … <c>[0x00 .. 0x0E]</c>.
    /// </remarks>
    protected override IEnumerable<string> GetIncrementalHashValue(SingleTestVariant variant) => new[]
    {
        "AAAAAAAA", "FFAAAAEA", "56F85747", "5E2C41E4", "C02AFE15", "9C8F54E8",
        "1A3495B4", "9F9F9DE8", "9B74DAFC", "80777B8E", "CA534BAE", "B9DC2B9E",
        "A8B5C03B", "1177E2DC", "5B134236", "BEFC8311",
    };
}
