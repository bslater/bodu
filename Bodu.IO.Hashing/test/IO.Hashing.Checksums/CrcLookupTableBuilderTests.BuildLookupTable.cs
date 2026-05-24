// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CrcLookupTableBuilderTests.BuildLookupTable.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing.Checksums;

public partial class CrcLookupTableBuilderTests
{

    /// <summary>
    /// Verifies that an all-ones polynomial produces a lookup table whose entries never exceed the declared bit width.
    /// </summary>
    [TestMethod]
    public void BuildLookupTable_AllOnesPolynomial_ShouldNotOverflow()
    {
        const int size = 16;
        var poly = (1UL << size) - 1;
        var table = CrcLookupTableBuilder.BuildLookupTable(size, poly, false);

        var mask = ulong.MaxValue >> (64 - size);
        foreach (var value in table)
        {
            Assert.IsTrue((value & ~mask) == 0);
        }
    }

    /// <summary>
    /// Verifies that polynomial bits above the declared <paramref name="size" /> are masked away before the table is populated.
    /// </summary>
    [TestMethod]
    public void BuildLookupTable_PolynomialHasExcessBits_ShouldStillMaskCorrectly()
    {
        const int size = 4;
        var poly = 0xFFUL; // Higher than 4 bits
        var table = CrcLookupTableBuilder.BuildLookupTable(size, poly, false);

        var mask = ulong.MaxValue >> (64 - size);
        foreach (var entry in table)
        {
            Assert.IsTrue((entry & ~mask) == 0, $"Entry {entry:X} exceeds size {size} bits.");
        }
    }

    /// <summary>
    /// Verifies that every entry in the generated lookup table is confined to the declared bit width.
    /// </summary>
    [TestMethod]
    public void BuildLookupTable_ShouldMaskUpperBits()
    {
        const int size = 16;
        var table = CrcLookupTableBuilder.BuildLookupTable(size, 0x8005UL, false);
        var mask = ulong.MaxValue >> (64 - size);

        foreach (var value in table)
        {
            Assert.AreEqual(value, value & mask, $"Value {value:X} exceeds {size}-bit mask.");
        }
    }

    /// <summary>
    /// Verifies that a zero polynomial still produces a well-formed table in which every entry is zero.
    /// </summary>
    [TestMethod]
    public void BuildLookupTable_WhenPolynomialIsZero_ShouldStillGenerateTable()
    {
        var table = CrcLookupTableBuilder.BuildLookupTable(8, 0x00UL, false);

        Assert.IsTrue(Array.TrueForAll(table, v => v == 0), "All entries should be zero with zero polynomial.");
    }

    /// <summary>
    /// Verifies that the non-reflected and reflected variants of the same CRC-8 polynomial produce lookup
    /// tables whose entry at index <c>1</c> is the bit-reversal of one another, confirming that the
    /// <c>reflectIn</c> flag is the sole contributor to the difference.
    /// </summary>
    [TestMethod]
    public void BuildLookupTable_WhenReflected_ShouldBeBitReversedAgainstNonReflected()
    {
        var nonReflected = CrcLookupTableBuilder.BuildLookupTable(8, 0x07UL, reflectIn: false);
        var reflected = CrcLookupTableBuilder.BuildLookupTable(8, 0x07UL, reflectIn: true);

        // Non-reflected table[1] is 0x07; reflected table[0x80] computes the CRC for the "reflected"
        // single-bit input 0x01, which should match the bit-reverse of 0x07 = 0b00000111 → 0b11100000 = 0xE0.
        Assert.AreEqual(0x07UL, nonReflected[0x01]);
        Assert.AreEqual(0xE0UL, reflected[0x80]);
    }

    /// <summary>
    /// Verifies that the reflected and non-reflected tables for the same polynomial differ for at least one entry.
    /// </summary>
    [TestMethod]
    public void BuildLookupTable_WhenReflected_ShouldProduceDifferentEntriesThanNonReflected()
    {
        const int size = 8;
        const ulong polynomial = 0x07;

        var reflected = CrcLookupTableBuilder.BuildLookupTable(size, polynomial, true);
        var nonReflected = CrcLookupTableBuilder.BuildLookupTable(size, polynomial, false);

        var anyDifference = false;
        for (var i = 0; i < reflected.Length; i++)
        {
            if (reflected[i] != nonReflected[i])
            {
                anyDifference = true;
                break;
            }
        }

        Assert.IsTrue(anyDifference, "Reflected and non-reflected tables should differ.");
    }

    /// <summary>
    /// Verifies that a 64-bit polynomial produces a fully populated 256-entry table whose entries fit in 64 bits.
    /// </summary>
    [TestMethod]
    public void BuildLookupTable_WhenSizeIs64_ShouldGenerateFullByteTable()
    {
        var table = CrcLookupTableBuilder.BuildLookupTable(64, 0x42F0E1EBA9EA3693UL, false);

        foreach (var value in table)
        {
            Assert.IsTrue(value <= ulong.MaxValue, "Value should not exceed 64 bits.");
        }
    }

    /// <summary>
    /// Verifies that invalid <paramref name="size" /> values surface via <see cref="ArgumentOutOfRangeException" />
    /// whose <c>ParamName</c> matches the original parameter name, confirming the <see cref="ThrowHelper" />
    /// contract rather than merely relying on the exception type.
    /// </summary>
    [TestMethod]
    [DataRow(-1)]
    [DataRow(0)]
    [DataRow(65)]
    public void BuildLookupTable_WhenSizeIsInvalid_ShouldReportSizeParamName(int size)
    {
        ArgumentOutOfRangeException ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(
            () => CrcLookupTableBuilder.BuildLookupTable(size, 0x1UL, false));
        Assert.AreEqual("size", ex.ParamName);
    }

    /// <summary>
    /// Verifies that an out-of-range <paramref name="size" /> throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    [DataRow(int.MinValue)]
    [DataRow(-1)]
    [DataRow(0)]
    [DataRow(65)]
    [DataRow(int.MaxValue)]
    public void BuildLookupTable_WhenSizeIsInvalid_ShouldThrowExactly(int size)
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = CrcLookupTableBuilder.BuildLookupTable(size, 0x1UL, false);
        });
    }

    /// <summary>
    /// Verifies that the reflected sub-8-bit branch of <see cref="CrcLookupTableBuilder.BuildLookupTable" />
    /// (<c>size &lt; 8</c>, <c>reflectIn = true</c>) produces a 2-entry table and masks its entries to the
    /// declared width.
    /// </summary>
    [TestMethod]
    [DataRow(1)]
    [DataRow(2)]
    [DataRow(4)]
    [DataRow(7)]
    public void BuildLookupTable_WhenSizeIsLessThanEight_AndReflected_ShouldProduceTwoEntryMaskedTable(int size)
    {
        var table = CrcLookupTableBuilder.BuildLookupTable(size, 0x1UL, reflectIn: true);

        Assert.AreEqual(2, table.Length);

        var mask = ulong.MaxValue >> (64 - size);
        foreach (var entry in table)
            Assert.IsTrue((entry & ~mask) == 0, $"Entry {entry:X} exceeds size {size} bits.");
    }

    /// <summary>
    /// Verifies that a 1-bit CRC produces a table whose entries are strictly <c>0</c> or <c>1</c>.
    /// </summary>
    [TestMethod]
    public void BuildLookupTable_WhenSizeIsOne_ShouldRespectBitMasking()
    {
        var table = CrcLookupTableBuilder.BuildLookupTable(1, 0x01, false);
        foreach (var entry in table)
        {
            Assert.IsTrue(entry == 0 || entry == 1, "Only 1-bit values should appear.");
        }
    }

    /// <summary>
    /// Verifies that the generated table length is <c>2</c> for sub-8-bit widths and <c>256</c> for 8-bit or wider CRCs.
    /// </summary>
    [TestMethod]
    [DataRow(1, 2)]     // Less than 8 bits: table size = 2
    [DataRow(2, 2)]
    [DataRow(4, 2)]
    [DataRow(7, 2)]
    [DataRow(8, 256)]   // 8 or more bits: table size = 256
    [DataRow(16, 256)]
    [DataRow(32, 256)]
    [DataRow(64, 256)]
    public void BuildLookupTable_WhenSizeIsValid_ShouldGenerateExpectedSize(int size, int expected)
    {
        var table = CrcLookupTableBuilder.BuildLookupTable(size, 0x1UL, false);

        Assert.AreEqual(expected, table.Length);
    }

    /// <summary>
    /// Verifies that <see cref="CrcLookupTableBuilder.BuildLookupTable" /> produces the canonical first row
    /// of the 8-bit <c>CRC-8/SMBUS</c> lookup table for polynomial <c>0x07</c> in non-reflected mode. The
    /// verified entries are drawn from the published SMBus / CRC-8 reference table, ensuring the builder's
    /// output is numerically correct rather than merely well-formed.
    /// </summary>
    [TestMethod]
    public void BuildLookupTable_WhenStandardIsCRC8_SMBUS_ShouldMatchPublishedEntries()
    {
        var table = CrcLookupTableBuilder.BuildLookupTable(8, 0x07UL, reflectIn: false);

        Assert.AreEqual(256, table.Length);
        Assert.AreEqual(0x00UL, table[0x00]);
        Assert.AreEqual(0x07UL, table[0x01]);
        Assert.AreEqual(0x0EUL, table[0x02]);
        Assert.AreEqual(0x09UL, table[0x03]);
    }

    /// <summary>
    /// Verifies that reflected and non-reflected lookup tables for the same polynomial are not equal.
    /// </summary>
    [TestMethod]
    public void BuildLookupTable_WithAndWithoutReflection_ShouldProduceDifferentTables()
    {
        var table1 = CrcLookupTableBuilder.BuildLookupTable(16, 0x1021UL, false);
        var table2 = CrcLookupTableBuilder.BuildLookupTable(16, 0x1021UL, true);

        CollectionAssert.AreNotEqual(table1, table2);
    }

    /// <summary>
    /// Verifies that different polynomials of the same width produce distinct lookup tables.
    /// </summary>
    [TestMethod]
    public void BuildLookupTable_WithDifferentPolynomials_ShouldProduceDifferentTables()
    {
        var table1 = CrcLookupTableBuilder.BuildLookupTable(16, 0x1021UL, false);
        var table2 = CrcLookupTableBuilder.BuildLookupTable(16, 0x8005UL, false);

        CollectionAssert.AreNotEqual(table1, table2);
    }

}
