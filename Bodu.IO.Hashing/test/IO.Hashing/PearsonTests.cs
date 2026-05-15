// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PearsonTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing;

/// <summary>
/// Contains unit tests for the <see cref="Pearson" /> hash algorithm.
/// </summary>
[TestClass]
public sealed partial class PearsonTests
    : NonCryptographicHashAlgorithmTests<PearsonTests, Pearson, Pearson.PearsonTableType>
{

    /// <inheritdoc />
    public override IEnumerable<Pearson.PearsonTableType> GetNonCryptographicHashAlgorithmVariants() =>
        Enum.GetValues<Pearson.PearsonTableType>().Where(t => t != Pearson.PearsonTableType.UserDefined);

    /// <inheritdoc />
    protected override Pearson CreateAlgorithm(Pearson.PearsonTableType variant) =>
        new(Pearson.MinHashSizeBits, variant);

    /// <inheritdoc />
    /// <remarks>
    /// Entries are the documented Pearson known-answer sequences for incremental inputs
    /// <c>[]</c>, <c>[0x00]</c>, <c>[0x00, 0x01]</c>, … <c>[0x00 .. 0x0E]</c>.
    /// </remarks>
    protected override IReadOnlyList<string> GetExpectedHashesForIncrementalInput(Pearson.PearsonTableType variant) => variant switch
    {
        Pearson.PearsonTableType.Pearson => new[]
        {
            "00", "01", "01", "0C",
            "A3", "10", "55", "3A",
            "E3", "C4", "CD", "04",
            "A3", "7B", "C9", "04",
            "54", "3C",
        },
        Pearson.PearsonTableType.AESSBox =>
        [
            "00", "63", "AA", "C2",
            "78", "10", "59", "CF",
            "E8", "E1", "9B", "81",
            "7E", "40", "E3", "55",
            "BE", "E4",
        ],
        Pearson.PearsonTableType.CRC32HighByte =>
        [
            "00", "00", "77", "20",
            "A2", "3F", "C6", "9B",
            "F9", "CA", "02", "0E",
            "70", "59", "6C", "A3",
            "DF", "0B",
        ],
        Pearson.PearsonTableType.SHA256Constants =>
        [
            "00", "48", "87", "63",
            "B3", "0F", "A0", "09",
            "E6", "56", "FF", "05",
            "E6", "06", "3C", "34",
            "1E", "E6",
        ],
        _ => throw new ArgumentOutOfRangeException(nameof(variant), variant, null),
    };

    /// <inheritdoc />
    protected override NonCryptographicHashAlgorithmSpecification GetSpecification(Pearson.PearsonTableType variant) =>
        new()
        {
            HashLengthInBytes = 1,
            MinNonZeroBytesForLongInput = 1,
            BoundaryLengths = new[] { 1, 8, 16, 64 },
            LongInputLength = 200,
            KnownAnswers = variant switch
            {
                Pearson.PearsonTableType.Pearson => new()
                {
                    Empty = "00",
                    Abc = "51",
                    QuickBrownFox = "A5",
                    Zeros16 = "7A",
                },
                Pearson.PearsonTableType.AESSBox => new()
                {
                    Empty = "00",
                    Abc = "E2",
                    QuickBrownFox = "46",
                    Zeros16 = "D5",
                },
                Pearson.PearsonTableType.CRC32HighByte => new()
                {
                    Empty = "00",
                    Abc = "DF",
                    QuickBrownFox = "F0",
                    Zeros16 = "00",
                },
                Pearson.PearsonTableType.SHA256Constants => new()
                {
                    Empty = "00",
                    Abc = "A0",
                    QuickBrownFox = "C4",
                    Zeros16 = "DD",
                },
                _ => throw new ArgumentOutOfRangeException(nameof(variant), variant, null),
            },
        };

}
