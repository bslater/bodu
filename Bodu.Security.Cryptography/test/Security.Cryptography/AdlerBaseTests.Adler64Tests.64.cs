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
            "0000000000000001",
            "0000000100000001",
            "0000000300000002",
            "0000000700000004",
            "0000000E00000007",
            "000000190000000B",
            "0000002900000010",
            "0000003F00000016",
            "0000005C0000001D",
            "0000008100000025",
            "000000AF0000002E",
            "000000E700000038",
            "0000012A00000043",
            "000001790000004F",
            "000001D50000005C",
            "0000023F0000006A",
            "000002B800000079",
            "0000034100000089",
            "000003DB0000009A",
            "00000487000000AC",
            "00000546000000BF",
            "00000619000000D3",
            "00000701000000E8",
            "000007FF000000FE",
            "0000091400000115",
            "00000A410000012D",
            "00000B8700000146",
            "00000CE700000160",
            "00000E620000017B",
            "00000FF900000197",
            "000011AD000001B4",
            "0000137F000001D2",
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