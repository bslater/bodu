// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfLessThanOrEqualOther.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu;

public partial class ThrowHelperTests
{

    /// <summary>
    /// Verifies the full <see cref="ThrowHelper.ThrowIfLessThanOrEqualOther{T}" /> contract matrix with
    /// explicit ParamName disambiguation: when the guard fails, ParamName must be the name of the
    /// <c>value</c> parameter, never the <c>other</c> parameter.
    /// </summary>
    /// <param name="testName">The data-row label.</param>
    /// <param name="value">The value compared against <paramref name="other" />.</param>
    /// <param name="other">The comparison reference.</param>
    /// <param name="expectsException">Whether the guard must throw.</param>
    [TestMethod]
    [DataRow("less → throw on value", 0, 1, true)]
    [DataRow("equal → throw on value", 5, 5, true)]
    [DataRow("greater → pass", 6, 5, false)]
    [DataRow("MinValue vs MaxValue → throw on value", int.MinValue, int.MaxValue, true)]
    [DataRow("MaxValue vs MinValue → pass", int.MaxValue, int.MinValue, false)]
    public void ThrowIfLessThanOrEqualOther_WhenInvokedWithVariousPairs_ShouldFollowContract(
        string testName, int value, int other, bool expectsException)
    {
        Type? expected = expectsException ? typeof(ArgumentException) : null;
        var expectedParam = expectsException ? "value" : null;

        AssertGuard(
            testName,
            () => ThrowHelper.ThrowIfLessThanOrEqualOther(value, other, nameof(value), "other"),
            expected,
            expectedParam);
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfLessThanOrEqualOther" />, when ValueIsGreaterThanOther, NotThrow.
    /// </summary>
    [TestMethod]
    [DataRow(1, 0)]
    [DataRow(6, 5)]
    [DataRow(int.MaxValue, int.MinValue)]
    public void ThrowIfLessThanOrEqualOther_WhenValueIsGreaterThanOther_ShouldNotThrow(int value, int other) => ThrowHelper.ThrowIfLessThanOrEqualOther(value, other);

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfLessThanOrEqualOther" />, when ValueIsLessThanOrEqualToOther, throws <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    [DataRow(0, 1)]
    [DataRow(5, 6)]
    [DataRow(3, 3)]
    [DataRow(int.MinValue, int.MaxValue)]
    public void ThrowIfLessThanOrEqualOther_WhenValueIsLessThanOrEqualToOther_ShouldThrowArgumentException(int value, int other)
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            ThrowHelper.ThrowIfLessThanOrEqualOther(value, other);
        });
    }

}
