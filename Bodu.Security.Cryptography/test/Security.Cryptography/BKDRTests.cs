// -----------------------------------------------------------------------
// <copyright file="BKDR.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Runtime.Intrinsics.X86;

namespace Bodu.Security.Cryptography
{
    public enum BKDRVariant
    {
        Default,
        Seed31,
        Seed1313,
    }

    /// <summary>
    /// Contains unit tests for the <see cref="BKDR" /> hash algorithm.
    /// </summary>
    [TestClass]
    public partial class BKDRTests
        : Security.Cryptography.HashAlgorithmTests<BKDRTests, BKDR, BKDRVariant>
    {
        public override IEnumerable<BKDRVariant> GetHashAlgorithmVariants() => new[]
        {
            BKDRVariant.Default,
            BKDRVariant.Seed31,
            BKDRVariant.Seed1313,
        };

        /// <inheritdoc />
        protected override HashAlgorithmSpecification GetSpecification(BKDRVariant variant) =>
            new HashAlgorithmSpecification
            {
                HashSize = 32,
                InputBlockSize = 1,
                OutputBlockSize = 1,
            };

        protected override BKDR CreateAlgorithm() => new BKDR();

        protected override BKDR CreateAlgorithm(BKDRVariant variant) =>
            variant switch
            {
                BKDRVariant.Default => this.CreateAlgorithm(),
                BKDRVariant.Seed31 => new BKDR
                {
                    Seed = 31
                },
                BKDRVariant.Seed1313 => new BKDR
                {
                    Seed = 1313
                },
                _ => throw new ArgumentOutOfRangeException(nameof(variant))
            };

        protected override IReadOnlyDictionary<string, string> GetExpectedHashesForNamedInputs(BKDRVariant variant) =>
            variant switch
            {
                BKDRVariant.Default => new Dictionary<string, string>
                {
                    ["Empty"] = "00000083",
                    ["ABC"] = "119EDDA3",
                    ["Zeros_16"] = "760E2E43",
                    ["QuickBrownFox"] = "FDAD9AD8",
                    ["Sequential_0_255"] = "752AC0AC",
                },
                BKDRVariant.Seed31 => new Dictionary<string, string>
                {
                    ["Empty"] = "0000001F",
                    ["ABC"] = "000F13C3",
                    ["Zeros_16"] = "C491E21F",
                    ["QuickBrownFox"] = "764D9FD4",
                    ["Sequential_0_255"] = "B7D06C60",
                },
                BKDRVariant.Seed1313 => new Dictionary<string, string>
                {
                    ["Empty"] = "00000521",
                    ["ABC"] = "03CEDDC7",
                    ["Zeros_16"] = "62687721",
                    ["QuickBrownFox"] = "373C737A",
                    ["Sequential_0_255"] = "DA826D62",
                },
                _ => throw new ArgumentOutOfRangeException(nameof(variant))
            };

        protected override IReadOnlyList<string> GetExpectedHashesForIncrementalInput(BKDRVariant variant) =>
            variant switch
            {
                BKDRVariant.Default => new[]
                {
                    "00000083",  // []
                    "00004309",  // [0x00]
                    "00224D9C",  // [0x00, 0x01]
                    "118DB6D6",  // [0x00, 0x01, 0x02]
                    "FB848F85",  // [0x00, 0x01, 0x02, 0x03]
                    "B4D57113",  // [0x00, 0x01, 0x02, 0x03, 0x04]
                    "8938DCBE",  // [0x00, 0x01, 0x02, 0x03, 0x04, 0x05]
                    "3818F540",  // [0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06]
                    "B4C57FC7",  // [0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07]
                    "811062DD",  // ...
                    "0B629720",
                    "D373556A",
                    "3404B549",
                    "9E68C467",
                    "0F9C80C2",
                    "FD15E354",
                },
                BKDRVariant.Seed31 => new[]
                {
                    "0000001F",  // []
                    "000003C1",  // [0x00]
                    "00007460",  // [0x00, 0x01]
                    "000E17A2",  // [0x00, 0x01, 0x02]
                    "01B4DCA1",  // [0x00, 0x01, 0x02, 0x03]
                    "34E6B783",  // [0x00, 0x01, 0x02, 0x03, 0x04]
                    "67F038E2",  // [0x00, 0x01, 0x02, 0x03, 0x04, 0x05]
                    "9616E364",  // [0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06]
                    "2CC58923",  // [0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07]
                    "6BEB9B45",  // ...
                    "1187CD64",
                    "1F71DF26",
                    "CECA05A5",
                    "0A76AF07",
                    "445F31E6",
                    "47870AE8",
                },
                BKDRVariant.Seed1313 => new[]
                {
                    "00000521",  // []
                    "001A4E41",  // [0x00]
                    "86EB5B62",  // [0x00, 0x01]
                    "FD1FB1A4",  // [0x00, 0x01, 0x02]
                    "3F8E1A27",  // [0x00, 0x01, 0x02, 0x03]
                    "F7D4220B",  // [0x00, 0x01, 0x02, 0x03, 0x04]
                    "17029A70",  // [0x00, 0x01, 0x02, 0x03, 0x04, 0x05]
                    "045A1876",  // [0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06]
                    "5217753D",  // [0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07]
                    "0A504DE5",  // ...
                    "E5DF838E",
                    "FF61BB58",
                    "D441DE63",
                    "A5D599CF",
                    "8C89DEBC",
                    "CF1F624A",
                },
                _ => throw new ArgumentOutOfRangeException(nameof(variant))
            };
    }
}