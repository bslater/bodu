// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AdlerTests.Adler32Tests.32.cs" company="PlaceholderCompany">
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
            "00000001",  // []
            "00010001",  // [0x00]
            "00030002",  // [0x00, 0x01]
            "00070004",  // [0x00, 0x01, 0x02]
            "000E0007",  // [0x00, 0x01, 0x02, 0x03]
            "0019000B",  // [0x00, 0x01, 0x02, 0x03, 0x04]
            "00290010",  // [0x00, 0x01, 0x02, 0x03, 0x04, 0x05]
            "003F0016",  // [0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06]
            "005C001D",  // [0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07]
            "00810025",  // ...
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