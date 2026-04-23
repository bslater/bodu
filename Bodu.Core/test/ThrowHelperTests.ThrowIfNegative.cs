// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfNegative.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public partial class ThrowHelperTests
{
    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfNegative(int)" /> throws an <see cref="ArgumentOutOfRangeException" /> when the
    /// value is negative.
    /// </summary>
    [TestMethod]
    [DataRow(-1)]
    [DataRow(int.MinValue)]
    public void ThrowIfNegative_WhenValueIsNegative_ShouldThrowArgumentOutOfRangeException(int value)
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            ThrowHelper.ThrowIfNegative(value);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfNegative(int)" /> does not throw when the value is zero or positive.
    /// </summary>
    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(int.MaxValue)]
    public void ThrowIfNegative_WhenValueIsZeroOrPositive_ShouldNotThrow(int value)
    {
        ThrowHelper.ThrowIfNegative(value);
    }
}
