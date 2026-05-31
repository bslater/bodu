// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BufferConverterTests.CopyTo.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class BufferConverterTests
{
    // =========================================================================
    // CopyTo<T>(byte[], int, T[], int, int)
    // =========================================================================

    /// <summary>
    /// Provides valid byte-array → byte-array copy cases covering varying offsets and lengths.
    /// </summary>
    public static IEnumerable<object[]> CopyToByteArrayToByteArrayValidData()
    {
        yield return new object[] { AscendingBytes, 0, new byte[16], 0, 16, AscendingBytes };
        yield return new object[] { AscendingBytes, 4, new byte[8], 0, 8, new byte[] { 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0x0B } };
        yield return new object[] { AscendingBytes, 0, new byte[8], 2, 4, new byte[] { 0x00, 0x00, 0x00, 0x01, 0x02, 0x03, 0x00, 0x00 } };
    }

    /// <summary>
    /// Verifies that negative or overflowing <c>sourceIndex</c>, <c>targetIndex</c>, or <c>count</c> values throw
    /// <see cref="ArgumentOutOfRangeException" /> for the byte-array → byte-array path.
    /// </summary>
    [TestMethod]
    [DataRow(-1, 0, 1, DisplayName = "Negative source index")]
    [DataRow(0, -1, 1, DisplayName = "Negative target index")]
    [DataRow(0, 0, -1, DisplayName = "Negative count")]
    [DataRow(0, 0, 17, DisplayName = "Count exceeds source length")]
    public void CopyTo_WhenArgumentsAreInvalid_ForByteArrayToByteArray_ShouldThrowExactly(
        int sourceIndex,
        int targetIndex,
        int count)
    {
        var source = AscendingBytes;
        var target = new byte[16];

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            source.CopyTo(sourceIndex, target, targetIndex, count);
        });
    }

    /// <summary>
    /// Verifies that a count exceeding the source span throws <see cref="ArgumentException" /> from
    /// <c>ThrowIfSpanLengthIsInsufficient</c>.
    /// </summary>
    [TestMethod]
    public void CopyTo_WhenCountExceedsSourceSpan_ForByteSpanToTypedSpan_ShouldThrowExactly()
    {
        var sourceData = AscendingBytes;
        var targetData = new int[8];

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            ReadOnlySpan<byte> source = sourceData;
            Span<int> target = targetData;
            source.CopyTo(target, 5);
        });
    }

    /// <summary>
    /// Verifies that a count exceeding the target span throws <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    public void CopyTo_WhenCountExceedsTargetSpan_ForByteSpanToTypedSpan_ShouldThrowExactly()
    {
        var sourceData = AscendingBytes;
        var targetData = new int[2];

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            ReadOnlySpan<byte> source = sourceData;
            Span<int> target = targetData;
            source.CopyTo(target, 3);
        });
    }

    /// <summary>
    /// Verifies that a zero-count copy between byte arrays performs no work and leaves the target unchanged.
    /// </summary>
    [TestMethod]
    public void CopyTo_WhenCountIsZero_ForByteArrayToByteArray_ShouldLeaveTargetUnchanged()
    {
        var source = AscendingBytes;
        byte[] target = [99, 98, 97, 96];
        var snapshot = (byte[])target.Clone();

        source.CopyTo(0, target, 0, 0);

        CollectionAssert.AreEqual(snapshot, target);
    }

    /// <summary>
    /// Verifies that copying zero elements from a byte span to a typed span is a no-op.
    /// </summary>
    [TestMethod]
    public void CopyTo_WhenCountIsZero_ForByteSpanToTypedSpan_ShouldLeaveTargetUnchanged()
    {
        ReadOnlySpan<byte> source = AscendingBytes;
        Span<int> target = stackalloc int[2];
        target[0] = 42;
        target[1] = 43;

        source.CopyTo(target, 0);

        Assert.AreEqual(42, target[0]);
        Assert.AreEqual(43, target[1]);
    }

    /// <summary>
    /// Verifies the round-trip invariant that a value written by <c>CopyTo</c> can be recovered by <c>Read</c>.
    /// </summary>
    [TestMethod]
    public void CopyTo_WhenFollowedByRead_ForUnmanagedValueToByteArray_ShouldRoundTripValue()
    {
        var buffer = new byte[sizeof(long)];
        var value = unchecked((long)0xDEADBEEFCAFEBABEUL);

        value.CopyTo(buffer, 0);

        Assert.AreEqual(value, buffer.Read<long>(0));
    }

    /// <summary>
    /// Verifies that writing a single value at a negative index throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void CopyTo_WhenIndexIsNegative_ForUnmanagedValueToByteArray_ShouldThrowExactly()
    {
        var target = new byte[8];

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            0x04030201.CopyTo(target, -1);
        });
    }

    /// <summary>
    /// Verifies that writing a single value at an index that leaves insufficient space throws <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    public void CopyTo_WhenIndexLeavesInsufficientSpace_ForUnmanagedValueToByteArray_ShouldThrowExactly()
    {
        var target = new byte[8];

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            0x04030201.CopyTo(target, 5);
        });
    }

    /// <summary>
    /// Verifies that copying between byte arrays with a valid range produces the expected slice
    /// at the correct target offset.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(CopyToByteArrayToByteArrayValidData))]
    public void CopyTo_WhenRangeIsValid_ForByteArrayToByteArray_ShouldProduceExpectedBytes(
        byte[] source,
        int sourceIndex,
        byte[] target,
        int targetIndex,
        int count,
        byte[] expected)
    {
        source.CopyTo(sourceIndex, target, targetIndex, count);

        CollectionAssert.AreEqual(expected, target);
    }

    // =========================================================================
    // CopyTo<T>(Memory<byte>, Memory<T>, int)
    // =========================================================================

    /// <summary>
    /// Verifies that copying from a byte memory block to a typed memory block produces the expected little-endian interpretation.
    /// </summary>
    [TestMethod]
    public void CopyTo_WhenRangeIsValid_ForByteMemoryToTypedMemory_ShouldProduceExpectedElements()
    {
        Memory<byte> source = AscendingBytes;
        Memory<int> target = new int[4];

        source.CopyTo(target, 4);

        CollectionAssert.AreEqual(
            new[] { 0x03020100, 0x07060504, 0x0B0A0908, 0x0F0E0D0C },
            target.ToArray());
    }

    // =========================================================================
    // CopyTo<T>(ReadOnlySpan<byte>, Span<T>, int)
    // =========================================================================

    /// <summary>
    /// Verifies that copying from a byte span to a typed span produces the expected little-endian interpretation.
    /// </summary>
    [TestMethod]
    public void CopyTo_WhenRangeIsValid_ForByteSpanToTypedSpan_ShouldProduceExpectedElements()
    {
        ReadOnlySpan<byte> source = AscendingBytes;
        Span<int> target = stackalloc int[4];

        source.CopyTo(target, 4);

        CollectionAssert.AreEqual(
            new[] { 0x03020100, 0x07060504, 0x0B0A0908, 0x0F0E0D0C },
            target.ToArray());
    }

    // =========================================================================
    // CopyTo<T>(T, byte[], int) — single-value writer
    // =========================================================================

    /// <summary>
    /// Verifies that writing a single unmanaged value into a byte array at the specified index produces the expected little-endian bytes.
    /// </summary>
    [TestMethod]
    public void CopyTo_WhenSingleValueIsWritten_ForUnmanagedValueToByteArray_ShouldWriteExpectedBytes()
    {
        var target = new byte[8];

        0x04030201.CopyTo(target, 2);

        CollectionAssert.AreEqual(
            new byte[] { 0x00, 0x00, 0x01, 0x02, 0x03, 0x04, 0x00, 0x00 },
            target);
    }

    /// <summary>
    /// Verifies that a valid <c>sourceIndex</c> combined with a <c>count</c> that extends past the source array
    /// throws <see cref="ArgumentException" />, matching the combination contract of <c>ThrowIfArrayOffsetOrCountInvalid</c>.
    /// </summary>
    [TestMethod]
    public void CopyTo_WhenSourceIndexPlusCountExceedsSourceLength_ForByteArrayToByteArray_ShouldThrowExactly()
    {
        var source = AscendingBytes;
        var target = new byte[16];

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            source.CopyTo(14, target, 0, 4);
        });
    }

    /// <summary>
    /// Verifies that a null source byte array throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void CopyTo_WhenSourceIsNull_ForByteArrayToTypedArray_ShouldThrowExactly()
    {
        byte[]? source = null;
        var target = new byte[8];

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            source!.CopyTo<byte>(0, target, 0, 1);
        });
    }

    /// <summary>
    /// Verifies that the implicit-target-index overload throws <see cref="ArgumentNullException" /> when the source is null.
    /// </summary>
    [TestMethod]
    public void CopyTo_WhenSourceIsNull_ForByteArrayToTypedArrayImplicitTargetIndex_ShouldThrowExactly()
    {
        byte[]? source = null;
        var target = new byte[4];

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            source!.CopyTo<byte>(0, target, 1);
        });
    }

    // =========================================================================
    // CopyTo<T>(byte[], int, T[], int) — overload without explicit targetIndex
    // =========================================================================

    /// <summary>
    /// Verifies that the overload without an explicit target index copies into the target starting at index zero.
    /// </summary>
    [TestMethod]
    public void CopyTo_WhenTargetIndexIsImplicit_ForByteArrayToByteArray_ShouldCopyFromIndexZero()
    {
        var source = AscendingBytes;
        var target = new byte[8];

        source.CopyTo(0, target, 8);

        CollectionAssert.AreEqual(
            new byte[] { 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07 },
            target);
    }

    /// <summary>
    /// Verifies that a null target array throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void CopyTo_WhenTargetIsNull_ForByteArrayToTypedArray_ShouldThrowExactly()
    {
        var source = AscendingBytes;
        byte[]? target = null;

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            source.CopyTo<byte>(0, target!, 0, 1);
        });
    }

    /// <summary>
    /// Verifies that writing a single value into a null byte array throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void CopyTo_WhenTargetIsNull_ForUnmanagedValueToByteArray_ShouldThrowExactly()
    {
        byte[]? target = null;

        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            0x04030201.CopyTo(target!, 0);
        });

        Assert.AreEqual("targetArray", ex.ParamName);
    }

}
