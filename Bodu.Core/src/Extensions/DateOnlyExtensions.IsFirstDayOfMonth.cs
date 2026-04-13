// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateOnlyExtensions.IsFirstDayOfMonth.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Extensions;

public static partial class DateOnlyExtensions
{
    /// <summary>
    /// Determines whether the current <see cref="DateOnly"/> instance represents the first day of its month.
    /// </summary>
    /// <param name="date">The date to evaluate.</param>
    /// <returns><see langword="true"/> if the <paramref name="date"/> is the first day of its month; otherwise, <see langword="false"/>.</returns>
    /// <remarks>This method checks whether the day component of the <paramref name="date"/> is equal to 1.</remarks>
    public static bool IsFirstDayOfMonth(this DateOnly date) => date.Day == 1;
}