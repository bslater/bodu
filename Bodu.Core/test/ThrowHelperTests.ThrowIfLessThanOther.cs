// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfLessThanOther.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public partial class ThrowHelperTests
{
    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfLessThanOther" />, when ValueIsLessThanOther, throws <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    [DataRow(-1, 0)]
    [DataRow(0, 1)]
    [DataRow(5, 6)]
    [DataRow(int.MinValue, int.MaxValue)]
    public void ThrowIfLessThanOther_WhenValueIsLessThanOther_ShouldThrowArgumentException(int value, int other)
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            ThrowHelper.ThrowIfLessThanOther(value, other);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfLessThanOther" />, when ValueIsEqualOrGreaterThanOther, NotThrow.
    /// </summary>
    [TestMethod]
    [DataRow(0, 0)]
    [DataRow(6, 5)]
    [DataRow(int.MaxValue, int.MinValue)]
    public void ThrowIfLessThanOther_WhenValueIsEqualOrGreaterThanOther_ShouldNotThrow(int value, int other) => ThrowHelper.ThrowIfLessThanOther(value, other);
}
