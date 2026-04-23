// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FnvTests.Fnv132.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing;

/// <summary>
/// Contains unit tests for the <see cref="Fnv132" /> hash algorithm.
/// </summary>
[TestClass]
public sealed partial class Fnv132Tests
    : FnvTests<Fnv132Tests, Fnv132>
{
    /// <inheritdoc />
    protected override NonCryptographicHashAlgorithmSpecification GetSpecification(SingleTestVariant variant) => new()
    {
        HashLengthInBytes = 4,
        AlgorithmName = "FNV-1-32",
        MinNonZeroBytesForLongInput = 3,
        KnownAnswers = new()
        {
            Empty = "811C9DC5",
            Abc = "634CAFEB",
            QuickBrownFox = "E9C86C6E",
            Zeros16 = "69691905",
            Sequential0To255 = "5051A61E",
        },
    };

    /// <inheritdoc />
    /// <remarks>
    /// Entries are the documented FNV-1 32-bit known-answer sequence for incremental inputs
    /// <c>[]</c>, <c>[0x00]</c>, <c>[0x00, 0x01]</c>, … <c>[0x00 .. 0x0E]</c>.
    /// </remarks>
    protected override IEnumerable<string> GetIncrementalHashValue(SingleTestVariant variant) => new[]
    {
        "811C9DC5", "050C5D1F", "117697CC", "49B0F626", "27937DD1", "1E2F1007",
        "8B163B00", "F3FEE106", "203C3C75", "33D32C27", "BC6E816C", "0DF5BD0E",
        "07D89D01", "5AFF289F", "DEACF240", "CA415ACE",
    };
}
