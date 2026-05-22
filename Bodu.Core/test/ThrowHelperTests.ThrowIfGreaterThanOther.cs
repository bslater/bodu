// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfGreaterThanOther.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public partial class ThrowHelperTests
{

    /// <summary>
    /// Verifies the full <see cref="ThrowHelper.ThrowIfGreaterThanOther{T}" /> contract matrix with explicit
    /// ParamName disambiguation: when the guard fails, ParamName must be the name of the <c>value</c>
    /// parameter, never the <c>other</c> parameter.
    /// </summary>
    /// <param name="testName">The data-row label.</param>
    /// <param name="value">The value compared against <paramref name="other" />.</param>
    /// <param name="other">The comparison reference.</param>
    /// <param name="expectsException">Whether the guard must throw.</param>
    [TestMethod]
    [DataRow("equal → pass", 5, 5, false)]
    [DataRow("less → pass", 4, 5, false)]
    [DataRow("greater → throw on value", 6, 5, true)]
    [DataRow("MaxValue vs MaxValue-1 → throw on value", int.MaxValue, int.MaxValue - 1, true)]
    [DataRow("MinValue vs MaxValue → pass", int.MinValue, int.MaxValue, false)]
    public void ThrowIfGreaterThanOther_WhenInvokedWithVariousPairs_ShouldFollowContract(
        string testName, int value, int other, bool expectsException)
    {
        Type? expected = expectsException ? typeof(ArgumentException) : null;
        var expectedParam = expectsException ? "value" : null;

        AssertGuard(
            testName,
            () => ThrowHelper.ThrowIfGreaterThanOther(value, other, nameof(value), "other"),
            expected,
            expectedParam);
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfGreaterThanOther" />, when ValueIsGreaterThanOther, throws <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    [DataRow(6, 5)]
    [DataRow(1, 0)]
    [DataRow(int.MaxValue, int.MaxValue - 1)]
    public void ThrowIfGreaterThanOther_WhenValueIsGreaterThanOther_ShouldThrowExactly(int value, int other)
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            ThrowHelper.ThrowIfGreaterThanOther(value, other);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfGreaterThanOther" />, when ValueIsLessThanOrEqualToOther, NotThrow.
    /// </summary>
    [TestMethod]
    [DataRow(3, 3)]
    [DataRow(2, 3)]
    [DataRow(0, 0)]
    [DataRow(-1, 0)]
    [DataRow(int.MinValue, int.MaxValue)]
    public void ThrowIfGreaterThanOther_WhenValueIsLessThanOrEqualToOther_ShouldNotThrow(int value, int other) => ThrowHelper.ThrowIfGreaterThanOther(value, other);

}
