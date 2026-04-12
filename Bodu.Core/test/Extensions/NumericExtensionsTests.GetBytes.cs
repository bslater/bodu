// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NumericExtensionsTests.GetBytes.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Extensions;

public partial class NumericExtensionsTests
{
    // --------------------------------------------------
    // Output length
    // --------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="NumericExtensions.GetBytes{T}" /> returns a single byte for
    /// <see cref="byte" /> and <see cref="sbyte" /> values.
    /// </summary>
    [TestMethod]
    [DataRow((byte)0x00)]
    [DataRow((byte)0xFF)]
    public void GetBytes_WhenValueIsByte_ShouldReturnSingleByte(byte value) =>
        Assert.AreEqual(1, value.GetBytes().Length);

    /// <summary>
    /// Verifies that <see cref="NumericExtensions.GetBytes{T}" /> returns a single byte for
    /// all <see cref="sbyte" /> values.
    /// </summary>
    [TestMethod]
    [DataRow((sbyte)0)]
    [DataRow((sbyte)-1)]
    [DataRow(sbyte.MinValue)]
    [DataRow(sbyte.MaxValue)]
    public void GetBytes_WhenValueIsSByte_ShouldReturnSingleByte(sbyte value) =>
        Assert.AreEqual(1, value.GetBytes().Length);

    /// <summary>
    /// Verifies that <see cref="NumericExtensions.GetBytes{T}" /> returns two bytes for
    /// <see cref="short" /> and <see cref="ushort" /> values.
    /// </summary>
    [TestMethod]
    [DataRow((short)1)]
    [DataRow((ushort)1)]
    public void GetBytes_WhenValueIsTwoByteType_ShouldReturnTwoBytes(object value)
    {
        int length = value switch
        {
            short s => s.GetBytes().Length,
            ushort u => u.GetBytes().Length,
            _ => throw new InvalidOperationException()
        };
        Assert.AreEqual(2, length);
    }

    /// <summary>
    /// Verifies that <see cref="NumericExtensions.GetBytes{T}" /> returns four bytes for
    /// <see cref="int" />, <see cref="uint" />, and <see cref="float" /> values.
    /// </summary>
    [TestMethod]
    [DataRow(1)]
    [DataRow(1U)]
    [DataRow(1.0f)]
    public void GetBytes_WhenValueIsFourByteType_ShouldReturnFourBytes(object value)
    {
        int length = value switch
        {
            int i => i.GetBytes().Length,
            uint u => u.GetBytes().Length,
            float f => f.GetBytes().Length,
            _ => throw new InvalidOperationException()
        };
        Assert.AreEqual(4, length);
    }

    /// <summary>
    /// Verifies that <see cref="NumericExtensions.GetBytes{T}" /> returns eight bytes for
    /// <see cref="long" />, <see cref="ulong" />, and <see cref="double" /> values.
    /// </summary>
    [TestMethod]
    [DataRow(1L)]
    [DataRow(1UL)]
    [DataRow(1.0)]
    public void GetBytes_WhenValueIsEightByteType_ShouldReturnEightBytes(object value)
    {
        int length = value switch
        {
            long l => l.GetBytes().Length,
            ulong u => u.GetBytes().Length,
            double d => d.GetBytes().Length,
            _ => throw new InvalidOperationException()
        };
        Assert.AreEqual(8, length);
    }

    // --------------------------------------------------
    // byte / sbyte — single-byte types are endianness-neutral
    // --------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="NumericExtensions.GetBytes{T}" /> returns the correct single byte
    /// for representative <see cref="byte" /> values regardless of the <paramref name="asBigEndian" /> flag.
    /// </summary>
    [TestMethod]
    [DataRow((byte)0x00, new byte[] { 0x00 })]
    [DataRow((byte)0x01, new byte[] { 0x01 })]
    [DataRow((byte)0x7F, new byte[] { 0x7F })]
    [DataRow((byte)0x80, new byte[] { 0x80 })]
    [DataRow((byte)0xFF, new byte[] { 0xFF })]
    public void GetBytes_WhenValueIsByte_ShouldReturnExpectedBytes(byte value, byte[] expected)
    {
        CollectionAssert.AreEqual(expected, value.GetBytes());
        CollectionAssert.AreEqual(expected, value.GetBytes(asBigEndian: true));
    }

    /// <summary>
    /// Verifies that <see cref="NumericExtensions.GetBytes{T}" /> returns the correct single byte
    /// for representative <see cref="sbyte" /> values, preserving the two's complement bit pattern.
    /// </summary>
    [TestMethod]
    [DataRow((sbyte)0, new byte[] { 0x00 })]
    [DataRow((sbyte)1, new byte[] { 0x01 })]
    [DataRow((sbyte)127, new byte[] { 0x7F })]
    [DataRow((sbyte)-1, new byte[] { 0xFF })]
    [DataRow((sbyte)-128, new byte[] { 0x80 })]
    public void GetBytes_WhenValueIsSByte_ShouldReturnExpectedBytes(sbyte value, byte[] expected)
    {
        CollectionAssert.AreEqual(expected, value.GetBytes());
        CollectionAssert.AreEqual(expected, value.GetBytes(asBigEndian: true));
    }

    // --------------------------------------------------
    // short / ushort — little-endian
    // --------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="NumericExtensions.GetBytes{T}" /> returns the correct little-endian
    /// bytes for representative <see cref="short" /> values.
    /// </summary>
    [TestMethod]
    [DataRow((short)0, new byte[] { 0x00, 0x00 })]
    [DataRow((short)1, new byte[] { 0x01, 0x00 })]
    [DataRow((short)0x1234, new byte[] { 0x34, 0x12 })]
    [DataRow((short)-1, new byte[] { 0xFF, 0xFF })]
    [DataRow(short.MaxValue, new byte[] { 0xFF, 0x7F })]
    [DataRow(short.MinValue, new byte[] { 0x00, 0x80 })]
    public void GetBytes_WhenValueIsShort_ShouldReturnLittleEndianBytes(short value, byte[] expected) =>
        CollectionAssert.AreEqual(expected, value.GetBytes());

    /// <summary>
    /// Verifies that <see cref="NumericExtensions.GetBytes{T}" /> returns the correct big-endian
    /// bytes for representative <see cref="short" /> values when <c>asBigEndian</c> is <see langword="true" />.
    /// </summary>
    [TestMethod]
    [DataRow((short)0, new byte[] { 0x00, 0x00 })]
    [DataRow((short)1, new byte[] { 0x00, 0x01 })]
    [DataRow((short)0x1234, new byte[] { 0x12, 0x34 })]
    [DataRow((short)-1, new byte[] { 0xFF, 0xFF })]
    [DataRow(short.MaxValue, new byte[] { 0x7F, 0xFF })]
    [DataRow(short.MinValue, new byte[] { 0x80, 0x00 })]
    public void GetBytes_WhenValueIsShort_AndBigEndianRequested_ShouldReturnBigEndianBytes(short value, byte[] expected) =>
        CollectionAssert.AreEqual(expected, value.GetBytes(asBigEndian: true));

    /// <summary>
    /// Verifies that <see cref="NumericExtensions.GetBytes{T}" /> returns the correct little-endian
    /// bytes for representative <see cref="ushort" /> values.
    /// </summary>
    [TestMethod]
    [DataRow((ushort)0x0000, new byte[] { 0x00, 0x00 })]
    [DataRow((ushort)0x0001, new byte[] { 0x01, 0x00 })]
    [DataRow((ushort)0x1234, new byte[] { 0x34, 0x12 })]
    [DataRow((ushort)0x8000, new byte[] { 0x00, 0x80 })]
    [DataRow((ushort)0xFFFF, new byte[] { 0xFF, 0xFF })]
    public void GetBytes_WhenValueIsUShort_ShouldReturnLittleEndianBytes(ushort value, byte[] expected) =>
        CollectionAssert.AreEqual(expected, value.GetBytes());

    /// <summary>
    /// Verifies that <see cref="NumericExtensions.GetBytes{T}" /> returns the correct big-endian
    /// bytes for representative <see cref="ushort" /> values when <c>asBigEndian</c> is <see langword="true" />.
    /// </summary>
    [TestMethod]
    [DataRow((ushort)0x0000, new byte[] { 0x00, 0x00 })]
    [DataRow((ushort)0x0001, new byte[] { 0x00, 0x01 })]
    [DataRow((ushort)0x1234, new byte[] { 0x12, 0x34 })]
    [DataRow((ushort)0x8000, new byte[] { 0x80, 0x00 })]
    [DataRow((ushort)0xFFFF, new byte[] { 0xFF, 0xFF })]
    public void GetBytes_WhenValueIsUShort_AndBigEndianRequested_ShouldReturnBigEndianBytes(ushort value, byte[] expected) =>
        CollectionAssert.AreEqual(expected, value.GetBytes(asBigEndian: true));

    // --------------------------------------------------
    // int / uint — little-endian
    // --------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="NumericExtensions.GetBytes{T}" /> returns the correct little-endian
    /// bytes for representative <see cref="int" /> values, including boundary and two's complement cases.
    /// </summary>
    [TestMethod]
    [DataRow(0, new byte[] { 0x00, 0x00, 0x00, 0x00 })]
    [DataRow(1, new byte[] { 0x01, 0x00, 0x00, 0x00 })]
    [DataRow(0x12345678, new byte[] { 0x78, 0x56, 0x34, 0x12 })]
    [DataRow(-1, new byte[] { 0xFF, 0xFF, 0xFF, 0xFF })]
    [DataRow(int.MaxValue, new byte[] { 0xFF, 0xFF, 0xFF, 0x7F })]
    [DataRow(int.MinValue, new byte[] { 0x00, 0x00, 0x00, 0x80 })]
    [DataRow(-int.MaxValue, new byte[] { 0x01, 0x00, 0x00, 0x80 })]
    public void GetBytes_WhenValueIsInt_ShouldReturnLittleEndianBytes(int value, byte[] expected) =>
        CollectionAssert.AreEqual(expected, value.GetBytes());

    /// <summary>
    /// Verifies that <see cref="NumericExtensions.GetBytes{T}" /> returns the correct big-endian
    /// bytes for representative <see cref="int" /> values when <c>asBigEndian</c> is <see langword="true" />.
    /// </summary>
    [TestMethod]
    [DataRow(0, new byte[] { 0x00, 0x00, 0x00, 0x00 })]
    [DataRow(1, new byte[] { 0x00, 0x00, 0x00, 0x01 })]
    [DataRow(0x12345678, new byte[] { 0x12, 0x34, 0x56, 0x78 })]
    [DataRow(-1, new byte[] { 0xFF, 0xFF, 0xFF, 0xFF })]
    [DataRow(int.MaxValue, new byte[] { 0x7F, 0xFF, 0xFF, 0xFF })]
    [DataRow(int.MinValue, new byte[] { 0x80, 0x00, 0x00, 0x00 })]
    [DataRow(-int.MaxValue, new byte[] { 0x80, 0x00, 0x00, 0x01 })]
    public void GetBytes_WhenValueIsInt_AndBigEndianRequested_ShouldReturnBigEndianBytes(int value, byte[] expected) =>
        CollectionAssert.AreEqual(expected, value.GetBytes(asBigEndian: true));

    /// <summary>
    /// Verifies that <see cref="NumericExtensions.GetBytes{T}" /> returns the correct little-endian
    /// bytes for representative <see cref="uint" /> values.
    /// </summary>
    [TestMethod]
    [DataRow(0x00000000U, new byte[] { 0x00, 0x00, 0x00, 0x00 })]
    [DataRow(0x00000001U, new byte[] { 0x01, 0x00, 0x00, 0x00 })]
    [DataRow(0x12345678U, new byte[] { 0x78, 0x56, 0x34, 0x12 })]
    [DataRow(0x80000000U, new byte[] { 0x00, 0x00, 0x00, 0x80 })]
    [DataRow(0xFFFFFFFFU, new byte[] { 0xFF, 0xFF, 0xFF, 0xFF })]
    public void GetBytes_WhenValueIsUInt_ShouldReturnLittleEndianBytes(uint value, byte[] expected) =>
        CollectionAssert.AreEqual(expected, value.GetBytes());

    /// <summary>
    /// Verifies that <see cref="NumericExtensions.GetBytes{T}" /> returns the correct big-endian
    /// bytes for representative <see cref="uint" /> values when <c>asBigEndian</c> is <see langword="true" />.
    /// </summary>
    [TestMethod]
    [DataRow(0x00000000U, new byte[] { 0x00, 0x00, 0x00, 0x00 })]
    [DataRow(0x00000001U, new byte[] { 0x00, 0x00, 0x00, 0x01 })]
    [DataRow(0x12345678U, new byte[] { 0x12, 0x34, 0x56, 0x78 })]
    [DataRow(0x80000000U, new byte[] { 0x80, 0x00, 0x00, 0x00 })]
    [DataRow(0xFFFFFFFFU, new byte[] { 0xFF, 0xFF, 0xFF, 0xFF })]
    public void GetBytes_WhenValueIsUInt_AndBigEndianRequested_ShouldReturnBigEndianBytes(uint value, byte[] expected) =>
        CollectionAssert.AreEqual(expected, value.GetBytes(asBigEndian: true));

    // --------------------------------------------------
    // long / ulong — little-endian
    // --------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="NumericExtensions.GetBytes{T}" /> returns the correct little-endian
    /// bytes for representative <see cref="long" /> values, including boundary and two's complement cases.
    /// </summary>
    [TestMethod]
    [DataRow(0L, new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 })]
    [DataRow(1L, new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 })]
    [DataRow(-1L, new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF })]
    [DataRow(long.MaxValue, new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F })]
    [DataRow(long.MinValue, new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80 })]
    [DataRow(-long.MaxValue, new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80 })]
    public void GetBytes_WhenValueIsLong_ShouldReturnLittleEndianBytes(long value, byte[] expected) =>
        CollectionAssert.AreEqual(expected, value.GetBytes());

    /// <summary>
    /// Verifies that <see cref="NumericExtensions.GetBytes{T}" /> returns the correct big-endian
    /// bytes for representative <see cref="long" /> values when <c>asBigEndian</c> is <see langword="true" />.
    /// </summary>
    [TestMethod]
    [DataRow(0L, new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 })]
    [DataRow(1L, new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 })]
    [DataRow(-1L, new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF })]
    [DataRow(long.MaxValue, new byte[] { 0x7F, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF })]
    [DataRow(long.MinValue, new byte[] { 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 })]
    [DataRow(-long.MaxValue, new byte[] { 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 })]
    public void GetBytes_WhenValueIsLong_AndBigEndianRequested_ShouldReturnBigEndianBytes(long value, byte[] expected) =>
        CollectionAssert.AreEqual(expected, value.GetBytes(asBigEndian: true));

    /// <summary>
    /// Verifies that <see cref="NumericExtensions.GetBytes{T}" /> returns the correct little-endian
    /// bytes for representative <see cref="ulong" /> values.
    /// </summary>
    [TestMethod]
    [DataRow(0x0000000000000000UL, new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 })]
    [DataRow(0x0000000000000001UL, new byte[] { 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 })]
    [DataRow(0x0102030405060708UL, new byte[] { 0x08, 0x07, 0x06, 0x05, 0x04, 0x03, 0x02, 0x01 })]
    [DataRow(0x8000000000000000UL, new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80 })]
    [DataRow(0xFFFFFFFFFFFFFFFFUL, new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF })]
    public void GetBytes_WhenValueIsULong_ShouldReturnLittleEndianBytes(ulong value, byte[] expected) =>
        CollectionAssert.AreEqual(expected, value.GetBytes());

    /// <summary>
    /// Verifies that <see cref="NumericExtensions.GetBytes{T}" /> returns the correct big-endian
    /// bytes for representative <see cref="ulong" /> values when <c>asBigEndian</c> is <see langword="true" />.
    /// </summary>
    [TestMethod]
    [DataRow(0x0000000000000000UL, new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 })]
    [DataRow(0x0000000000000001UL, new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 })]
    [DataRow(0x0102030405060708UL, new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08 })]
    [DataRow(0x8000000000000000UL, new byte[] { 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 })]
    [DataRow(0xFFFFFFFFFFFFFFFFUL, new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF })]
    public void GetBytes_WhenValueIsULong_AndBigEndianRequested_ShouldReturnBigEndianBytes(ulong value, byte[] expected) =>
        CollectionAssert.AreEqual(expected, value.GetBytes(asBigEndian: true));

    // --------------------------------------------------
    // float / double — IEEE 754, compared against BitConverter
    // --------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="NumericExtensions.GetBytes{T}" /> returns bytes that are identical to
    /// those produced by <see cref="BitConverter.GetBytes(float)" /> for representative <see cref="float" /> values.
    /// </summary>
    [TestMethod]
    [DataRow(0.0f)]
    [DataRow(1.0f)]
    [DataRow(-1.0f)]
    [DataRow(float.MaxValue)]
    [DataRow(float.MinValue)]
    [DataRow(float.Epsilon)]
    [DataRow(float.NaN)]
    [DataRow(float.PositiveInfinity)]
    [DataRow(float.NegativeInfinity)]
    public void GetBytes_WhenValueIsFloat_ShouldMatchBitConverterOutput(float value) =>
        CollectionAssert.AreEqual(BitConverter.GetBytes(value), value.GetBytes());

    /// <summary>
    /// Verifies that <see cref="NumericExtensions.GetBytes{T}" /> returns the byte-reversed output of
    /// <see cref="BitConverter.GetBytes(float)" /> when <c>asBigEndian</c> is <see langword="true" /> on a
    /// little-endian system.
    /// </summary>
    [TestMethod]
    [DataRow(0.0f)]
    [DataRow(1.0f)]
    [DataRow(-1.0f)]
    [DataRow(float.MaxValue)]
    [DataRow(float.NaN)]
    public void GetBytes_WhenValueIsFloat_AndBigEndianRequested_ShouldReturnReversedBytes(float value)
    {
        byte[] leBytes = BitConverter.GetBytes(value);

        if (!BitConverter.IsLittleEndian)
            return; // Skip: on a big-endian system GetBytes(LE) and GetBytes(BE) differ differently.

        byte[] expected = (byte[])leBytes.Clone();
        Array.Reverse(expected);

        CollectionAssert.AreEqual(expected, value.GetBytes(asBigEndian: true));
    }

    /// <summary>
    /// Verifies that <see cref="NumericExtensions.GetBytes{T}" /> returns bytes that are identical to
    /// those produced by <see cref="BitConverter.GetBytes(double)" /> for representative <see cref="double" /> values.
    /// </summary>
    [TestMethod]
    [DataRow(0.0)]
    [DataRow(1.0)]
    [DataRow(-1.0)]
    [DataRow(double.MaxValue)]
    [DataRow(double.MinValue)]
    [DataRow(double.Epsilon)]
    [DataRow(double.NaN)]
    [DataRow(double.PositiveInfinity)]
    [DataRow(double.NegativeInfinity)]
    public void GetBytes_WhenValueIsDouble_ShouldMatchBitConverterOutput(double value) =>
        CollectionAssert.AreEqual(BitConverter.GetBytes(value), value.GetBytes());

    /// <summary>
    /// Verifies that <see cref="NumericExtensions.GetBytes{T}" /> returns the byte-reversed output of
    /// <see cref="BitConverter.GetBytes(double)" /> when <c>asBigEndian</c> is <see langword="true" /> on a
    /// little-endian system.
    /// </summary>
    [TestMethod]
    [DataRow(0.0)]
    [DataRow(1.0)]
    [DataRow(-1.0)]
    [DataRow(double.MaxValue)]
    [DataRow(double.NaN)]
    public void GetBytes_WhenValueIsDouble_AndBigEndianRequested_ShouldReturnReversedBytes(double value)
    {
        if (!BitConverter.IsLittleEndian)
            return; // Skip: on a big-endian system GetBytes(LE) and GetBytes(BE) differ differently.

        byte[] expected = BitConverter.GetBytes(value);
        Array.Reverse(expected);

        CollectionAssert.AreEqual(expected, value.GetBytes(asBigEndian: true));
    }

    // --------------------------------------------------
    // Big-endian is the exact byte-reversal of little-endian
    // --------------------------------------------------

    /// <summary>
    /// Verifies that the big-endian output is the exact byte-reversal of the little-endian output for
    /// <see cref="int" /> values, confirming that <c>asBigEndian</c> is consistently honoured.
    /// </summary>
    [TestMethod]
    [DataRow(0x12345678)]
    [DataRow(-1)]
    [DataRow(int.MaxValue)]
    [DataRow(int.MinValue)]
    public void GetBytes_WhenBigEndianRequested_ForInt_ShouldProduceExactReversalOfLittleEndian(int value)
    {
        byte[] le = value.GetBytes(asBigEndian: false);
        byte[] be = value.GetBytes(asBigEndian: true);
        Array.Reverse(le);
        CollectionAssert.AreEqual(le, be);
    }

    // --------------------------------------------------
    // Unsupported type
    // --------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="NumericExtensions.GetBytes{T}" /> throws <see cref="InvalidOperationException" />
    /// when called with a type that is not a supported numeric type.
    /// </summary>
    [TestMethod]
    public void GetBytes_WhenTypeIsUnsupported_ShouldThrowInvalidOperationException() =>
        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            1.0m.GetBytes();
        });
}