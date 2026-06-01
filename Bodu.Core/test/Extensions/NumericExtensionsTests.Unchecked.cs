// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NumericExtensionsTests.Unchecked.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class NumericExtensionsTests
{

    /// <summary>
    /// Verifies that <see cref="NumericExtensions.ReverseBitsUnchecked(byte, int)" /> reverses the least-significant bits within the
    /// supplied width.
    /// </summary>
    [TestMethod]
    public void ReverseBitsUnchecked_Byte_ShouldReverseLeastSignificantBits()
    {
        Assert.AreEqual((byte)0b0000_0001, ((byte)0b1000_0000).ReverseBitsUnchecked(8));
        Assert.AreEqual((byte)0b0000_0010, ((byte)0b0100_0000).ReverseBitsUnchecked(8));
        Assert.AreEqual((byte)0b0000_0111, ((byte)0b0000_0111).ReverseBitsUnchecked(3));
    }

    /// <summary>
    /// Verifies that <see cref="NumericExtensions.ReverseBitsUnchecked(uint, int)" /> reverses 32-bit bit windows.
    /// </summary>
    [TestMethod]
    public void ReverseBitsUnchecked_UInt_ShouldReverseLeastSignificantBits()
    {
        Assert.AreEqual(0x80000000u, 0x00000001u.ReverseBitsUnchecked(32));
        Assert.AreEqual(0x00000007u, 0x00000007u.ReverseBitsUnchecked(3));
    }

    /// <summary>
    /// Verifies that <see cref="NumericExtensions.ReverseBitsUnchecked(ulong, int)" /> reverses 64-bit bit windows.
    /// </summary>
    [TestMethod]
    public void ReverseBitsUnchecked_ULong_ShouldReverseLeastSignificantBits()
    {
        Assert.AreEqual(0x8000000000000000UL, 1UL.ReverseBitsUnchecked(64));
        Assert.AreEqual(0x0000000000000007UL, 0x0000000000000007UL.ReverseBitsUnchecked(3));
    }

    /// <summary>
    /// Verifies that <see cref="NumericExtensions.ReverseBitsUnchecked(ushort, int)" /> reverses 16-bit bit windows.
    /// </summary>
    [TestMethod]
    public void ReverseBitsUnchecked_UShort_ShouldReverseLeastSignificantBits()
    {
        Assert.AreEqual((ushort)0x0001, ((ushort)0x8000).ReverseBitsUnchecked(16));
        Assert.AreEqual((ushort)0x0007, ((ushort)0x0007).ReverseBitsUnchecked(3));
    }

    /// <summary>
    /// Verifies that <see cref="NumericExtensions.ReverseBytesUnchecked(uint)" /> reverses byte order of a 32-bit value.
    /// </summary>
    [TestMethod]
    [DataRow(0x12345678u, 0x78563412u)]
    [DataRow(0xDEADBEEFu, 0xEFBEADDEu)]
    [DataRow(0u, 0u)]
    public void ReverseBytesUnchecked_UInt_ShouldReverseByteOrder(uint value, uint expected) => Assert.AreEqual(expected, value.ReverseBytesUnchecked());

    /// <summary>
    /// Verifies that <see cref="NumericExtensions.ReverseBytesUnchecked(ulong)" /> reverses byte order of a 64-bit value.
    /// </summary>
    [TestMethod]
    [DataRow(0x123456789ABCDEF0UL, 0xF0DEBC9A78563412UL)]
    [DataRow(0UL, 0UL)]
    public void ReverseBytesUnchecked_ULong_ShouldReverseByteOrder(ulong value, ulong expected) => Assert.AreEqual(expected, value.ReverseBytesUnchecked());

    /// <summary>
    /// Verifies that <see cref="NumericExtensions.ReverseBytesUnchecked(ushort)" /> reverses byte order of a 16-bit value.
    /// </summary>
    [TestMethod]
    [DataRow((ushort)0x1234, (ushort)0x3412)]
    [DataRow((ushort)0x00FF, (ushort)0xFF00)]
    [DataRow((ushort)0x0000, (ushort)0x0000)]
    public void ReverseBytesUnchecked_UShort_ShouldReverseByteOrder(ushort value, ushort expected) => Assert.AreEqual(expected, value.ReverseBytesUnchecked());
    /// <summary>
    /// Verifies that <see cref="NumericExtensions.RotateBitsLeftUnchecked(byte, int)" /> matches the result of
    /// <see cref="NumericExtensions.RotateBitsLeft(byte, int)" /> across the valid range.
    /// </summary>
    [TestMethod]
    [DataRow((byte)0b0000_0001, 1, (byte)0b0000_0010)]
    [DataRow((byte)0b1000_0000, 1, (byte)0b0000_0001)]
    [DataRow((byte)0b1010_1010, 4, (byte)0b1010_1010)]
    [DataRow((byte)0xFF, 3, (byte)0xFF)]
    public void RotateBitsLeftUnchecked_Byte_ShouldRotateCorrectly(byte value, int count, byte expected) => Assert.AreEqual(expected, value.RotateBitsLeftUnchecked(count));

    /// <summary>
    /// Verifies that <see cref="NumericExtensions.RotateBitsLeftUnchecked(uint, int)" /> rotates uint values correctly.
    /// </summary>
    [TestMethod]
    [DataRow(0x00000001u, 1, 0x00000002u)]
    [DataRow(0x80000000u, 1, 0x00000001u)]
    [DataRow(0x12345678u, 16, 0x56781234u)]
    public void RotateBitsLeftUnchecked_UInt_ShouldRotateCorrectly(uint value, int count, uint expected) => Assert.AreEqual(expected, value.RotateBitsLeftUnchecked(count));

    /// <summary>
    /// Verifies that <see cref="NumericExtensions.RotateBitsLeftUnchecked(ulong, int)" /> rotates ulong values correctly.
    /// </summary>
    [TestMethod]
    [DataRow(0x0000000000000001UL, 1, 0x0000000000000002UL)]
    [DataRow(0x8000000000000000UL, 1, 0x0000000000000001UL)]
    [DataRow(0x123456789ABCDEF0UL, 32, 0x9ABCDEF012345678UL)]
    public void RotateBitsLeftUnchecked_ULong_ShouldRotateCorrectly(ulong value, int count, ulong expected) => Assert.AreEqual(expected, value.RotateBitsLeftUnchecked(count));

    /// <summary>
    /// Verifies that <see cref="NumericExtensions.RotateBitsLeftUnchecked(ushort, int)" /> rotates ushort values correctly.
    /// </summary>
    [TestMethod]
    [DataRow((ushort)0x0001, 1, (ushort)0x0002)]
    [DataRow((ushort)0x8000, 1, (ushort)0x0001)]
    [DataRow((ushort)0x1234, 8, (ushort)0x3412)]
    public void RotateBitsLeftUnchecked_UShort_ShouldRotateCorrectly(ushort value, int count, ushort expected) => Assert.AreEqual(expected, value.RotateBitsLeftUnchecked(count));

    /// <summary>
    /// Verifies that <see cref="NumericExtensions.RotateBitsRightUnchecked(byte, int)" /> rotates byte values correctly.
    /// </summary>
    [TestMethod]
    [DataRow((byte)0b0000_0010, 1, (byte)0b0000_0001)]
    [DataRow((byte)0b0000_0001, 1, (byte)0b1000_0000)]
    [DataRow((byte)0b1010_1010, 4, (byte)0b1010_1010)]
    public void RotateBitsRightUnchecked_Byte_ShouldRotateCorrectly(byte value, int count, byte expected) => Assert.AreEqual(expected, value.RotateBitsRightUnchecked(count));

    /// <summary>
    /// Verifies that <see cref="NumericExtensions.RotateBitsRightUnchecked(uint, int)" /> rotates uint values correctly.
    /// </summary>
    [TestMethod]
    [DataRow(0x00000002u, 1, 0x00000001u)]
    [DataRow(0x00000001u, 1, 0x80000000u)]
    [DataRow(0x12345678u, 16, 0x56781234u)]
    public void RotateBitsRightUnchecked_UInt_ShouldRotateCorrectly(uint value, int count, uint expected) => Assert.AreEqual(expected, value.RotateBitsRightUnchecked(count));

    /// <summary>
    /// Verifies that <see cref="NumericExtensions.RotateBitsRightUnchecked(ulong, int)" /> rotates ulong values correctly.
    /// </summary>
    [TestMethod]
    [DataRow(0x0000000000000002UL, 1, 0x0000000000000001UL)]
    [DataRow(0x0000000000000001UL, 1, 0x8000000000000000UL)]
    [DataRow(0x123456789ABCDEF0UL, 32, 0x9ABCDEF012345678UL)]
    public void RotateBitsRightUnchecked_ULong_ShouldRotateCorrectly(ulong value, int count, ulong expected) => Assert.AreEqual(expected, value.RotateBitsRightUnchecked(count));

    /// <summary>
    /// Verifies that <see cref="NumericExtensions.RotateBitsRightUnchecked(ushort, int)" /> rotates ushort values correctly.
    /// </summary>
    [TestMethod]
    [DataRow((ushort)0x0002, 1, (ushort)0x0001)]
    [DataRow((ushort)0x0001, 1, (ushort)0x8000)]
    [DataRow((ushort)0x1234, 8, (ushort)0x3412)]
    public void RotateBitsRightUnchecked_UShort_ShouldRotateCorrectly(ushort value, int count, ushort expected) => Assert.AreEqual(expected, value.RotateBitsRightUnchecked(count));

}
