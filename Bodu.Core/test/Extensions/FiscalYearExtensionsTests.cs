// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FiscalYearExtensionsTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

[TestClass]
public class FiscalYearExtensionsTests
{

    /// <summary>
    /// Verifies that <see cref="DateOnlyExtensions.AddFiscalYears" /> applied to the first day of one fiscal year lands
    /// on the first day of the target fiscal year.
    /// </summary>
    [TestMethod]
    public void AddFiscalYears_WhenStartOfFiscalYear_ShouldReturnStartOfTargetFiscalYear()
    {
        FiscalWeekQuarterProvider provider = BuildProvider();
        DateOnly fy2026Start = DateOnlyExtensions.FirstDateOfFiscalYear(2026, provider);

        DateOnly result = fy2026Start.AddFiscalYears(2, provider);

        Assert.AreEqual(DateOnlyExtensions.FirstDateOfFiscalYear(2028, provider), result);
    }

    /// <summary>
    /// Verifies that <see cref="DateOnlyExtensions.FirstDateOfFiscalYear" /> equals the supplied provider's
    /// <see cref="IQuarterDefinitionProvider.GetQuarterStartDate(int, int)" /> for quarter 1.
    /// </summary>
    [TestMethod]
    public void FirstDateOfFiscalYear_WhenQueryingKnownYear_ShouldMatchProviderQuarterStart()
    {
        FiscalWeekQuarterProvider provider = BuildProvider();

        DateOnly first = DateOnlyExtensions.FirstDateOfFiscalYear(2026, provider);
        DateOnly providerStart = provider.GetQuarterStartDate(1, 2026);

        Assert.AreEqual(providerStart, first);
    }

    /// <summary>
    /// Verifies that the <see cref="DateTime" /> overload of <c>FiscalYear</c> agrees with the <see cref="DateOnly" /> overload.
    /// </summary>
    [TestMethod]
    public void FiscalYear_WhenDateTimeAndDateOnlyOverloadsCalled_ShouldAgree()
    {
        FiscalWeekQuarterProvider provider = BuildProvider();
        DateOnly fy2026Start = DateOnlyExtensions.FirstDateOfFiscalYear(2026, provider);

        var fromDateOnly = fy2026Start.FiscalYear(provider);
        var fromDateTime = fy2026Start.ToDateTime(TimeOnly.MinValue).FiscalYear(provider);

        Assert.AreEqual(fromDateOnly, fromDateTime);
    }

    /// <summary>
    /// Verifies that <see cref="DateOnlyExtensions.FiscalYear" /> throws when the supplied provider is
    /// <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void FiscalYear_WhenProviderIsNull_ShouldThrowExactly()
    {
        var date = new DateOnly(2026, 5, 14);

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = date.FiscalYear(null!);
        });
    }

    /// <summary>
    /// Verifies that <see cref="DateOnlyExtensions.FiscalYear" /> returns a consistent fiscal year for a date within a
    /// known fiscal year of the supplied provider.
    /// </summary>
    [TestMethod]
    public void FiscalYear_WhenWithinKnownFiscalYear_ShouldReturnFiscalYear()
    {
        FiscalWeekQuarterProvider provider = BuildProvider();
        // Probe with the Q1 start of fiscal year 2026 — by definition, that date's fiscal year is 2026.
        DateOnly q1Start = DateOnlyExtensions.FirstDateOfFiscalYear(2026, provider);

        var fy = q1Start.FiscalYear(provider);

        Assert.AreEqual(2026, fy);
    }

    /// <summary>
    /// Verifies that <see cref="DateOnlyExtensions.IsFirstDateOfFiscalYear" /> returns <see langword="true" /> on the
    /// fiscal year's start date and <see langword="false" /> on any other day.
    /// </summary>
    [TestMethod]
    public void IsFirstDateOfFiscalYear_WhenStartDate_ShouldReturnTrueOnlyOnTheStart()
    {
        FiscalWeekQuarterProvider provider = BuildProvider();
        DateOnly first = DateOnlyExtensions.FirstDateOfFiscalYear(2026, provider);

        Assert.IsTrue(first.IsFirstDateOfFiscalYear(provider));
        Assert.IsFalse(first.AddDays(1).IsFirstDateOfFiscalYear(provider));
    }

    /// <summary>
    /// Verifies that <see cref="DateOnlyExtensions.IsLastDateOfFiscalYear" /> returns <see langword="true" /> on the
    /// fiscal year's end date and <see langword="false" /> on any other day.
    /// </summary>
    [TestMethod]
    public void IsLastDateOfFiscalYear_WhenEndDate_ShouldReturnTrueOnlyOnTheEnd()
    {
        FiscalWeekQuarterProvider provider = BuildProvider();
        DateOnly last = DateOnlyExtensions.LastDateOfFiscalYear(2026, provider);

        Assert.IsTrue(last.IsLastDateOfFiscalYear(provider));
        Assert.IsFalse(last.AddDays(-1).IsLastDateOfFiscalYear(provider));
    }

    /// <summary>
    /// Verifies that <see cref="DateOnlyExtensions.LastDateOfFiscalYear" /> equals the supplied provider's
    /// <see cref="IQuarterDefinitionProvider.GetQuarterEndDate(int, int)" /> for quarter 4.
    /// </summary>
    [TestMethod]
    public void LastDateOfFiscalYear_WhenQueryingKnownYear_ShouldMatchProviderQuarterEnd()
    {
        FiscalWeekQuarterProvider provider = BuildProvider();

        DateOnly last = DateOnlyExtensions.LastDateOfFiscalYear(2026, provider);
        DateOnly providerEnd = provider.GetQuarterEndDate(4, 2026);

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
