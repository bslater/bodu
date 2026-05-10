// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfIndexOutOfRange.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public partial class ThrowHelperTests
{
    private static readonly int[] TestArray = new[] { 1, 2, 3, 4, 5 };

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfIndexOutOfRange(long, Array, string?)" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> when the index is out of bounds.
    /// </summary>
    [TestMethod]
    [DataRow(5)]          // index == Length
    [DataRow(6)]          // index > Length
    [DataRow(int.MaxValue)]
    [DataRow(-1)]         // negative index
    public void ThrowIfIndexOutOfRange_Array_WhenIndexIsInvalid_ShouldThrowArgumentOutOfRangeException(int index)
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            ThrowHelper.ThrowIfIndexOutOfRange(index, TestArray);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfIndexOutOfRange(long, Array, string?)" /> throws
    /// <see cref="ArgumentNullException" /> when the array is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void ThrowIfIndexOutOfRange_Array_WhenArrayIsNull_ShouldThrowArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            ThrowHelper.ThrowIfIndexOutOfRange(0L, (Array)null!);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfIndexOutOfRange(long, Array, string?)" /> does not throw
    /// when the index is within bounds.
    /// </summary>
    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(4)]
    public void ThrowIfIndexOutOfRange_Array_WhenIndexIsWithinBounds_ShouldNotThrow(int index)
    {
        ThrowHelper.ThrowIfIndexOutOfRange(index, TestArray);
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfIndexOutOfRange{T}(int, ReadOnlySpan{T}, string?)" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> when the index is out of bounds.
    /// </summary>
    [TestMethod]
    [DataRow(5)]          // index == Length
    [DataRow(6)]          // index > Length
    [DataRow(int.MaxValue)]
    [DataRow(-1)]         // negative index
    public void ThrowIfIndexOutOfRange_ReadOnlySpan_WhenIndexIsInvalid_ShouldThrowArgumentOutOfRangeException(int index)
    {
        ReadOnlySpan<int> span = TestArray;

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            ThrowHelper.ThrowIfIndexOutOfRange(index, span);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfIndexOutOfRange{T}(int, ReadOnlySpan{T}, string?)" /> does
    /// not throw when the index is within bounds.
    /// </summary>
    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(4)]
    public void ThrowIfIndexOutOfRange_ReadOnlySpan_WhenIndexIsWithinBounds_ShouldNotThrow(int index)
    {
        ReadOnlySpan<int> span = TestArray;

        ThrowHelper.ThrowIfIndexOutOfRange(index, span);
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfIndexOutOfRange{T}(int, Span{T}, string?)" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> when the index is out of bounds.
    /// </summary>
    [TestMethod]
    [DataRow(5)]          // index == Length
    [DataRow(6)]          // index > Length
    [DataRow(int.MaxValue)]
    [DataRow(-1)]         // negative index
    public void ThrowIfIndexOutOfRange_Span_WhenIndexIsInvalid_ShouldThrowArgumentOutOfRangeException(int index)
    {
        Span<int> span = new int[] { 1, 2, 3, 4, 5 };

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            ThrowHelper.ThrowIfIndexOutOfRange(index, span);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfIndexOutOfRange{T}(int, Span{T}, string?)" /> does not
    /// throw when the index is within bounds.
    /// </summary>
    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(4)]
    public void ThrowIfIndexOutOfRange_Span_WhenIndexIsWithinBounds_ShouldNotThrow(int index)
    {
        Span<int> span = new int[] { 1, 2, 3, 4, 5 };

        ThrowHelper.ThrowIfIndexOutOfRange(index, span);
    }
}
