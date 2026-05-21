// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BufferConverterTests.SwapEndian.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class BufferConverterTests
{

    // =========================================================================
    // SwapEndian(ReadOnlySpan<byte>, Span<byte>, int)
    // =========================================================================

    /// <summary>
    /// Verifies that copying a byte span to a destination while swapping endianness reverses the bytes of each element-sized block.
    /// </summary>
    [TestMethod]
    public void SwapEndian_WhenCopyingWithElementSizeFour_ForByteSpans_ShouldReverseBytesPerElement()
    {
        byte[] source = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08];
        var destination = new byte[source.Length];

        BufferConverter.SwapEndian(source, destination, 4);

        CollectionAssert.AreEqual(
            new byte[] { 0x04, 0x03, 0x02, 0x01, 0x08, 0x07, 0x06, 0x05 },
            destination);
    }

    /// <summary>
    /// Verifies that swap-endian between byte spans correctly handles an element size of two.
    /// </summary>
    [TestMethod]
    public void SwapEndian_WhenCopyingWithElementSizeTwo_ForByteSpans_ShouldReverseBytesPerElement()
    {
        byte[] source = [0x01, 0x02, 0x03, 0x04];
        var destination = new byte[source.Length];

        BufferConverter.SwapEndian(source, destination, 2);

        CollectionAssert.AreEqual(new byte[] { 0x02, 0x01, 0x04, 0x03 }, destination);
    }

    /// <summary>
    /// Verifies that a destination shorter than the source throws an appropriate exception.
    /// </summary>
    [TestMethod]
    public void SwapEndian_WhenDestinationIsShorterThanSource_ForByteSpans_ShouldThrowArgumentOutOfRangeException()
    {
        var source = new byte[8];
        var destination = new byte[4];

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            BufferConverter.SwapEndian(source, destination, 4);
        });
    }

    /// <summary>
    /// Verifies that an <paramref name="elementSize"/> of one or less throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    [DataRow(0, DisplayName = "Element size zero")]
    [DataRow(1, DisplayName = "Element size one")]
    [DataRow(-1, DisplayName = "Negative element size")]
    public void SwapEndian_WhenElementSizeIsInvalid_ForByteSpans_ShouldThrowArgumentOutOfRangeException(int elementSize)
    {
        byte[] source = [0x01, 0x02, 0x03, 0x04];
        var destination = new byte[source.Length];

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            BufferConverter.SwapEndian(source, destination, elementSize);
        });
    }

    /// <summary>
    /// Verifies that swap-endian on a single-byte element type is a no-op and leaves the span unchanged.
    /// </summary>
    [TestMethod]
    public void SwapEndian_WhenElementSizeIsOneByte_ForTypedSpan_ShouldLeaveSpanUnchanged()
    {
        byte[] data = [0x01, 0x02, 0x03];

        Span<byte> span = data;
        span.SwapEndian();

        CollectionAssert.AreEqual(new byte[] { 0x01, 0x02, 0x03 }, data);
    }

    /// <summary>
    /// Verifies that in-place swap-endian (source and destination referring to the same storage) produces the expected reversed-per-element result.
    /// </summary>
    [TestMethod]
    public void SwapEndian_WhenSourceAndDestinationOverlap_ForByteSpans_ShouldProduceCorrectResult()
    {
        byte[] buffer = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08];

        BufferConverter.SwapEndian(buffer, buffer, 4);

        CollectionAssert.AreEqual(
            new byte[] { 0x04, 0x03, 0x02, 0x01, 0x08, 0x07, 0x06, 0x05 },
            buffer);
    }

    /// <summary>
    /// Verifies that source or destination lengths that are not a positive multiple of the element size throw <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    public void SwapEndian_WhenSourceLengthIsNotMultipleOfElementSize_ForByteSpans_ShouldThrowArgumentException()
    {
        var source = new byte[5];
        var destination = new byte[8];

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            BufferConverter.SwapEndian(source, destination, 4);
        });
    }

    /// <summary>
    /// Verifies that swapping endianness of a 16-bit typed span reverses the byte order of each element.
    /// </summary>
    [TestMethod]
    public void SwapEndian_WhenSpanContainsInt16Elements_ForTypedSpan_ShouldReverseBytesOfEachElement()
    {
        short[] data = [0x0102, 0x0304, 0x0506];

        Span<short> span = data;
        span.SwapEndian();

        Assert.AreEqual(unchecked((short)0x0201), data[0]);
        Assert.AreEqual(unchecked((short)0x0403), data[1]);
        Assert.AreEqual(unchecked((short)0x0605), data[2]);
    }
    // =========================================================================
    // SwapEndian<T>(Span<T>)
    // =========================================================================

    /// <summary>
    /// Verifies that swapping endianness of a 32-bit typed span reverses the byte order of each element while preserving element count and positions.
    /// </summary>
    [TestMethod]
    public void SwapEndian_WhenSpanContainsInt32Elements_ForTypedSpan_ShouldReverseBytesOfEachElement()
    {
        int[] data = [0x01020304, 0x05060708];

        Span<int> span = data;
        span.SwapEndian();

        Assert.AreEqual(0x04030201, data[0]);
        Assert.AreEqual(0x08070605, data[1]);
    }

    /// <summary>
    /// Verifies that swap-endian on an empty span is a no-op and does not throw.
    /// </summary>
    [TestMethod]
    public void SwapEndian_WhenSpanIsEmpty_ForTypedSpan_ShouldBeNoOp()
    {
        Span<int> span = [];

        span.SwapEndian();

        Assert.AreEqual(0, span.Length);
    }

}
