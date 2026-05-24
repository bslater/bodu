// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FiscalYearExtensionsTests.DateTime.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

/// <summary>
/// Verifies the <see cref="DateTime" /> fiscal-year extension overloads on
/// <see cref="DateTimeExtensions" /> against a known <see cref="FiscalWeekQuarterProvider" />.
/// </summary>
[TestClass]
public sealed class FiscalYearExtensionsDateTimeTests
{

    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.AddFiscalYears" /> returns an equivalent <see cref="DateTime" />
    /// (same ticks and <see cref="DateTime.Kind" />) when the <c>count</c> argument is zero — exercising the
    /// short-circuit branch of the method.
    /// </summary>
    [TestMethod]
    public void AddFiscalYears_WhenCountIsZero_ShouldReturnEquivalentDateTimePreservingKind()
    {
        FiscalWeekQuarterProvider provider = BuildProvider();
        DateTime input = new(2026, 5, 14, 12, 30, 45, DateTimeKind.Local);

        DateTime result = input.AddFiscalYears(0, provider);

        Assert.AreEqual(input.Ticks, result.Ticks);
        Assert.AreEqual(input.Kind, result.Kind);
    }

    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.AddFiscalYears" /> applied to the first day of one fiscal year
    /// lands on the first day of the target fiscal year and preserves time-of-day and <see cref="DateTime.Kind" />.
    /// </summary>
    [TestMethod]
    public void AddFiscalYears_WhenStartOfFiscalYear_ShouldReturnStartOfTargetFiscalYearPreservingKindAndTime()
    {
        FiscalWeekQuarterProvider provider = BuildProvider();
        DateTime fy2026Start = DateTimeExtensions.FirstDateOfFiscalYear(2026, provider);
        DateTime starting = fy2026Start.AddHours(9).AddMinutes(15);
        starting = DateTime.SpecifyKind(starting, DateTimeKind.Utc);

        DateTime result = starting.AddFiscalYears(2, provider);

        DateTime expectedStart = DateTimeExtensions.FirstDateOfFiscalYear(2028, provider);
        Assert.AreEqual(expectedStart.Date, result.Date);
        Assert.AreEqual(starting.TimeOfDay, result.TimeOfDay);
        Assert.AreEqual(DateTimeKind.Utc, result.Kind);
    }

    /// <summary>
    /// Verifies that the <see cref="DateTime" /> fiscal-year overloads throw <see cref="ArgumentNullException" />
    /// when the provider argument is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void DateTimeFiscalYearMethods_WhenProviderIsNull_ShouldThrowExactly()
    {
        var date = new DateTime(2026, 5, 14);

        Assert.ThrowsExactly<ArgumentNullException>(() => _ = date.FiscalYear(null!));
        Assert.ThrowsExactly<ArgumentNullException>(() => _ = DateTimeExtensions.FirstDateOfFiscalYear(2026, null!));
        Assert.ThrowsExactly<ArgumentNullException>(() => _ = DateTimeExtensions.LastDateOfFiscalYear(2026, null!));
        Assert.ThrowsExactly<ArgumentNullException>(() => _ = date.AddFiscalYears(1, null!));
    }

    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.FirstDateOfFiscalYear" /> matches the provider's quarter-1
    /// start.
    /// </summary>
    [TestMethod]
    public void FirstDateOfFiscalYear_WhenQueryingKnownYear_ShouldMatchProviderQuarterStart()
    {
        FiscalWeekQuarterProvider provider = BuildProvider();

        DateTime first = DateTimeExtensions.FirstDateOfFiscalYear(2026, provider);
        DateTime providerStart = provider.GetQuarterStart(1, 2026);

        Assert.AreEqual(providerStart, first);
    }

    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.FiscalYear" /> returns the provider's fiscal year for a date
    /// known to fall within FY 2026.
    /// </summary>
    [TestMethod]
    public void FiscalYear_WhenWithinKnownFiscalYear_ShouldReturnFiscalYear()
    {
        FiscalWeekQuarterProvider provider = BuildProvider();
        DateTime q1Start = DateTimeExtensions.FirstDateOfFiscalYear(2026, provider);

        Assert.AreEqual(2026, q1Start.FiscalYear(provider));
    }

    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.IsFirstDateOfFiscalYear" /> returns <see langword="true" /> on
    /// the fiscal year's start date and <see langword="false" /> on any other day.
    /// </summary>
    [TestMethod]
    public void IsFirstDateOfFiscalYear_WhenStartDate_ShouldReturnTrueOnlyOnTheStart()
    {
        FiscalWeekQuarterProvider provider = BuildProvider();
        DateTime first = DateTimeExtensions.FirstDateOfFiscalYear(2026, provider);

        Assert.IsTrue(first.IsFirstDateOfFiscalYear(provider));
        Assert.IsFalse(first.AddDays(1).IsFirstDateOfFiscalYear(provider));
    }

    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.IsLastDateOfFiscalYear" /> returns <see langword="true" /> on
    /// the fiscal year's end date and <see langword="false" /> on any other day.
    /// </summary>
    [TestMethod]
    public void IsLastDateOfFiscalYear_WhenEndDate_ShouldReturnTrueOnlyOnTheEnd()
    {
        FiscalWeekQuarterProvider provider = BuildProvider();
        DateTime last = DateTimeExtensions.LastDateOfFiscalYear(2026, provider);

        Assert.IsTrue(last.IsLastDateOfFiscalYear(provider));
        Assert.IsFalse(last.AddDays(-1).IsLastDateOfFiscalYear(provider));
    }

    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.LastDateOfFiscalYear" /> matches the provider's quarter-4 end.
    /// </summary>
    [TestMethod]
    public void LastDateOfFiscalYear_WhenQueryingKnownYear_ShouldMatchProviderQuarterEnd()
    {
        FiscalWeekQuarterProvider provider = BuildProvider();

        DateTime last = DateTimeExtensions.LastDateOfFiscalYear(2026, provider);
        DateTime providerEnd = provider.GetQuarterEnd(4, 2026);

        Assert.AreEqual(providerEnd, last);
    }
    private static FiscalWeekQuarterProvider BuildProvider() =>
        // 4-4-5 pattern anchored to the Saturday nearest to 31 January (Fiscal-year-end).
        new FiscalWeekQuarterProvider(
            month: 1,
            dayOfWeek: DayOfWeek.Saturday,
            isFiscalYearEnd: true,
            useNearestDayOfWeek: true,
            pattern: FiscalWeekPattern.Weeks445);

}
