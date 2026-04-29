// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateTimeExtensions.IsLastDayOfMonth.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Extensions;

public static partial class DateTimeExtensions
{
    /// <summary>
    /// Determines whether the specified <see cref="DateTime"/> falls on the last day of its calendar month.
    /// </summary>
    /// <param name="dateTime">The date and time value to evaluate.</param>
    /// <returns><see langword="true"/> if <paramref name="dateTime"/> represents the last day of its month; otherwise, <see langword="false"/>.</returns>
    /// <remarks>
    /// <para>This method compares the <see cref="DateTime.Day"/> component to the total number of days in the same month and year, accounting for leap-year adjustments to February.</para>
    /// <para>Equivalent to checking whether <c>dateTime.Day == DateTime.DaysInMonth(dateTime.Year, dateTime.Month)</c>.</para>
    /// </remarks>
    public static bool IsLastDayOfMonth(this DateTime dateTime) => dateTime.Day == DateTime.DaysInMonth(dateTime.Year, dateTime.Month);
}
