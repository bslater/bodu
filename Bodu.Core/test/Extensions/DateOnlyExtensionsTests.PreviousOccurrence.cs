// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateOnlyExtensionsTests.PreviousOccurrence.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class DateOnlyExtensionsTests
{

    /// <summary>
    /// Verifies that when <c>before</c> equals the start date, the occurrence one interval before the start is
    /// returned.
    /// </summary>
    [TestMethod]
    public void PreviousOccurrence_WhenBeforeEqualsStart_ShouldReturnStartMinusInterval()
    {
        var start = new DateOnly(2025, 7, 7);

        DateOnly actual = start.PreviousOccurrence(7, start);

        Assert.AreEqual(new DateOnly(2025, 6, 30), actual);
    }

    /// <summary>
    /// Verifies that when <c>before</c> sits between occurrences, the greatest occurrence strictly earlier than
    /// <c>before</c> is returned.
    /// </summary>
    [TestMethod]
    public void PreviousOccurrence_WhenBeforeIsBetweenOccurrences_ShouldReturnMostRecentOccurrence()
    {
        var start = new DateOnly(2024, 1, 1);
        var before = new DateOnly(2024, 1, 13); // occurrences: Jan 1, 6, 11, 16 (interval 5)

        DateOnly actual = start.PreviousOccurrence(5, before);

        Assert.AreEqual(new DateOnly(2024, 1, 11), actual);
    }

    /// <summary>
    /// Verifies that when <c>before</c> precedes the start date, the occurrence one interval before the start is
    /// returned.
    /// </summary>
    [TestMethod]
    public void PreviousOccurrence_WhenBeforeIsEarlierThanStart_ShouldReturnStartMinusInterval()
    {
        var start = new DateOnly(2025, 7, 7);
        var before = new DateOnly(2025, 7, 1);

        DateOnly actual = start.PreviousOccurrence(7, before);

        Assert.AreEqual(new DateOnly(2025, 6, 30), actual);
    }

    /// <summary>
    /// Verifies that when <c>before</c> is many intervals after the start, the method returns the occurrence
    /// immediately before it without drift.
    /// </summary>
    [TestMethod]
    public void PreviousOccurrence_WhenBeforeIsManyIntervalsAhead_ShouldReturnCorrectOccurrence()
    {
        var start = new DateOnly(2025, 1, 1);
        DateOnly before = start.AddDays(100); // 100 / 7 = 14 remainder 2 → previous = start + 14*7 = 98 days

        DateOnly actual = start.PreviousOccurrence(7, before);

        Assert.AreEqual(start.AddDays(98), actual);
    }

    /// <summary>
    /// Verifies that when <c>before</c> falls exactly on an occurrence boundary, the previous occurrence (one interval
    /// earlier) is returned instead.
    /// </summary>
    [TestMethod]
    public void PreviousOccurrence_WhenBeforeIsOnOccurrenceBoundary_ShouldReturnPriorOccurrence()
    {
        var start = new DateOnly(2024, 1, 1);
        var before = new DateOnly(2024, 1, 11); // exactly two 5-day intervals after start

        DateOnly actual = start.PreviousOccurrence(5, before);

        Assert.AreEqual(new DateOnly(2024, 1, 6), actual);
    }

    /// <summary>
    /// Verifies that a zero or negative interval throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    [DataRow(0)]
    [DataRow(-1)]
    public void PreviousOccurrence_WhenIntervalIsNotPositive_ShouldThrowExactly(int intervalDays)
    {
        var start = new DateOnly(2025, 7, 7);

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = start.PreviousOccurrence(intervalDays, start);
        });
    }

}
