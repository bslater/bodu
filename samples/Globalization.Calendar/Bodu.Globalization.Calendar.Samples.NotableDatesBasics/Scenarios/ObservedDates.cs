// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ObservedDates.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar;

namespace Bodu.Globalization.Calendar.Samples.NotableDatesBasics.Scenarios;

/// <summary>
/// Demonstrates observed-date substitution (the weekend/in-lieu machinery): when a holiday falls on a
/// weekend, adjustment policies in the rule data move the *observed* day off to a weekday, and the
/// resolved occurrence says exactly what happened — the emitted date, the actual (original) date, and
/// the observed flag.
/// </summary>
public static class ObservedDates
{
    /// <summary>
    /// Resolves Christmas/Boxing Day 2021 for Australia — the classic double-substitution year.
    /// </summary>
    public static void Run()
    {
        SampleConsole.Scenario(
            "Observed dates - weekend substitution",
            what: "Resolves the last week of December 2021 for Australia, the year Christmas fell on a Saturday "
                + "and Boxing Day on a Sunday, then contrasts it with 2024 where Christmas falls mid-week.",
            why: "When a public holiday lands on a weekend most jurisdictions move the day off to a weekday, and "
                + "a calendar that reports only the nominal date is wrong about the thing people actually need - "
                + "which day the office is shut. Reporting only the substituted date is also wrong, because the "
                + "nominal date is what the holiday is. So each occurrence carries both: the emitted date, the "
                + "original one, and why it moved. 2021 is the hard case rather than the easy one, because "
                + "Boxing Day's natural in-lieu slot is the Monday that Christmas has already taken - "
                + "substitution has to be conflict-aware, or two holidays collapse into one day off.",
            expect: "Both holidays are observed on weekdays and each names the actual date it stands in for, so "
                + "no information is lost either way. They land on consecutive days rather than both on the "
                + "Monday, which is the conflict resolution working. The 2024 contrast shows the same rules "
                + "producing no substitution at all - the machinery is quiet when it is not needed.");

        NotableDateService service = AsiaPacificCalendarData.CreateService("AU");

        // 2021: Christmas Day was a Saturday and Boxing Day a Sunday, so both were observed on the
        // following working days (Mon 27 and Tue 28 December) - conflict-aware substitution, since
        // Boxing Day's natural in-lieu slot (Monday) is already taken by Christmas.
        var december = service.Resolve(
            new DateRange(new DateOnly(2021, 12, 24), new DateOnly(2021, 12, 29)), "AU");

        foreach (NotableDate date in december.OrderBy(d => d.Date))
        {
            var detail = date.IsObserved
                ? $"observed (actual {date.ActualDate:yyyy-MM-dd}, reason: {date.AdjustmentReason})"
                : "actual";
            Console.WriteLine($"    {date.Date:yyyy-MM-dd} ({date.Date.DayOfWeek,-9}) {date.DisplayName,-18} {detail}");
        }

        Console.WriteLine("  (consecutive weekdays, not both on the Monday - Boxing Day's natural in-lieu slot was already taken, so substitution has to be conflict-aware)");

        // A year where Christmas falls mid-week has no substitution - the same rules, quiet.
        var christmas2024 = service.Resolve(2024, "AU", NotableDateFilter.WithName("Christmas Day"));
        foreach (NotableDate date in christmas2024)
            Console.WriteLine($"  2024 contrast: {date.Date:yyyy-MM-dd} ({date.Date.DayOfWeek}) observed flag: {date.IsObserved}"
                + "  (expected False - the same rules stay quiet when the holiday already falls on a weekday)");

        Console.WriteLine();
    }
}
