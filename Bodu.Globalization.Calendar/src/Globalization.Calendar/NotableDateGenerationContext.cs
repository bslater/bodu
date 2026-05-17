// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateGenerationContext.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Maintains the in-progress notable-date state for a single generated year.
/// </summary>
/// <remarks>
/// <para>
/// The generation context is deliberately separate from the public per-year cache. During generation, adjustment logic
/// must be able to ask whether a date is already non-working without re-entering <see cref="NotableDateService" /> and
/// without observing an incomplete cached year.
/// </para>
/// <para>
/// Base occurrences are added first, followed by adjusted occurrences. As each non-working occurrence is added, it
/// immediately becomes visible to subsequent adjustment evaluation.
/// </para>
/// </remarks>
internal sealed class NotableDateGenerationContext
{
    /// <summary>
    /// The full output sequence emitted so far during generation.
    /// </summary>
    private readonly List<NotableDate> _output = [];

    /// <summary>
    /// The subset of <see cref="_output" /> flagged non-working, used for working-day arithmetic during generation.
    /// </summary>
    private readonly List<NotableDate> _nonWorkingDates = [];

    /// <summary>
    /// Gets the generated output accumulated so far.
    /// </summary>
    /// <returns>
    /// A read-only snapshot of the dates emitted into the context so far. Never <see langword="null" />.
    /// </returns>
    public IReadOnlyList<NotableDate> Output => _output;

    /// <summary>
    /// Adds a notable date to the generated output and, when applicable, to the non-working lookup set.
    /// </summary>
    /// <param name="notable">The notable date to add.</param>
    /// <exception cref="ArgumentNullException"><paramref name="notable" /> is <see langword="null" />.</exception>
    public void Add(NotableDate notable)
    {
        ArgumentNullException.ThrowIfNull(notable);

        _output.Add(notable);

        if (notable.IsNonWorkingDay)
            _nonWorkingDates.Add(notable);
    }

    /// <summary>
    /// Determines whether the specified date is already treated as non-working within this generation context.
    /// </summary>
    /// <param name="date">The date to test.</param>
    /// <param name="territoryCode">The active territory code, or <see langword="null" /> for any territory.</param>
    /// <param name="calendarType">The active calendar type, or <see langword="null" /> for any calendar.</param>
    /// <param name="isWeekend">A predicate used to determine whether a date is a weekend.</param>
    /// <returns>
    /// <see langword="true" /> if the date is a weekend or is already covered by a non-working notable date; otherwise,
    /// <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="isWeekend" /> is <see langword="null" />.</exception>
    public bool IsNonWorkingDay(
        DateTime date,
        string? territoryCode,
        Type? calendarType,
        Func<DateTime, bool> isWeekend)
    {
        ArgumentNullException.ThrowIfNull(isWeekend);

        if (isWeekend(date))
            return true;

        foreach (NotableDate notable in _nonWorkingDates)
        {
            if (!ContainsDay(notable, date.Date))
                continue;

            if (!MatchesContext(notable, territoryCode, calendarType))
                continue;

            return true;
        }

        return false;
    }

    /// <summary>
    /// Resolves a generated notable date by name from the in-progress context.
    /// </summary>
    /// <param name="ruleName">The rule name to resolve.</param>
    /// <param name="year">The requested year. Included to match the adjuster callback shape.</param>
    /// <param name="territoryCode">The active territory code, or <see langword="null" /> for any territory.</param>
    /// <param name="calendarType">The active calendar type, or <see langword="null" /> for any calendar.</param>
    /// <returns>The first matching generated date, or <see langword="null" /> if no matching date exists.</returns>
    public DateTime? ResolveByName(
        string ruleName,
        int year,
        string? territoryCode,
        Type? calendarType)
    {
        foreach (NotableDate notable in _output.OrderBy(date => date.Date))
        {
            if (notable.Date.Year != year)
                continue;

            if (!string.Equals(notable.Name, ruleName, StringComparison.OrdinalIgnoreCase))
                continue;

            if (!MatchesContext(notable, territoryCode, calendarType))
                continue;

            return notable.Date;
        }

        return null;
    }

    /// <summary>
    /// Returns <see langword="true" /> when the notable date covers the specified day.
    /// </summary>
    /// <param name="notable">The notable date.</param>
    /// <param name="day">The day to test.</param>
    /// <returns><see langword="true" /> if the day is covered; otherwise, <see langword="false" />.</returns>
    private static bool ContainsDay(NotableDate notable, DateTime day) =>
        day >= notable.Date.Date &&
        day <= notable.EndDate.Date;

    /// <summary>
    /// Returns <see langword="true" /> when the notable date applies to the supplied territory and calendar context.
    /// </summary>
    /// <param name="date">The candidate notable date.</param>
    /// <param name="territoryCode">The requested territory code, or <see langword="null" /> to match any.</param>
    /// <param name="calendarType">The requested calendar type, or <see langword="null" /> to match any.</param>
    /// <returns><see langword="true" /> if the candidate matches; otherwise, <see langword="false" />.</returns>
    private static bool MatchesContext(NotableDate date, string? territoryCode, Type? calendarType)
    {
        if (calendarType is not null &&
            date.CalendarType is not null &&
            date.CalendarType != calendarType)
        {
            return false;
        }

        if (string.IsNullOrEmpty(territoryCode) ||
            string.IsNullOrEmpty(date.TerritoryCode))
        {
            return true;
        }

        if (!TerritoryCode.TryParse(territoryCode, out TerritoryCode requested))
            return false;

        return !TerritoryCode.TryParse(date.TerritoryCode, out TerritoryCode owned)
            ? false
            : requested.Contains(owned) || owned.Contains(requested);
    }
}