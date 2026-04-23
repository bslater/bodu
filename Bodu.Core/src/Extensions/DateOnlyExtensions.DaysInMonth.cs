// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateOnlyExtensions.DaysInMonth.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Extensions;

public static partial class DateOnlyExtensions
{
    /// <summary>
    /// Returns the number of days in the month of the specified <see cref="DateOnly"/>, using the Gregorian calendar.
    /// </summary>
    /// <param name="date">The <see cref="DateOnly"/> whose month and year are used to determine the number of days.</param>
    /// <returns>The total number of days in the month for the given <paramref name="date"/>, based on the <see cref="System.Globalization.GregorianCalendar"/>.</returns>
    /// <remarks>
    /// This method always evaluates the number of days using the proleptic Gregorian calendar, regardless of the current culture or
    /// calendar settings.
    /// </remarks>
    public static int DaysInMonth(this DateOnly date) => DateTime.DaysInMonth(date.Year, date.Month);
}
