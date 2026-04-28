// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfArrayOffsetOrCountInvalid.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public sealed partial class ThrowHelperTests
{
    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfArrayOffsetOrCountInvalid" />, when OffsetOrCountOutOfRange, throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    [DataRow(-1, 2)]  // Negative offset
    [DataRow(6, 2)]   // Offset > array length
    [DataRow(2, -1)]  // Negative count
    [DataRow(2, 10)]  // Count > array length
    public void ThrowIfArrayOffsetOrCountInvalid_WhenOffsetOrCountOutOfRange_ShouldThrowArgumentOutOfRangeException(int offset, int count)
    {
        var array = new int[5];
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            ThrowHelper.ThrowIfArrayOffsetOrCountInvalid(array, offset, count);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfArrayOffsetOrCountInvalid" />, when SumExceedsLength, throws <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    [DataRow(3, 3)]  // Offset + count exceeds array length
    [DataRow(4, 2)]  // Offset + count exceeds array length
    public void ThrowIfArrayOffsetOrCountInvalid_WhenSumExceedsLength_ShouldThrowArgumentException(int offset, int count)
    {
        var array = new int[5];
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            ThrowHelper.ThrowIfArrayOffsetOrCountInvalid(array, offset, count);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfArrayOffsetOrCountInvalid" />, when ParametersAreValid, NotThrow.
    /// </summary>
    [TestMethod]
    [DataRow(0, 5)]
    [DataRow(1, 4)]
    [DataRow(2, 3)]
    [DataRow(3, 2)]
    [DataRow(4, 1)]
    public void ThrowIfArrayOffsetOrCountInvalid_WhenParametersAreValid_ShouldNotThrow(int offset, int count)
    {
        var array = new int[5];
        ThrowHelper.ThrowIfArrayOffsetOrCountInvalid(array, offset, count);
    }
}
