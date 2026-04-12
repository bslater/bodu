// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Adler32Tests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography
{
    /// <summary>
    /// Contains unit tests for the <see cref="Adler" /> hash algorithm.
    /// </summary>
    [TestClass]
    public partial class Adler32Tests
        : Adler32BaseTests<Adler32Tests, Adler32>
    {
        public override IEnumerable<SingleTestVariant> GetHashAlgorithmVariants() => new[]
        {
            SingleTestVariant.Default
        };

        /// <inheritdoc />
        protected override Adler32 CreateAlgorithm() => new Adler32();

        protected override Adler32 CreateAlgorithm(SingleTestVariant variant) => new Adler32();

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