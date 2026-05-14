// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfLessThanOrEqual.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu;

public partial class ThrowHelperTests
{
    /// <summary>
    /// Verifies the <see cref="ThrowHelper.ThrowIfLessThanOrEqual{T}" /> contract with ParamName
    /// assertions: <c>value &lt;= min</c> throws <see cref="ArgumentOutOfRangeException" /> with ParamName
    /// "value"; <c>value &gt; min</c> passes.
    /// </summary>
    /// <param name="testName">The data-row label.</param>
    /// <param name="value">The value compared against <paramref name="min" />.</param>
    /// <param name="min">The exclusive lower bound.</param>
    /// <param name="expectsException">Whether the guard must throw.</param>
    [TestMethod]
    [DataRow("equal → throw on value", 5, 5, true)]
    [DataRow("less → throw on value", 4, 5, true)]
    [DataRow("greater → pass", 6, 5, false)]
    [DataRow("MinValue vs MinValue → throw on value", int.MinValue, int.MinValue, true)]
    [DataRow("MaxValue vs MinValue → pass", int.MaxValue, int.MinValue, false)]
    public void ThrowIfLessThanOrEqual_WhenInvokedWithVariousPairs_ShouldFollowContract(
        string testName, int value, int min, bool expectsException)
    {
        Type? expected = expectsException ? typeof(ArgumentOutOfRangeException) : null;
        var expectedParam = expectsException ? "value" : null;

        AssertGuard(
            testName,
            () => ThrowHelper.ThrowIfLessThanOrEqual(value, min, nameof(value)),
            expected,
            expectedParam);
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfLessThanOrEqual" />, when ValueIsLessThanOrEqualToMin, throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    [DataRow(0, 1)]
    [DataRow(5, 6)]
    [DataRow(int.MinValue, int.MaxValue)]
    [DataRow(3, 3)]
    public void ThrowIfLessThanOrEqual_WhenValueIsLessThanOrEqualToMin_ShouldThrowArgumentOutOfRangeException(int value, int min)
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            ThrowHelper.ThrowIfLessThanOrEqual(value, min);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfLessThanOrEqual" />, when ValueIsGreaterThanMin, NotThrow.
    /// </summary>
    [TestMethod]
    [DataRow(1, 0)]
    [DataRow(6, 5)]
    [DataRow(int.MaxValue, int.MinValue)]
    public void ThrowIfLessThanOrEqual_WhenValueIsGreaterThanMin_ShouldNotThrow(int value, int min) => ThrowHelper.ThrowIfLessThanOrEqual(value, min);
}
