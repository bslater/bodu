// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompanyCalendarDataTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Globalization.Calendar;

namespace Bodu.Globalization.Calendar.Samples.CustomAlgorithm;

/// <summary>
/// Verifies that the sample's authored company calendar satisfies the shared data-pack contract by
/// deriving <see cref="CalendarDataTestsBase" /> — the same base every regional
/// <c>&lt;Region&gt;CalendarData</c> test derives — and pins the custom algorithm's output with
/// known-answer rows. This is the pattern for validating any consumer-built calendar or algorithm:
/// supply the factory seams, inherit the load-and-resolve smoke contract, and add exact-date rows
/// for the rules whose dates are deterministic.
/// </summary>
[TestClass]
public sealed class CompanyCalendarDataTests
    : CalendarDataTestsBase
{
    /// <inheritdoc />
    protected override IReadOnlyList<string> SupportedCountries => ContosoCalendarData.SupportedCountries;

    /// <inheritdoc />
    protected override INotableDateService CreateService(string territory) =>
        ContosoCalendarData.CreateService(territory);

    /// <summary>
    /// Verifies that the founding-day algorithm resolves the published expectation for each year:
    /// the Friday of the week containing 12 March.
    /// </summary>
    /// <param name="year">The year to resolve.</param>
    /// <param name="expectedDate">The expected celebration date (ISO format).</param>
    [TestMethod]
    [DataRow(2021, "2021-03-12")] // 12 March 2021 is already a Friday - no movement
    [DataRow(2022, "2022-03-11")] // Saturday anniversary -> Friday before
    [DataRow(2024, "2024-03-15")] // Tuesday anniversary -> Friday after
    [DataRow(2026, "2026-03-13")] // Thursday anniversary -> Friday after
    public void Resolve_ForFoundingDay_ShouldMatchKnownDate(int year, string expectedDate)
    {
        NotableDate foundingDay = ResolveSingle("AU", year, "founding-day");

        Assert.AreEqual(
            DateOnly.Parse(expectedDate, CultureInfo.InvariantCulture),
            foundingDay.Date,
            $"founding-day {year}");
    }

    /// <summary>
    /// Verifies that the algorithm reports no occurrence for years before the company existed, so
    /// the calendar emits nothing rather than a fabricated date.
    /// </summary>
    [TestMethod]
    public void Resolve_WhenYearPrecedesFounding_ShouldEmitNoOccurrence()
    {
        IReadOnlyList<NotableDate> dates = ResolveYear("AU", 1997);

        Assert.IsFalse(dates.Any(d => d.NotableDateId == "founding-day"));
    }

    /// <summary>
    /// Verifies that the algorithm itself honours its contract at the unit level: every returned
    /// date is a Friday, and pre-founding years return <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Calculate_ForAnySupportedYear_ShouldReturnFridayOrNull()
    {
        var algorithm = new CompanyFoundingDayAlgorithm();

        Assert.IsNull(algorithm.Calculate(1997));

        for (var year = 1998; year <= 2100; year++)
        {
            DateOnly? date = algorithm.Calculate(year);

            Assert.IsNotNull(date, $"expected an occurrence for {year}");
            Assert.AreEqual(DayOfWeek.Friday, date!.Value.DayOfWeek, $"founding day {year} must be a Friday");
        }
    }
}
