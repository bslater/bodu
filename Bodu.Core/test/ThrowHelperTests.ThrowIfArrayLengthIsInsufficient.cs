// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfArrayLengthIsInsufficient.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public partial class ThrowHelperTests
{
    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfArrayOffsetOrCountInvalid" />, when ArrayIsNull, throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void ThrowIfArrayOffsetOrCountInvalid_WhenArrayIsNull_ShouldThrowException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            ThrowHelper.ThrowIfArrayOffsetOrCountInvalid(null!, 0, 0);
        });
    }

    // --- Offset edge cases ---

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfArrayOffsetOrCountInvalid" />, when OffsetInvalid, throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    [DataRow(5, -1, 0)]   // negative offset
    [DataRow(5, 6, 0)]    // offset > array.Length
    public void ThrowIfArrayOffsetOrCountInvalid_WhenOffsetInvalid_ShouldThrowArgumentOutOfRangeException(int arrayLength, int offset, int count)
    {
        var array = new int[arrayLength];
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            ThrowHelper.ThrowIfArrayOffsetOrCountInvalid(array, offset, count);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfArrayOffsetOrCountInvalid" />, when OffsetValid, NotThrow.
    /// </summary>
    [TestMethod]
    [DataRow(5, 0, 0)]    // offset at lower bound
    [DataRow(5, 5, 0)]    // offset == array.Length (valid: empty slice from end)
    [DataRow(5, 3, 0)]    // offset somewhere in the middle
    public void ThrowIfArrayOffsetOrCountInvalid_WhenOffsetValid_ShouldNotThrow(int arrayLength, int offset, int count)
    {
        var array = new int[arrayLength];
        ThrowHelper.ThrowIfArrayOffsetOrCountInvalid(array, offset, count);
    }

    // --- Count edge cases ---

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfArrayOffsetOrCountInvalid" />, when CountInvalid, throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    [DataRow(5, 0, -1)]   // negative count
    [DataRow(5, 0, 6)]    // count > array.Length
    public void ThrowIfArrayOffsetOrCountInvalid_WhenCountInvalid_ShouldThrowArgumentOutOfRangeException(int arrayLength, int offset, int count)
    {
        var array = new int[arrayLength];
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            ThrowHelper.ThrowIfArrayOffsetOrCountInvalid(array, offset, count);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfArrayOffsetOrCountInvalid" />, when CountValid, NotThrow.
    /// </summary>
    [TestMethod]
    [DataRow(5, 0, 0)]    // count at lower bound
    [DataRow(5, 0, 5)]    // count == array.Length
    [DataRow(5, 0, 3)]    // count somewhere in the middle
    public void ThrowIfArrayOffsetOrCountInvalid_WhenCountValid_ShouldNotThrow(int arrayLength, int offset, int count)
    {
        var array = new int[arrayLength];
        ThrowHelper.ThrowIfArrayOffsetOrCountInvalid(array, offset, count);
    }

    // --- Offset + count combination edge cases ---

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfArrayOffsetOrCountInvalid" />, when ArrayTooShort, throws <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    [DataRow(5, 2, 5)]    // 2 + 5 = 7 > 5 => insufficient
    [DataRow(4, 4, 1)]    // 4 + 1 = 5 > 4 => insufficient
    [DataRow(3, 1, 3)]    // 1 + 3 = 4 > 3 => insufficient
    public void ThrowIfArrayOffsetOrCountInvalid_WhenArrayTooShort_ShouldThrowArgumentException(int arrayLength, int offset, int count)
    {
        var array = new int[arrayLength];
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            ThrowHelper.ThrowIfArrayOffsetOrCountInvalid(array, offset, count);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfArrayOffsetOrCountInvalid" />, when ArrayIsSufficient, NotThrow.
    /// </summary>
    [TestMethod]
    [DataRow(10, 2, 5)]   // 2 + 5 = 7 <= 10 => sufficient
    [DataRow(5, 0, 5)]    // 0 + 5 = 5 <= 5 => sufficient
    [DataRow(8, 3, 5)]    // 3 + 5 = 8 <= 8 => sufficient
    public void ThrowIfArrayOffsetOrCountInvalid_WhenArrayIsSufficient_ShouldNotThrow(int arrayLength, int offset, int count)
    {
        var array = new int[arrayLength];
        ThrowHelper.ThrowIfArrayOffsetOrCountInvalid(array, offset, count);
    }
}
