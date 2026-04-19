// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BufferConverterTests.Read.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Extensions;

public partial class BufferConverterTests
{
    // =========================================================================
    // Read<T>(byte[], int)
    // =========================================================================

    /// <summary>
    /// Verifies that reading primitive unmanaged values from a byte array at varying indices returns the expected little-endian value.
    /// </summary>
    [TestMethod]
    public void Read_WhenCalled_ForByteArrayToInt32_ShouldReturnExpectedLittleEndianValue()
    {
        byte[] source = AscendingBytes;

        Assert.AreEqual(0x03020100, source.Read<int>(0));
        Assert.AreEqual(0x07060504, source.Read<int>(4));
        Assert.AreEqual(0x0F0E0D0C, source.Read<int>(12));
    }

    /// <summary>
    /// Verifies that reading a long value from a byte array produces the expected 64-bit interpretation.
    /// </summary>
    [TestMethod]
    public void Read_WhenCalled_ForByteArrayToInt64_ShouldReturnExpectedLittleEndianValue()
    {
        byte[] source = AscendingBytes;

        Assert.AreEqual(0x0706050403020100L, source.Read<long>(0));
        Assert.AreEqual(0x0F0E0D0C0B0A0908L, source.Read<long>(8));
    }

    /// <summary>
    /// Verifies that reading values from a byte array and writing them back using <c>CopyTo</c> preserves each value.
    /// </summary>
    [TestMethod]
    public void Read_WhenFollowedByCopyTo_ForByteArray_ShouldRoundTripValues()
    {
        byte[] source = AscendingBytes;
        byte[] destination = new byte[source.Length];

        for (int i = 0; i < source.Length; i += sizeof(int))
        {
            int value = source.Read<int>(i);
            value.CopyTo(destination, i);
        }

        CollectionAssert.AreEqual(source, destination);
    }

    /// <summary>
    /// Verifies that reading from a null byte array throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void Read_WhenSourceIsNull_ForByteArray_ShouldThrowArgumentNullException()
    {
        byte[]? source = null;

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = source!.Read<int>(0);
        });
    }

    /// <summary>
    /// Verifies that a negative index throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void Read_WhenIndexIsNegative_ForByteArray_ShouldThrowArgumentOutOfRangeException()
    {
        byte[] source = AscendingBytes;

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = source.Read<int>(-1);
        });
    }

    /// <summary>
    /// Verifies that an index whose combination with <c>sizeof(T)</c> exceeds the array length throws
    /// <see cref="ArgumentException" />, matching the <c>ThrowIfArrayOffsetOrCountInvalid</c> combination contract.
    /// </summary>
    [TestMethod]
    [DataRow(13, DisplayName = "Insufficient bytes remaining for int")]
    [DataRow(16, DisplayName = "Index at array length")]
    public void Read_WhenIndexPlusElementSizeExceedsArrayLength_ForByteArray_ShouldThrowArgumentException(int index)
    {
        byte[] source = AscendingBytes;

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = source.Read<int>(index);
        });
    }

    // =========================================================================
    // Read<T>(ReadOnlySpan<byte>)
    // =========================================================================

    /// <summary>
    /// Verifies that reading from a byte span returns the expected little-endian value.
    /// </summary>
    [TestMethod]
    public void Read_WhenCalled_ForReadOnlySpanToInt32_ShouldReturnExpectedLittleEndianValue()
    {
        byte[] data = AscendingBytes;

        ReadOnlySpan<byte> source = data.AsSpan(4, 4);

        Assert.AreEqual(0x07060504, source.Read<int>());
    }

    /// <summary>
    /// Verifies that reading from a span shorter than the element size throws <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    public void Read_WhenSpanLengthIsInsufficient_ForReadOnlySpan_ShouldThrowArgumentException()
    {
        byte[] data = new byte[3];

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            ReadOnlySpan<byte> source = data;
            _ = source.Read<int>();
        });
    }

    /// <summary>
    /// Verifies that reading from an empty span throws <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    public void Read_WhenSpanIsEmpty_ForReadOnlySpan_ShouldThrowArgumentException()
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            ReadOnlySpan<byte> source = ReadOnlySpan<byte>.Empty;
            _ = source.Read<int>();
        });
    }
}
