// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ApHashTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography
{
    /// <summary>
    /// Contains unit tests for the <see cref="ApHash" /> hash algorithm.
    /// </summary>
    [TestClass]
    public partial class ApHashTests
        : Security.Cryptography.HashAlgorithmTests<ApHashTests, ApHash, SingleTestVariant>
    {
        /// <inheritdoc />
        protected override ApHash CreateAlgorithm() => new ApHash();

        /// <inheritdoc />
        protected override ApHash CreateAlgorithm(SingleTestVariant variant) => new ApHash();

        /// <inheritdoc />
        public override IEnumerable<SingleTestVariant> GetHashAlgorithmVariants() => new[]
        {
            SingleTestVariant.Default
        };

        /// <inheritdoc />
        protected override IReadOnlyDictionary<string, string> GetExpectedHashesForNamedInputs(SingleTestVariant variant) =>
            new Dictionary<string, string>
            {
                ["Empty"] = "AAAAAAAA",
                ["ABC"] = "1F6547E5",
                ["QuickBrownFox"] = "4724B335",
                ["Zeros_16"] = "8D323DA9",
                ["Sequential_0_255"] = "8EF230D2",
            };

        /// <inheritdoc />
        protected override IReadOnlyList<string> GetExpectedHashesForIncrementalInput(SingleTestVariant variant) => new[]
        {
            "AAAAAAAA",
            "FFAAAAEA",
            "56F85747",
            "5E2C41E4",
            "C02AFE15",
            "9C8F54E8",
            "1A3495B4",
            "9F9F9DE8",
            "9B74DAFC",
            "80777B8E",
            "CA534BAE",
            "B9DC2B9E",
            "A8B5C03B",
            "1177E2DC",
            "5B134236",
            "BEFC8311",
        };
    }
}
