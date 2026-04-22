// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfSpanLengthOutOfRange.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public partial class ThrowHelperTests
{
    // Span<T> overload

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfSpanLengthOutOfRange" />, Span, when LengthIsOutOfRange, throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    [DataRow(0, 1, 10)]
    [DataRow(11, 1, 10)]
    [DataRow(5, 10, 20)]
    [DataRow(25, 10, 20)]
    public void ThrowIfSpanLengthOutOfRange_Span_WhenLengthIsOutOfRange_ShouldThrowArgumentOutOfRangeException(int spanLength, int minLength, int maxLength)
    {
        int[] buffer = new int[spanLength];
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            ThrowHelper.ThrowIfSpanLengthOutOfRange(buffer.AsSpan(), minLength, maxLength);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfSpanLengthOutOfRange" />, Span, when LengthIsWithinRange, NotThrow.
    /// </summary>
    [TestMethod]
    [DataRow(1, 1, 10)]
    [DataRow(10, 1, 10)]
    [DataRow(5, 1, 10)]
    [DataRow(0, 0, 0)]
    [DataRow(7, 7, 7)]
    public void ThrowIfSpanLengthOutOfRange_Span_WhenLengthIsWithinRange_ShouldNotThrow(int spanLength, int minLength, int maxLength)
    {
        int[] buffer = new int[spanLength];
        ThrowHelper.ThrowIfSpanLengthOutOfRange(buffer.AsSpan(), minLength, maxLength);
    }

    // ReadOnlySpan<T> overload

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfSpanLengthOutOfRange" />, ReadOnlySpan, when LengthIsOutOfRange, throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    [DataRow(0, 1, 10)]
    [DataRow(11, 1, 10)]
    [DataRow(5, 10, 20)]
    [DataRow(25, 10, 20)]
    public void ThrowIfSpanLengthOutOfRange_ReadOnlySpan_WhenLengthIsOutOfRange_ShouldThrowArgumentOutOfRangeException(int spanLength, int minLength, int maxLength)
    {
        int[] buffer = new int[spanLength];
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            ThrowHelper.ThrowIfSpanLengthOutOfRange((ReadOnlySpan<int>)buffer, minLength, maxLength);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfSpanLengthOutOfRange" />, ReadOnlySpan, when LengthIsWithinRange, NotThrow.
    /// </summary>
    [TestMethod]
    [DataRow(1, 1, 10)]
    [DataRow(10, 1, 10)]
    [DataRow(5, 1, 10)]
    [DataRow(0, 0, 0)]
    [DataRow(7, 7, 7)]
    public void ThrowIfSpanLengthOutOfRange_ReadOnlySpan_WhenLengthIsWithinRange_ShouldNotThrow(int spanLength, int minLength, int maxLength)
    {
        int[] buffer = new int[spanLength];
        ThrowHelper.ThrowIfSpanLengthOutOfRange((ReadOnlySpan<int>)buffer, minLength, maxLength);
    }
}
