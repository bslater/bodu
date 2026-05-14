// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfGreaterThanOrEqual.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu;

public partial class ThrowHelperTests
{
    /// <summary>
    /// Verifies the <see cref="ThrowHelper.ThrowIfGreaterThanOrEqual{T}" /> contract with ParamName
    /// assertions: <c>value &gt;= max</c> throws <see cref="ArgumentOutOfRangeException" /> with ParamName
    /// "value"; <c>value &lt; max</c> passes.
    /// </summary>
    /// <param name="testName">The data-row label.</param>
    /// <param name="value">The value compared against <paramref name="max" />.</param>
    /// <param name="max">The exclusive upper bound.</param>
    /// <param name="expectsException">Whether the guard must throw.</param>
    [TestMethod]
    [DataRow("equal → throw on value", 5, 5, true)]
    [DataRow("greater → throw on value", 6, 5, true)]
    [DataRow("less → pass", 4, 5, false)]
    [DataRow("MaxValue vs MaxValue → throw on value", int.MaxValue, int.MaxValue, true)]
    [DataRow("MinValue vs MaxValue → pass", int.MinValue, int.MaxValue, false)]
    public void ThrowIfGreaterThanOrEqual_WhenInvokedWithVariousPairs_ShouldFollowContract(
        string testName, int value, int max, bool expectsException)
    {
        Type? expected = expectsException ? typeof(ArgumentOutOfRangeException) : null;
        var expectedParam = expectsException ? "value" : null;

        AssertGuard(
            testName,
            () => ThrowHelper.ThrowIfGreaterThanOrEqual(value, max, nameof(value)),
            expected,
            expectedParam);
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfGreaterThanOrEqual" />, when ValueIsGreaterThanOrEqualToMax, throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    [DataRow(5, 5)]
    [DataRow(6, 5)]
    [DataRow(1, 0)]
    [DataRow(0, 0)]
    [DataRow(int.MaxValue, int.MaxValue)]
    public void ThrowIfGreaterThanOrEqual_WhenValueIsGreaterThanOrEqualToMax_ShouldThrowArgumentOutOfRangeException(int value, int max)
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            ThrowHelper.ThrowIfGreaterThanOrEqual(value, max);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfGreaterThanOrEqual" />, when ValueIsLessThanMax, NotThrow.
    /// </summary>
    [TestMethod]
    [DataRow(-1, 0)]
    [DataRow(4, 5)]
    [DataRow(int.MinValue, int.MaxValue)]
    public void ThrowIfGreaterThanOrEqual_WhenValueIsLessThanMax_ShouldNotThrow(int value, int max) => ThrowHelper.ThrowIfGreaterThanOrEqual(value, max);
}
