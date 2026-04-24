// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FnvTests.Fnv164.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing;

/// <summary>
/// Contains unit tests for the <see cref="Fnv164" /> hash algorithm.
/// </summary>
[TestClass]
public sealed partial class Fnv164Tests
    : FnvTests<Fnv164Tests, Fnv164>
{
    /// <inheritdoc />
    protected override NonCryptographicHashAlgorithmSpecification GetSpecification(SingleTestVariant variant) => new()
    {
        HashLengthInBytes = 8,
        AlgorithmName = "FNV-1-64",
        MinNonZeroBytesForLongInput = 6,
        KnownAnswers = new()
        {
            Empty = "CBF29CE484222325",
            Abc = "D86FEA186B53126B",
            QuickBrownFox = "A8B2F3117DE37ACE",
            Zeros16 = "88201FB960FF6465",
            Sequential0To255 = "46F4BC763E8FD1BE",
        },
    };

    /// <inheritdoc />
    /// <remarks>
    /// Entries are the documented FNV-1 64-bit known-answer sequence for incremental inputs
    /// <c>[]</c>, <c>[0x00]</c>, <c>[0x00, 0x01]</c>, … <c>[0x00 .. 0x0E]</c>.
    /// </remarks>
    protected override IEnumerable<string> GetIncrementalHashValue(SingleTestVariant variant) => new[]
    {
        "CBF29CE484222325", "AF63BD4C8601B7DF", "08328807B4EB6FEC", "D94D11186C0F2E06",
        "4D22127F9DCB3431", "DC199FD92049AF47", "4939E4F1DD34D5A0", "A235A6FAE0C6FEE6",
        "6829A24BF22320D5", "21DF9C0C71B0C9E7", "3FC010252F67138C", "BA6EFB2F8C2636EE",
        "F0CBBFCB24EF5661", "198D472FC2AFC6DF", "1AD6D527D0AEECE0", "49F912A7993C80AE",
        "EEBB60C961CEA7A5", "770D1B313226DD4F", "72228398380A0D2C", "FAB2C7A7391461D6",
        "122B1725FDA23EB1", "8177068DFAB086D7", "ADC6FA40F9F51F40", "3E3A7C68BB8419D6",
        "417B3BF6A177E6B5", "BC4D99145EBD0597", "B4E0B49CFB307F8C", "8A5272BED368BAFE",
        "72D3F8413AF5BD81", "13EC57D72F91022F", "6B9B71A5D366B5C0", "3FD7DCC63786D55E",
    };
}
