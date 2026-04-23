// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateOnlyExtensions.LastDayOfYear.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Extensions;

public static partial class DateOnlyExtensions
{
    /// <summary>
    /// Returns a new <see cref="DateOnly"/> representing the last day of the same calendar year as the specified <paramref name="date"/>.
    /// </summary>
    /// <param name="date">The input <see cref="DateOnly"/> whose year is used to determine the result.</param>
    /// <returns>A <see cref="DateOnly"/> set to December 31 of the same year as <paramref name="date"/>.</returns>
    /// <remarks>
    /// <para>
    /// This method returns December 31 of the specified year using the Gregorian calendar. The result always falls within the same
    /// calendar year as <paramref name="date"/>.
    /// </para>
    /// </remarks>
    public static DateOnly LastDayOfYear(this DateOnly date) => new DateOnly(date.Year, 12, 31);
}