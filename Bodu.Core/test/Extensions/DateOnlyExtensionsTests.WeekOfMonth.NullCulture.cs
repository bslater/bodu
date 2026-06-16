// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateOnlyExtensionsTests.WeekOfMonth.NullCulture.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Extensions;

public partial class DateOnlyExtensionsTests
{

    /// <summary>
    /// Verifies that <see cref="DateOnlyExtensions.WeekOfMonth(DateOnly, CultureInfo)" /> falls back to the current culture's
    /// <see cref="DateTimeFormatInfo" /> when <paramref name="culture" /> is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void WeekOfMonth_DateOnly_WhenCultureIsNull_ShouldUseCurrentCulture()
    {
        var date = new DateOnly(2024, 1, 8);
        int expected = date.WeekOfMonth(
            Thread.CurrentThread.CurrentCulture.DateTimeFormat.CalendarWeekRule,
            Thread.CurrentThread.CurrentCulture.DateTimeFormat.FirstDayOfWeek);

        int actual = date.WeekOfMonth((CultureInfo?)null);

        Assert.AreEqual(expected, actual);
    }

}
