// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OccurrenceSourceEnumerator.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Algorithms;

/// <summary>
/// Enumerates the base occurrences of a rule's single occurrence source — a single-date strategy or a recurrence
/// strategy — for a given Gregorian year, without any concrete-type branching in the caller.
/// </summary>
/// <remarks>
/// This is the single seam the resolution engine and the business-day context use to obtain "the dates a rule produces
/// in a year". A recurrence source is generated against the year's inclusive window; a single-date source that opts in
/// to <see cref="IMultiOccurrenceCalculation" /> (a non-Gregorian <see cref="FixedDateStrategy" />) yields all of its
/// occurrences; any other single-date source yields zero or one.
/// </remarks>
internal static class OccurrenceSourceEnumerator
{
    /// <summary>
    /// Enumerates the base occurrences a rule produces for the supplied Gregorian year.
    /// </summary>
    /// <param name="rule">The rule whose occurrence source is enumerated.</param>
    /// <param name="year">The Gregorian year to calculate against.</param>
    /// <param name="yearRange">The inclusive window for the year, used to bound recurrence generation.</param>
    /// <param name="context">The resolution context for referential lookups.</param>
    /// <returns>The base occurrences for the year; an empty sequence when there is none.</returns>
    public static IEnumerable<DateOnly> Enumerate(NotableDateRule rule, int year, DateRange yearRange, StrategyResolutionContext context)
    {
        if (rule.Recurrence is IDateRecurrenceStrategy recurrence)
            return recurrence.GetOccurrences(yearRange, context);

        if (rule.Strategy is IMultiOccurrenceCalculation multi)
            return multi.CalculateAll(year, context);

        return rule.Strategy?.Calculate(year, context) is DateOnly date ? new[] { date } : [];
    }
}
