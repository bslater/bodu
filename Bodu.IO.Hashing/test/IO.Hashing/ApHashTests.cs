// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ApHashTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
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
    /// <remarks>
    /// Entries are the documented APHash known-answer sequence for incremental inputs
    /// <c>[]</c>, <c>[0x00]</c>, <c>[0x00, 0x01]</c>, … <c>[0x00 .. 0x0E]</c>.
    /// </remarks>
    protected override IReadOnlyList<string> GetExpectedHashesForIncrementalInput(SingleTestVariant variant) =>
    [
        "AAAAAAAA", "FFAAAAEA", "56F85747", "5E2C41E4",
        "C02AFE15", "9C8F54E8", "1A3495B4", "9F9F9DE8",
        "9B74DAFC", "80777B8E", "CA534BAE", "B9DC2B9E",
        "A8B5C03B", "1177E2DC", "5B134236", "BEFC8311",
        "ABEC15F1", "AE04C165", "65833591", "9B81D219",
        "7B32EF72", "A0E92B0B", "0749C0AB", "B1C39C5E",
        "44528A45", "143A1288", "22F47DE2", "BC5BC8C0",
        "8506EC7B", "C9C49282", "0DE55FEF", "329F465D",
        "2BC4AA94", "0E895DD3",
    ];

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

}
