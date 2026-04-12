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
        protected override JSHash CreateAlgorithm() => new JSHash();

        protected override JSHash CreateAlgorithm(SingleTestVariant variant) => new JSHash();

        protected override IReadOnlyList<string> GetExpectedHashesForIncrementalInput(SingleTestVariant variant) => new[]
        {
            "A7C6674E",
            "2E00F5AE",
            "E245A8A4",
            "58889A1A",
            "41256D43",
            "35D4123D",
            "87EF8D8C",
            "40836C38",
            "970BC723",
            "5A3E14A2",
            "85E418C9",
            "4E2D7A9C",
            "50181E2A",
            "70885464",
            "59B8F2C7",
            "1D01A1F7",
        };

        protected override IReadOnlyDictionary<string, string> GetExpectedHashesForNamedInputs(SingleTestVariant variant) => new Dictionary<string, string>
        {
            ["Empty"] = "A7C6674E",
            ["ABC"] = "37889B1A",
            ["Zeros_16"] = "B1DD413D",
            ["QuickBrownFox"] = "38FEEFDF",
        };
    }
}