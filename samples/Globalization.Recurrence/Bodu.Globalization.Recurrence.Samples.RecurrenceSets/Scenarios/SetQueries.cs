// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SetQueries.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Globalization.Recurrence.Samples.RecurrenceSets.Scenarios;

/// <summary>
/// Demonstrates querying a composed <see cref="RecurrenceSet" />: the windowed enumeration used to
/// render a calendar page, and the next/previous point queries that skip exception dates without
/// the caller having to filter them.
/// </summary>
public static class SetQueries
{
    /// <summary>
    /// Builds a realistic set and queries it by window and by point.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- A term timetable with holidays removed ---");

        // A realistic shape: a weekly class, plus a make-up session, minus a public holiday and a
        // mid-term break. The set is the single object a UI or scheduler queries.
        var start = new DateTime(2026, 2, 2, 10, 0, 0);   // a Monday
        var timetable = new RecurrenceSet(
            start,
            [RecurrenceRule.Parse("FREQ=WEEKLY;BYDAY=MO;UNTIL=20260427T100000")],
            [new DateTime(2026, 3, 7, 10, 0, 0)],                                        // Saturday make-up
            [new DateTime(2026, 3, 2, 10, 0, 0), new DateTime(2026, 4, 6, 10, 0, 0)]);   // break, holiday

        Console.WriteLine($"start        : {Format(start)}");
        Console.WriteLine($"rule         : {timetable.Rules[0]}");
        Console.WriteLine($"make-up      : {Join(timetable.Dates)}");
        Console.WriteLine($"cancelled    : {Join(timetable.ExceptionDates)}");
        Console.WriteLine();
        Console.WriteLine($"all sessions : {Join(timetable.GetOccurrences())}");

        Console.WriteLine();
        Console.WriteLine("--- Windowed enumeration: one month at a time ---");

        // The windowed overload yields only what falls inside an inclusive from/to pair, which is
        // what a calendar view needs -- no need to enumerate the series and filter.
        foreach ((string label, DateTime from, DateTime to) in new[]
        {
            ("February", new DateTime(2026, 2, 1), new DateTime(2026, 2, 28, 23, 59, 59)),
            ("March", new DateTime(2026, 3, 1), new DateTime(2026, 3, 31, 23, 59, 59)),
            ("April", new DateTime(2026, 4, 1), new DateTime(2026, 4, 30, 23, 59, 59)),
        })
        {
            Console.WriteLine($"  {label,-9}: {Join(timetable.GetOccurrences(from, to))}");
        }

        Console.WriteLine();
        Console.WriteLine("--- Point queries skip exception dates ---");

        // GetNextOccurrence answers across the whole composition, so a cancelled session is never
        // returned -- the caller does not re-apply the exception list.
        var beforeCancelled = new DateTime(2026, 2, 24, 10, 0, 0);
        Console.WriteLine($"query        : {Format(beforeCancelled)}");
        Console.WriteLine($"next         : {Format(timetable.GetNextOccurrence(beforeCancelled))}");
        Console.WriteLine("               (2026-03-02 is an EXDATE, so it is skipped)");

        // The make-up session is likewise reachable through the point queries, not only through
        // enumeration.
        var beforeMakeUp = new DateTime(2026, 3, 5, 10, 0, 0);
        Console.WriteLine($"query        : {Format(beforeMakeUp)}");
        Console.WriteLine($"next         : {Format(timetable.GetNextOccurrence(beforeMakeUp))} (the RDATE make-up session)");

        Console.WriteLine();
        Console.WriteLine("--- Both directions, with the inclusive flag ---");

        var exactHit = new DateTime(2026, 3, 16, 10, 0, 0);
        Console.WriteLine($"query           : {Format(exactHit)} (an occurrence)");
        Console.WriteLine($"next  exclusive : {Format(timetable.GetNextOccurrence(exactHit))}");
        Console.WriteLine($"next  inclusive : {Format(timetable.GetNextOccurrence(exactHit, inclusive: true))}");
        Console.WriteLine($"prev  exclusive : {Format(timetable.GetPreviousOccurrence(exactHit))}");
        Console.WriteLine($"prev  inclusive : {Format(timetable.GetPreviousOccurrence(exactHit, inclusive: true))}");

        Console.WriteLine();
        Console.WriteLine("--- Past the end of the term ---");

        // Once the bounded rule is exhausted and no RDATE remains, the set reports no occurrence
        // rather than searching on.
        var afterTerm = new DateTime(2026, 9, 1, 10, 0, 0);
        Console.WriteLine($"query        : {Format(afterTerm)}");
        Console.WriteLine($"next         : {Format(timetable.GetNextOccurrence(afterTerm))}");
        Console.WriteLine($"previous     : {Format(timetable.GetPreviousOccurrence(afterTerm))} (the last session of term)");

        Console.WriteLine();
    }

    /// <summary>
    /// Formats an optional instant with the invariant culture, rendering the absent case.
    /// </summary>
    /// <param name="value">The instant to format, or <see langword="null" />.</param>
    /// <returns>The formatted text.</returns>
    private static string Format(DateTime? value) =>
        value?.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture) ?? "(none)";

    /// <summary>
    /// Formats a sequence of occurrences as a comma-separated invariant-culture date list.
    /// </summary>
    /// <param name="occurrences">The occurrences to format.</param>
    /// <returns>The formatted list.</returns>
    private static string Join(IEnumerable<DateTime> occurrences) =>
        string.Join(", ", occurrences.Select(d => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)));
}
