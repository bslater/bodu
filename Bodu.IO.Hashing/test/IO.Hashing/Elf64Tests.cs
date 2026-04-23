// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Elf64Tests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing;

/// <summary>
/// Contains unit tests for the <see cref="Elf64" /> hash algorithm.
/// </summary>
[TestClass]
public sealed partial class Elf64Tests
    : NonCryptographicHashAlgorithmTests<Elf64Tests, Elf64, Elf64Variant>
{
    /// <inheritdoc />
    protected override Elf64 CreateAlgorithm(Elf64Variant variant) => variant switch
    {
        Elf64Variant.Default => new Elf64(),
        Elf64Variant.Seed31 => new Elf64(31UL),
        Elf64Variant.Seed131 => new Elf64(131UL),
        _ => throw new ArgumentOutOfRangeException(nameof(variant)),
    };

    /// <inheritdoc />
    protected override NonCryptographicHashAlgorithmSpecification GetSpecification(Elf64Variant variant) => variant switch
    {
        Elf64Variant.Default => new()
        {
            HashLengthInBytes = 8,
            MinNonZeroBytesForLongInput = 4,
            KnownAnswers = new()
            {
                Empty = "0000000000000000",
                Abc = "0000000000004563",
                QuickBrownFox = "06CBBC9912066B07",
                Zeros16 = "0000000000000000",
                Sequential0To255 = "00C794F30E5FBF6E",
            },
        },
        Elf64Variant.Seed31 => new()
        {
            HashLengthInBytes = 8,
            MinNonZeroBytesForLongInput = 4,
            KnownAnswers = new()
            {
                Empty = "000000000000001F",
                Abc = "0000000000023563",
                QuickBrownFox = "06CBBC9912066937",
                Zeros16 = "0000000000001F00",
                Sequential0To255 = "00C794F30E530F6E",
            },
        },
        Elf64Variant.Seed131 => new()
        {
            HashLengthInBytes = 8,
            MinNonZeroBytesForLongInput = 4,
            KnownAnswers = new()
            {
                Empty = "0000000000000083",
                Abc = "0000000000087563",
                QuickBrownFox = "06CBBC99120663F7",
                Zeros16 = "0000000000008300",
                Sequential0To255 = "00C794F30E6A4F6E",
            },
        },
        _ => throw new ArgumentOutOfRangeException(nameof(variant)),
    };

    /// <inheritdoc />
    /// <remarks>
    /// Entries are the documented ELF-64 known-answer sequences for incremental inputs
    /// <c>[]</c>, <c>[0x00]</c>, <c>[0x00, 0x01]</c>, … <c>[0x00 .. 0x0E]</c>.
    /// </remarks>
    protected override IEnumerable<string> GetIncrementalHashValue(Elf64Variant variant) => variant switch
    {
        Elf64Variant.Default => new[]
        {
            "0000000000000000", "0000000000000000", "0000000000000001", "0000000000000012",
            "0000000000000123", "0000000000001234", "0000000000012345", "0000000000123456",
            "0000000001234567", "0000000012345678", "0000000123456789", "000000123456789A",
            "00000123456789AB", "0000123456789ABC", "000123456789ABCD", "00123456789ABCDE",
        },
        Elf64Variant.Seed31 => new[]
        {
            "000000000000001F", "00000000000001F0", "0000000000001F01", "000000000001F012",
            "00000000001F0123", "0000000001F01234", "000000001F012345", "00000001F0123456",
            "0000001F01234567", "000001F012345678", "00001F0123456789", "0001F0123456789A",
            "001F0123456789AB", "01F0123456789ABC", "0F0123456789ABDD", "00123456789ABD2E",
        },
        Elf64Variant.Seed131 => new[]
        {
            "0000000000000083", "0000000000000830", "0000000000008301", "0000000000083012",
            "0000000000830123", "0000000008301234", "0000000083012345", "0000000830123456",
            "0000008301234567", "0000083012345678", "0000830123456789", "000830123456789A",
            "00830123456789AB", "0830123456789ABC", "030123456789AB4D", "00123456789AB4EE",
        },
        _ => throw new ArgumentOutOfRangeException(nameof(variant)),
    };
}
