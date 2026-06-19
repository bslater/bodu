// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateOnlyExtensionsTests.PreviousNotableDate.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public sealed partial class NotableDateOnlyExtensionsTests
{
    /// <summary>
    /// Verifies that the previous notable date strictly before a date returns the current year's recurrence.
    /// </summary>
    [TestMethod]
    public void PreviousNotableDate_WhenAfterOccurrence_ShouldReturnSameYearOccurrence()
    {
        Assert.AreEqual(new DateOnly(2025, 1, 1), new DateOnly(2025, 2, 1).PreviousNotableDate(Service, "XX")?.Date);
    }

    /// <summary>
    /// Verifies that the previous notable date is <see langword="null" /> when no occurrence exists down to the minimum
    /// year.
    /// </summary>
    [TestMethod]
    public void PreviousNotableDate_WhenNoEarlierOccurrence_ShouldReturnNull()
    {
        Assert.IsNull(new DateOnly(1, 1, 1).PreviousNotableDate(Service, "XX"));
    }
}
