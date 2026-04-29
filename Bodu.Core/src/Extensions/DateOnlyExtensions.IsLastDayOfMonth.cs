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
    /// Determines whether the specified <see cref="DateOnly"/> falls on the last day of its calendar month.
    /// </summary>
    /// <param name="date">The date value to evaluate.</param>
    /// <returns><see langword="true"/> if <paramref name="date"/> represents the last day of its month; otherwise, <see langword="false"/>.</returns>
    /// <remarks>
    /// <para>This method compares the <see cref="DateOnly.Day"/> component to the total number of days in the same month and year, accounting for leap-year adjustments to February.</para>
    /// <para>Equivalent to checking whether <c>date.Day == DateTime.DaysInMonth(date.Year, date.Month)</c>.</para>
    /// </remarks>
    public static bool IsLastDayOfMonth(this DateOnly date) => date.Day == DateTime.DaysInMonth(date.Year, date.Month);
}
