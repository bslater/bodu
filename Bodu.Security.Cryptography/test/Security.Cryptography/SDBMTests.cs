// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SDBMTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography
{
    /// <summary>
    /// Contains unit tests for the <see cref="SDBM" /> hash algorithm.
    /// </summary>
    [TestClass]
    public partial class SDBMTests
        : Security.Cryptography.HashAlgorithmTests<SDBMTests, SDBM, SingleTestVariant>
    {

        /// <inheritdoc />
        protected override SDBM CreateAlgorithm() => new SDBM();

        /// <inheritdoc />
        protected override SDBM CreateAlgorithm(SingleTestVariant variant) => new SDBM();

        /// <inheritdoc />
        protected override HashAlgorithmSpecification GetSpecification(SingleTestVariant variant) => new()
        {
            HashSize = 32,
            InputBlockSize = 1,
            OutputBlockSize = 1,
            IsStateless = false,
            LongInputLength = 200,
            MinNonZeroBytesForLongInput = 2,   // 4 output bytes; SDBM has moderate avalanche, zero initialisation means short inputs may produce sparse output
            BoundaryLengths = [1, 8, 16, 64],
        };

        /// <inheritdoc />
        protected override IReadOnlyList<string> GetExpectedHashesForIncrementalInput(SingleTestVariant variant) => new[]
        {
            "00000000",  // []
            "00000000",  // [0x00]
            "00000001",  // [0x00, 0x01]
            "00010041",  // [0x00, 0x01, 0x02]
            "00801002",  // [0x00, 0x01, 0x02, 0x03]
            "2F85F082",  // [0x00, 0x01, 0x02, 0x03, 0x04]
            "A2783003",  // [0x00, 0x01, 0x02, 0x03, 0x04, 0x05]
            "2B96D0C3",  // [0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06]
            "8AE06004",  // [0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07]
            "8D3BA104",  // ...
            "62B0A005",
            "E97C6145",
            "D6E0F006",
            "D1611186",
            "98695007",
            "D1F1B1C7",
        };

        /// <inheritdoc />
        protected override IReadOnlyDictionary<string, string> GetExpectedHashesForNamedInputs(SingleTestVariant variant) => new Dictionary<string, string>
        {
            ["Empty"] = "00000000",
            ["ABC"] = "20440042",
            ["Zeros_16"] = "00000000",
            ["QuickBrownFox"] = "8CA77173",
        };
    }
}