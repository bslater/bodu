// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Adler32Tests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography
{
    /// <summary>
    /// Contains unit tests for the <see cref="Fnv1a32" /> hash algorithm.
    /// </summary>
    [TestClass]
    public partial class Fnv1a32Tests
        : Security.Cryptography.FnvTests<Fnv1a32Tests, Fnv1a32>
    {
        /// <inheritdoc />
        protected override int ExpectedInputBlockSize => 1;

        /// <inheritdoc />
        protected override int ExpectedOutputBlockSize => 1;

        /// <inheritdoc />
        protected override Fnv1a32 CreateAlgorithm() => new Fnv1a32();

        protected override Fnv1a32 CreateAlgorithm(SingleTestVariant variant) => new Fnv1a32();

        protected override IReadOnlyList<string> GetExpectedHashesForIncrementalInput(SingleTestVariant variant) => new[]
        {
            "811C9DC5",
            "050C5D1F",
            "1076963A",
            "22AE7A28",
            "C3AA51B1",
            "BA1E9FEF",
            "E835BD5E",
            "E4991188",
            "6BF6A41D",
            "0A444D0F",
            "2F854072",
            "46C47CE8",
            "4A509959",
            "51E160CF",
            "A7CB5166",
            "8D1126B8",
        };

        protected override IReadOnlyDictionary<string, string> GetExpectedHashesForNamedInputs(SingleTestVariant variant) => new Dictionary<string, string>
        {
            ["Empty"] = "811C9DC5",
            ["ABC"] = "5C842F6B",
            ["Zeros_16"] = "69691905",
            ["QuickBrownFox"] = "048FFF90",
            ["Sequential_0_255"] = "1C2213B8",
        };
    }
}