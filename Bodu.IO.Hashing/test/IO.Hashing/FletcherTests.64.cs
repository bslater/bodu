// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FletcherTests.64.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing;

/// <summary>
/// Contains unit tests for the <see cref="Fletcher64" /> hash algorithm.
/// </summary>
[TestClass]
public sealed partial class Fletcher64Tests
    : FletcherTests<Fletcher64Tests, Fletcher64>
{
    /// <inheritdoc />
    protected override NonCryptographicHashAlgorithmSpecification GetSpecification(SingleTestVariant variant) =>
        new()
        {
            HashLengthInBytes = 8,
            AlgorithmName = "Fletcher-64",
            BlockSizeBytes = 4,
            KnownAnswers = new()
            {
                Empty = "0000000000000000",
                Abc = "0043424100434241",
                QuickBrownFox = "7CA0BCD01F153C78",
                Zeros16 = "0000000000000000",
            },
        };

    /// <inheritdoc />
    /// <remarks>
    /// Entries are the documented Fletcher-64 known-answer sequence for incremental inputs
    /// <c>[]</c>, <c>[0x00]</c>, <c>[0x00, 0x01]</c>, … <c>[0x00 .. 0x1E]</c>.
    /// </remarks>
    protected override IEnumerable<string> GetIncrementalHashValue(SingleTestVariant variant) => new[]
    {
        "0000000000000000", "0000000000000000", "0000010000000100", "0002010000020100",
        "0302010003020100", "0604020403020104", "0604070403020604", "060A070403080604",
        "0D0A07040A080604", "17120D100A08060C", "171216100A080F0C", "171C16100A120F0C",
        "221C161015120F0C", "372E252815120F18", "372E322815121C18", "373C322815201C18",
        "463C322824201C18", "6A5C4E5024201C28", "6A5C5F5024202D28", "6A6E5F5024322D28",
        "7D6E5F5037322D28", "B4A08C8C37322D3C", "B4A0A18C3732423C", "B4B6A18C3748423C",
        "CBB6A18C4E48423C", "19FFE3E04E484254", "19FFFCE04E485B54", "1919FDE04E625B54",
        "3419FDE069625B54", "9E7B585169625B70", "9E7B755169627870", "9E99755169807870",
    };
}
