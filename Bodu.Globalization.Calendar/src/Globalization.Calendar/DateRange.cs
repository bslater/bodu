// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateRange.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Represents an inclusive range of calendar days bounded by a start and end date.
/// </summary>
/// <param name="StartDate">The first day of the range, inclusive.</param>
/// <param name="EndDate">The last day of the range, inclusive.</param>
/// <remarks>
/// <para>
/// Both endpoints are inclusive, so a range whose start equals its end spans a single day and reports a
/// <see cref="DayCount" /> of one. The struct does not enforce ordering at construction; <see cref="IsValid" /> reports
/// whether the start is on or before the end, and the containment and overlap members return <see langword="false" />
/// for an ill-formed range rather than throwing.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// // The whole of 2026, passed to a service query.
/// DateRange year = new(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31));
/// int days = year.DayCount;                       // 365
/// bool inRange = year.Contains(new DateOnly(2026, 7, 4)); // true
///
/// IReadOnlyList<NotableDate> occurrences = service.Resolve(year, "US");
///]]>
/// </code>
/// </example>
/// <seealso cref="INotableDateService" />
public readonly record struct DateRange(DateOnly StartDate, DateOnly EndDate)
{
    /// <summary>
    /// Gets a value indicating whether the range is well-formed, with a start no later than its end.
    /// </summary>
    /// <value>
    /// <see langword="true" /> when the start is on or before the end; otherwise <see langword="false" />.
    /// </value>
    public bool IsValid =>
        StartDate <= EndDate;

    /// <summary>
    /// Gets the number of days the range spans, inclusive of both endpoints.
    /// </summary>
    /// <value>The inclusive day count, or zero when the range is not well-formed.</value>
    public int DayCount =>
        IsValid ? (EndDate.DayNumber - StartDate.DayNumber) + 1 : 0;

    /// <summary>
    /// Determines whether the range contains the supplied date.
    /// </summary>
    /// <param name="date">The date to test.</param>
    /// <returns>
    /// <see langword="true" /> if the date falls within the range; otherwise <see langword="false" />.
    /// </returns>
    public bool Contains(DateOnly date) =>
        date >= StartDate && date <= EndDate;

    /// <summary>
    /// Determines whether the range fully contains the supplied range.
    /// </summary>
    /// <param name="other">The range to test for containment.</param>
    /// <returns>
    /// <see langword="true" /> when both ranges are well-formed and every day of <paramref name="other" /> falls within
    /// this range; otherwise <see langword="false" />.
    /// </returns>
    public bool Contains(DateRange other) =>
        IsValid && other.IsValid && StartDate <= other.StartDate && other.EndDate <= EndDate;

    /// <summary>
    /// Determines whether the range shares at least one day with the supplied range.
    /// </summary>
    /// <param name="other">The range to test for overlap.</param>
    /// <returns>
    /// <see langword="true" /> when both ranges are well-formed and overlap on at least one day; otherwise
    /// <see langword="false" />.
    /// </returns>
    public bool Intersects(DateRange other) =>
        IsValid && other.IsValid && StartDate <= other.EndDate && other.StartDate <= EndDate;
}
