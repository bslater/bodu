// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Fletcher16Tests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing;

/// <summary>
/// Contains unit tests for the <see cref="Fletcher16" /> hash algorithm.
/// </summary>
[TestClass]
public sealed partial class Fletcher16Tests
    : FletcherTests<Fletcher16Tests, Fletcher16>
{
    /// <inheritdoc />
    protected override NonCryptographicHashAlgorithmSpecification GetSpecification(SingleTestVariant variant) =>
        new()
        {
            HashLengthInBytes = 2,
            AlgorithmName = "Fletcher-16",
            BlockSizeBytes = 1,
        };

    /// <inheritdoc />
    protected override IReadOnlyDictionary<string, string> GetExpectedHashesForNamedInputs(SingleTestVariant variant) =>
        new Dictionary<string, string>
        {
            ["Empty"] = "0000",
            ["ABC"] = "8BC6",
            ["QuickBrownFox"] = "FEE8",
            ["Zeros_16"] = "0000",
        };

    /// <inheritdoc />
    /// <remarks>
    /// Entries are the documented Fletcher-16 known-answer sequence for incremental inputs
    /// <c>[]</c>, <c>[0x00]</c>, <c>[0x00, 0x01]</c>, … <c>[0x00 .. 0x0E]</c>.
    /// </remarks>
    protected override IEnumerable<string> GetIncrementalHashValue(SingleTestVariant variant) => new[]
    {
        "0000", "0000", "0101", "0403", "0A06", "140A", "230F", "3815",
        "541C", "7824", "A52D", "DC37", "1F42", "6D4E", "C85B", "3269",
    };
}
