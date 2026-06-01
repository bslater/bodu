// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateFiscalExtensionsTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Extensions;

namespace Bodu.Globalization.Calendar;

[TestClass]
public class NotableDateFiscalExtensionsTests
{
    private static FiscalWeekQuarterProvider BuildProvider() =>
        new(
            month: 1,
            dayOfWeek: DayOfWeek.Saturday,
            isFiscalYearEnd: true,
            useNearestDayOfWeek: true,
            pattern: FiscalWeekPattern.Weeks445);

    private static NotableDateService BuildService() => new();

    /// <summary>
    /// Verifies that <see cref="NotableDateFiscalExtensions.FirstWorkingDayOfFiscalYear" /> returns a working day on or
    /// after the fiscal year start.
    /// </summary>
    [TestMethod]
    public void FirstWorkingDayOfFiscalYear_WhenKnownYear_ShouldReturnWorkingDay()
    {
        FiscalWeekQuarterProvider provider = BuildProvider();
        NotableDateService service = BuildService();

        DateOnly first = NotableDateFiscalExtensions.FirstWorkingDayOfFiscalYear(2026, provider, service);

        Assert.IsTrue(first.IsWorkingDay(service));
        Assert.IsTrue(first >= DateOnlyExtensions.FirstDateOfFiscalYear(2026, provider));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFiscalExtensions.LastWorkingDayOfFiscalYear" /> returns a working day on or
    /// before the fiscal year end.
    /// </summary>
    [TestMethod]
    public void LastWorkingDayOfFiscalYear_WhenKnownYear_ShouldReturnWorkingDay()
    {
        FiscalWeekQuarterProvider provider = BuildProvider();
        NotableDateService service = BuildService();

        DateOnly last = NotableDateFiscalExtensions.LastWorkingDayOfFiscalYear(2026, provider, service);

        Assert.IsTrue(last.IsWorkingDay(service));
        Assert.IsTrue(last <= DateOnlyExtensions.LastDateOfFiscalYear(2026, provider));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFiscalExtensions.FirstWorkingDayOfFiscalQuarter" /> returns a working day on
    /// or after the fiscal quarter start.
    /// </summary>
    [TestMethod]
    public void FirstWorkingDayOfFiscalQuarter_WhenKnownQuarter_ShouldReturnWorkingDay()
    {
        FiscalWeekQuarterProvider provider = BuildProvider();
        NotableDateService service = BuildService();

        DateOnly first = NotableDateFiscalExtensions.FirstWorkingDayOfFiscalQuarter(2, 2026, provider, service);

        Assert.IsTrue(first.IsWorkingDay(service));
        Assert.IsTrue(first >= provider.GetQuarterStartDate(2, 2026));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFiscalExtensions.LastWorkingDayOfFiscalQuarter" /> returns a working day on
    /// or before the fiscal quarter end.
    /// </summary>
    [TestMethod]
    public void LastWorkingDayOfFiscalQuarter_WhenKnownQuarter_ShouldReturnWorkingDay()
    {
        FiscalWeekQuarterProvider provider = BuildProvider();
        NotableDateService service = BuildService();

        DateOnly last = NotableDateFiscalExtensions.LastWorkingDayOfFiscalQuarter(3, 2026, provider, service);

        Assert.IsTrue(last.IsWorkingDay(service));
        Assert.IsTrue(last <= provider.GetQuarterEndDate(3, 2026));
    }

    /// <summary>
    /// Verifies that <see cref="NotableDateFiscalExtensions.FirstWorkingDayOfFiscalQuarter" /> rejects quarter values
    /// outside the 1-4 range.
    /// </summary>
    [TestMethod]
    public void FirstWorkingDayOfFiscalQuarter_WhenQuarterIsZero_ShouldThrowExactly()
    {
        FiscalWeekQuarterProvider provider = BuildProvider();
        NotableDateService service = BuildService();

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = NotableDateFiscalExtensions.FirstWorkingDayOfFiscalQuarter(0, 2026, provider, service);
        });
    }
}
