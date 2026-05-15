// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MonitoringHashAlgorithmTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

[TestClass]
public partial class MonitoringHashAlgorithmTests
    : Security.Cryptography.HashAlgorithmTests<MonitoringHashAlgorithmTests, MonitoringHashAlgorithm, SingleTestVariant>
{

    /// <inheritdoc />
    protected override HashAlgorithmSpecification GetSpecification(SingleTestVariant variant) => new()
    {
        HashSize = 32,
        InputBlockSize = 1,
        OutputBlockSize = 1,
        MinNonZeroBytesForLongInput = 2,
        KnownAnswers = new()
        {
            Empty = "00000000",
            Abc = "C6000000",
            Zeros16 = "00000000",
            QuickBrownFox = "D90F0000",
        },
    };

    /// <inheritdoc />
    protected override IReadOnlyCollection<string> ExcludedFieldNames => [
        "<DisposeCallCount>k__BackingField",
        ];

    private static IReadOnlyCollection<string> ExcludedPropertyName = [
        "BytesProcessed",
        "InitializeCallCount",
        "HashCoreCallCount",
        "HashFinalCallCount",
        "DisposeCallCount",
        "TryHashFinalCallCount",
        "HashSizeAccessCount",
        "HashAccessCount",
        "HashCoreSpanCallCount",
        ];

    /// <inheritdoc />
    protected override IReadOnlyCollection<string> ExcludedWriteablePropertyNames => ExcludedPropertyName;

    /// <inheritdoc />
    protected override IReadOnlyCollection<string> ExcludedReadablePropertyNames => ExcludedPropertyName;

    /// <inheritdoc />
    protected override MonitoringHashAlgorithm CreateAlgorithm() => new MonitoringHashAlgorithm();

    /// <inheritdoc />
    protected override MonitoringHashAlgorithm CreateAlgorithm(SingleTestVariant variant) => new MonitoringHashAlgorithm();

    /// <inheritdoc />
    protected override IReadOnlyList<string> GetExpectedHashesForIncrementalInput(SingleTestVariant variant) => new[]
    {
        "00000000", "00000000", "01000000", "03000000",
        "06000000", "0A000000", "0F000000", "15000000",
        "1C000000", "24000000", "2D000000", "37000000",
        "42000000", "4E000000", "5B000000", "69000000",
        "78000000", "88000000",
    };

}
