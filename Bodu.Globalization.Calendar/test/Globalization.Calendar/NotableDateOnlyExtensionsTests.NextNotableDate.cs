// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateOnlyExtensionsTests.NextNotableDate.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public sealed partial class NotableDateOnlyExtensionsTests
{
    /// <summary>
    /// Verifies that the next notable date strictly after a date returns the following year's recurrence.
    /// </summary>
    [TestMethod]
    public void NextNotableDate_WhenAfterOccurrence_ShouldReturnNextYearOccurrence()
    {
        Assert.AreEqual(new DateOnly(2026, 1, 1), new DateOnly(2025, 1, 2).NextNotableDate(Service, "XX")?.Date);
    }

    /// <summary>
    /// Verifies that the next notable date is strictly after the reference date, skipping an occurrence that falls on it.
    /// </summary>
    [TestMethod]
    public void NextNotableDate_WhenOnOccurrence_ShouldSkipToNextYear()
    {
        Assert.AreEqual(new DateOnly(2026, 1, 1), new DateOnly(2025, 1, 1).NextNotableDate(Service, "XX")?.Date);
    }

    /// <summary>
    /// Verifies that the next notable date is <see langword="null" /> when no occurrence exists up to the maximum year.
    /// </summary>
    [TestMethod]
    public void NextNotableDate_WhenNoFurtherOccurrence_ShouldReturnNull()
    {
        Assert.IsNull(new DateOnly(9999, 12, 31).NextNotableDate(Service, "XX"));
    }
}
