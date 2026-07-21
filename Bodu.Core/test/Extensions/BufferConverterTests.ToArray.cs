// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BufferConverterTests.ToArray.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class BufferConverterTests
{

    /// <summary>
    /// Verifies that negative or overflowing source index or count values throw <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    [DataRow(-1, 1, DisplayName = "Negative source index")]
    [DataRow(0, -1, DisplayName = "Negative count")]
    [DataRow(0, 17, DisplayName = "Count exceeds source length")]
    public void ToArray_WhenArgumentsAreInvalid_ForByteArray_ShouldThrowExactly(int sourceIndex, int count)
    {
        byte[] source = AscendingBytes;

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = source.ToArray<byte>(sourceIndex, count);
        });
    }

    /// <summary>
    /// Verifies that the result is a fresh heap allocation independent of the source buffer.
    /// </summary>
    [TestMethod]
    public void ToArray_WhenCalled_ForByteArray_ShouldReturnNewAllocation()
    {
        byte[] source = AscendingBytes;

        byte[] result = source.ToArray<byte>(0, source.Length);

        Assert.IsFalse(ReferenceEquals(source, result));
    }
    // =========================================================================
    // ToArray<T>(byte[], int, int)
    // =========================================================================

    /// <summary>
    /// Verifies that converting a byte array into a new byte array copies the expected range.
    /// </summary>
    [TestMethod]
    public void ToArray_WhenCalled_ForByteArrayToByteArray_ShouldReturnExpectedElements()
    {
        byte[] source = AscendingBytes;

        byte[] result = source.ToArray<byte>(0, 8);

        CollectionAssert.AreEqual(
            new byte[] { 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07 },
            result);
    }

    /// <summary>
    /// Verifies that the span ToArray result is a fresh heap allocation independent of the source data.
    /// </summary>
    [TestMethod]
    public void ToArray_WhenCalled_ForReadOnlySpan_ShouldReturnNewAllocation()
    {
        byte[] data = AscendingBytes;

        ReadOnlySpan<byte> source = data;
        byte[] result = source.ToArray<byte>(data.Length);

        Assert.IsFalse(ReferenceEquals(data, result));
    }

    // =========================================================================
    // ToArray<T>(ReadOnlySpan<byte>, int)
    // =========================================================================

    /// <summary>
    /// Verifies that converting a byte span into a typed int array produces the expected little-endian elements.
    /// </summary>
    [TestMethod]
    public void ToArray_WhenCalled_ForReadOnlySpanToInt32_ShouldReturnExpectedElements()
    {
        byte[] data = AscendingBytes;

        ReadOnlySpan<byte> source = data;
        int[] result = source.ToArray<int>(4);

        CollectionAssert.AreEqual(
            new[] { 0x03020100, 0x07060504, 0x0B0A0908, 0x0F0E0D0C },
            result);
    }

    /// <summary>
    /// Verifies that converting a byte span into a typed long array produces the expected 64-bit elements.
    /// </summary>
    [TestMethod]
    public void ToArray_WhenCalled_ForReadOnlySpanToInt64_ShouldReturnExpectedElements()
    {
        byte[] data = AscendingBytes;

        ReadOnlySpan<byte> source = data;
        long[] result = source.ToArray<long>(2);

        CollectionAssert.AreEqual(
            new[] { 0x0706050403020100L, 0x0F0E0D0C0B0A0908L },
            result);
    }

    /// <summary>
    /// Verifies that a count exceeding the available bytes in the span throws <see cref="ArgumentException" />
    /// from the span-length-insufficient guard.
    /// </summary>
    [TestMethod]
    public void ToArray_WhenCountExceedsAvailableBytes_ForReadOnlySpan_ShouldThrowExactly()
    {
        byte[] data = new byte[8];

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            ReadOnlySpan<byte> source = data;
            _ = source.ToArray<int>(3);
        });
    }

    /// <summary>
    /// Verifies that a count of zero returns an empty array.
    /// </summary>
    [TestMethod]
    public void ToArray_WhenCountIsZero_ForByteArray_ShouldReturnEmptyArray()
    {
        byte[] source = AscendingBytes;

        byte[] result = source.ToArray<byte>(0, 0);

        Assert.IsEmpty(result);
    }

    /// <summary>
    /// Verifies that a count of zero on a byte span returns an empty array.
    /// </summary>
    [TestMethod]
    public void ToArray_WhenCountIsZero_ForReadOnlySpan_ShouldReturnEmptyArray()
    {
        byte[] data = AscendingBytes;

        ReadOnlySpan<byte> source = data;
        int[] result = source.ToArray<int>(0);

        Assert.IsEmpty(result);
    }

    /// <summary>
    /// Verifies that converting a byte array with a non-zero source index returns the expected slice.
    /// </summary>
    [TestMethod]
    public void ToArray_WhenSourceIndexIsNonZero_ForByteArrayToByteArray_ShouldReturnExpectedElements()
    {
        byte[] source = AscendingBytes;

        byte[] result = source.ToArray<byte>(8, 4);

        CollectionAssert.AreEqual(new byte[] { 0x08, 0x09, 0x0A, 0x0B }, result);
    }

    /// <summary>
    /// Verifies that a valid <c>sourceIndex</c> combined with a <c>count</c> that extends past the end of the source
    /// throws <see cref="ArgumentException" /> from the combination-violation branch of <c>ThrowIfArrayOffsetOrCountInvalid</c>.
    /// </summary>
    [TestMethod]
    public void ToArray_WhenSourceIndexPlusCountExceedsSourceLength_ForByteArray_ShouldThrowExactly()
    {
        byte[] source = AscendingBytes;

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = source.ToArray<byte>(14, 4);
        });
    }

    /// <summary>
    /// Verifies that a null source byte array throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void ToArray_WhenSourceIsNull_ForByteArray_ShouldThrowExactly()
    {
        byte[]? source = null;

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = source!.ToArray<byte>(0, 1);
        });
    }

    /// <summary>
    /// Verifies that the array-based overload converts a byte array into elements wider than one byte, sizing the
    /// requested range in bytes for the source and in elements for the result.
    /// </summary>
    [TestMethod]
    public void ToArray_WhenElementIsInt32_ForByteArray_ShouldConvertElements()
    {
        byte[] source = [.. BitConverter.GetBytes(0x01020304), .. BitConverter.GetBytes(0x05060708)];

        int[] actual = source.ToArray<int>(0, 2);

        CollectionAssert.AreEqual(new[] { 0x01020304, 0x05060708 }, actual);
    }

    /// <summary>
    /// Verifies that the array-based overload honors a non-zero source index when converting elements wider than one
    /// byte.
    /// </summary>
    [TestMethod]
    public void ToArray_WhenElementIsInt64WithOffset_ForByteArray_ShouldConvertElements()
    {
        byte[] source = [0xFF, 0xFF, .. BitConverter.GetBytes(0x0102030405060708L)];

        long[] actual = source.ToArray<long>(2, 1);

        CollectionAssert.AreEqual(new[] { 0x0102030405060708L }, actual);
    }

}
