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
    public partial class Adler64Tests
        : Adler64BaseTests<Adler64Tests, Adler64>
    {
        public override IEnumerable<SingleTestVariant> GetHashAlgorithmVariants() => new[]
        {
            SingleTestVariant.Default
        };

        /// <inheritdoc />
        protected override Adler64 CreateAlgorithm() => new Adler64();

        protected override Adler64 CreateAlgorithm(SingleTestVariant variant) => new Adler64();

        protected override IReadOnlyList<string> GetExpectedHashesForIncrementalInput(SingleTestVariant variant) => new[]
        {
            "0000000000000001",  // []
            "0000000100000001",  // [0x00]
            "0000000300000002",  // [0x00, 0x01]
            "0000000700000004",  // [0x00, 0x01, 0x02]
            "0000000E00000007",  // [0x00, 0x01, 0x02, 0x03]
            "000000190000000B",  // [0x00, 0x01, 0x02, 0x03, 0x04]
            "0000002900000010",  // [0x00, 0x01, 0x02, 0x03, 0x04, 0x05]
            "0000003F00000016",  // [0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06]
            "0000005C0000001D",  // [0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07]
            "0000008100000025",  // ...
            "000000AF0000002E",
            "000000E700000038",
            "0000012A00000043",
            "000001790000004F",
            "000001D50000005C",
            "0000023F0000006A",
        };

        protected override IReadOnlyDictionary<string, string> GetExpectedHashesForNamedInputs(SingleTestVariant variant) => new Dictionary<string, string>
        {
            ["Empty"] = "0000000000000001",
            ["ABC"] = "0000018D000000C7",
            ["Zeros_16"] = "0000001000000001",
            ["QuickBrownFox"] = "00015BCD00000FDA",
        };
    }
}