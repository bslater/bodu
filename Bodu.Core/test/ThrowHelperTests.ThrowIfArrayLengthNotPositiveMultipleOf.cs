// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfArrayLengthNotPositiveMultipleOf.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public partial class ThrowHelperTests
{
    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfArrayLengthNotPositiveMultipleOf" />, when LengthIsNotPositiveMultiple, throws <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    [DataRow(5, 2)]   // 5 % 2 != 0
    [DataRow(7, 3)]   // 7 % 3 != 0
    [DataRow(0, 1)]   // 0 is not a positive multiple
    public void ThrowIfArrayLengthNotPositiveMultipleOf_WhenLengthIsNotPositiveMultiple_ShouldThrowExactly(int arrayLength, int factor)
    {
        var array = new int[arrayLength];
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            ThrowHelper.ThrowIfArrayLengthNotPositiveMultipleOf(array, factor);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfArrayLengthNotPositiveMultipleOf" />, when LengthIsPositiveMultiple, NotThrow.
    /// </summary>
    [TestMethod]
    [DataRow(6, 3)]   // 6 % 3 == 0
    [DataRow(4, 2)]   // 4 % 2 == 0
    [DataRow(8, 4)]   // 8 % 4 == 0
    public void ThrowIfArrayLengthNotPositiveMultipleOf_WhenLengthIsPositiveMultiple_ShouldNotThrow(int arrayLength, int factor)
    {
        var array = new int[arrayLength];
        ThrowHelper.ThrowIfArrayLengthNotPositiveMultipleOf(array, factor);
    }
}
