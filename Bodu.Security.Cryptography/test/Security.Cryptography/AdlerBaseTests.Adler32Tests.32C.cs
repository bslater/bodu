// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Adler32CTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography
{
    /// <summary>
    /// Contains unit tests for the <see cref="Adler" /> hash algorithm.
    /// </summary>
    [TestClass]
    public partial class Adler32CTests
        : Adler32BaseTests<Adler32CTests, Adler32C>
    {
        public override IEnumerable<SingleTestVariant> GetHashAlgorithmVariants() => new[]
        {
            SingleTestVariant.Default
        };

        /// <inheritdoc />
        protected override Adler32C CreateAlgorithm() => new Adler32C();

        protected override Adler32C CreateAlgorithm(SingleTestVariant variant) => new Adler32C();

        protected override IReadOnlyList<string> GetExpectedHashesForIncrementalInput(SingleTestVariant variant) => new[]
        {
            "00000001",
            "00010001",
            "00030002",
            "00070004",
            "000E0007",
            "0019000B",
            "00290010",
            "003F0016",
            "005C001D",
            "00810025",
            "00AF002E",
            "00E70038",
            "012A0043",
            "0179004F",
            "01D5005C",
            "023F006A",
        };

        protected override IReadOnlyDictionary<string, string> GetExpectedHashesForNamedInputs(SingleTestVariant variant) => new Dictionary<string, string>
        {
            ["Empty"] = "00000001",
            ["ABC"] = "018D00C7",
            ["Zeros_16"] = "00100001",
            ["QuickBrownFox"] = "5BCD0FDA",
        };
    }
}