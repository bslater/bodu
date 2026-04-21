namespace Bodu.Security.Cryptography
{
    [TestClass]
    public partial class PearsonTests
        : Security.Cryptography.HashAlgorithmTests<PearsonTests, Pearson, Pearson.PearsonTableType>
    {
        /// <inheritdoc />
        protected override Pearson CreateAlgorithm() => new Pearson();

        /// <inheritdoc />
        public override IEnumerable<Pearson.PearsonTableType> GetHashAlgorithmVariants()
        {
            return base.GetHashAlgorithmVariants().Where(e => e != Pearson.PearsonTableType.UserDefined);
        }

        /// <inheritdoc />
        protected override HashAlgorithmSpecification GetSpecification(Pearson.PearsonTableType variant) => new()
        {
            HashSize = 8,
            InputBlockSize = 1,
            OutputBlockSize = 1,
            IsStateless = false,
            LongInputLength = 200,
            MinNonZeroBytesForLongInput = 1,   // only 1 output byte total, must be non-zero for varied input
            BoundaryLengths = [1, 8, 16, 64],
        };

        /// <inheritdoc />
        protected override Pearson CreateAlgorithm(Pearson.PearsonTableType variant)
        {
            var algorithm = new Pearson();
            algorithm.TableType = variant;
            return algorithm;
        }

        /// <inheritdoc />
        protected override IReadOnlyList<string> GetExpectedHashesForIncrementalInput(Pearson.PearsonTableType variant) =>
            variant switch
            {
                Pearson.PearsonTableType.Pearson => new[]
                {
                    "00",  // []
                    "01",  // [0x00]
                    "01",  // [0x00, 0x01]
                    "0C",  // [0x00, 0x01, 0x02]
                    "A3",  // [0x00, 0x01, 0x02, 0x03]
                    "10",  // [0x00, 0x01, 0x02, 0x03, 0x04]
                    "55",  // [0x00, 0x01, 0x02, 0x03, 0x04, 0x05]
                    "3A",  // [0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06]
                    "E3",  // [0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07]
                    "C4",  // ...
                    "CD",
                    "04",
                    "A3",
                    "7B",
                    "C9",
                    "04",
               },
                Pearson.PearsonTableType.AESSBox => new[]
                {
                    "00",  // []
                    "63",  // [0x00]
                    "AA",  // [0x00, 0x01]
                    "C2",  // [0x00, 0x01, 0x02]
                    "78",  // [0x00, 0x01, 0x02, 0x03]
                    "10",  // [0x00, 0x01, 0x02, 0x03, 0x04]
                    "59",  // [0x00, 0x01, 0x02, 0x03, 0x04, 0x05]
                    "CF",  // [0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06]
                    "E8",  // [0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07]
                    "E1",  // ...
                    "9B",
                    "81",
                    "7E",
                    "40",
                    "E3",
                    "55",
                },
                Pearson.PearsonTableType.CRC32HighByte => new[]
                {
                    "00",  // []
                    "00",  // [0x00]
                    "77",  // [0x00, 0x01]
                    "20",  // [0x00, 0x01, 0x02]
                    "A2",  // [0x00, 0x01, 0x02, 0x03]
                    "3F",  // [0x00, 0x01, 0x02, 0x03, 0x04]
                    "C6",  // [0x00, 0x01, 0x02, 0x03, 0x04, 0x05]
                    "9B",  // [0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06]
                    "F9",  // [0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07]
                    "CA",  // ...
                    "02",
                    "0E",
                    "70",
                    "59",
                    "6C",
                    "A3",
                },
                Pearson.PearsonTableType.SHA256Constants => new[]
                {
                    "00",  // []
                    "48",  // [0x00]
                    "87",  // [0x00, 0x01]
                    "63",  // [0x00, 0x01, 0x02]
                    "B3",  // [0x00, 0x01, 0x02, 0x03]
                    "0F",  // [0x00, 0x01, 0x02, 0x03, 0x04]
                    "A0",  // [0x00, 0x01, 0x02, 0x03, 0x04, 0x05]
                    "09",  // [0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06]
                    "E6",  // [0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07]
                    "56",  // ...
                    "FF",
                    "05",
                    "E6",
                    "06",
                    "3C",
                    "34",
                },
                _ => throw new ArgumentOutOfRangeException(nameof(variant), variant, null)
            };

        /// <inheritdoc />
        protected override IReadOnlyDictionary<string, string> GetExpectedHashesForNamedInputs(Pearson.PearsonTableType variant) =>
            variant switch
            {
                Pearson.PearsonTableType.Pearson => new Dictionary<string, string>
                {
                    ["Empty"] = "00",
                    ["ABC"] = "51",
                    ["Zeros_16"] = "7A",
                    ["QuickBrownFox"] = "A5",
                },
                Pearson.PearsonTableType.AESSBox => new Dictionary<string, string>
                {
                    ["Empty"] = "00",
                    ["ABC"] = "E2",
                    ["Zeros_16"] = "D5",
                    ["QuickBrownFox"] = "46",
                },
                Pearson.PearsonTableType.CRC32HighByte => new Dictionary<string, string>
                {
                    ["Empty"] = "00",
                    ["ABC"] = "DF",
                    ["Zeros_16"] = "00",
                    ["QuickBrownFox"] = "F0",
                },
                Pearson.PearsonTableType.SHA256Constants => new Dictionary<string, string>
                {
                    ["Empty"] = "00",
                    ["ABC"] = "A0",
                    ["Zeros_16"] = "DD",
                    ["QuickBrownFox"] = "C4",
                },
                _ => throw new ArgumentOutOfRangeException(nameof(variant), variant, null)
            };
    }
}