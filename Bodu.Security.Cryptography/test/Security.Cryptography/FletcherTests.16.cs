// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Adler32Tests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography
{
    /// <summary>
    /// Contains unit tests for the <see cref="Fletcher16" /> hash algorithm.
    /// </summary>
    [TestClass]
    public partial class Fletcher16Tests
        : Security.Cryptography.FletcherTests<Fletcher16Tests, Fletcher16>
    {
        /// <inheritdoc />
        protected override HashAlgorithmSpecification GetSpecification(SingleTestVariant variant) => new()
        {
            HashSize = 16,
            InputBlockSize = 1,
            OutputBlockSize = 1,
            IsStateless = false,
            LongInputLength = 200,
            MinNonZeroBytesForLongInput = 1,   // conservative — only 2 output bytes total
            BoundaryLengths = [1, 8, 16, 64],
        };

        /// <inheritdoc />
        protected override Fletcher16 CreateAlgorithm() => new Fletcher16();

        protected override Fletcher16 CreateAlgorithm(SingleTestVariant variant) => new Fletcher16();

        protected override IReadOnlyList<string> GetExpectedHashesForIncrementalInput(SingleTestVariant variant) => new[]
        {
            "0000",  // []
            "0000",  // [0x00]
            "0101",  // [0x00, 0x01]
            "0403",  // [0x00, 0x01, 0x02]
            "0A06",  // [0x00, 0x01, 0x02, 0x03]
            "140A",  // [0x00, 0x01, 0x02, 0x03, 0x04]
            "230F",  // [0x00, 0x01, 0x02, 0x03, 0x04, 0x05]
            "3815",  // [0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06]
            "541C",  // [0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07]
            "7824",  // ...
            "A52D",
            "DC37",
            "1F42",
            "6D4E",
            "C85B",
            "3269",
        };

        protected override IReadOnlyDictionary<string, string> GetExpectedHashesForNamedInputs(SingleTestVariant variant) => new Dictionary<string, string>
        {
            ["Empty"] = "0000",
            ["ABC"] = "8BC6",
            ["Zeros_16"] = "0000",
            ["QuickBrownFox"] = "FEE8",
        };
    }
}