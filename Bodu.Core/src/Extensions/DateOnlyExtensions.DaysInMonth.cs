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
    /// Returns the number of days in the calendar month of the specified <see cref="DateOnly"/>, using the proleptic Gregorian calendar.
    /// </summary>
    /// <param name="date">The date value whose year and month are used to determine the result.</param>
    /// <returns>The total number of days in the specified month and year of <paramref name="date"/>, based on the <see cref="System.Globalization.GregorianCalendar"/>.</returns>
    /// <remarks>
    /// <para>This overload always evaluates the result using the proleptic Gregorian calendar, regardless of the current culture or calendar settings.</para>
    /// </remarks>
    public static int DaysInMonth(this DateOnly date) => DateTime.DaysInMonth(date.Year, date.Month);
}
