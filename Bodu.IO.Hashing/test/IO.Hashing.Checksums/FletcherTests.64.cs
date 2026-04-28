// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FletcherTests.64.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing.Checksums;

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
    protected override IReadOnlyList<string> GetExpectedHashesForIncrementalInput(SingleTestVariant variant) => new[]
    {
        "0000000000000000", "0000000000000000", "0000010000000100", "0002010000020100",
        "0302010003020100", "0604020403020104", "0604070403020604", "060A070403080604",
        "0D0A07040A080604", "17120D100A08060C", "171216100A080F0C", "171C16100A120F0C",
        "221C161015120F0C", "372E252815120F18", "372E322815121C18", "373C322815201C18",
        "463C322824201C18", "6A5C4E5024201C28", "6A5C5F5024202D28", "6A6E5F5024322D28",
        "7D6E5F5037322D28", "B4A08C8C37322D3C", "B4A0A18C3732423C", "B4B6A18C3748423C",
        "CBB6A18C4E48423C", "19FFE3E04E484254", "19FFFCE04E485B54", "1919FDE04E625B54",
        "3419FDE069625B54", "9E7B585169625B70", "9E7B755169627870", "9E99755169807870",
        "BD99755188807870", "451AEEE188807890", "451A0FE288809990", "453C0FE288A29990",
        "683C0FE2ABA29990", "14DFA896ABA299B4", "14DFCD96ABA2BEB4", "1405CE96ABC8BEB4",
        "3B05CE96D2C8BEB4", "0ECE8C73D2C8BEDC", "0ECEB573D2C8E7DC", "0EF8B573D2F2E7DC",
        "39F8B573FDF2E7DC", "37EB9D7CFEF2E708", "37EBCA7CFEF21409", "3719CB7CFE201509",
        "6619CB7C2D211509", "933AE0B52D211539", "933A11B62D214639", "936C11B62D534639",
        "C66C11B660534639", "27C057236053466D", "27C08C2360537B6D", "27F68C2360897B6D",
        "5EF68C2397897B6D", "F57F08C997897BA5", "F57F41C99789B4A5", "F5B941C997C3B4A5",
        "30BA41C9D2C3B4A5", "037EF6AAD2C3B4E1", "037E33ABD2C3F1E1", "03BC33ABD201F2E1",
        "42BC33AB1102F2E1", "54BE25CD1202F221",
    };
}
