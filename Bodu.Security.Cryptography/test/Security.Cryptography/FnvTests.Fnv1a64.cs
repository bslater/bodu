// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FnvTests.Fnv1a64.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography
{
    /// <summary>
    /// Contains unit tests for the <see cref="Fnv1a64" /> hash algorithm.
    /// </summary>
    [TestClass]
    public partial class Fnv1a64Tests
        : Security.Cryptography.FnvTests<Fnv1a64Tests, Fnv1a64>
    {
        /// <inheritdoc />
        protected override HashAlgorithmSpecification GetSpecification(SingleTestVariant variant) => new()
        {
            HashSize = 64,
            InputBlockSize = 1,
            OutputBlockSize = 1,
            IsStateless = false,
            LongInputLength = 200,
            MinNonZeroBytesForLongInput = 6,   // 8 output bytes; FNV-1a 64-bit has excellent avalanche so nearly all bytes should be non-zero
            BoundaryLengths = [1, 8, 16, 64],
        };

        /// <inheritdoc />
        protected override Fnv1a64 CreateAlgorithm() => new Fnv1a64();

        protected override Fnv1a64 CreateAlgorithm(SingleTestVariant variant) => new Fnv1a64();

        protected override IReadOnlyList<string> GetExpectedHashesForIncrementalInput(SingleTestVariant variant) => new[]
        {
            "CBF29CE484222325",  // []
            "AF63BD4C8601B7DF",  // [0x00]
            "08328707B4EB6E3A",  // [0x00, 0x01]
            "D949AA186C0C4928",  // [0x00, 0x01, 0x02]
            "4475327F98E05411",  // [0x00, 0x01, 0x02, 0x03]
            "3378E3D0C52EDFAF",  // [0x00, 0x01, 0x02, 0x03, 0x04]
            "A54AC5BF0EA60DDE",  // [0x00, 0x01, 0x02, 0x03, 0x04, 0x05]
            "841BDBA5E4298608",  // [0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06]
            "A4DC49E2B28ECB7D",  // [0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07]
            "B11D013568A3B7CF",  // ...
            "9800D3C0CE314572",
            "7AAD489E5DB90AE8",
            "2D7D4819416D7FB9",
            "B96137EA2D10008F",
            "103284EA9230DCE6",
            "B6B4C29669075E38",
        };

        protected override IReadOnlyDictionary<string, string> GetExpectedHashesForNamedInputs(SingleTestVariant variant) => new Dictionary<string, string>
        {
            ["Empty"] = "CBF29CE484222325",
            ["ABC"] = "FA2FE219A07442EB",
            ["Zeros_16"] = "88201FB960FF6465",
            ["QuickBrownFox"] = "F3F9B7F5E7E47110",
            ["Sequential_0_255"] = "49CC0AA461DC8C38",
        };
    }
}