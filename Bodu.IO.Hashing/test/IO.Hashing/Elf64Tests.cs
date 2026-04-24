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
            "0123456789ABCDEF", "023456789ABCDF10", "03456789ABCDF131", "0456789ABCDF1312",
            "056789ABCDF13173", "06789ABCDF131714", "0789ABCDF1317135", "089ABCDF13171316",
            "09ABCDF1317131F7", "0ABCDF1317131F18", "0BCDF1317131F139", "0CDF1317131F131A",
            "0DF1317131F1317B", "0F1317131F13171C", "01317131F131712D", "0317131F131712FE",
        },
        Elf64Variant.Seed31 => new[]
        {
            "000000000000001F", "00000000000001F0", "0000000000001F01", "000000000001F012",
            "00000000001F0123", "0000000001F01234", "000000001F012345", "00000001F0123456",
            "0000001F01234567", "000001F012345678", "00001F0123456789", "0001F0123456789A",
            "001F0123456789AB", "01F0123456789ABC", "0F0123456789ABDD", "00123456789ABD2E",
            "0123456789ABD2EF", "023456789ABD2F10", "03456789ABD2F131", "0456789ABD2F1312",
            "056789ABD2F13173", "06789ABD2F131714", "0789ABD2F1317135", "089ABD2F13171316",
            "09ABD2F1317131F7", "0ABD2F1317131F18", "0BD2F1317131F139", "0D2F1317131F131A",
            "02F1317131F1316B", "0F1317131F1316EC", "01317131F1316E2D", "0317131F1316E2FE",
        },
        Elf64Variant.Seed131 => new[]
        {
            "0000000000000083", "0000000000000830", "0000000000008301", "0000000000083012",
            "0000000000830123", "0000000008301234", "0000000083012345", "0000000830123456",
            "0000008301234567", "0000083012345678", "0000830123456789", "000830123456789A",
            "00830123456789AB", "0830123456789ABC", "030123456789AB4D", "00123456789AB4EE",
            "0123456789AB4EEF", "023456789AB4EF10", "03456789AB4EF131", "0456789AB4EF1312",
            "056789AB4EF13173", "06789AB4EF131714", "0789AB4EF1317135", "089AB4EF13171316",
            "09AB4EF1317131F7", "0AB4EF1317131F18", "0B4EF1317131F139", "04EF1317131F131A",
            "0EF1317131F131FB", "0F1317131F131F2C", "01317131F131F22D", "0317131F131F22FE",
        },
        _ => throw new ArgumentOutOfRangeException(nameof(variant)),
    };
}
