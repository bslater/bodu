// ---------------------------------------------------------------------------------------------------------------
// <copyright file="JSHashTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography
{
    /// <summary>
    /// Contains unit tests for the <see cref="Pjw32" /> hash algorithm.
    /// </summary>
    [TestClass]
    public partial class Pjw32Tests
        : Security.Cryptography.HashAlgorithmTests<Pjw32Tests, Pjw32, SingleTestVariant>
    {
        /// <inheritdoc />
        protected override HashAlgorithmSpecification GetSpecification(SingleTestVariant variant) => new()
        {
            HashSize = 32,
            InputBlockSize = 1,
            OutputBlockSize = 1,
            IsStateless = false,
            LongInputLength = 200,
            MinNonZeroBytesForLongInput = 2,   // 4 output bytes; PJW has moderate avalanche due to high-bit extraction
            BoundaryLengths = [1, 8, 16, 64],
        };

        /// <inheritdoc />
        protected override Pjw32 CreateAlgorithm() => new Pjw32();

        /// <inheritdoc />
        protected override Pjw32 CreateAlgorithm(SingleTestVariant variant) => new Pjw32();

        /// <inheritdoc />
        protected override IReadOnlyList<string> GetExpectedHashesForIncrementalInput(SingleTestVariant variant) => new[]
        {
            "00000000",  // []
            "00000000",  // [0x00]
            "00000001",  // [0x00, 0x01]
            "00000012",  // [0x00, 0x01, 0x02]
            "00000123",  // [0x00, 0x01, 0x02, 0x03]
            "00001234",  // [0x00, 0x01, 0x02, 0x03, 0x04]
            "00012345",  // [0x00, 0x01, 0x02, 0x03, 0x04, 0x05]
            "00123456",  // [0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06]
            "01234567",  // [0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07]
            "02345679",  // ...
            "0345679B",
            "045679B9",
            "05679B9F",
            "0679B9F9",
            "079B9F9B",
            "09B9F9B9",
        };

        /// <inheritdoc />
        protected override IReadOnlyDictionary<string, string> GetExpectedHashesForNamedInputs(SingleTestVariant variant) => new Dictionary<string, string>
        {
            ["Empty"] = "00000000",
            ["ABC"] = "00004563",
            ["Zeros_16"] = "00000000",
            ["QuickBrownFox"] = "021B6694",
        };
    }
}