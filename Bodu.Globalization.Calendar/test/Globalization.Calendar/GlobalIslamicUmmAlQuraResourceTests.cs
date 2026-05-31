// ---------------------------------------------------------------------------------------------------------------
// <copyright file="GlobalIslamicUmmAlQuraResourceTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies the end-to-end wiring of the embedded <c>global-islamic-umm-al-qura.xml</c> resource: anchor rules resolve
/// via the <see cref="DateResolutionStrategy.Fixed" /> strategy with <see cref="System.Globalization.UmAlQuraCalendar" />
/// and <see cref="NotableDateRule.SweepCalendarYears" />, without requiring callers to populate an
/// <see cref="INotableDateAlgorithmRegistry" />. Expected dates are sourced from the official Saudi Umm al-Qura
/// calendar (ummulqura.org.sa) and cross-checked against <see cref="System.Globalization.UmAlQuraCalendar" />.
/// </summary>
[TestClass]
public sealed class GlobalIslamicUmmAlQuraResourceTests
{
    private const string UmmAlQuraResourceName = "Bodu/Globalization/Calendar/Resources/global-islamic-umm-al-qura.xml";

    /// <summary>
    /// Builds a <see cref="NotableDateService" /> over the embedded <c>global-islamic-umm-al-qura.xml</c> resource
    /// without any algorithm registry, override providers, or plugins.
    /// </summary>
    /// <returns>The configured service.</returns>
    private static NotableDateService CreateBareService() =>
        new(
            ruleProviders:
            [
                (INotableDateRuleProvider)new XmlResourceNotableDateRuleProvider(UmmAlQuraResourceName, new ResourcePathResolver()),
            ],
            workingDaysOfWeek: WorkingDaysOfWeek.MondayToFriday);

    /// <summary>
    /// Verifies that all six Umm al-Qura observances declared in the resource resolve into <see cref="NotableDate" />
    /// instances for a representative Gregorian year.
    /// </summary>
    [TestMethod]
    public void GetNotableDates_WhenLoadingGlobalIslamicUmmAlQura_ShouldResolveAllSixObservances()
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
    /// Verifies that each Umm al-Qura observance resolves to its Saudi-official Gregorian date for representative
    /// years (2023–2025, Hijri years 1444–1447). Expected dates come from the published Umm al-Qura calendar
    /// (ummulqura.org.sa) and are reproduced by <see cref="System.Globalization.UmAlQuraCalendar" />.
    /// </summary>
    // 2023 (Hijri 1444 / start of 1445)
    [DataRow(2023, "Ramadan", 3, 23)]
    [DataRow(2023, "Eid al-Fitr", 4, 21)]
    [DataRow(2023, "Eid al-Adha", 6, 28)]
    [DataRow(2023, "Islamic New Year", 7, 19)]
    [DataRow(2023, "Day of Ashura", 7, 28)]
    [DataRow(2023, "Mawlid al-Nabi", 9, 27)]
    // 2024 (Hijri 1445 / start of 1446)
    [DataRow(2024, "Ramadan", 3, 11)]
    [DataRow(2024, "Eid al-Fitr", 4, 10)]
    [DataRow(2024, "Eid al-Adha", 6, 16)]
    [DataRow(2024, "Islamic New Year", 7, 7)]
    [DataRow(2024, "Day of Ashura", 7, 16)]
    [DataRow(2024, "Mawlid al-Nabi", 9, 15)]
    // 2025 (Hijri 1446 / start of 1447)
    [DataRow(2025, "Ramadan", 3, 1)]
    [DataRow(2025, "Eid al-Fitr", 3, 30)]
    [DataRow(2025, "Eid al-Adha", 6, 6)]
    [DataRow(2025, "Islamic New Year", 6, 26)]
    [DataRow(2025, "Day of Ashura", 7, 5)]
    [DataRow(2025, "Mawlid al-Nabi", 9, 4)]
    [TestMethod]
    public void GetNotableDates_WhenLoadingGlobalIslamicUmmAlQura_ShouldYieldSaudiOfficialDates(int year, string observance, int expectedMonth, int expectedDay)
    {
        NotableDateService service = CreateBareService();

        NotableDate? resolved = service.GetNotableDates(year)
            .FirstOrDefault(r => string.Equals(r.Name, observance, StringComparison.OrdinalIgnoreCase));

        Assert.IsNotNull(resolved);
        Assert.AreEqual(new DateTime(year, expectedMonth, expectedDay), resolved!.Date);
    }

    /// <summary>
    /// Verifies that the multi-day Umm al-Qura observances preserve their authored
    /// <see cref="NotableDateRule.DurationDays" /> across the resolution pipeline.
    /// </summary>
    [DataRow("Ramadan", 30)]
    [DataRow("Eid al-Fitr", 3)]
    [DataRow("Eid al-Adha", 4)]
    [DataRow("Islamic New Year", 1)]
    [DataRow("Day of Ashura", 1)]
    [DataRow("Mawlid al-Nabi", 1)]
    [TestMethod]
    public void GetNotableDates_WhenLoadingGlobalIslamicUmmAlQura_ShouldPreserveAuthoredDurationDays(string observance, int expectedDuration)
    {
        NotableDateService service = CreateBareService();

        NotableDate? resolved = service.GetNotableDates(2024)
            .FirstOrDefault(r => string.Equals(r.Name, observance, StringComparison.OrdinalIgnoreCase));

        Assert.IsNotNull(resolved);
        Assert.AreEqual(expectedDuration, resolved!.DurationDays);
    }

    /// <summary>
    /// Verifies that Umm al-Qura dates can diverge from the tabular <see cref="System.Globalization.HijriCalendar" />
    /// used by <c>global-islamic.xml</c> by up to one day. This is the design motivation for shipping both resources:
    /// tabular Hijri is portable and deterministic, Umm al-Qura is the Saudi calendar of record. For Ramadan 1445 AH,
    /// tabular Hijri resolves to 2024-03-10 and Umm al-Qura to 2024-03-11 (the date the Royal Court announced).
    /// </summary>
    [TestMethod]
    public void Ramadan_WhenComparedWithTabularHijri_ShouldDifferByUpToOneDayInRepresentativeYears()
    {
        NotableDateService uaq = CreateBareService();
        var tabular = new NotableDateService(
            ruleProviders:
            [
                (INotableDateRuleProvider)new XmlResourceNotableDateRuleProvider("Bodu/Globalization/Calendar/Resources/global-islamic.xml", new ResourcePathResolver()),
            ],
            workingDaysOfWeek: WorkingDaysOfWeek.MondayToFriday);

        foreach (var year in new[] { 2023, 2024, 2025 })
        {
            DateTime uaqRamadan = uaq.GetNotableDates(year).First(d => d.Name == "Ramadan").Date;
            DateTime tabularRamadan = tabular.GetNotableDates(year).First(d => d.Name == "Ramadan").Date;
            var delta = Math.Abs((int)(uaqRamadan - tabularRamadan).TotalDays);
            Assert.IsTrue(delta <= 1, $"Ramadan {year}: UmAlQura={uaqRamadan:yyyy-MM-dd}, tabular Hijri={tabularRamadan:yyyy-MM-dd}, delta={delta} days (expected 0-1).");
        }
    }
}
