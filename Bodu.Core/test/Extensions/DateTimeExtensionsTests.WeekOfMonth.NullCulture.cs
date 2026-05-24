// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateTimeExtensionsTests.WeekOfMonth.NullCulture.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Extensions;

public partial class DateTimeExtensionsTests
{

    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.WeekOfMonth(DateTime, CultureInfo)" /> falls back to the current culture's
    /// <see cref="DateTimeFormatInfo" /> when <paramref name="culture" /> is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void WeekOfMonth_DateTime_WhenCultureIsNull_ShouldUseCurrentCulture()
    {
        var date = new DateTime(2024, 1, 8);
        var expected = date.WeekOfMonth(
            Thread.CurrentThread.CurrentCulture.DateTimeFormat.CalendarWeekRule,
            Thread.CurrentThread.CurrentCulture.DateTimeFormat.FirstDayOfWeek);

        var actual = date.WeekOfMonth((CultureInfo?)null);

        Assert.AreEqual(expected, actual);
    }

}
