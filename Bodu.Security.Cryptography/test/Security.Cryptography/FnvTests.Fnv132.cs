// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Adler32Tests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography
{
    /// <summary>
    /// Contains unit tests for the <see cref="Fnv132" /> hash algorithm.
    /// </summary>
    [TestClass]
    public partial class Fnv132Tests
        : Security.Cryptography.FnvTests<Fnv132Tests, Fnv132>
    {
        /// <inheritdoc />
        protected override int ExpectedInputBlockSize => 1;

        /// <inheritdoc />
        protected override int ExpectedOutputBlockSize => 1;

        /// <inheritdoc />
        protected override Fnv132 CreateAlgorithm() => new Fnv132();

        protected override Fnv132 CreateAlgorithm(SingleTestVariant variant) => new Fnv132();

        protected override IReadOnlyList<string> GetExpectedHashesForIncrementalInput(SingleTestVariant variant) => new[]
        {
            "811C9DC5",
            "050C5D1F",
            "117697CC",
            "49B0F626",
            "27937DD1",
            "1E2F1007",
            "8B163B00",
            "F3FEE106",
            "203C3C75",
            "33D32C27",
            "BC6E816C",
            "0DF5BD0E",
            "07D89D01",
            "5AFF289F",
            "DEACF240",
            "CA415ACE",
        };

        protected override IReadOnlyDictionary<string, string> GetExpectedHashesForNamedInputs(SingleTestVariant variant) => new Dictionary<string, string>
        {
            ["Empty"] = "811C9DC5",
            ["ABC"] = "634CAFEB",
            ["Zeros_16"] = "69691905",
            ["QuickBrownFox"] = "E9C86C6E",
            ["Sequential_0_255"] = "5051A61E",
        };
    }
}