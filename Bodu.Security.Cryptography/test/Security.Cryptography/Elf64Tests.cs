// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Elf64Tests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography
{
    /// <summary>
    /// Contains unit tests for the <see cref="Elf64" /> hash algorithm.
    /// </summary>
    [TestClass]
    public partial class Elf64Tests
        : Security.Cryptography.HashAlgorithmTests<Elf64Tests, Elf64, Elf64Variant>
    {
        public override IEnumerable<Elf64Variant> GetHashAlgorithmVariants() => new[]
        {
            Elf64Variant.Default,
            Elf64Variant.Seed31,
            Elf64Variant.Seed131,
        };

        protected override HashAlgorithmSpecification GetSpecification(Elf64Variant variant) => new()
        {
            HashSize = 64,
            InputBlockSize = 1,
            OutputBlockSize = 1,
            IsStateless = false,
            LongInputLength = 200,
            MinNonZeroBytesForLongInput = 4,   // conservative for a 64-bit accumulator
            BoundaryLengths = [1, 8, 16, 64],
        };

        protected override Elf64 CreateAlgorithm() => new Elf64();

        protected override Elf64 CreateAlgorithm(Elf64Variant variant) =>
            variant switch
            {
                Elf64Variant.Default => CreateAlgorithm(),
                Elf64Variant.Seed31 => new Elf64
                {
                    Seed = 31
                },
                Elf64Variant.Seed131 => new Elf64
                {
                    Seed = 131
                },
                _ => throw new ArgumentOutOfRangeException(nameof(variant))
            };

        protected override IReadOnlyDictionary<string, string> GetExpectedHashesForNamedInputs(Elf64Variant variant) =>
            variant switch
            {
                Elf64Variant.Default => new Dictionary<string, string>
                {
                    ["Empty"] = "0000000000000000",
                    ["ABC"] = "0000000000004563",
                    ["Zeros_16"] = "0000000000000000",
                    ["QuickBrownFox"] = "06CBBC9912066B07",
                    ["Sequential_0_255"] = "00C794F30E5FBF6E",
                },
                Elf64Variant.Seed31 => new Dictionary<string, string>
                {
                    ["Empty"] = "000000000000001F",
                    ["ABC"] = "0000000000023563",
                    ["Zeros_16"] = "0000000000001F00",
                    ["QuickBrownFox"] = "06CBBC9912066937",
                    ["Sequential_0_255"] = "00C794F30E530F6E",
                },
                Elf64Variant.Seed131 => new Dictionary<string, string>
                {
                    ["Empty"] = "0000000000000083",
                    ["ABC"] = "0000000000087563",
                    ["Zeros_16"] = "0000000000008300",
                    ["QuickBrownFox"] = "06CBBC99120663F7",
                    ["Sequential_0_255"] = "00C794F30E6A4F6E",
                },
                _ => throw new ArgumentOutOfRangeException(nameof(variant))
            };

        protected override IReadOnlyList<string> GetExpectedHashesForIncrementalInput(Elf64Variant variant) =>
            variant switch
            {
                Elf64Variant.Default => new[]
                {
                    "0000000000000000",  // []
                    "0000000000000000",  // [0x00]
                    "0000000000000001",  // [0x00, 0x01]
                    "0000000000000012",  // [0x00, 0x01, 0x02]
                    "0000000000000123",  // [0x00, 0x01, 0x02, 0x03]
                    "0000000000001234",  // [0x00, 0x01, 0x02, 0x03, 0x04]
                    "0000000000012345",  // [0x00, 0x01, 0x02, 0x03, 0x04, 0x05]
                    "0000000000123456",  // [0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06]
                    "0000000001234567",  // [0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07]
                    "0000000012345678",  // ...
                    "0000000123456789",
                    "000000123456789A",
                    "00000123456789AB",
                    "0000123456789ABC",
                    "000123456789ABCD",
                    "00123456789ABCDE",
                },
                Elf64Variant.Seed31 => new[]
                {
                    "000000000000001F",  // []
                    "00000000000001F0",  // [0x00]
                    "0000000000001F01",  // [0x00, 0x01]
                    "000000000001F012",  // [0x00, 0x01, 0x02]
                    "00000000001F0123",  // [0x00, 0x01, 0x02, 0x03]
                    "0000000001F01234",  // [0x00, 0x01, 0x02, 0x03, 0x04]
                    "000000001F012345",  // [0x00, 0x01, 0x02, 0x03, 0x04, 0x05]
                    "00000001F0123456",  // [0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06]
                    "0000001F01234567",  // [0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07]
                    "000001F012345678",  // ...
                    "00001F0123456789",
                    "0001F0123456789A",
                    "001F0123456789AB",
                    "01F0123456789ABC",
                    "0F0123456789ABDD",
                    "00123456789ABD2E",
                },
                Elf64Variant.Seed131 => new[]
                {
                    "0000000000000083",  // []
                    "0000000000000830",  // [0x00]
                    "0000000000008301",  // [0x00, 0x01]
                    "0000000000083012",  // [0x00, 0x01, 0x02]
                    "0000000000830123",  // [0x00, 0x01, 0x02, 0x03]
                    "0000000008301234",  // [0x00, 0x01, 0x02, 0x03, 0x04]
                    "0000000083012345",  // [0x00, 0x01, 0x02, 0x03, 0x04, 0x05]
                    "0000000830123456",  // [0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06]
                    "0000008301234567",  // [0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07]
                    "0000083012345678",  // ...
                    "0000830123456789",
                    "000830123456789A",
                    "00830123456789AB",
                    "0830123456789ABC",
                    "030123456789AB4D",
                    "00123456789AB4EE",
                },
                _ => throw new ArgumentOutOfRangeException(nameof(variant))
            };
    }
}