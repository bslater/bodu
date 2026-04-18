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
    public partial class Fletcher32Tests
        : Security.Cryptography.FletcherTests<Fletcher32Tests, Fletcher32>
    {
        /// <inheritdoc />
        protected override HashAlgorithmSpecification GetSpecification(SingleTestVariant variant) => new()
        {
            HashSize = 32,
            InputBlockSize = 2,
            OutputBlockSize = 2,
            IsStateless = false,
            LongInputLength = 200,
            MinNonZeroBytesForLongInput = 2,   // 4 output bytes; both sum1 and sum2 should be non-zero for varied input
            BoundaryLengths = [2, 8, 16, 64],
        };

        /// <inheritdoc />
        protected override Fletcher32 CreateAlgorithm() => new Fletcher32();

        protected override Fletcher32 CreateAlgorithm(SingleTestVariant variant) => new Fletcher32();

        protected override IReadOnlyList<string> GetExpectedHashesForIncrementalInput(SingleTestVariant variant) => new[]
        {
            "00000000",  // []
            "00000000",  // [0x00]
            "01000100",  // [0x00, 0x01]
            "02020102",  // [0x00, 0x01, 0x02]
            "05020402",  // [0x00, 0x01, 0x02, 0x03]
            "09080406",  // [0x00, 0x01, 0x02, 0x03, 0x04]
            "0E080906",  // [0x00, 0x01, 0x02, 0x03, 0x04, 0x05]
            "1714090C",  // [0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06]
            "1E14100C",  // [0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07]
            "2E281014",  // ...
            "37281914",
            "5046191E",
            "5B46241E",
            "7F70242A",
            "8C70312A",
            "BDA83138",
        };

        protected override IReadOnlyDictionary<string, string> GetExpectedHashesForNamedInputs(SingleTestVariant variant) => new Dictionary<string, string>
        {
            ["Empty"] = "00000000",
            ["ABC"] = "84C54284",
            ["Zeros_16"] = "00000000",
            ["QuickBrownFox"] = "53CD5B8D",
        };
    }
}