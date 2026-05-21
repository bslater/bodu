// --------------------------------------------------------------------------------------------------------------- //
// <copyright file="FiscalWeekQuarterProviderTests.Is53WeekFiscalYear.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class FiscalWeekQuarterProviderTests
{

    /// <summary>
    /// Verifies that <see cref="FiscalWeekQuarterProvider.GetWeeksInFiscalYear(int)" /> and
    /// <see cref="FiscalWeekQuarterProvider.Is53WeekFiscalYear(int)" /> agree for every shared
    /// fixture provider.
    /// </summary>
    [TestMethod]
    public void GetWeeksInFiscalYear_ShouldBeConsistentWithIs53WeekFiscalYear()
    {
        Assert.AreEqual(s_sunday52.Is53WeekFiscalYear(Sunday52FiscalYear) ? 53 : 52, s_sunday52.GetWeeksInFiscalYear(Sunday52FiscalYear));
        Assert.AreEqual(s_monday52Leap.Is53WeekFiscalYear(Monday52LeapFiscalYear) ? 53 : 52, s_monday52Leap.GetWeeksInFiscalYear(Monday52LeapFiscalYear));
        Assert.AreEqual(s_sunday53.Is53WeekFiscalYear(Sunday53FiscalYear) ? 53 : 52, s_sunday53.GetWeeksInFiscalYear(Sunday53FiscalYear));
        Assert.AreEqual(s_saturday52.Is53WeekFiscalYear(Saturday52FiscalYear) ? 53 : 52, s_saturday52.GetWeeksInFiscalYear(Saturday52FiscalYear));
    }

    /// <summary>
    /// Verifies that <see cref="FiscalWeekQuarterProvider.GetWeeksInFiscalYear(int)" /> returns 52
    /// for a standard 52-week fiscal year.
    /// </summary>
    [TestMethod]
    public void GetWeeksInFiscalYear_When52WeekYear_ShouldReturn52() => Assert.AreEqual(52, s_sunday52.GetWeeksInFiscalYear(Sunday52FiscalYear));

    /// <summary>
    /// Verifies that <see cref="FiscalWeekQuarterProvider.GetWeeksInFiscalYear(int)" /> returns 53
    /// for a 53-week fiscal year.
    /// </summary>
    [TestMethod]
    public void GetWeeksInFiscalYear_When53WeekYear_ShouldReturn53() => Assert.AreEqual(53, s_sunday53.GetWeeksInFiscalYear(Sunday53FiscalYear));

    /// <summary>
    /// Verifies that <see cref="FiscalWeekQuarterProvider.Is53WeekFiscalYear(int)" /> returns the
    /// correct value across a range of 52-week and 53-week fiscal year configurations.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(Get53WeekYearTestData))]
    public void Is53WeekFiscalYear_WhenEvaluated_ShouldReturnExpected(
        int year,
        int month,
        DayOfWeek dayOfWeek,
        bool isFiscalYearEnd,
        bool useNearestDayOfWeek,
        bool expected)
    {
        var provider = new FiscalWeekQuarterProvider(month, dayOfWeek, isFiscalYearEnd, useNearestDayOfWeek);
        Assert.AreEqual(expected, provider.Is53WeekFiscalYear(year));
    }

    /// <summary>
    /// Verifies that <see cref="FiscalWeekQuarterProvider.Is53WeekFiscalYear(int)" /> returns
    /// <see langword="false" /> for the shared <see cref="s_sunday52" /> provider in fiscal year 2023.
    /// </summary>
    [TestMethod]
    public void Is53WeekFiscalYear_WhenFiscalYearIs52Weeks_ShouldReturnFalse() => Assert.IsFalse(s_sunday52.Is53WeekFiscalYear(Sunday52FiscalYear));

    /// <summary>
    /// Verifies that <see cref="FiscalWeekQuarterProvider.Is53WeekFiscalYear(int)" /> returns
    /// <see langword="true" /> for the shared <see cref="s_sunday53" /> provider in fiscal year 2020.
    /// </summary>
    [TestMethod]
    public void Is53WeekFiscalYear_WhenFiscalYearIs53Weeks_ShouldReturnTrue() => Assert.IsTrue(s_sunday53.Is53WeekFiscalYear(Sunday53FiscalYear));
    private static IEnumerable<object[]> Get53WeekYearTestData()
    {
        // =========================================================
        // Nearest-day alignment
        // =========================================================

        // 52-week years — the nearest anchor day for the following year remains within a 364-day span.

        // Jan 1, 2023 = Sunday; next Jan 1, 2024 = Monday → nearest Sunday = Dec 31, 2023.
        // Span: Dec 31, 2023 − Jan 1, 2023 = 364 days.
        yield return new object[] { 2023, 1, DayOfWeek.Sunday, false, true, false };

        // Jan 1, 2024 = Monday; next Jan 1, 2025 = Wednesday → nearest Monday = Dec 30, 2024.
        // Span: Dec 30, 2024 − Jan 1, 2024 = 364 days.
        yield return new object[] { 2024, 1, DayOfWeek.Monday, false, true, false };

        // Apr 1, 2023 = Saturday; next Apr 1, 2024 = Monday → nearest Saturday = Mar 30, 2024.
        // Span: Mar 30, 2024 − Apr 1, 2023 = 364 days.
        yield return new object[] { 2023, 4, DayOfWeek.Saturday, false, true, false };

        // Jan 1, 2022 = Saturday; next Jan 1, 2023 = Sunday → nearest Saturday = Dec 31, 2022.
        // Span: Dec 31, 2022 − Jan 1, 2022 = 364 days.
        yield return new object[] { 2022, 1, DayOfWeek.Saturday, false, true, false };

        // Jan 1, 2015 = Thursday → nearest Sunday = Jan 4, 2015.
        // Next anchor: Jan 1, 2016 = Friday → nearest Sunday = Jan 3, 2016.
        // Span: Jan 3, 2016 − Jan 4, 2015 = 364 days.
        yield return new object[] { 2015, 1, DayOfWeek.Sunday, false, true, false };

        // 53-week years — the nearest anchor day for the following year falls into the next 7-day cycle,
        // producing a 371-day fiscal year.

        // Jan 1, 2020 = Wednesday → nearest Sunday = Dec 29, 2019.
        // Next anchor: Jan 1, 2021 = Friday → nearest Sunday = Jan 3, 2021.
        // Span: Jan 3, 2021 − Dec 29, 2019 = 371 days.
        yield return new object[] { 2020, 1, DayOfWeek.Sunday, false, true, true };

        // Apr 1, 2010 = Thursday → nearest Monday = Mar 29, 2010.
        // Next anchor: Apr 1, 2011 = Friday → nearest Monday = Apr 4, 2011.
        // Span: Apr 4, 2011 − Mar 29, 2010 = 371 days.
        yield return new object[] { 2010, 4, DayOfWeek.Monday, false, true, true };

        // Sep 1, 2010 = Wednesday → nearest Sunday = Aug 29, 2010.
        // Next anchor: Sep 1, 2011 = Thursday → nearest Sunday = Sep 4, 2011.
        // Span: Sep 4, 2011 − Aug 29, 2010 = 371 days.
        yield return new object[] { 2010, 9, DayOfWeek.Sunday, false, true, true };

        // Jun 1, 2011 = Wednesday → nearest Monday = May 30, 2011.
        // Next anchor: Jun 1, 2012 = Friday → nearest Monday = Jun 4, 2012.
        // Span: Jun 4, 2012 − May 30, 2011 = 371 days.
        yield return new object[] { 2011, 6, DayOfWeek.Monday, false, true, true };

        // =========================================================
        // On-or-before alignment
        // =========================================================

        // 52-week years — the aligned day on or before the next anchor remains 364 days from the current start.

        // Jan 1, 2023 = Sunday → on-or-before Sunday = Jan 1, 2023.
        // Next anchor: Jan 1, 2024 = Monday → on-or-before Sunday = Dec 31, 2023.
        // Span: Dec 31, 2023 − Jan 1, 2023 = 364 days.
        yield return new object[] { 2023, 1, DayOfWeek.Sunday, false, false, false };

        // Jan 1, 2015 = Thursday → on-or-before Sunday = Dec 28, 2014.
        // Next anchor: Jan 1, 2016 = Friday → on-or-before Sunday = Dec 27, 2015.
        // Span: Dec 27, 2015 − Dec 28, 2014 = 364 days.
        yield return new object[] { 2015, 1, DayOfWeek.Sunday, false, false, false };

        // Apr 1, 2023 = Saturday → on-or-before Saturday = Apr 1, 2023.
        // Next anchor: Apr 1, 2024 = Monday → on-or-before Saturday = Mar 30, 2024.
        // Span: Mar 30, 2024 − Apr 1, 2023 = 364 days.
        yield return new object[] { 2023, 4, DayOfWeek.Saturday, false, false, false };

        // 53-week years — the aligned day on or before the next anchor lands 371 days after the current start.

        // Jan 1, 2020 = Wednesday → on-or-before Sunday = Dec 29, 2019.
        // Next anchor: Jan 1, 2021 = Friday → on-or-before Sunday = Dec 27, 2020.
        // Span: Dec 27, 2020 − Dec 29, 2019 = 364 days.
        // Therefore this is NOT a 53-week year under on-or-before.
        yield return new object[] { 2020, 1, DayOfWeek.Sunday, false, false, false };

        // Jan 1, 2017 = Sunday → on-or-before Sunday = Jan 1, 2017.
        // Next anchor: Jan 1, 2018 = Monday → on-or-before Sunday = Dec 31, 2017.
        // Span: Dec 31, 2017 − Jan 1, 2017 = 364 days.
        yield return new object[] { 2017, 1, DayOfWeek.Sunday, false, false, false };

        // Feb 1, 2020 = Saturday → on-or-before Monday = Jan 27, 2020.
        // Next anchor: Feb 1, 2021 = Monday → on-or-before Monday = Feb 1, 2021.
        // Span: Feb 1, 2021 − Jan 27, 2020 = 371 days.
        yield return new object[] { 2020, 2, DayOfWeek.Monday, false, false, true };

        // Aug 1, 2019 = Thursday → on-or-before Sunday = Jul 28, 2019.
        // Next anchor: Aug 1, 2020 = Saturday → on-or-before Sunday = Jul 26, 2020.
        // Span: Jul 26, 2020 − Jul 28, 2019 = 364 days.
        yield return new object[] { 2019, 8, DayOfWeek.Sunday, false, false, false };

        // May 1, 2020 = Friday → on-or-before Monday = Apr 27, 2020.
        // Next anchor: May 1, 2021 = Saturday → on-or-before Monday = Apr 26, 2021.
        // Span: Apr 26, 2021 − Apr 27, 2020 = 364 days.
        yield return new object[] { 2020, 5, DayOfWeek.Monday, false, false, false };

        // Nov 1, 2019 = Friday → on-or-before Wednesday = Oct 30, 2019.
        // Next anchor: Nov 1, 2020 = Sunday → on-or-before Wednesday = Oct 28, 2020.
        // Span: Oct 28, 2020 − Oct 30, 2019 = 364 days.
        yield return new object[] { 2019, 11, DayOfWeek.Wednesday, false, false, false };

        // Mar 1, 2019 = Friday → on-or-before Monday = Feb 25, 2019.
        // Next anchor: Mar 1, 2020 = Sunday → on-or-before Monday = Feb 24, 2020.
        // Span: Feb 24, 2020 − Feb 25, 2019 = 364 days.
        yield return new object[] { 2019, 3, DayOfWeek.Monday, false, false, false };
    }

}
