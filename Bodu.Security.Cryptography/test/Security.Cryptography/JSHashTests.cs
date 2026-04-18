// ---------------------------------------------------------------------------------------------------------------
// <copyright file="JSHashTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography
{
    /// <summary>
    /// Contains unit tests for the <see cref="JSHash" /> hash algorithm.
    /// </summary>
    [TestClass]
    public partial class JSHashTests
        : Security.Cryptography.HashAlgorithmTests<JSHashTests, JSHash, SingleTestVariant>
    {
        public override IEnumerable<SingleTestVariant> GetHashAlgorithmVariants() => new[]
        {
            SingleTestVariant.Default
        };

        /// <inheritdoc />
        protected override HashAlgorithmSpecification GetSpecification(SingleTestVariant variant) => new()
        {
            HashSize = 32,
            InputBlockSize = 1,
            OutputBlockSize = 1,
            IsStateless = false,
            LongInputLength = 200,
            MinNonZeroBytesForLongInput = 2,   // 4 output bytes; JS hash has moderate avalanche, conservative threshold
            BoundaryLengths = [1, 8, 16, 64],
        };

        /// <inheritdoc />
        protected override JSHash CreateAlgorithm() => new JSHash();

        /// <inheritdoc />
        protected override JSHash CreateAlgorithm(SingleTestVariant variant) => new JSHash();

        /// <inheritdoc />
        protected override IReadOnlyList<string> GetExpectedHashesForIncrementalInput(SingleTestVariant variant) => new[]
        {
            "A7C6674E",  // []
            "2E00F5AE",  // [0x00]
            "E245A8A4",  // [0x00, 0x01]
            "58889A1A",  // [0x00, 0x01, 0x02]
            "41256D43",  // [0x00, 0x01, 0x02, 0x03]
            "35D4123D",  // [0x00, 0x01, 0x02, 0x03, 0x04]
            "87EF8D8C",  // [0x00, 0x01, 0x02, 0x03, 0x04, 0x05]
            "40836C38",  // [0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06]
            "970BC723",  // [0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07]
            "5A3E14A2",  // ...
            "85E418C9",
            "4E2D7A9C",
            "50181E2A",
            "70885464",
            "59B8F2C7",
            "1D01A1F7",
        };

        /// <inheritdoc />
        protected override IReadOnlyDictionary<string, string> GetExpectedHashesForNamedInputs(SingleTestVariant variant) => new Dictionary<string, string>
        {
            ["Empty"] = "A7C6674E",
            ["ABC"] = "37889B1A",
            ["Zeros_16"] = "B1DD413D",
            ["QuickBrownFox"] = "38FEEFDF",
        };
    }
}