// ---------------------------------------------------------------------------------------------------------------
// <copyright file="JSHashTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing;

/// <summary>
/// Contains unit tests for the <see cref="JSHash" /> hash algorithm.
/// </summary>
[TestClass]
public sealed partial class JSHashTests
    : NonCryptographicHashAlgorithmTests<JSHashTests, JSHash, SingleTestVariant>
{

    /// <inheritdoc />
    protected override JSHash CreateAlgorithm(SingleTestVariant variant) => new();

    /// <inheritdoc />
    /// <remarks>
    /// Entries are the documented JSHash known-answer sequence for incremental inputs
    /// <c>[]</c>, <c>[0x00]</c>, <c>[0x00, 0x01]</c>, … <c>[0x00 .. 0x0E]</c>.
    /// </remarks>
    protected override IReadOnlyList<string> GetExpectedHashesForIncrementalInput(SingleTestVariant variant) => new[]
    {
        "A7C6674E", "2E00F5AE", "E245A8A4", "58889A1A",
        "41256D43", "35D4123D", "87EF8D8C", "40836C38",
        "970BC723", "5A3E14A2", "85E418C9", "4E2D7A9C",
        "50181E2A", "70885464", "59B8F2C7", "1D01A1F7",
        "EB62A9C5", "C1D43F23", "A059F553", "DAD35DC0",
        "93238CFB", "CB58EB4B", "6CB78EDB", "1D2C3473",
        "A3A266F0", "835F8BB8", "DA970587", "8AF67185",
        "77B8EA4A", "6E05FB3A", "566AE454", "250F22E5",
        "AD67A838", "06689F1B",
    };

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
                Empty = "A7C6674E",
                Abc = "37889B1A",
                QuickBrownFox = "38FEEFDF",
                Zeros16 = "B1DD413D",
                Sequential0To255 = "9CFDA033",
            },
        };

}
