// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfGreaterThanOrEqualOther.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public partial class ThrowHelperTests
{

    /// <summary>
    /// Verifies the full <see cref="ThrowHelper.ThrowIfGreaterThanOrEqualOther{T}" /> contract matrix with
    /// explicit ParamName disambiguation: when the guard fails, ParamName must be the name of the
    /// <c>value</c> parameter, never the <c>other</c> parameter — the offending caller-supplied input is
    /// <c>value</c>, not the comparison reference.
    /// </summary>
    /// <param name="testName">The data-row label.</param>
    /// <param name="value">The value compared against <paramref name="other" />.</param>
    /// <param name="other">The comparison reference.</param>
    /// <param name="expectsException">Whether the guard must throw.</param>
    [TestMethod]
    [DataRow("equal → throw on value", 5, 5, true)]
    [DataRow("greater → throw on value", 6, 5, true)]
    [DataRow("less → pass", 4, 5, false)]
    [DataRow("MinValue vs MaxValue → pass", int.MinValue, int.MaxValue, false)]
    [DataRow("MaxValue vs MaxValue → throw on value", int.MaxValue, int.MaxValue, true)]
    public void ThrowIfGreaterThanOrEqualOther_WhenInvokedWithVariousPairs_ShouldFollowContract(
        string testName, int value, int other, bool expectsException)
    {
        Type? expected = expectsException ? typeof(ArgumentException) : null;
        var expectedParam = expectsException ? "value" : null;

        AssertGuard(
            testName,
            () => ThrowHelper.ThrowIfGreaterThanOrEqualOther(value, other, nameof(value), "other"),
            expected,
            expectedParam);
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfGreaterThanOrEqualOther" />, when ValueIsGreaterThanOrEqualToOther, throws <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    [DataRow(5, 5)]
    [DataRow(6, 5)]
    [DataRow(1, 0)]
    [DataRow(0, 0)]
    [DataRow(int.MaxValue, int.MaxValue)]
    public void ThrowIfGreaterThanOrEqualOther_WhenValueIsGreaterThanOrEqualToOther_ShouldThrowExactly(int value, int other)
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            ThrowHelper.ThrowIfGreaterThanOrEqualOther(value, other);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfGreaterThanOrEqualOther" />, when ValueIsLessThanOther, NotThrow.
    /// </summary>
    [TestMethod]
    [DataRow(-1, 0)]
    [DataRow(4, 5)]
    [DataRow(int.MinValue, int.MaxValue)]
    public void ThrowIfGreaterThanOrEqualOther_WhenValueIsLessThanOther_ShouldNotThrow(int value, int other) => ThrowHelper.ThrowIfGreaterThanOrEqualOther(value, other);

}
