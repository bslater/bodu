// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateOnlyExtensions.IsRestDay.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public static partial class DateOnlyExtensions
{
    /// <summary>
    /// Determines whether the specified <see cref="DateOnly" /> falls on a day that is not selected in the supplied
    /// <see cref="WeekPattern" /> working week.
    /// </summary>
    /// <param name="date">The date to evaluate.</param>
    /// <param name="workingWeek">The working-week pattern.</param>
    /// <returns>
    /// <see langword="true" /> if <paramref name="date" />'s <see cref="DayOfWeek" /> is not selected in
    /// <paramref name="workingWeek" />; otherwise, <see langword="false" />.
    /// </returns>
    /// <remarks>
    /// This predicate is the complement of <see cref="IsInWorkingWeek(DateOnly, WeekPattern)" /> and considers only the
    /// day-of-week dimension. It does not consult any holiday catalogue.
    /// </remarks>
    public static bool IsRestDay(this DateOnly date, WeekPattern workingWeek) =>
        !workingWeek.Contains(date.DayOfWeek);

    /// <summary>
    /// Determines whether the specified <see cref="DateOnly" /> falls on a day that is not selected in the supplied
    /// <see cref="WorkingDaysOfWeek" /> working week.
    /// </summary>
    /// <param name="date">The date to evaluate.</param>
    /// <param name="workingWeek">The named working-week pattern.</param>
    /// <returns>
    /// <see langword="true" /> if <paramref name="date" />'s <see cref="DayOfWeek" /> is not in the working week;
    /// otherwise, <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="workingWeek" /> is not a defined value of the <see cref="WorkingDaysOfWeek" />
    /// enumeration.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="workingWeek" /> is <see cref="WorkingDaysOfWeek.Custom" />, which has no canonical
    /// pattern.
    /// </exception>
    public static bool IsRestDay(this DateOnly date, WorkingDaysOfWeek workingWeek) =>
        IsRestDay(date, workingWeek.ToWeekPattern());
}
