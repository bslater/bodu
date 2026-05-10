// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateResolutionWindow.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Represents a flat chronological working set used while resolving notable dates.
/// </summary>
/// <remarks>
/// <para>
/// The resolution window separates notable dates that should be emitted to callers from dates that are known only because
/// they may block adjustment candidates. This allows boundary dates from neighbouring ranges to participate in
/// non-working-day checks without polluting the caller's output.
/// </para>
/// </remarks>
internal sealed class NotableDateResolutionWindow
{
    private readonly List<NotableDate> _knownDates = new();
    private readonly List<NotableDate> _outputDates = new();
    private readonly List<NotableDate> _nonWorkingDates = new();
    private readonly List<ResolvedNotableDateOccurrence> _baseOccurrences = new();

    /// <summary>
    /// Initialises a new instance of the <see cref="NotableDateResolutionWindow" /> class.
    /// </summary>
    /// <param name="startDate">The inclusive start of the known chronological window.</param>
    /// <param name="endDate">The inclusive end of the known chronological window.</param>
    /// <exception cref="ArgumentException"><paramref name="endDate" /> is earlier than <paramref name="startDate" />.</exception>
    public NotableDateResolutionWindow(DateTime startDate, DateTime endDate)
    {
        DateTime start = startDate.Date;
        DateTime end = endDate.Date;

        if (end < start)
            throw new ArgumentException("The end date must not be earlier than the start date.", nameof(endDate));

        StartDate = start;
        EndDate = end;
    }

    /// <summary>
    /// Gets the inclusive start of the known chronological window.
    /// </summary>
    public DateTime StartDate { get; private set; }

    /// <summary>
    /// Gets the inclusive end of the known chronological window.
    /// </summary>
    public DateTime EndDate { get; private set; }

    /// <summary>
    /// Gets the emitted dates in chronological order.
    /// </summary>
    public IReadOnlyList<NotableDate> OutputDates => Sort(_outputDates);

    /// <summary>
    /// Gets all known dates in chronological order, including blockers that are not emitted.
    /// </summary>
    public IReadOnlyList<NotableDate> KnownDates => Sort(_knownDates);

    /// <summary>
    /// Gets the base occurrences that have been materialised into the window.
    /// </summary>
    public IReadOnlyList<ResolvedNotableDateOccurrence> BaseOccurrences => _baseOccurrences
        .OrderBy(occurrence => occurrence.AnchorDate)
        .ThenBy(occurrence => occurrence.Rule.Priority)
        .ThenBy(occurrence => occurrence.Rule.Name, StringComparer.OrdinalIgnoreCase)
        .ThenBy(occurrence => occurrence.TerritoryCode, StringComparer.OrdinalIgnoreCase)
        .ToList();

    /// <summary>
    /// Adds a base occurrence to the known window and emitted output.
    /// </summary>
    /// <param name="occurrence">The resolved base occurrence.</param>
    /// <exception cref="ArgumentNullException"><paramref name="occurrence" /> is <see langword="null" />.</exception>
    public void AddBase(ResolvedNotableDateOccurrence occurrence)
    {
        ArgumentNullException.ThrowIfNull(occurrence);

        _baseOccurrences.Add(occurrence);
        AddKnownDate(occurrence.BaseDate, emit: true);
    }

    /// <summary>
    /// Adds an adjusted notable date to the known window and emitted output.
    /// </summary>
    /// <param name="notable">The adjusted notable date.</param>
    /// <exception cref="ArgumentNullException"><paramref name="notable" /> is <see langword="null" />.</exception>
    public void AddAdjusted(NotableDate notable)
    {
        AddKnownDate(notable, emit: true);
    }

    /// <summary>
    /// Adds a notable date as a blocker without emitting it to callers.
    /// </summary>
    /// <param name="notable">The notable date to add as a blocker.</param>
    /// <exception cref="ArgumentNullException"><paramref name="notable" /> is <see langword="null" />.</exception>
    public void AddBlocker(NotableDate notable)
    {
        AddKnownDate(notable, emit: false);
    }

    /// <summary>
    /// Determines whether the specified date is inside the known chronological window.
    /// </summary>
    /// <param name="date">The date to test.</param>
    /// <returns><see langword="true" /> if the date is inside the known window; otherwise, <see langword="false" />.</returns>
    public bool Contains(DateTime date)
    {
        DateTime day = date.Date;

        return day >= StartDate && day <= EndDate;
    }

    /// <summary>
    /// Expands the known chronological window forward to include the specified date.
    /// </summary>
    /// <param name="date">The date that must be included.</param>
    public void ExpandForwardTo(DateTime date)
    {
        DateTime day = date.Date;

        if (day > EndDate)
            EndDate = day;
    }

    /// <summary>
    /// Expands the known chronological window backward to include the specified date.
    /// </summary>
    /// <param name="date">The date that must be included.</param>
    public void ExpandBackwardTo(DateTime date)
    {
        DateTime day = date.Date;

        if (day < StartDate)
            StartDate = day;
    }

    /// <summary>
    /// Determines whether the specified date is unavailable because it is a weekend or a known non-working notable date.
    /// </summary>
    /// <param name="date">The date to test.</param>
    /// <param name="territoryCode">The optional territory context.</param>
    /// <param name="calendarType">The optional calendar type context.</param>
    /// <param name="isWeekend">The optional weekend predicate.</param>
    /// <returns><see langword="true" /> if the date is unavailable; otherwise, <see langword="false" />.</returns>
    public bool IsNonWorkingDay(
        DateTime date,
        string? territoryCode,
        Type? calendarType,
        Func<DateTime, bool>? isWeekend = null)
    {
        DateTime day = date.Date;

        if (isWeekend is not null && isWeekend(day))
            return true;

        foreach (NotableDate notable in _nonWorkingDates)
        {
            if (!ContainsDay(notable, day))
                continue;

            if (!MatchesContext(notable, territoryCode, calendarType))
                continue;

            return true;
        }

        return false;
    }

    /// <summary>
    /// Resolves the date of a known notable date by name.
    /// </summary>
    /// <param name="ruleName">The notable-date rule name to resolve.</param>
    /// <param name="year">The civil year to match.</param>
    /// <param name="territoryCode">The optional territory context.</param>
    /// <param name="calendarType">The optional calendar type context.</param>
    /// <returns>The matching date, or <see langword="null" /> when no matching date is known.</returns>
    /// <exception cref="ArgumentException">
    /// <paramref name="ruleName" /> is <see langword="null" />, empty, or consists only of white-space characters.
    /// </exception>
    public DateTime? ResolveByName(
        string ruleName,
        int year,
        string? territoryCode,
        Type? calendarType)
    {
        if (string.IsNullOrWhiteSpace(ruleName))
            throw new ArgumentException("The rule name cannot be null, empty, or white-space.", nameof(ruleName));

        foreach (NotableDate notable in Sort(_knownDates))
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
    /// Adds a notable date to the known window and optionally to the emitted output.
    /// </summary>
    /// <param name="notable">The notable date to add.</param>
    /// <param name="emit">Whether the date should be included in emitted output.</param>
    /// <exception cref="ArgumentNullException"><paramref name="notable" /> is <see langword="null" />.</exception>
    private void AddKnownDate(NotableDate notable, bool emit)
    {
        ArgumentNullException.ThrowIfNull(notable);

        _knownDates.Add(notable);
        ExpandBackwardTo(notable.Date);
        ExpandForwardTo(notable.EndDate);

        if (emit)
            _outputDates.Add(notable);

        if (notable.IsNonWorkingDay)
            _nonWorkingDates.Add(notable);
    }

    /// <summary>
    /// Determines whether the notable date's inclusive span contains the specified day.
    /// </summary>
    /// <param name="notable">The notable date.</param>
    /// <param name="day">The date to test.</param>
    /// <returns><see langword="true" /> when the date is inside the notable date span.</returns>
    private static bool ContainsDay(NotableDate notable, DateTime day) =>
        day >= notable.Date.Date &&
        day <= notable.EndDate.Date;

    /// <summary>
    /// Determines whether a notable date matches the requested territory and calendar context.
    /// </summary>
    /// <param name="date">The notable date.</param>
    /// <param name="territoryCode">The requested territory code.</param>
    /// <param name="calendarType">The requested calendar type.</param>
    /// <returns><see langword="true" /> when the notable date matches the requested context.</returns>
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

        if (!TerritoryCode.TryParse(date.TerritoryCode, out TerritoryCode owned))
            return false;

        return requested.Contains(owned) || owned.Contains(requested);
    }

    /// <summary>
    /// Sorts notable dates into deterministic chronological order.
    /// </summary>
    /// <param name="dates">The dates to sort.</param>
    /// <returns>The sorted dates.</returns>
    private static IReadOnlyList<NotableDate> Sort(IEnumerable<NotableDate> dates) =>
        dates
            .OrderBy(date => date.Date)
            .ThenBy(date => date.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(date => date.TerritoryCode, StringComparer.OrdinalIgnoreCase)
            .ThenBy(date => date.CalendarType?.FullName, StringComparer.OrdinalIgnoreCase)
            .ToList();
}