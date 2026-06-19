// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateOnlyExtensionsTests.GetNotableDates.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public sealed partial class NotableDateOnlyExtensionsTests
{
    /// <summary>
    /// Verifies that <see cref="NotableDateOnlyExtensions.GetNotableDates" /> returns the holiday occurrence on its day.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WhenHolidayOnTheDay_ShouldReturnHoliday()
    {
        Assert.AreEqual("new-years-day", new DateOnly(2025, 1, 1).GetNotableDates(Service, "XX").Single().NotableDateId);
    }
}
