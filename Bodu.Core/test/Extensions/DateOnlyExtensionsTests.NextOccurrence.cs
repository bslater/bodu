// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateOnlyExtensionsTests.NextOccurrence.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class DateOnlyExtensionsTests
{

    /// <summary>
    /// Verifies that <see cref="DateOnlyExtensions.NextOccurrence(DateOnly, int, DateOnly)" /> returns the start when <c>after</c> is
    /// earlier than <c>start</c>.
    /// </summary>
    [TestMethod]
    public void NextOccurrence_WhenAfterIsEarlierThanStart_ShouldReturnStart()
    {
        var start = new DateOnly(2024, 1, 10);
        var after = new DateOnly(2024, 1, 1);

        DateOnly next = start.NextOccurrence(intervalDays: 3, after);

        Assert.AreEqual(start, next);
    }
    /// <summary>
    /// Verifies that <see cref="DateOnlyExtensions.NextOccurrence(DateOnly, int, DateOnly)" /> returns the next aligned occurrence
    /// strictly after the supplied reference date.
    /// </summary>
    [TestMethod]
    public void NextOccurrence_WhenAfterIsLaterThanStart_ShouldReturnNextAlignedDate()
    {
        var start = new DateOnly(2024, 1, 1);
        var after = new DateOnly(2024, 1, 12);

        DateOnly next = start.NextOccurrence(intervalDays: 5, after);

        Assert.AreEqual(new DateOnly(2024, 1, 16), next);
    }

    /// <summary>
    /// Verifies that <see cref="DateOnlyExtensions.NextOccurrence(DateOnly, int, DateOnly)" /> rolls forward to the next interval when
    /// <c>after</c> exactly matches an occurrence boundary.
    /// </summary>
    [TestMethod]
    public void NextOccurrence_WhenAfterMatchesOccurrence_ShouldAdvanceToNextOccurrence()
    {
        var start = new DateOnly(2024, 1, 1);
        var after = new DateOnly(2024, 1, 8); // aligns with 7-day interval

        DateOnly next = start.NextOccurrence(intervalDays: 7, after);

        Assert.AreEqual(new DateOnly(2024, 1, 15), next);
    }

    /// <summary>
    /// Verifies that <see cref="DateOnlyExtensions.NextOccurrence(DateOnly, int, DateOnly)" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> when <c>intervalDays</c> is not positive.
    /// </summary>
    [TestMethod]
    [DataRow(0)]
    [DataRow(-1)]
    public void NextOccurrence_WhenIntervalIsNotPositive_ShouldThrowExactly(int intervalDays)
    {
        var start = new DateOnly(2024, 1, 1);
        var after = new DateOnly(2024, 1, 10);

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = start.NextOccurrence(intervalDays, after);
        });
    }

}
