// ---------------------------------------------------------------------------------------------------------------
// <copyright file="GlobalPersianResourceTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies the end-to-end wiring of the embedded <c>global-persian.xml</c> resource: anchor rules resolve via the
/// <see cref="DateResolutionStrategy.Fixed" /> strategy with <see cref="System.Globalization.PersianCalendar" />,
/// without requiring callers to populate an <see cref="INotableDateAlgorithmRegistry" />. Expected dates are sourced
/// from the Iranian civil calendar (time.ir / Astronomical Applications Department) and reproduced by the BCL
/// <see cref="System.Globalization.PersianCalendar" />.
/// </summary>
[TestClass]
public sealed class GlobalPersianResourceTests
{
    private const string PersianResourceName = "Bodu/Globalization/Calendar/Resources/global-persian.xml";

    /// <summary>
    /// Builds a <see cref="NotableDateService" /> over the embedded <c>global-persian.xml</c> resource without any
    /// algorithm registry, override providers, or plugins.
    /// </summary>
    /// <returns>The configured service.</returns>
    private static NotableDateService CreateBareService() =>
        new(
            ruleProviders:
            [
                (INotableDateRuleProvider)new XmlResourceNotableDateRuleProvider(PersianResourceName, new ResourcePathResolver()),
            ],
            workingDaysOfWeek: WorkingDaysOfWeek.MondayToFriday);

    /// <summary>
    /// Verifies that all three Persian observances declared in <c>global-persian.xml</c> resolve into
    /// <see cref="NotableDate" /> instances for a representative Gregorian year.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WhenLoadingGlobalPersian_ShouldResolveAllThreeObservances()
    {
        NotableDateService service = CreateBareService();

        var resolvedNames = service.GetNotableDates(2024)
            .Select(r => r.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.IsTrue(resolvedNames.Contains("Nowruz"));
        Assert.IsTrue(resolvedNames.Contains("Sizdah Bedar"));
        Assert.IsTrue(resolvedNames.Contains("Yalda Night"));
    }

    /// <summary>
    /// Verifies that each Persian observance resolves to its known Gregorian date for representative years
    /// (Persian years 1401–1405, Gregorian 2022–2026). Expected dates are reproduced by the BCL
    /// <see cref="System.Globalization.PersianCalendar" /> and cross-checked against Iranian civil-calendar
    /// publications (time.ir).
    /// </summary>
    // Nowruz (1 Farvardin) — vernal equinox; 20 or 21 March depending on the year.
    [DataRow(2022, "Nowruz", 3, 21)]
    [DataRow(2023, "Nowruz", 3, 21)]
    [DataRow(2024, "Nowruz", 3, 20)]
    [DataRow(2025, "Nowruz", 3, 21)]
    [DataRow(2026, "Nowruz", 3, 21)]
    // Sizdah Bedar (13 Farvardin) — Nature's Day; 1 or 2 April.
    [DataRow(2022, "Sizdah Bedar", 4, 2)]
    [DataRow(2023, "Sizdah Bedar", 4, 2)]
    [DataRow(2024, "Sizdah Bedar", 4, 1)]
    [DataRow(2025, "Sizdah Bedar", 4, 2)]
    // Yalda Night (30 Azar) — longest night; 20 or 21 December.
    [DataRow(2022, "Yalda Night", 12, 21)]
    [DataRow(2023, "Yalda Night", 12, 21)]
    [DataRow(2024, "Yalda Night", 12, 20)]
    [DataRow(2025, "Yalda Night", 12, 21)]
    [TestMethod]
    public void GetNotableDates_WhenLoadingGlobalPersian_ShouldYieldIranianCivilDates(int year, string observance, int expectedMonth, int expectedDay)
    {
        NotableDateService service = CreateBareService();

        NotableDate? resolved = service.GetNotableDates(year)
            .FirstOrDefault(r => string.Equals(r.Name, observance, StringComparison.OrdinalIgnoreCase));

        Assert.IsNotNull(resolved);
        Assert.AreEqual(new DateTime(year, expectedMonth, expectedDay), resolved!.Date);
    }

    /// <summary>
    /// Verifies that Nowruz preserves its authored 13-day duration (capturing the traditional Nowruz holiday period
    /// that culminates in Sizdah Bedar) across the resolution pipeline.
    /// </summary>
    [TestMethod]
    public void Nowruz_WhenResolved_ShouldPreserveThirteenDayDuration()
    {
        NotableDateService service = CreateBareService();

        NotableDate? resolved = service.GetNotableDates(2024)
            .FirstOrDefault(r => string.Equals(r.Name, "Nowruz", StringComparison.OrdinalIgnoreCase));

        Assert.IsNotNull(resolved);
        Assert.AreEqual(13, resolved!.DurationDays);
    }

    /// <summary>
    /// Verifies that Nowruz always falls on 20 or 21 March across a multi-decade span. This is a contract of the
    /// Persian solar calendar: the new year is anchored to the day containing the vernal equinox at the central
    /// meridian of Iran, which is invariably one of those two Gregorian dates within the BCL's supported range.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Nowruz_WhenResolvedAcrossDecades_ShouldAlwaysFallOnMarch20Or21()
    {
        NotableDateService service = CreateBareService();

        for (var year = 1990; year <= 2050; year++)
        {
            NotableDate? resolved = service.GetNotableDates(year)
                .FirstOrDefault(r => string.Equals(r.Name, "Nowruz", StringComparison.OrdinalIgnoreCase));

            Assert.IsNotNull(resolved, $"Nowruz unresolved for Gregorian year {year}.");
            Assert.AreEqual(3, resolved!.Date.Month, $"Nowruz {year} fell outside March: {resolved.Date:yyyy-MM-dd}.");
            Assert.IsTrue(
                resolved.Date.Day == 20 || resolved.Date.Day == 21,
                $"Nowruz {year} fell on March {resolved.Date.Day} (expected 20 or 21).");
        }
    }
}
