// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BufferConverterTests.Reverse.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Extensions;

public partial class BufferConverterTests
{
    // =========================================================================
    // Reverse(Span<byte>)
    // =========================================================================

    /// <summary>
    /// Provides byte-span inputs of varying lengths paired with their reversed expectations.
    /// </summary>
    public static IEnumerable<object[]> ReverseByteSpanData()
    {
        yield return new object[] { Array.Empty<byte>(), Array.Empty<byte>() };
        yield return new object[] { new byte[] { 0xAA }, new byte[] { 0xAA } };
        yield return new object[] { new byte[] { 0x01, 0x02 }, new byte[] { 0x02, 0x01 } };
        yield return new object[] { new byte[] { 0x01, 0x02, 0x03 }, new byte[] { 0x03, 0x02, 0x01 } };
        yield return new object[] { new byte[] { 0x01, 0x02, 0x03, 0x04 }, new byte[] { 0x04, 0x03, 0x02, 0x01 } };
    }

    /// <summary>
    /// Verifies that reversing a byte span of any length produces the correct in-place reversal.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(ReverseByteSpanData), DynamicDataSourceType.Method)]
    public void Reverse_WhenSpanHasVaryingLengths_ForByteSpan_ShouldReverseInPlace(byte[] input, byte[] expected)
    {
        Span<byte> span = input;

        span.Reverse();

        CollectionAssert.AreEqual(expected, input);
    }

    /// <summary>
    /// Verifies that reversing an empty byte span is a no-op and does not throw.
    /// </summary>
    [TestMethod]
    public void Reverse_WhenSpanIsEmpty_ForByteSpan_ShouldBeNoOp()
    {
        Span<byte> span = Span<byte>.Empty;

        span.Reverse();

        Assert.AreEqual(0, span.Length);
    }

    // =========================================================================
    // Reverse<T>(Span<T>)
    // =========================================================================

    /// <summary>
    /// Verifies that reversing a typed span of unmanaged elements reverses the sequence in place.
    /// </summary>
    [TestMethod]
    public void Reverse_WhenSpanHasMultipleElements_ForTypedSpan_ShouldReverseInPlace()
    {
        int[] data = { 1, 2, 3, 4, 5 };

        Span<int> span = data;
        span.Reverse();

        CollectionAssert.AreEqual(new[] { 5, 4, 3, 2, 1 }, data);
    }

    /// <summary>
    /// Verifies that a single-element typed span is left unchanged by reverse.
    /// </summary>
    [TestMethod]
    public void Reverse_WhenSpanHasSingleElement_ForTypedSpan_ShouldLeaveElementUnchanged()
    {
        int[] data = { 42 };

        Span<int> span = data;
        span.Reverse();

        CollectionAssert.AreEqual(new[] { 42 }, data);
    }

    /// <summary>
    /// Verifies that an empty typed span is accepted by reverse as a no-op.
    /// </summary>
    [TestMethod]
    public void Reverse_WhenSpanIsEmpty_ForTypedSpan_ShouldBeNoOp()
    {
        Span<long> span = Span<long>.Empty;

        span.Reverse();

        Assert.AreEqual(0, span.Length);
    }

    // =========================================================================
    // Reverse(Memory<byte>)
    // =========================================================================

    /// <summary>
    /// Verifies that reversing a byte memory block reverses the underlying storage in place.
    /// </summary>
    [TestMethod]
    public void Reverse_WhenMemoryHasMultipleElements_ForByteMemory_ShouldReverseInPlace()
    {
        byte[] data = { 0x01, 0x02, 0x03, 0x04 };

        Memory<byte> memory = data;
        memory.Reverse();

        CollectionAssert.AreEqual(new byte[] { 0x04, 0x03, 0x02, 0x01 }, data);
    }

    // =========================================================================
    // Reverse<T>(Memory<T>)
    // =========================================================================

    /// <summary>
    /// Verifies that reversing a typed memory block reverses the underlying storage in place.
    /// </summary>
    [TestMethod]
    public void Reverse_WhenMemoryHasMultipleElements_ForTypedMemory_ShouldReverseInPlace()
    {
        long[] data = { 10L, 20L, 30L };

        Memory<long> memory = data;
        memory.Reverse();

        CollectionAssert.AreEqual(new[] { 30L, 20L, 10L }, data);
    }
}
