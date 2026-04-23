// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FnvTests.Fnv1a64.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing;

/// <summary>
/// Contains unit tests for the <see cref="Fnv1a64" /> hash algorithm.
/// </summary>
[TestClass]
public sealed partial class Fnv1a64Tests
    : FnvTests<Fnv1a64Tests, Fnv1a64>
{
    /// <inheritdoc />
    protected override NonCryptographicHashAlgorithmSpecification GetSpecification(SingleTestVariant variant) => new()
    {
        HashLengthInBytes = 8,
        AlgorithmName = "FNV-1a-64",
        MinNonZeroBytesForLongInput = 6,
        KnownAnswers = new()
        {
            Empty = "CBF29CE484222325",
            Abc = "FA2FE219A07442EB",
            QuickBrownFox = "F3F9B7F5E7E47110",
            Zeros16 = "88201FB960FF6465",
            Sequential0To255 = "49CC0AA461DC8C38",
        },
    };

    /// <inheritdoc />
    /// <remarks>
    /// Entries are the documented FNV-1a 64-bit known-answer sequence for incremental inputs
    /// <c>[]</c>, <c>[0x00]</c>, <c>[0x00, 0x01]</c>, … <c>[0x00 .. 0x0E]</c>.
    /// </remarks>
    protected override IEnumerable<string> GetIncrementalHashValue(SingleTestVariant variant) => new[]
    {
        "CBF29CE484222325", "AF63BD4C8601B7DF", "08328707B4EB6E3A", "D949AA186C0C4928",
        "4475327F98E05411", "3378E3D0C52EDFAF", "A54AC5BF0EA60DDE", "841BDBA5E4298608",
        "A4DC49E2B28ECB7D", "B11D013568A3B7CF", "9800D3C0CE314572", "7AAD489E5DB90AE8",
        "2D7D4819416D7FB9", "B96137EA2D10008F", "103284EA9230DCE6", "B6B4C29669075E38",
    };
}
