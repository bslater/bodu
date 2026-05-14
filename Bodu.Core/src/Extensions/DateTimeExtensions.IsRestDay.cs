// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateTimeExtensions.IsRestDay.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Extensions;

public static partial class DateTimeExtensions
{
    /// <summary>
    /// Determines whether the specified <see cref="DateTime" /> falls on a day that is not selected in the supplied
    /// <see cref="WeekPattern" /> working week.
    /// </summary>
    /// <param name="dateTime">The date and time to evaluate.</param>
    /// <param name="workingWeek">The working-week pattern.</param>
    /// <returns>
    /// <see langword="true" /> if <paramref name="dateTime" />'s <see cref="DayOfWeek" /> is not selected in
    /// <paramref name="workingWeek" />; otherwise, <see langword="false" />.
    /// </returns>
    /// <remarks>
    /// This predicate is the complement of <see cref="IsInWorkingWeek(DateTime, WeekPattern)" /> and considers only
    /// the day-of-week dimension. It does not consult any holiday catalogue.
    /// </remarks>
    public static bool IsRestDay(this DateTime dateTime, WeekPattern workingWeek) =>
        !workingWeek.Contains(dateTime.DayOfWeek);

    /// <summary>
    /// Determines whether the specified <see cref="DateTime" /> falls on a day that is not selected in the supplied
    /// <see cref="WorkingDaysOfWeek" /> working week.
    /// </summary>
    /// <param name="dateTime">The date and time to evaluate.</param>
    /// <param name="workingWeek">The named working-week pattern.</param>
    /// <returns>
    /// <see langword="true" /> if <paramref name="dateTime" />'s <see cref="DayOfWeek" /> is not in the working week;
    /// otherwise, <see langword="false" />.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="workingWeek" /> is not a defined value of the <see cref="WorkingDaysOfWeek" /> enumeration.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="workingWeek" /> is <see cref="WorkingDaysOfWeek.Custom" />, which has no canonical pattern.
    /// </exception>
    public static bool IsRestDay(this DateTime dateTime, WorkingDaysOfWeek workingWeek) =>
        IsRestDay(dateTime, workingWeek.ToWeekPattern());
}
