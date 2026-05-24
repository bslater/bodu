// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfCountExceedsAvailable.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public partial class ThrowHelperTests
{

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfCountExceedsAvailable" />, when CountIsInvalid, throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    [DataRow(6, 5)]   // Count exceeds available
    [DataRow(-1, 5)]  // Count is negative
    [DataRow(-10, 0)] // Count is negative regardless of available
    public void ThrowIfCountExceedsAvailable_WhenCountIsInvalid_ShouldThrowExactly(int count, int available)
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            ThrowHelper.ThrowIfCountExceedsAvailable(count, available);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfCountExceedsAvailable" />, when CountIsValid, NotThrow.
    /// </summary>
    [TestMethod]
    [DataRow(0, 0)]
    [DataRow(0, 5)]
    [DataRow(3, 5)]
    [DataRow(5, 5)]
    public void ThrowIfCountExceedsAvailable_WhenCountIsValid_ShouldNotThrow(int count, int available) => ThrowHelper.ThrowIfCountExceedsAvailable(count, available);
    /// <summary>
    /// Verifies the contract for <see cref="ThrowHelper.ThrowIfCountExceedsAvailable" />: when the guard
    /// fails, ParamName must reference the <c>count</c> parameter — the offending caller-supplied input —
    /// never the <c>available</c> parameter, which is a derived quantity.
    /// </summary>
    /// <param name="testName">The data-row label.</param>
    /// <param name="count">The count being validated.</param>
    /// <param name="available">The number of available items.</param>
    /// <param name="expectsException">Whether the guard must throw.</param>
    [TestMethod]
    [DataRow("count > available → throw on count", 6, 5, true)]
    [DataRow("negative count → throw on count", -1, 5, true)]
    [DataRow("count == 0, available == 0 → pass", 0, 0, false)]
    [DataRow("count == available → pass", 5, 5, false)]
    [DataRow("count < available → pass", 3, 5, false)]
    public void ThrowIfCountExceedsAvailable_WhenInvokedWithVariousInputs_ShouldFollowContract(
        string testName, int count, int available, bool expectsException)
    {
        Type? expected = expectsException ? typeof(ArgumentOutOfRangeException) : null;
        var expectedParam = expectsException ? "count" : null;

        AssertGuard(
            testName,
            () => ThrowHelper.ThrowIfCountExceedsAvailable(count, available, nameof(count)),
            expected,
            expectedParam);
    }

}
