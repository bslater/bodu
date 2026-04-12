// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateOnlyExtensions.IsLastDayOfMonth.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Extensions;

public static partial class DateOnlyExtensions
{
    /// <summary>
    /// Determines whether the current <see cref="DateOnly" /> instance represents the last day of its month.
    /// </summary>
    /// <param name="date">The date to evaluate.</param>
    /// <returns><see langword="true" /> if the <paramref name="date" /> is the last day of its month; otherwise, <see langword="false" />.</returns>
    /// <remarks>
    /// This method compares the day component of the <paramref name="date" /> with the total number of days in its month, accounting
    /// for variations such as leap years.
    /// </remarks>
    public static bool IsLastDayOfMonth(this DateOnly date) => date.Day == DateTime.DaysInMonth(date.Year, date.Month);
}