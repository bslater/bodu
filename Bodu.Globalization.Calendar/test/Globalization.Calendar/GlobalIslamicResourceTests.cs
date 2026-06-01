// ---------------------------------------------------------------------------------------------------------------
// <copyright file="GlobalIslamicResourceTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies the end-to-end wiring of the embedded <c>global-islamic.xml</c> resource: anchor rules resolve via the
/// <see cref="DateResolutionStrategy.Fixed" /> strategy with <see cref="System.Globalization.HijriCalendar" /> (the
/// tabular Islamic calendar) and <see cref="NotableDateRule.SweepCalendarYears" />, without requiring callers to
/// populate an <see cref="INotableDateAlgorithmRegistry" />. Expected dates are produced by the BCL's tabular
/// <see cref="System.Globalization.HijriCalendar" /> at the default <c>HijriAdjustment=0</c>.
/// </summary>
[TestClass]
public sealed class GlobalIslamicResourceTests
{
    private const string IslamicResourceName = "Bodu/Globalization/Calendar/Resources/global-islamic.xml";

    /// <summary>
    /// Builds a <see cref="NotableDateService" /> over the embedded <c>global-islamic.xml</c> resource without any
    /// algorithm registry, override providers, or plugins.
    /// </summary>
    /// <returns>The configured service.</returns>
    private static NotableDateService CreateBareService() =>
        new(
            ruleProviders:
            [
                (INotableDateRuleProvider)new XmlResourceNotableDateRuleProvider(IslamicResourceName, new ResourcePathResolver()),
            ],
            workingDaysOfWeek: WorkingDaysOfWeek.MondayToFriday);

    /// <summary>
    /// Verifies that all six Islamic observances declared in <c>global-islamic.xml</c> resolve into
    /// <see cref="NotableDate" /> instances for a representative Gregorian year.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WhenLoadingGlobalIslamic_ShouldResolveAllSixObservances()
    {
        NotableDateService service = CreateBareService();

        var resolvedNames = service.GetNotableDates(2024)
            .Select(r => r.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.IsTrue(resolvedNames.Contains("Ramadan"));
        Assert.IsTrue(resolvedNames.Contains("Eid al-Fitr"));
        Assert.IsTrue(resolvedNames.Contains("Eid al-Adha"));
        Assert.IsTrue(resolvedNames.Contains("Islamic New Year"));
        Assert.IsTrue(resolvedNames.Contains("Day of Ashura"));
        Assert.IsTrue(resolvedNames.Contains("Mawlid al-Nabi"));
    }

    /// <summary>
    /// Verifies that each Islamic observance resolves to the date produced by the BCL tabular
    /// <see cref="System.Globalization.HijriCalendar" /> for representative years (2023–2025, Hijri years 1444–1447).
    /// These tabular dates may differ from Saudi-announced observation dates by 0–1 days — consumers requiring
    /// Saudi-aligned dates should use <c>global-islamic-umm-al-qura.xml</c>.
    /// </summary>
    // 2023 (Hijri 1444 / start of 1445)
    [DataRow(2023, "Ramadan", 3, 22)]
    [DataRow(2023, "Eid al-Fitr", 4, 21)]
    [DataRow(2023, "Eid al-Adha", 6, 28)]
    [DataRow(2023, "Islamic New Year", 7, 18)]
    // 2024 (Hijri 1445 / start of 1446)
    [DataRow(2024, "Ramadan", 3, 10)]
    [DataRow(2024, "Eid al-Fitr", 4, 9)]
    [DataRow(2024, "Eid al-Adha", 6, 16)]
    [DataRow(2024, "Islamic New Year", 7, 7)]
    // 2025 (Hijri 1446 / start of 1447)
    [DataRow(2025, "Ramadan", 2, 28)]
    [DataRow(2025, "Eid al-Fitr", 3, 30)]
    [DataRow(2025, "Eid al-Adha", 6, 6)]
    [DataRow(2025, "Islamic New Year", 6, 26)]
    [TestMethod]
    public void GetNotableDates_WhenLoadingGlobalIslamic_ShouldYieldTabularHijriDates(int year, string observance, int expectedMonth, int expectedDay)
    {
        NotableDateService service = CreateBareService();

        NotableDate? resolved = service.GetNotableDates(year)
            .FirstOrDefault(r => string.Equals(r.Name, observance, StringComparison.OrdinalIgnoreCase));

        Assert.IsNotNull(resolved);
        Assert.AreEqual(new DateTime(year, expectedMonth, expectedDay), resolved!.Date);
    }

    /// <summary>
    /// Verifies that the multi-day Islamic observances preserve their authored
    /// <see cref="NotableDateRule.DurationDays" /> across the resolution pipeline.
    /// </summary>
    [DataRow("Ramadan", 30)]
    [DataRow("Eid al-Fitr", 3)]
    [DataRow("Eid al-Adha", 4)]
    [DataRow("Islamic New Year", 1)]
    [DataRow("Day of Ashura", 1)]
    [DataRow("Mawlid al-Nabi", 1)]
    [TestMethod]
    public void GetNotableDates_WhenLoadingGlobalIslamic_ShouldPreserveAuthoredDurationDays(string observance, int expectedDuration)
    {
        NotableDateService service = CreateBareService();

        NotableDate? resolved = service.GetNotableDates(2024)
            .FirstOrDefault(r => string.Equals(r.Name, observance, StringComparison.OrdinalIgnoreCase));

        Assert.IsNotNull(resolved);
        Assert.AreEqual(expectedDuration, resolved!.DurationDays);
    }
}
