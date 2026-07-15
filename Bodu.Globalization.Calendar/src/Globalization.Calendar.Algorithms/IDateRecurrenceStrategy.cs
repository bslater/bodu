// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IDateRecurrenceStrategy.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Algorithms;

/// <summary>
/// Calculates every occurrence of a recurring notable-date pattern that falls within a bounded date range, such as
/// every second Tuesday or the last Friday of every month.
/// </summary>
/// <remarks>
/// <para>
/// A recurrence strategy is the multi-occurrence counterpart to <see cref="IDateCalculationStrategy" />: where a
/// single-date strategy yields at most one occurrence per year, a recurrence strategy can yield zero, one, or many
/// occurrences within the requested window. A <see cref="NotableDateRule" /> declares exactly one occurrence source —
/// either a single-date strategy or a recurrence strategy.
/// </para>
/// <para>
/// Implementations must be deterministic and independent of
/// <see cref="System.Globalization.CultureInfo.CurrentCulture" />. The returned dates must be in ascending
/// chronological order, contain no duplicates, and fall entirely within the supplied <paramref name="range" />.
/// Generation is always bounded by the range; an implementation must never expose or produce an unbounded sequence, and
/// whether a given date is an occurrence must not depend on the size or start of the query range (anchor-based patterns
/// are query-window invariant).
/// </para>
/// </remarks>
/// <seealso cref="IDateCalculationStrategy" />
public interface IDateRecurrenceStrategy
{
    /// <summary>
    /// Calculates the occurrences of the recurrence that fall within the supplied inclusive date range.
    /// </summary>
    /// <param name="range">The inclusive window to generate occurrences within.</param>
    /// <param name="context">The resolution context for referential lookups.</param>
    /// <returns>
    /// The occurrences within <paramref name="range" /> in ascending chronological order, without duplicates; an empty
    /// sequence when the recurrence produces no occurrence in the window.
    /// </returns>
    IEnumerable<DateOnly> GetOccurrences(DateRange range, StrategyResolutionContext context);
}
