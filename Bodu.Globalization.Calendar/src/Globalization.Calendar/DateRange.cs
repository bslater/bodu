// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateRange.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Represents an inclusive chronological range bounded by a <see cref="StartDate" /> and <see cref="EndDate" />.
/// </summary>
/// <param name="StartDate">The inclusive start of the range.</param>
/// <param name="EndDate">The inclusive end of the range.</param>
/// <remarks>
/// <para>
/// <see cref="DateRange" /> is a value type with day-resolution semantics: the time component of either bound is ignored. A range
/// where <see cref="StartDate" /> equals <see cref="EndDate" /> describes a single day. Callers are expected to supply
/// <see cref="StartDate" /> on or before <see cref="EndDate" />; range operations on an inverted range yield empty results.
/// </para>
/// </remarks>
public readonly record struct DateRange(DateTime StartDate, DateTime EndDate)
{
    /// <summary>
    /// Gets a value indicating whether the range is non-empty (<see cref="StartDate" /> is on or before <see cref="EndDate" />).
    /// </summary>
    /// <returns><see langword="true" /> when the range covers at least one day; <see langword="false" /> when inverted.</returns>
    public bool IsValid => StartDate.Date <= EndDate.Date;

    /// <summary>
    /// Gets the inclusive number of days covered by the range. Returns zero when the range is inverted.
    /// </summary>
    /// <returns>A non-negative day count, or <c>0</c> when <see cref="IsValid" /> is <see langword="false" />.</returns>
    public int DayCount => IsValid ? (int)(EndDate.Date - StartDate.Date).TotalDays + 1 : 0;

    /// <summary>
    /// Determines whether the range covers the supplied date (day-resolution comparison).
    /// </summary>
    /// <param name="date">The date to test.</param>
    /// <returns><see langword="true" /> when the date falls within the inclusive range; otherwise, <see langword="false" />.</returns>
    public bool Contains(DateTime date)
    {
        DateTime day = date.Date;
        return day >= StartDate.Date && day <= EndDate.Date;
    }

    /// <summary>
    /// Determines whether this range fully contains <paramref name="other" />.
    /// </summary>
    /// <param name="other">The range to test for containment.</param>
    /// <returns><see langword="true" /> when every day of <paramref name="other" /> lies inside this range.</returns>
    public bool Contains(DateRange other) =>
        IsValid && other.IsValid && other.StartDate.Date >= StartDate.Date && other.EndDate.Date <= EndDate.Date;

    /// <summary>
    /// Determines whether this range overlaps <paramref name="other" /> by at least one day.
    /// </summary>
    /// <param name="other">The range to test for overlap.</param>
    /// <returns><see langword="true" /> when the ranges share at least one day.</returns>
    public bool Intersects(DateRange other) =>
        IsValid && other.IsValid && other.StartDate.Date <= EndDate.Date && other.EndDate.Date >= StartDate.Date;

    /// <summary>
    /// Returns a string representation of the range in <c>yyyy-MM-dd … yyyy-MM-dd</c> form.
    /// </summary>
    /// <returns>The string representation.</returns>
    public override string ToString() =>
        $"{StartDate:yyyy-MM-dd} … {EndDate:yyyy-MM-dd}";
}
