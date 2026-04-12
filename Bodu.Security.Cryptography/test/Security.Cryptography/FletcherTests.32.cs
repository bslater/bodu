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
        protected override int ExpectedInputBlockSize => 2;

        /// <inheritdoc />
        protected override int ExpectedOutputBlockSize => 2;

        /// <inheritdoc />
        protected override Fletcher32 CreateAlgorithm() => new Fletcher32();

        protected override Fletcher32 CreateAlgorithm(SingleTestVariant variant) => new Fletcher32();

        protected override IReadOnlyList<string> GetExpectedHashesForIncrementalInput(SingleTestVariant variant) => new[]
        {
            "00000000",
            "00000000",
            "01000100",
            "02020102",
            "05020402",
            "09080406",
            "0E080906",
            "1714090C",
            "1E14100C",
            "2E281014",
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