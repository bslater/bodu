// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NumericExtensionsTests.ReverseBits.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Bodu.Extensions;

public partial class NumericExtensionsTests
{
    // --------------------------------------------------
    // byte
    // --------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="NumericExtensions.ReverseBits(byte)" /> returns the bit-reversed value
    /// for a representative set of <see cref="byte" /> inputs.
    /// </summary>
    [TestMethod]
    [DataRow((byte)0x00, (byte)0x00)] // all bits zero
    [DataRow((byte)0x01, (byte)0x80)] // only LSB set
    [DataRow((byte)0x80, (byte)0x01)] // only MSB set
    [DataRow((byte)0xFF, (byte)0xFF)] // all bits set
    [DataRow((byte)0xAA, (byte)0x55)] // alternating: 10101010 → 01010101
    [DataRow((byte)0x55, (byte)0xAA)] // alternating: 01010101 → 10101010
    [DataRow((byte)0x0F, (byte)0xF0)] // low nibble → high nibble
    [DataRow((byte)0xF0, (byte)0x0F)] // high nibble → low nibble
    [DataRow((byte)0x12, (byte)0x48)] // arbitrary non-palindromic value
    [DataRow((byte)0x3F, (byte)0xFC)] // 6-bit run in low half
    [DataRow((byte)0xC0, (byte)0x03)] // 2 bits in high byte
    public void ReverseBits_WhenValueIsByte_ShouldReturnBitReversedValue(byte value, byte expected)
    {
        byte actual = value.ReverseBits();

        Trace.WriteLineIf(actual != expected, $"value   : {value:X2}");
        Trace.WriteLineIf(actual != expected, $"expected: {expected:X2}");
        Trace.WriteLineIf(actual != expected, $"actual  : {actual:X2}");

        Assert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Verifies that applying <see cref="NumericExtensions.ReverseBits(byte)" /> twice returns the original value
    /// for a representative set of <see cref="byte" /> inputs.
    /// </summary>
    [TestMethod]
    [DataRow((byte)0x00)]
    [DataRow((byte)0x01)]
    [DataRow((byte)0xAA)]
    [DataRow((byte)0xFF)]
    [DataRow((byte)0x3C)]
    public void ReverseBits_WhenAppliedTwice_ForByte_ShouldReturnOriginalValue(byte value) =>
        Assert.AreEqual(value, value.ReverseBits().ReverseBits());

    // --------------------------------------------------
    // ushort
    // --------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="NumericExtensions.ReverseBits(ushort)" /> returns the bit-reversed value
    /// for a representative set of <see cref="ushort" /> inputs.
    /// </summary>
    [TestMethod]
    [DataRow((ushort)0x0000, (ushort)0x0000)] // all bits zero
    [DataRow((ushort)0x0001, (ushort)0x8000)] // only LSB set
    [DataRow((ushort)0x8000, (ushort)0x0001)] // only MSB set
    [DataRow((ushort)0xFFFF, (ushort)0xFFFF)] // all bits set
    [DataRow((ushort)0xAAAA, (ushort)0x5555)] // alternating: 10101010... → 01010101...
    [DataRow((ushort)0x5555, (ushort)0xAAAA)] // alternating: 01010101... → 10101010...
    [DataRow((ushort)0x0F0F, (ushort)0xF0F0)] // nibble-alternating pattern
    [DataRow((ushort)0xF0F0, (ushort)0x0F0F)] // inverse nibble-alternating pattern
    [DataRow((ushort)0x1234, (ushort)0x2C48)] // arbitrary non-palindromic value
    [DataRow((ushort)0x00FF, (ushort)0xFF00)] // lower byte set
    [DataRow((ushort)0x0100, (ushort)0x0080)] // single bit in high byte
    public void ReverseBits_WhenValueIsUShort_ShouldReturnBitReversedValue(ushort value, ushort expected)
    {
        ushort actual = value.ReverseBits();

        Trace.WriteLineIf(actual != expected, $"value   : {value:X4}");
        Trace.WriteLineIf(actual != expected, $"expected: {expected:X4}");
        Trace.WriteLineIf(actual != expected, $"actual  : {actual:X4}");

        Assert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Verifies that applying <see cref="NumericExtensions.ReverseBits(ushort)" /> twice returns the original value.
    /// </summary>
    [TestMethod]
    [DataRow((ushort)0x0000)]
    [DataRow((ushort)0x0001)]
    [DataRow((ushort)0xAAAA)]
    [DataRow((ushort)0xFFFF)]
    [DataRow((ushort)0x1234)]
    public void ReverseBits_WhenAppliedTwice_ForUShort_ShouldReturnOriginalValue(ushort value) =>
        Assert.AreEqual(value, value.ReverseBits().ReverseBits());

    // --------------------------------------------------
    // uint
    // --------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="NumericExtensions.ReverseBits(uint)" /> returns the bit-reversed value
    /// for a representative set of <see cref="uint" /> inputs.
    /// </summary>
    [TestMethod]
    [DataRow(0x00000000U, 0x00000000U)] // all bits zero
    [DataRow(0x00000001U, 0x80000000U)] // only LSB set
    [DataRow(0x80000000U, 0x00000001U)] // only MSB set
    [DataRow(0xFFFFFFFFU, 0xFFFFFFFFU)] // all bits set
    [DataRow(0xAAAAAAAAU, 0x55555555U)] // alternating: 1010... → 0101...
    [DataRow(0x55555555U, 0xAAAAAAAAU)] // alternating: 0101... → 1010...
    [DataRow(0x0F0F0F0FU, 0xF0F0F0F0U)] // nibble-alternating pattern
    [DataRow(0xF0F0F0F0U, 0x0F0F0F0FU)] // inverse nibble-alternating pattern
    [DataRow(0x01234567U, 0xE6A2C480U)] // arbitrary non-palindromic value
    [DataRow(0x0000FFFFU, 0xFFFF0000U)] // lower 16 bits set
    [DataRow(0x000000FFU, 0xFF000000U)] // lower 8 bits set
    [DataRow(0x00000100U, 0x00800000U)] // single bit in second byte
    public void ReverseBits_WhenValueIsUInt_ShouldReturnBitReversedValue(uint value, uint expected)
    {
        uint actual = value.ReverseBits();

        Trace.WriteLineIf(actual != expected, $"value   : {value:X8}");
        Trace.WriteLineIf(actual != expected, $"expected: {expected:X8}");
        Trace.WriteLineIf(actual != expected, $"actual  : {actual:X8}");

        Assert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Verifies that applying <see cref="NumericExtensions.ReverseBits(uint)" /> twice returns the original value.
    /// </summary>
    [TestMethod]
    [DataRow(0x00000000U)]
    [DataRow(0x00000001U)]
    [DataRow(0xAAAAAAAAU)]
    [DataRow(0xFFFFFFFFU)]
    [DataRow(0x01234567U)]
    public void ReverseBits_WhenAppliedTwice_ForUInt_ShouldReturnOriginalValue(uint value) =>
        Assert.AreEqual(value, value.ReverseBits().ReverseBits());

    // --------------------------------------------------
    // ulong
    // --------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="NumericExtensions.ReverseBits(ulong)" /> returns the bit-reversed value
    /// for a representative set of <see cref="ulong" /> inputs.
    /// </summary>
    [TestMethod]
    [DataRow(0x0000000000000000UL, 0x0000000000000000UL)] // all bits zero
    [DataRow(0x0000000000000001UL, 0x8000000000000000UL)] // only LSB set
    [DataRow(0x8000000000000000UL, 0x0000000000000001UL)] // only MSB set
    [DataRow(0xFFFFFFFFFFFFFFFFUL, 0xFFFFFFFFFFFFFFFFUL)] // all bits set
    [DataRow(0xAAAAAAAAAAAAAAAAUL, 0x5555555555555555UL)] // alternating: 1010... → 0101...
    [DataRow(0x5555555555555555UL, 0xAAAAAAAAAAAAAAAAUL)] // alternating: 0101... → 1010...
    [DataRow(0x0F0F0F0F0F0F0F0FUL, 0xF0F0F0F0F0F0F0F0UL)] // nibble-alternating pattern
    [DataRow(0xF0F0F0F0F0F0F0F0UL, 0x0F0F0F0F0F0F0F0FUL)] // inverse nibble-alternating pattern
    [DataRow(0x0123456789ABCDEFUL, 0xF7B3D591E6A2C480UL)] // arbitrary non-palindromic value
    [DataRow(0x0000FFFF0000FFFFUL, 0xFFFF0000FFFF0000UL)] // word-alternating pattern
    [DataRow(0x00000000FFFFFFFFUL, 0xFFFFFFFF00000000UL)] // lower 32 bits set
    [DataRow(0xFFFFFFFF00000000UL, 0x00000000FFFFFFFFUL)] // upper 32 bits set
    [DataRow(0x000000000000FFFFUL, 0xFFFF000000000000UL)] // lower 16 bits set
    [DataRow(0x0000000100000000UL, 0x0000000080000000UL)] // single bit in upper half
    public void ReverseBits_WhenValueIsULong_ShouldReturnBitReversedValue(ulong value, ulong expected)
    {
        ulong actual = value.ReverseBits();

        Trace.WriteLineIf(actual != expected, $"value   : {value:X16}");
        Trace.WriteLineIf(actual != expected, $"expected: {expected:X16}");
        Trace.WriteLineIf(actual != expected, $"actual  : {actual:X16}");

        Assert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Verifies that applying <see cref="NumericExtensions.ReverseBits(ulong)" /> twice returns the original value.
    /// </summary>
    [TestMethod]
    [DataRow(0x0000000000000000UL)]
    [DataRow(0x0000000000000001UL)]
    [DataRow(0xAAAAAAAAAAAAAAAAUL)]
    [DataRow(0xFFFFFFFFFFFFFFFFUL)]
    [DataRow(0x0123456789ABCDEFUL)]
    public void ReverseBits_WhenAppliedTwice_ForULong_ShouldReturnOriginalValue(ulong value) =>
        Assert.AreEqual(value, value.ReverseBits().ReverseBits());

    // --------------------------------------------------
    // byte[] — null / exception
    // --------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="NumericExtensions.ReverseBits(byte[])" /> throws
    /// <see cref="ArgumentNullException" /> when the input array is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void ReverseBits_WhenByteArrayIsNull_ShouldThrowException() =>
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            ((byte[])null!).ReverseBits();
        });

    // --------------------------------------------------
    // byte[] — valid inputs
    // --------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="NumericExtensions.ReverseBits(byte[])" /> returns a new array in which
    /// the bits of each byte are individually reversed, for a representative set of inputs.
    /// </summary>
    [TestMethod]
    [DataRow(new byte[] { 0x00 }, new byte[] { 0x00 })]             // single byte, all zero
    [DataRow(new byte[] { 0x01 }, new byte[] { 0x80 })]             // single byte, LSB set
    [DataRow(new byte[] { 0x80 }, new byte[] { 0x01 })]             // single byte, MSB set
    [DataRow(new byte[] { 0xFF }, new byte[] { 0xFF })]             // single byte, all set
    [DataRow(new byte[] { 0xAA }, new byte[] { 0x55 })]             // alternating 10101010
    [DataRow(new byte[] { 0x55 }, new byte[] { 0xAA })]             // alternating 01010101
    [DataRow(new byte[] { 0x0F, 0xF0 }, new byte[] { 0xF0, 0x0F })]       // nibble pairs swapped per byte
    [DataRow(new byte[] { 0xF0, 0x0F }, new byte[] { 0x0F, 0xF0 })]       // inverse of above
    [DataRow(new byte[] { 0x12, 0x34 }, new byte[] { 0x48, 0x2C })]       // arbitrary two-byte value
    [DataRow(new byte[] { 0x3F, 0xC0 }, new byte[] { 0xFC, 0x03 })]       // 6-bit run mirrored
    [DataRow(new byte[] { 0x01, 0x80 }, new byte[] { 0x80, 0x01 })]       // LSB and MSB in separate bytes
    [DataRow(new byte[] { 0x00, 0x00, 0x00 }, new byte[] { 0x00, 0x00, 0x00 })] // three bytes, all zero
    [DataRow(new byte[] { 0xAA, 0xAA, 0xAA }, new byte[] { 0x55, 0x55, 0x55 })] // three bytes, alternating 10101010
    [DataRow(new byte[] { 0x55, 0x55, 0x55 }, new byte[] { 0xAA, 0xAA, 0xAA })] // three bytes, alternating 01010101
    [DataRow(
        new byte[] { 0x00, 0x00, 0x00, 0x00 },
        new byte[] { 0x00, 0x00, 0x00, 0x00 })]                                  // four bytes, all zero
    [DataRow(
        new byte[] { 0x01, 0x01, 0x01, 0x01 },
        new byte[] { 0x80, 0x80, 0x80, 0x80 })]                                  // four bytes, LSB set in each
    [DataRow(
        new byte[] { 0xFF, 0xFF, 0xFF, 0xFF },
        new byte[] { 0xFF, 0xFF, 0xFF, 0xFF })]                                  // four bytes, all set
    [DataRow(
        new byte[] { 0x00, 0xFF, 0x00, 0xFF },
        new byte[] { 0x00, 0xFF, 0x00, 0xFF })]                                  // alternating 00000000 / 11111111
    public void ReverseBits_WhenByteArrayIsValid_ShouldReturnBitReversedArray(byte[] bytes, byte[] expected)
    {
        byte[] actual = bytes.ReverseBits();

        Trace.WriteLineIf(!actual.SequenceEqual(expected), $"Input   : {BitConverter.ToString(bytes)}");
        Trace.WriteLineIf(!actual.SequenceEqual(expected), $"Expected: {BitConverter.ToString(expected)}");
        Trace.WriteLineIf(!actual.SequenceEqual(expected), $"Actual  : {BitConverter.ToString(actual)}");

        CollectionAssert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Verifies that <see cref="NumericExtensions.ReverseBits(byte[])" /> returns an empty array unchanged.
    /// </summary>
    [TestMethod]
    public void ReverseBits_WhenByteArrayIsEmpty_ShouldReturnEmptyArray() =>
        Assert.AreEqual(0, Array.Empty<byte>().ReverseBits().Length);

    /// <summary>
    /// Verifies that <see cref="NumericExtensions.ReverseBits(byte[])" /> does not modify the original
    /// array — the result is a distinct new allocation.
    /// </summary>
    [TestMethod]
    public void ReverseBits_WhenByteArrayIsValid_ShouldNotMutateOriginalArray()
    {
        byte[] input = { 0x01, 0x02, 0x03, 0x04 };
        byte[] snapshot = (byte[])input.Clone();

        _ = input.ReverseBits();

        CollectionAssert.AreEqual(snapshot, input);
    }

    // --------------------------------------------------
    // byte[] — incremental lookup-table cross-check
    // --------------------------------------------------

    /// <summary>
    /// Provides pairs of (input, expected) byte arrays for arrays of length 1 through 256, where the expected
    /// output is computed from a precomputed bit-reversal lookup table.
    /// </summary>
    public static IEnumerable<object[]> ByteArrayReverseBitsData()
    {
        for (int len = 1; len <= 256; len++)
        {
            byte[] input = Enumerable.Range(1, len).Select(i => (byte)i).ToArray();
            byte[] expected = input.Select(b => ByteReverseLookup[b]).ToArray();
            yield return new object[] { input, expected };
        }
    }

    /// <summary>
    /// Verifies that <see cref="NumericExtensions.ReverseBits(byte[])" /> correctly reverses the bits of
    /// each byte across arrays of length 1 through 256, cross-checked against a precomputed lookup table.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(ByteArrayReverseBitsData), DynamicDataSourceType.Method)]
    public void ReverseBits_WhenByteArrayIsIncremental_ShouldMatchLookupTable(byte[] bytes, byte[] expected)
    {
        byte[] actual = bytes.ReverseBits();

        Trace.WriteLineIf(!actual.SequenceEqual(expected), $"Input   : {BitConverter.ToString(bytes)}");
        Trace.WriteLineIf(!actual.SequenceEqual(expected), $"Expected: {BitConverter.ToString(expected)}");
        Trace.WriteLineIf(!actual.SequenceEqual(expected), $"Actual  : {BitConverter.ToString(actual)}");

        Assert.AreEqual(expected.Length, actual.Length);
        for (int i = 0; i < expected.Length; i++)
            Assert.AreEqual(expected[i], actual[i], $"Mismatch at index {i}");
    }

    /// <summary>
    /// Precomputed lookup table mapping each possible byte value to its bit-reversed counterpart,
    /// used to cross-check the <see cref="NumericExtensions.ReverseBits(byte[])" /> overload.
    /// </summary>
    public static readonly byte[] ByteReverseLookup =
    {
        0x00, 0x80, 0x40, 0xC0, 0x20, 0xA0, 0x60, 0xE0, 0x10, 0x90, 0x50, 0xD0, 0x30, 0xB0, 0x70, 0xF0,
        0x08, 0x88, 0x48, 0xC8, 0x28, 0xA8, 0x68, 0xE8, 0x18, 0x98, 0x58, 0xD8, 0x38, 0xB8, 0x78, 0xF8,
        0x04, 0x84, 0x44, 0xC4, 0x24, 0xA4, 0x64, 0xE4, 0x14, 0x94, 0x54, 0xD4, 0x34, 0xB4, 0x74, 0xF4,
        0x0C, 0x8C, 0x4C, 0xCC, 0x2C, 0xAC, 0x6C, 0xEC, 0x1C, 0x9C, 0x5C, 0xDC, 0x3C, 0xBC, 0x7C, 0xFC,
        0x02, 0x82, 0x42, 0xC2, 0x22, 0xA2, 0x62, 0xE2, 0x12, 0x92, 0x52, 0xD2, 0x32, 0xB2, 0x72, 0xF2,
        0x0A, 0x8A, 0x4A, 0xCA, 0x2A, 0xAA, 0x6A, 0xEA, 0x1A, 0x9A, 0x5A, 0xDA, 0x3A, 0xBA, 0x7A, 0xFA,
        0x06, 0x86, 0x46, 0xC6, 0x26, 0xA6, 0x66, 0xE6, 0x16, 0x96, 0x56, 0xD6, 0x36, 0xB6, 0x76, 0xF6,
        0x0E, 0x8E, 0x4E, 0xCE, 0x2E, 0xAE, 0x6E, 0xEE, 0x1E, 0x9E, 0x5E, 0xDE, 0x3E, 0xBE, 0x7E, 0xFE,
        0x01, 0x81, 0x41, 0xC1, 0x21, 0xA1, 0x61, 0xE1, 0x11, 0x91, 0x51, 0xD1, 0x31, 0xB1, 0x71, 0xF1,
        0x09, 0x89, 0x49, 0xC9, 0x29, 0xA9, 0x69, 0xE9, 0x19, 0x99, 0x59, 0xD9, 0x39, 0xB9, 0x79, 0xF9,
        0x05, 0x85, 0x45, 0xC5, 0x25, 0xA5, 0x65, 0xE5, 0x15, 0x95, 0x55, 0xD5, 0x35, 0xB5, 0x75, 0xF5,
        0x0D, 0x8D, 0x4D, 0xCD, 0x2D, 0xAD, 0x6D, 0xED, 0x1D, 0x9D, 0x5D, 0xDD, 0x3D, 0xBD, 0x7D, 0xFD,
        0x03, 0x83, 0x43, 0xC3, 0x23, 0xA3, 0x63, 0xE3, 0x13, 0x93, 0x53, 0xD3, 0x33, 0xB3, 0x73, 0xF3,
        0x0B, 0x8B, 0x4B, 0xCB, 0x2B, 0xAB, 0x6B, 0xEB, 0x1B, 0x9B, 0x5B, 0xDB, 0x3B, 0xBB, 0x7B, 0xFB,
        0x07, 0x87, 0x47, 0xC7, 0x27, 0xA7, 0x67, 0xE7, 0x17, 0x97, 0x57, 0xD7, 0x37, 0xB7, 0x77, 0xF7,
        0x0F, 0x8F, 0x4F, 0xCF, 0x2F, 0xAF, 0x6F, 0xEF, 0x1F, 0x9F, 0x5F, 0xDF, 0x3F, 0xBF, 0x7F, 0xFF
    };

    // --------------------------------------------------
    // byte — ReverseBits(value, bitLength)
    // --------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="NumericExtensions.ReverseBits(byte, int)" /> returns zero when
    /// <paramref name="bitLength" /> is zero, regardless of the input value.
    /// </summary>
    [TestMethod]
    [DataRow((byte)0x00)]
    [DataRow((byte)0x01)]
    [DataRow((byte)0xAA)]
    [DataRow((byte)0xFF)]
    public void ReverseBits_WhenBitLengthIsZero_ForByte_ShouldReturnZero(byte value) =>
        Assert.AreEqual((byte)0, value.ReverseBits(0));

    /// <summary>
    /// Verifies that <see cref="NumericExtensions.ReverseBits(byte, int)" /> with the full bit width
    /// produces the same result as the parameterless <see cref="NumericExtensions.ReverseBits(byte)" /> overload.
    /// </summary>
    [TestMethod]
    [DataRow((byte)0x00)]
    [DataRow((byte)0x01)]
    [DataRow((byte)0x12)]
    [DataRow((byte)0xAA)]
    [DataRow((byte)0xC3)]
    [DataRow((byte)0xFF)]
    public void ReverseBits_WhenBitLengthIsFullWidth_ForByte_ShouldMatchParameterlessOverload(byte value) =>
        Assert.AreEqual(value.ReverseBits(), value.ReverseBits(8));

    /// <summary>
    /// Verifies that <see cref="NumericExtensions.ReverseBits(byte, int)" /> reverses only the least
    /// significant <paramref name="bitLength" /> bits, with bits above the window cleared to zero.
    /// </summary>
    [TestMethod]
    [DataRow((byte)0x1B, 4, (byte)0x0D)] // low nibble 0b1011 → 0b1101
    [DataRow((byte)0xFB, 4, (byte)0x0D)] // high bits are ignored
    [DataRow((byte)0x56, 5, (byte)0x0D)] // low 5 bits 0b10110 → 0b01101
    [DataRow((byte)0x7F, 7, (byte)0x7F)] // seven-bit palindrome
    [DataRow((byte)0x81, 7, (byte)0x40)] // high bit ignored, low 7 bits 0b0000001 → 0b1000000
    public void ReverseBits_WhenBitLengthIsPartial_ForByte_ShouldReturnLowBitsReversed(byte value, int bitLength, byte expected) =>
        Assert.AreEqual(expected, value.ReverseBits(bitLength));

    /// <summary>
    /// Verifies that applying <see cref="NumericExtensions.ReverseBits(byte, int)" /> twice with the same
    /// <paramref name="bitLength" /> returns the value masked to the reflected window.
    /// </summary>
    [TestMethod]
    [DataRow((byte)0x56, 5)]
    [DataRow((byte)0x7B, 7)]
    [DataRow((byte)0xFF, 4)]
    [DataRow((byte)0x00, 6)]
    public void ReverseBits_WhenAppliedTwiceWithSameBitLength_ForByte_ShouldReturnLowBitsOfOriginal(byte value, int bitLength)
    {
        byte mask = (byte)((1 << bitLength) - 1);
        Assert.AreEqual((byte)(value & mask), value.ReverseBits(bitLength).ReverseBits(bitLength));
    }

    /// <summary>
    /// Verifies that <see cref="NumericExtensions.ReverseBits(byte, int)" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> when <paramref name="bitLength" /> is negative.
    /// </summary>
    [TestMethod]
    [DataRow(-1)]
    [DataRow(-8)]
    [DataRow(int.MinValue)]
    public void ReverseBits_WhenBitLengthIsNegative_ForByte_ShouldThrowArgumentOutOfRangeException(int bitLength) =>
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            ((byte)0xFF).ReverseBits(bitLength);
        });

    /// <summary>
    /// Verifies that <see cref="NumericExtensions.ReverseBits(byte, int)" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> when <paramref name="bitLength" /> exceeds the bit width (8).
    /// </summary>
    [TestMethod]
    [DataRow(9)]
    [DataRow(16)]
    [DataRow(int.MaxValue)]
    public void ReverseBits_WhenBitLengthExceedsTypeWidth_ForByte_ShouldThrowArgumentOutOfRangeException(int bitLength) =>
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            ((byte)0xFF).ReverseBits(bitLength);
        });

    // --------------------------------------------------
    // ushort — ReverseBits(value, bitLength)
    // --------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="NumericExtensions.ReverseBits(ushort, int)" /> returns zero when
    /// <paramref name="bitLength" /> is zero, regardless of the input value.
    /// </summary>
    [TestMethod]
    [DataRow((ushort)0x0000)]
    [DataRow((ushort)0x1234)]
    [DataRow((ushort)0xFFFF)]
    public void ReverseBits_WhenBitLengthIsZero_ForUShort_ShouldReturnZero(ushort value) =>
        Assert.AreEqual((ushort)0, value.ReverseBits(0));

    /// <summary>
    /// Verifies that <see cref="NumericExtensions.ReverseBits(ushort, int)" /> with the full bit width
    /// produces the same result as the parameterless <see cref="NumericExtensions.ReverseBits(ushort)" /> overload.
    /// </summary>
    [TestMethod]
    [DataRow((ushort)0x0000)]
    [DataRow((ushort)0x0001)]
    [DataRow((ushort)0x1234)]
    [DataRow((ushort)0xAAAA)]
    [DataRow((ushort)0xFFFF)]
    public void ReverseBits_WhenBitLengthIsFullWidth_ForUShort_ShouldMatchParameterlessOverload(ushort value) =>
        Assert.AreEqual(value.ReverseBits(), value.ReverseBits(16));

    /// <summary>
    /// Verifies that <see cref="NumericExtensions.ReverseBits(ushort, int)" /> reverses only the least
    /// significant <paramref name="bitLength" /> bits, with bits above the window cleared to zero.
    /// </summary>
    [TestMethod]
    [DataRow((ushort)0x1234, 8, (ushort)0x002C)] // low byte 0x34 → 0x2C
    [DataRow((ushort)0xFF34, 8, (ushort)0x002C)] // high byte ignored
    [DataRow((ushort)0x00AB, 12, (ushort)0x0D50)] // low 12 bits
    [DataRow((ushort)0xF0AB, 12, (ushort)0x0D50)] // high nibble ignored
    public void ReverseBits_WhenBitLengthIsPartial_ForUShort_ShouldReturnLowBitsReversed(ushort value, int bitLength, ushort expected) =>
        Assert.AreEqual(expected, value.ReverseBits(bitLength));

    /// <summary>
    /// Verifies that applying <see cref="NumericExtensions.ReverseBits(ushort, int)" /> twice with the same
    /// <paramref name="bitLength" /> returns the value masked to the reflected window.
    /// </summary>
    [TestMethod]
    [DataRow((ushort)0x1234, 8)]
    [DataRow((ushort)0xABCD, 12)]
    [DataRow((ushort)0xFFFF, 10)]
    public void ReverseBits_WhenAppliedTwiceWithSameBitLength_ForUShort_ShouldReturnLowBitsOfOriginal(ushort value, int bitLength)
    {
        ushort mask = (ushort)((1 << bitLength) - 1);
        Assert.AreEqual((ushort)(value & mask), value.ReverseBits(bitLength).ReverseBits(bitLength));
    }

    /// <summary>
    /// Verifies that <see cref="NumericExtensions.ReverseBits(ushort, int)" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> when <paramref name="bitLength" /> is outside [0, 16].
    /// </summary>
    [TestMethod]
    [DataRow(-1)]
    [DataRow(17)]
    [DataRow(int.MaxValue)]
    [DataRow(int.MinValue)]
    public void ReverseBits_WhenBitLengthIsOutOfRange_ForUShort_ShouldThrowArgumentOutOfRangeException(int bitLength) =>
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            ((ushort)0xFFFF).ReverseBits(bitLength);
        });

    // --------------------------------------------------
    // uint — ReverseBits(value, bitLength)
    // --------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="NumericExtensions.ReverseBits(uint, int)" /> returns zero when
    /// <paramref name="bitLength" /> is zero, regardless of the input value.
    /// </summary>
    [TestMethod]
    [DataRow(0x00000000u)]
    [DataRow(0x12345678u)]
    [DataRow(0xFFFFFFFFu)]
    public void ReverseBits_WhenBitLengthIsZero_ForUInt_ShouldReturnZero(uint value) =>
        Assert.AreEqual(0u, value.ReverseBits(0));

    /// <summary>
    /// Verifies that <see cref="NumericExtensions.ReverseBits(uint, int)" /> with the full bit width
    /// produces the same result as the parameterless <see cref="NumericExtensions.ReverseBits(uint)" /> overload.
    /// </summary>
    [TestMethod]
    [DataRow(0x00000000u)]
    [DataRow(0x00000001u)]
    [DataRow(0x12345678u)]
    [DataRow(0xAAAAAAAAu)]
    [DataRow(0xFFFFFFFFu)]
    public void ReverseBits_WhenBitLengthIsFullWidth_ForUInt_ShouldMatchParameterlessOverload(uint value) =>
        Assert.AreEqual(value.ReverseBits(), value.ReverseBits(32));

    /// <summary>
    /// Verifies that <see cref="NumericExtensions.ReverseBits(uint, int)" /> reverses only the least
    /// significant <paramref name="bitLength" /> bits, with bits above the window cleared to zero.
    /// </summary>
    [TestMethod]
    [DataRow(0x12345678u, 16, 0x1E6Au)]   // low 16 bits 0x5678 → 0x1E6A
    [DataRow(0xFFFF5678u, 16, 0x1E6Au)]   // high half ignored
    [DataRow(0x00000001u, 24, 0x800000u)] // lone LSB in 24-bit window
    [DataRow(0x000000FFu, 24, 0xFF0000u)] // low byte moves to top of 24-bit window
    public void ReverseBits_WhenBitLengthIsPartial_ForUInt_ShouldReturnLowBitsReversed(uint value, int bitLength, uint expected) =>
        Assert.AreEqual(expected, value.ReverseBits(bitLength));

    /// <summary>
    /// Verifies that applying <see cref="NumericExtensions.ReverseBits(uint, int)" /> twice with the same
    /// <paramref name="bitLength" /> returns the value masked to the reflected window.
    /// </summary>
    [TestMethod]
    [DataRow(0x12345678u, 16)]
    [DataRow(0xDEADBEEFu, 24)]
    [DataRow(0xFFFFFFFFu, 20)]
    public void ReverseBits_WhenAppliedTwiceWithSameBitLength_ForUInt_ShouldReturnLowBitsOfOriginal(uint value, int bitLength)
    {
        uint mask = (1u << bitLength) - 1u;
        Assert.AreEqual(value & mask, value.ReverseBits(bitLength).ReverseBits(bitLength));
    }

    /// <summary>
    /// Verifies that <see cref="NumericExtensions.ReverseBits(uint, int)" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> when <paramref name="bitLength" /> is outside [0, 32].
    /// </summary>
    [TestMethod]
    [DataRow(-1)]
    [DataRow(33)]
    [DataRow(int.MaxValue)]
    [DataRow(int.MinValue)]
    public void ReverseBits_WhenBitLengthIsOutOfRange_ForUInt_ShouldThrowArgumentOutOfRangeException(int bitLength) =>
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            0xFFFFFFFFu.ReverseBits(bitLength);
        });

    // --------------------------------------------------
    // ulong — ReverseBits(value, bitLength)
    // --------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="NumericExtensions.ReverseBits(ulong, int)" /> returns zero when
    /// <paramref name="bitLength" /> is zero, regardless of the input value.
    /// </summary>
    [TestMethod]
    [DataRow(0x0000000000000000ul)]
    [DataRow(0x0123456789ABCDEFul)]
    [DataRow(0xFFFFFFFFFFFFFFFFul)]
    public void ReverseBits_WhenBitLengthIsZero_ForULong_ShouldReturnZero(ulong value) =>
        Assert.AreEqual(0ul, value.ReverseBits(0));

    /// <summary>
    /// Verifies that <see cref="NumericExtensions.ReverseBits(ulong, int)" /> with the full bit width
    /// produces the same result as the parameterless <see cref="NumericExtensions.ReverseBits(ulong)" /> overload.
    /// </summary>
    [TestMethod]
    [DataRow(0x0000000000000000ul)]
    [DataRow(0x0000000000000001ul)]
    [DataRow(0x0123456789ABCDEFul)]
    [DataRow(0xAAAAAAAAAAAAAAAAul)]
    [DataRow(0xFFFFFFFFFFFFFFFFul)]
    public void ReverseBits_WhenBitLengthIsFullWidth_ForULong_ShouldMatchParameterlessOverload(ulong value) =>
        Assert.AreEqual(value.ReverseBits(), value.ReverseBits(64));

    /// <summary>
    /// Verifies that <see cref="NumericExtensions.ReverseBits(ulong, int)" /> reverses only the least
    /// significant <paramref name="bitLength" /> bits, with bits above the window cleared to zero.
    /// </summary>
    [TestMethod]
    [DataRow(0xFFFFFFFFFFFFFF12ul, 8, 0x48ul)]          // low byte 0x12 → 0x48, high bits ignored
    [DataRow(0x0000000000000012ul, 8, 0x48ul)]          // same low byte, zero high bits
    [DataRow(0xABCDEF0123456789ul, 32, 0x91E6A2C4ul)]   // low 32 bits 0x23456789 → 0x91E6A2C4
    [DataRow(0x00000000FFFFFFFFul, 40, 0xFFFFFFFF00ul)] // low 40 bits: 32 set bits shift to the top of the window
    public void ReverseBits_WhenBitLengthIsPartial_ForULong_ShouldReturnLowBitsReversed(ulong value, int bitLength, ulong expected) =>
        Assert.AreEqual(expected, value.ReverseBits(bitLength));

    /// <summary>
    /// Verifies that applying <see cref="NumericExtensions.ReverseBits(ulong, int)" /> twice with the same
    /// <paramref name="bitLength" /> returns the value masked to the reflected window.
    /// </summary>
    [TestMethod]
    [DataRow(0x0123456789ABCDEFul, 32)]
    [DataRow(0xDEADBEEFCAFEBABEul, 48)]
    [DataRow(0xFFFFFFFFFFFFFFFFul, 63)]
    public void ReverseBits_WhenAppliedTwiceWithSameBitLength_ForULong_ShouldReturnLowBitsOfOriginal(ulong value, int bitLength)
    {
        ulong mask = (1ul << bitLength) - 1ul;
        Assert.AreEqual(value & mask, value.ReverseBits(bitLength).ReverseBits(bitLength));
    }

    /// <summary>
    /// Verifies that <see cref="NumericExtensions.ReverseBits(ulong, int)" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> when <paramref name="bitLength" /> is outside [0, 64].
    /// </summary>
    [TestMethod]
    [DataRow(-1)]
    [DataRow(65)]
    [DataRow(int.MaxValue)]
    [DataRow(int.MinValue)]
    public void ReverseBits_WhenBitLengthIsOutOfRange_ForULong_ShouldThrowArgumentOutOfRangeException(int bitLength) =>
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            0xFFFFFFFFFFFFFFFFul.ReverseBits(bitLength);
        });

    // --------------------------------------------------
    // Exhaustive / representative DynamicData sweeps
    // --------------------------------------------------

    /// <summary>
    /// Computes the expected bit-reversed window using the canonical naive definition: the least
    /// significant <paramref name="bitLength" /> bits of <paramref name="value" /> are read LSB-first
    /// and written MSB-first into the result. All higher bits of the result are zero.
    /// </summary>
    /// <param name="value">The input value to reflect.</param>
    /// <param name="bitLength">The number of least significant bits to reverse.</param>
    /// <returns>A <see cref="ulong" /> whose low <paramref name="bitLength" /> bits hold the reflected window.</returns>
    private static ulong ReflectLowBits(ulong value, int bitLength)
    {
        ulong result = 0;

        for (int i = 0; i < bitLength; i++)
        {
            result = (result << 1) | (value & 1);
            value >>= 1;
        }

        return result;
    }

    /// <summary>
    /// Curated set of <see cref="ushort" /> inputs that exercises zero, single-bit, alternating,
    /// nibble-boundary, palindromic, and all-bits-set patterns.
    /// </summary>
    private static readonly ushort[] UShortReverseBitsSampleValues =
    {
        0x0000, 0x0001, 0x0002, 0x0080, 0x00FF, 0x0100, 0x0F0F, 0x1234,
        0x5555, 0x8000, 0xA5A5, 0xAAAA, 0xCAFE, 0xFF00, 0xFFFE, 0xFFFF,
    };

    /// <summary>
    /// Curated set of <see cref="uint" /> inputs that exercises zero, single-bit, alternating,
    /// byte-boundary, palindromic, and all-bits-set patterns.
    /// </summary>
    private static readonly uint[] UIntReverseBitsSampleValues =
    {
        0x00000000u, 0x00000001u, 0x00000080u, 0x000000FFu,
        0x00008000u, 0x0000FFFFu, 0x00800000u, 0x01234567u,
        0x55555555u, 0x80000000u, 0xAAAAAAAAu, 0xCAFEBABEu,
        0xDEADBEEFu, 0xFEEDFACEu, 0xFFFFFFFEu, 0xFFFFFFFFu,
    };

    /// <summary>
    /// Curated set of <see cref="ulong" /> inputs that exercises zero, single-bit, alternating,
    /// word-boundary, palindromic, and all-bits-set patterns.
    /// </summary>
    private static readonly ulong[] ULongReverseBitsSampleValues =
    {
        0x0000000000000000ul, 0x0000000000000001ul, 0x0000000000000080ul, 0x00000000000000FFul,
        0x0000000000008000ul, 0x000000000000FFFFul, 0x0000000080000000ul, 0x00000000FFFFFFFFul,
        0x0123456789ABCDEFul, 0x5555555555555555ul, 0x8000000000000000ul, 0xAAAAAAAAAAAAAAAAul,
        0xCAFEBABEDEADBEEFul, 0xFEEDFACE1337C0DEul, 0xFFFFFFFFFFFFFFFEul, 0xFFFFFFFFFFFFFFFFul,
    };

    /// <summary>
    /// Produces every combination of <c>(value, bitLength)</c> for exhaustive coverage of the
    /// <see cref="byte" /> overload: 256 values × 9 bit lengths = 2 304 cases.
    /// </summary>
    public static IEnumerable<object[]> ByteReverseBitsExhaustiveData()
    {
        for (int v = 0; v <= byte.MaxValue; v++)
            for (int n = 0; n <= 8; n++)
                yield return new object[] { (byte)v, n };
    }

    /// <summary>
    /// Produces <c>(value, bitLength)</c> pairs for the curated <see cref="ushort" /> sample set
    /// across every valid bit length in [0, 16].
    /// </summary>
    public static IEnumerable<object[]> UShortReverseBitsRepresentativeData()
    {
        foreach (ushort v in UShortReverseBitsSampleValues)
            for (int n = 0; n <= 16; n++)
                yield return new object[] { v, n };
    }

    /// <summary>
    /// Produces <c>(value, bitLength)</c> pairs for the curated <see cref="uint" /> sample set
    /// across every valid bit length in [0, 32].
    /// </summary>
    public static IEnumerable<object[]> UIntReverseBitsRepresentativeData()
    {
        foreach (uint v in UIntReverseBitsSampleValues)
            for (int n = 0; n <= 32; n++)
                yield return new object[] { v, n };
    }

    /// <summary>
    /// Produces <c>(value, bitLength)</c> pairs for the curated <see cref="ulong" /> sample set
    /// across every valid bit length in [0, 64].
    /// </summary>
    public static IEnumerable<object[]> ULongReverseBitsRepresentativeData()
    {
        foreach (ulong v in ULongReverseBitsSampleValues)
            for (int n = 0; n <= 64; n++)
                yield return new object[] { v, n };
    }

    /// <summary>
    /// Verifies exhaustively, for every <see cref="byte" /> value and every bit length in [0, 8], that
    /// <see cref="NumericExtensions.ReverseBits(byte, int)" /> matches the canonical naive reflection.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(ByteReverseBitsExhaustiveData), DynamicDataSourceType.Method)]
    public void ReverseBits_WhenExhaustive_ForByte_ShouldMatchReferenceImplementation(byte value, int bitLength)
    {
        byte expected = (byte)ReflectLowBits(value, bitLength);
        byte actual = value.ReverseBits(bitLength);

        Trace.WriteLineIf(actual != expected, $"value     : 0x{value:X2}");
        Trace.WriteLineIf(actual != expected, $"bitLength : {bitLength}");
        Trace.WriteLineIf(actual != expected, $"expected  : 0x{expected:X2}");
        Trace.WriteLineIf(actual != expected, $"actual    : 0x{actual:X2}");

        Assert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Verifies that <see cref="NumericExtensions.ReverseBits(ushort, int)" /> matches the canonical
    /// naive reflection across a representative set of <see cref="ushort" /> values and every valid bit length.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(UShortReverseBitsRepresentativeData), DynamicDataSourceType.Method)]
    public void ReverseBits_WhenRepresentative_ForUShort_ShouldMatchReferenceImplementation(ushort value, int bitLength)
    {
        ushort expected = (ushort)ReflectLowBits(value, bitLength);
        ushort actual = value.ReverseBits(bitLength);

        Trace.WriteLineIf(actual != expected, $"value     : 0x{value:X4}");
        Trace.WriteLineIf(actual != expected, $"bitLength : {bitLength}");
        Trace.WriteLineIf(actual != expected, $"expected  : 0x{expected:X4}");
        Trace.WriteLineIf(actual != expected, $"actual    : 0x{actual:X4}");

        Assert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Verifies that <see cref="NumericExtensions.ReverseBits(uint, int)" /> matches the canonical
    /// naive reflection across a representative set of <see cref="uint" /> values and every valid bit length.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(UIntReverseBitsRepresentativeData), DynamicDataSourceType.Method)]
    public void ReverseBits_WhenRepresentative_ForUInt_ShouldMatchReferenceImplementation(uint value, int bitLength)
    {
        uint expected = (uint)ReflectLowBits(value, bitLength);
        uint actual = value.ReverseBits(bitLength);

        Trace.WriteLineIf(actual != expected, $"value     : 0x{value:X8}");
        Trace.WriteLineIf(actual != expected, $"bitLength : {bitLength}");
        Trace.WriteLineIf(actual != expected, $"expected  : 0x{expected:X8}");
        Trace.WriteLineIf(actual != expected, $"actual    : 0x{actual:X8}");

        Assert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Verifies that <see cref="NumericExtensions.ReverseBits(ulong, int)" /> matches the canonical
    /// naive reflection across a representative set of <see cref="ulong" /> values and every valid bit length.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(ULongReverseBitsRepresentativeData), DynamicDataSourceType.Method)]
    public void ReverseBits_WhenRepresentative_ForULong_ShouldMatchReferenceImplementation(ulong value, int bitLength)
    {
        ulong expected = ReflectLowBits(value, bitLength);
        ulong actual = value.ReverseBits(bitLength);

        Trace.WriteLineIf(actual != expected, $"value     : 0x{value:X16}");
        Trace.WriteLineIf(actual != expected, $"bitLength : {bitLength}");
        Trace.WriteLineIf(actual != expected, $"expected  : 0x{expected:X16}");
        Trace.WriteLineIf(actual != expected, $"actual    : 0x{actual:X16}");

        Assert.AreEqual(expected, actual);
    }

    // --------------------------------------------------
    // Full-width involution (bitLength == W)
    // --------------------------------------------------

    /// <summary>
    /// Verifies that applying <see cref="NumericExtensions.ReverseBits(byte, int)" /> twice at full
    /// bit width (8) returns the original value.
    /// </summary>
    [TestMethod]
    [DataRow((byte)0x00)]
    [DataRow((byte)0x01)]
    [DataRow((byte)0x80)]
    [DataRow((byte)0xAA)]
    [DataRow((byte)0x55)]
    [DataRow((byte)0xC3)]
    [DataRow((byte)0xFF)]
    public void ReverseBits_WhenAppliedTwiceAtFullWidth_ForByte_ShouldReturnOriginal(byte value) =>
        Assert.AreEqual(value, value.ReverseBits(8).ReverseBits(8));

    /// <summary>
    /// Verifies that applying <see cref="NumericExtensions.ReverseBits(ushort, int)" /> twice at full
    /// bit width (16) returns the original value.
    /// </summary>
    [TestMethod]
    [DataRow((ushort)0x0000)]
    [DataRow((ushort)0x0001)]
    [DataRow((ushort)0x8000)]
    [DataRow((ushort)0xAAAA)]
    [DataRow((ushort)0x5555)]
    [DataRow((ushort)0x1234)]
    [DataRow((ushort)0xFFFF)]
    public void ReverseBits_WhenAppliedTwiceAtFullWidth_ForUShort_ShouldReturnOriginal(ushort value) =>
        Assert.AreEqual(value, value.ReverseBits(16).ReverseBits(16));

    /// <summary>
    /// Verifies that applying <see cref="NumericExtensions.ReverseBits(uint, int)" /> twice at full
    /// bit width (32) returns the original value.
    /// </summary>
    [TestMethod]
    [DataRow(0x00000000u)]
    [DataRow(0x00000001u)]
    [DataRow(0x80000000u)]
    [DataRow(0xAAAAAAAAu)]
    [DataRow(0x55555555u)]
    [DataRow(0xDEADBEEFu)]
    [DataRow(0xFFFFFFFFu)]
    public void ReverseBits_WhenAppliedTwiceAtFullWidth_ForUInt_ShouldReturnOriginal(uint value) =>
        Assert.AreEqual(value, value.ReverseBits(32).ReverseBits(32));

    /// <summary>
    /// Verifies that applying <see cref="NumericExtensions.ReverseBits(ulong, int)" /> twice at full
    /// bit width (64) returns the original value.
    /// </summary>
    [TestMethod]
    [DataRow(0x0000000000000000ul)]
    [DataRow(0x0000000000000001ul)]
    [DataRow(0x8000000000000000ul)]
    [DataRow(0xAAAAAAAAAAAAAAAAul)]
    [DataRow(0x5555555555555555ul)]
    [DataRow(0x0123456789ABCDEFul)]
    [DataRow(0xFFFFFFFFFFFFFFFFul)]
    public void ReverseBits_WhenAppliedTwiceAtFullWidth_ForULong_ShouldReturnOriginal(ulong value) =>
        Assert.AreEqual(value, value.ReverseBits(64).ReverseBits(64));

    // --------------------------------------------------
    // Cross-type consistency
    // --------------------------------------------------

    /// <summary>
    /// Produces <c>(byteValue, bitLength)</c> pairs for cross-type consistency checks, spanning every
    /// byte value and every bit length in [0, 8].
    /// </summary>
    public static IEnumerable<object[]> CrossTypeConsistencyByteData()
    {
        for (int v = 0; v <= byte.MaxValue; v++)
            for (int n = 0; n <= 8; n++)
                yield return new object[] { (byte)v, n };
    }

    /// <summary>
    /// Verifies that reflecting the same numeric value at the same bit length yields identical
    /// results across the <see cref="byte" />, <see cref="ushort" />, <see cref="uint" />, and
    /// <see cref="ulong" /> overloads, with all higher bits zero.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(CrossTypeConsistencyByteData), DynamicDataSourceType.Method)]
    public void ReverseBits_WhenValueFitsSmallerType_ShouldBeConsistentAcrossOverloads(byte value, int bitLength)
    {
        byte byteResult = value.ReverseBits(bitLength);
        ushort ushortResult = ((ushort)value).ReverseBits(bitLength);
        uint uintResult = ((uint)value).ReverseBits(bitLength);
        ulong ulongResult = ((ulong)value).ReverseBits(bitLength);

        Assert.AreEqual(byteResult, (byte)ushortResult, $"ushort overload disagreed for value=0x{value:X2}, bitLength={bitLength}");
        Assert.AreEqual(byteResult, (byte)uintResult, $"uint overload disagreed for value=0x{value:X2}, bitLength={bitLength}");
        Assert.AreEqual(byteResult, (byte)ulongResult, $"ulong overload disagreed for value=0x{value:X2}, bitLength={bitLength}");

        // All higher bits above the reflected window must be zero in every overload.
        Assert.AreEqual((ushort)byteResult, ushortResult);
        Assert.AreEqual((uint)byteResult, uintResult);
        Assert.AreEqual((ulong)byteResult, ulongResult);
    }

    /// <summary>
    /// Verifies that bits above the reflected window in the input do not affect the result: the
    /// output depends solely on the low <paramref name="bitLength" /> bits of the input.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(ByteReverseBitsExhaustiveData), DynamicDataSourceType.Method)]
    public void ReverseBits_WhenHighBitsSetInInput_ForULong_ShouldIgnoreBitsAboveWindow(byte lowByteValue, int bitLength)
    {
        // Inject noise into every bit above the 8-bit window; bitLength never exceeds 8 here,
        // so all noise bits are outside the reflected window and must be ignored.
        ulong noisy = (ulong)lowByteValue | 0xFFFFFFFFFFFFFF00ul;
        ulong clean = lowByteValue;

        Assert.AreEqual(clean.ReverseBits(bitLength), noisy.ReverseBits(bitLength));
    }
}