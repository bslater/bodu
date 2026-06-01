// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThrowHelperTests.ThrowIfCountExceedsAvailable.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
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
    /// Verifies that <see cref="ThrowHelper.ThrowIfCountExceedsAvailable" /> does not throw — and on the
    /// ParamName-asserting overload reports nothing — for valid (count, available) pairs including the
    /// zero-zero and count-equals-available boundaries.
    /// </summary>
    /// <param name="testName">The data-row label.</param>
    /// <param name="count">The count being validated.</param>
    /// <param name="available">The number of available items.</param>
    [TestMethod]
    [DataRow("count == 0, available == 0", 0, 0)]
    [DataRow("count == available", 5, 5)]
    [DataRow("count < available", 3, 5)]
    public void ThrowIfCountExceedsAvailable_WhenCountIsAccepted_ShouldNotThrowAndReportNothing(string testName, int count, int available) =>
        AssertGuard(testName, () => ThrowHelper.ThrowIfCountExceedsAvailable(count, available, nameof(count)), null, null);

    /// <summary>
    /// Verifies that <see cref="ThrowHelper.ThrowIfCountExceedsAvailable" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> with <c>ParamName == "count"</c> (never <c>"available"</c>)
    /// when the count exceeds the available budget or is negative.
    /// </summary>
    /// <param name="testName">The data-row label.</param>
    /// <param name="count">The count being validated.</param>
    /// <param name="available">The number of available items.</param>
    [TestMethod]
    [DataRow("count > available", 6, 5)]
    [DataRow("negative count", -1, 5)]
    public void ThrowIfCountExceedsAvailable_WhenCountIsRejected_ShouldThrowOnCount(string testName, int count, int available) =>
        AssertGuard(
            testName,
            () => ThrowHelper.ThrowIfCountExceedsAvailable(count, available, nameof(count)),
            typeof(ArgumentOutOfRangeException),
            "count");

}
