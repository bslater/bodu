// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfGreaterThan.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public partial class ThrowHelperTests
{

    /// <summary>
    /// Verifies the full <see cref="ThrowHelper.ThrowIfGreaterThan{T}" /> contract matrix with explicit
    /// ParamName assertions: <c>value &gt; max</c> throws <see cref="ArgumentOutOfRangeException" /> with
    /// ParamName "value"; <c>value &lt;= max</c> passes.
    /// </summary>
    /// <param name="testName">The data-row label.</param>
    /// <param name="value">The value compared against <paramref name="max" />.</param>
    /// <param name="max">The inclusive maximum.</param>
    /// <param name="expectsException">Whether the guard must throw.</param>
    [TestMethod]
    [DataRow("equal → pass", 5, 5, false)]
    [DataRow("less than → pass", 4, 5, false)]
    [DataRow("greater than → throw", 6, 5, true)]
    [DataRow("MaxValue vs MaxValue-1 → throw", int.MaxValue, int.MaxValue - 1, true)]
    [DataRow("MinValue vs MinValue → pass", int.MinValue, int.MinValue, false)]
    public void ThrowIfGreaterThan_WhenInvokedWithVariousPairs_ShouldFollowContract(
        string testName, int value, int max, bool expectsException)
    {
        Type? expected = expectsException ? typeof(ArgumentOutOfRangeException) : null;
        var expectedParam = expectsException ? "value" : null;

        AssertGuard(testName, () => ThrowHelper.ThrowIfGreaterThan(value, max, nameof(value)), expected, expectedParam);
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfGreaterThan" />, when ValueIsGreater, throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    [DataRow(6, 5)]
    [DataRow(1, 0)]
    [DataRow(int.MaxValue, int.MaxValue - 1)]
    public void ThrowIfGreaterThan_WhenValueIsGreater_ShouldThrowArgumentOutOfRangeException(int value, int max)
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            ThrowHelper.ThrowIfGreaterThan(value, max);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfGreaterThan" />, when ValueIsLessThanOrEqualToMax, NotThrow.
    /// </summary>
    [TestMethod]
    [DataRow(3, 3)]
    [DataRow(2, 3)]
    [DataRow(0, 0)]
    [DataRow(-1, 0)]
    [DataRow(int.MinValue, int.MinValue)]
    public void ThrowIfGreaterThan_WhenValueIsLessThanOrEqualToMax_ShouldNotThrow(int value, int max) => ThrowHelper.ThrowIfGreaterThan(value, max);

}
