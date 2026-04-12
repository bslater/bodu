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
        protected override int ExpectedInputBlockSize => 1;

        /// <inheritdoc />
        protected override int ExpectedOutputBlockSize => 1;

        /// <inheritdoc />
        protected override Fletcher16 CreateAlgorithm() => new Fletcher16();

        protected override Fletcher16 CreateAlgorithm(SingleTestVariant variant) => new Fletcher16();

        protected override IReadOnlyList<string> GetExpectedHashesForIncrementalInput(SingleTestVariant variant) => new[]
        {
            "0000",
            "0000",
            "0101",
            "0403",
            "0A06",
            "140A",
            "230F",
            "3815",
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