// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NumericExtensionsTests.RotateBitsRight.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class NumericExtensionsTests
{

    /// <summary>
    /// Verifies that <see cref="NumericExtensions.RotateBitsRight(byte, int)" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> when <paramref name="count" /> exceeds the bit width (8).
    /// </summary>
    [TestMethod]
    [DataRow(9)]
    [DataRow(16)]
    [DataRow(int.MaxValue)]
    public void RotateBitsRight_WhenCountExceedsBitWidth_ForByte_ShouldThrowArgumentOutOfRangeException(int count) =>
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            ((byte)0xFF).RotateBitsRight(count);
        });

    // --------------------------------------------------
    // byte — invalid count
    // --------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="NumericExtensions.RotateBitsRight(byte, int)" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> when <paramref name="count" /> is negative.
    /// </summary>
    [TestMethod]
    [DataRow(-1)]
    [DataRow(-8)]
    [DataRow(int.MinValue)]
    public void RotateBitsRight_WhenCountIsNegative_ForByte_ShouldThrowArgumentOutOfRangeException(int count) =>
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            ((byte)0xFF).RotateBitsRight(count);
        });

    /// <summary>
    /// Verifies that <see cref="NumericExtensions.RotateBitsRight(uint, int)" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> when <paramref name="count" /> is outside [0, 32].
    /// </summary>
    [TestMethod]
    [DataRow(-1)]
    [DataRow(33)]
    [DataRow(int.MaxValue)]
    public void RotateBitsRight_WhenCountIsOutOfRange_ForUInt_ShouldThrowArgumentOutOfRangeException(int count) =>
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            0xFFFFFFFFU.RotateBitsRight(count);
        });

    /// <summary>
    /// Verifies that <see cref="NumericExtensions.RotateBitsRight(ulong, int)" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> when <paramref name="count" /> is outside [0, 64].
    /// </summary>
    [TestMethod]
    [DataRow(-1)]
    [DataRow(65)]
    [DataRow(int.MaxValue)]
    public void RotateBitsRight_WhenCountIsOutOfRange_ForULong_ShouldThrowArgumentOutOfRangeException(int count) =>
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            0xFFFFFFFFFFFFFFFFUL.RotateBitsRight(count);
        });

    /// <summary>
    /// Verifies that <see cref="NumericExtensions.RotateBitsRight(ushort, int)" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> when <paramref name="count" /> is outside [0, 16].
    /// </summary>
    [TestMethod]
    [DataRow(-1)]
    [DataRow(17)]
    [DataRow(int.MaxValue)]
    public void RotateBitsRight_WhenCountIsOutOfRange_ForUShort_ShouldThrowArgumentOutOfRangeException(int count) =>
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            ((ushort)0xFFFF).RotateBitsRight(count);
        });
    // --------------------------------------------------
    // byte — valid rotations
    // --------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="NumericExtensions.RotateBitsRight(byte, int)" /> produces the expected result
    /// for a representative set of values and rotation counts within the valid range [0, 8].
    /// </summary>
    [TestMethod]
    [DataRow((byte)0x00, 0, (byte)0x00)] // all-zero, any rotation — identity
    [DataRow((byte)0xFF, 1, (byte)0xFF)] // all-ones, any rotation — identity
    [DataRow((byte)0xFF, 8, (byte)0xFF)] // full rotation — identity
    [DataRow((byte)0xAA, 8, (byte)0xAA)] // full rotation preserves alternating pattern
    [DataRow((byte)0x01, 1, (byte)0x80)] // LSB wraps to MSB
    [DataRow((byte)0x01, 7, (byte)0x02)] // LSB shifted to bit 1
    [DataRow((byte)0x80, 1, (byte)0x40)] // MSB shifts right by 1
    [DataRow((byte)0xAA, 3, (byte)0x55)] // 10101010 rotated right 3 → 01010101
    [DataRow((byte)0x55, 5, (byte)0xAA)] // 01010101 rotated right 5 → 10101010
    public void RotateBitsRight_WhenCountIsValid_ForByte_ShouldProduceExpectedResult(byte value, int count, byte expected) =>
        Assert.AreEqual(expected, value.RotateBitsRight(count));

    // --------------------------------------------------
    // uint — valid rotations
    // --------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="NumericExtensions.RotateBitsRight(uint, int)" /> produces the expected result
    /// for a representative set of values and rotation counts within the valid range [0, 32].
    /// </summary>
    [TestMethod]
    [DataRow(0x00000000U, 0, 0x00000000U)] // all-zero — identity
    [DataRow(0xFFFFFFFFU, 1, 0xFFFFFFFFU)] // all-ones — identity
    [DataRow(0xFFFFFFFFU, 32, 0xFFFFFFFFU)] // full rotation — identity
    [DataRow(0x00000001U, 1, 0x80000000U)] // LSB wraps to MSB
    [DataRow(0x80000000U, 1, 0x40000000U)] // MSB shifts right by 1
    [DataRow(0xAAAAAAAAU, 15, 0x55555555U)] // 1010... rotated 15 → 0101...
    [DataRow(0x55555555U, 17, 0xAAAAAAAAU)] // 0101... rotated 17 → 1010...
    [DataRow(0x00000001U, 31, 0x00000002U)] // single bit rotated to bit 1
    public void RotateBitsRight_WhenCountIsValid_ForUInt_ShouldProduceExpectedResult(uint value, int count, uint expected) =>
        Assert.AreEqual(expected, value.RotateBitsRight(count));

    // --------------------------------------------------
    // ulong — valid rotations
    // --------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="NumericExtensions.RotateBitsRight(ulong, int)" /> produces the expected result
    /// for a representative set of values and rotation counts within the valid range [0, 64].
    /// </summary>
    [TestMethod]
    [DataRow(0x0000000000000000UL, 0, 0x0000000000000000UL)] // all-zero — identity
    [DataRow(0xFFFFFFFFFFFFFFFFUL, 1, 0xFFFFFFFFFFFFFFFFUL)] // all-ones — identity
    [DataRow(0xFFFFFFFFFFFFFFFFUL, 64, 0xFFFFFFFFFFFFFFFFUL)] // full rotation — identity
    [DataRow(0x0000000000000001UL, 1, 0x8000000000000000UL)] // LSB wraps to MSB
    [DataRow(0x8000000000000000UL, 1, 0x4000000000000000UL)] // MSB shifts right by 1
    [DataRow(0xAAAAAAAAAAAAAAAAUL, 31, 0x5555555555555555UL)] // 1010... rotated 31 → 0101...
    [DataRow(0x5555555555555555UL, 33, 0xAAAAAAAAAAAAAAAAUL)] // 0101... rotated 33 → 1010...
    [DataRow(0x0000000000000001UL, 63, 0x0000000000000002UL)] // single bit rotated 63 positions
    public void RotateBitsRight_WhenCountIsValid_ForULong_ShouldProduceExpectedResult(ulong value, int count, ulong expected) =>
        Assert.AreEqual(expected, value.RotateBitsRight(count));

    // --------------------------------------------------
    // ushort — valid rotations
    // --------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="NumericExtensions.RotateBitsRight(ushort, int)" /> produces the expected result
    /// for a representative set of values and rotation counts within the valid range [0, 16].
    /// </summary>
    [TestMethod]
    [DataRow((ushort)0x0000, 0, (ushort)0x0000)] // all-zero — identity
    [DataRow((ushort)0xFFFF, 1, (ushort)0xFFFF)] // all-ones — identity
    [DataRow((ushort)0xFFFF, 16, (ushort)0xFFFF)] // full rotation — identity
    [DataRow((ushort)0xAAAA, 16, (ushort)0xAAAA)] // full rotation preserves pattern
    [DataRow((ushort)0x0001, 1, (ushort)0x8000)] // LSB wraps to MSB
    [DataRow((ushort)0x8000, 1, (ushort)0x4000)] // MSB shifts right by 1
    [DataRow((ushort)0xAAAA, 7, (ushort)0x5555)] // 1010... rotated 7 → 0101...
    [DataRow((ushort)0x5555, 9, (ushort)0xAAAA)] // 0101... rotated 9 → 1010...
    public void RotateBitsRight_WhenCountIsValid_ForUShort_ShouldProduceExpectedResult(ushort value, int count, ushort expected) =>
        Assert.AreEqual(expected, value.RotateBitsRight(count));

    /// <summary>
    /// Verifies that a right rotation of <c>n</c> positions followed by a left rotation of <c>n</c> positions
    /// returns the original <see cref="byte" /> value.
    /// </summary>
    [TestMethod]
    [DataRow((byte)0x01, 1)]
    [DataRow((byte)0xAA, 3)]
    [DataRow((byte)0xFF, 7)]
    [DataRow((byte)0x12, 5)]
    public void RotateBitsRight_WhenFollowedByRotateBitsLeft_ForByte_ShouldReturnOriginalValue(byte value, int count) =>
        Assert.AreEqual(value, value.RotateBitsRight(count).RotateBitsLeft(count));

}