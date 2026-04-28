// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateOnlyExtensions.FirstDayOfYear.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Extensions;

public static partial class DateOnlyExtensions
{
    /// <summary>
    /// Returns a new <see cref="DateOnly"/> representing the first day of the same calendar year as the specified <paramref name="date"/>.
    /// </summary>
    /// <param name="date">The input <see cref="DateOnly"/> whose year is used to calculate the result.</param>
    /// <returns>A <see cref="DateOnly"/> set to January 1 of the same year as <paramref name="date"/>.</returns>
    /// <remarks>
    /// <para>
    /// This method resets the date to January 1 of the year. The result always falls within the same calendar year as the original <paramref name="date"/>.
    /// </para>
    /// </remarks>
    public static DateOnly FirstDayOfYear(this DateOnly date) => new DateOnly(date.Year, 1, 1);
}
