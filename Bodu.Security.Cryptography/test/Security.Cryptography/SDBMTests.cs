// ---------------------------------------------------------------------------------------------------------------
// <copyright file="JSHashTests.cs" company="PlaceholderCompany">
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
        public override IEnumerable<SingleTestVariant> GetHashAlgorithmVariants() => new[]
        {
            SingleTestVariant.Default
        };

        /// <inheritdoc />
        protected override SDBM CreateAlgorithm() => new SDBM();

        protected override SDBM CreateAlgorithm(SingleTestVariant variant) => new SDBM();

        protected override IReadOnlyList<string> GetExpectedHashesForIncrementalInput(SingleTestVariant variant) => new[]
        {
            "00000000",
            "00000000",
            "00000001",
            "00010041",
            "00801002",
            "2F85F082",
            "A2783003",
            "2B96D0C3",
            "8AE06004",
            "8D3BA104",
            "62B0A005",
            "E97C6145",
            "D6E0F006",
            "D1611186",
            "98695007",
            "D1F1B1C7",
        };

        protected override IReadOnlyDictionary<string, string> GetExpectedHashesForNamedInputs(SingleTestVariant variant) => new Dictionary<string, string>
        {
            ["Empty"] = "00000000",
            ["ABC"] = "20440042",
            ["Zeros_16"] = "00000000",
            ["QuickBrownFox"] = "8CA77173",
        };
    }
}