// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfSpanOffsetOrCountInvalid.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public partial class ThrowHelperTests
{
    // Span<T> overload (delegates to ReadOnlySpan<T>)

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfSpanOffsetOrCountInvalid" />, Span, when OffsetOrCountOutOfRange, throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    [DataRow(-1, 2)]   // negative offset
    [DataRow(6, 2)]    // offset > length
    [DataRow(2, -1)]   // negative count
    [DataRow(2, 10)]   // count > length
    public void ThrowIfSpanOffsetOrCountInvalid_Span_WhenOffsetOrCountOutOfRange_ShouldThrowArgumentOutOfRangeException(int offset, int count)
    {
        var buffer = new int[5];
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            ThrowHelper.ThrowIfSpanOffsetOrCountInvalid(buffer.AsSpan(), offset, count);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfSpanOffsetOrCountInvalid" />, Span, when SumExceedsLength, throws <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    [DataRow(3, 3)]    // offset + count > length
    [DataRow(4, 2)]    // offset + count > length
    public void ThrowIfSpanOffsetOrCountInvalid_Span_WhenSumExceedsLength_ShouldThrowArgumentException(int offset, int count)
    {
        var buffer = new int[5];
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            ThrowHelper.ThrowIfSpanOffsetOrCountInvalid(buffer.AsSpan(), offset, count);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfSpanOffsetOrCountInvalid" />, Span, when ParametersAreValid, NotThrow.
    /// </summary>
    [TestMethod]
    [DataRow(0, 0)]
    [DataRow(0, 5)]
    [DataRow(1, 4)]
    [DataRow(2, 3)]
    [DataRow(5, 0)]   // offset at length with zero count is valid
    public void ThrowIfSpanOffsetOrCountInvalid_Span_WhenParametersAreValid_ShouldNotThrow(int offset, int count)
    {
        var buffer = new int[5];
        ThrowHelper.ThrowIfSpanOffsetOrCountInvalid(buffer.AsSpan(), offset, count);
    }

    // ReadOnlySpan<T> overload (canonical implementation)

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfSpanOffsetOrCountInvalid" />, ReadOnlySpan, when OffsetOrCountOutOfRange, throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    [DataRow(-1, 2)]
    [DataRow(6, 2)]
    [DataRow(2, -1)]
    [DataRow(2, 10)]
    public void ThrowIfSpanOffsetOrCountInvalid_ReadOnlySpan_WhenOffsetOrCountOutOfRange_ShouldThrowArgumentOutOfRangeException(int offset, int count)
    {
        var buffer = new int[5];
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            ThrowHelper.ThrowIfSpanOffsetOrCountInvalid((ReadOnlySpan<int>)buffer, offset, count);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfSpanOffsetOrCountInvalid" />, ReadOnlySpan, when SumExceedsLength, throws <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    [DataRow(3, 3)]
    [DataRow(4, 2)]
    public void ThrowIfSpanOffsetOrCountInvalid_ReadOnlySpan_WhenSumExceedsLength_ShouldThrowArgumentException(int offset, int count)
    {
        var buffer = new int[5];
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            ThrowHelper.ThrowIfSpanOffsetOrCountInvalid((ReadOnlySpan<int>)buffer, offset, count);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfSpanOffsetOrCountInvalid" />, ReadOnlySpan, when ParametersAreValid, NotThrow.
    /// </summary>
    [TestMethod]
    [DataRow(0, 0)]
    [DataRow(0, 5)]
    [DataRow(1, 4)]
    [DataRow(2, 3)]
    [DataRow(5, 0)]
    public void ThrowIfSpanOffsetOrCountInvalid_ReadOnlySpan_WhenParametersAreValid_ShouldNotThrow(int offset, int count)
    {
        var buffer = new int[5];
        ThrowHelper.ThrowIfSpanOffsetOrCountInvalid((ReadOnlySpan<int>)buffer, offset, count);
    }
}
