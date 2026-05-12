// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfPositive.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public partial class ThrowHelperTests
{
    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfPositive" />, when ValueIsPositive, throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    [DataRow(1)]
    [DataRow(42)]
    [DataRow(int.MaxValue)]
    public void ThrowIfPositive_WhenValueIsPositive_ShouldThrow(int value)
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            ThrowHelper.ThrowIfPositive(value);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfPositive" />, when ValueIsZeroOrNegative, NotThrow.
    /// </summary>
    [TestMethod]
    [DataRow(0)]
    [DataRow(-1)]
    [DataRow(int.MinValue)]
    public void ThrowIfPositive_WhenValueIsZeroOrNegative_ShouldNotThrow(int value) => ThrowHelper.ThrowIfPositive(value);
}
