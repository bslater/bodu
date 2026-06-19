// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateOnlyExtensionsTests.IsNotableDate.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public sealed partial class NotableDateOnlyExtensionsTests
{
    /// <summary>
    /// Verifies that <see cref="NotableDateOnlyExtensions.IsNotableDate" /> reports the holiday on its day and not on an
    /// ordinary day. In 2025, 1 January is the holiday and 2 January is an ordinary working day.
    /// </summary>
    /// <param name="year">The Gregorian year.</param>
    /// <param name="month">The month component.</param>
    /// <param name="day">The day component.</param>
    /// <param name="expected">The expected notable-date classification.</param>
    [TestMethod]
    [DataRow(2025, 1, 1, true)]    // holiday
    [DataRow(2025, 1, 2, false)]   // ordinary day
    public void IsNotableDate_WhenDayIsHolidayOrOrdinary_ShouldReturnExpected(int year, int month, int day, bool expected)
    {
        Assert.AreEqual(expected, new DateOnly(year, month, day).IsNotableDate(Service, "XX"));
    }
}
