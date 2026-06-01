// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateOnlyExtensions.DaysInYear.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Extensions;

public static partial class DateOnlyExtensions
{
    /// <summary>
    /// Returns the number of days in the calendar year of the specified <see cref="DateOnly" />, using the calendar of
    /// <see cref="CultureInfo.CurrentCulture" />.
    /// </summary>
    /// <param name="date">The date value whose year is used to determine the result.</param>
    /// <returns>
    /// The total number of days in the year of <paramref name="date" />, as defined by the calendar of
    /// <see cref="CultureInfo.CurrentCulture" />.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This overload uses the calendar defined by <see cref="CultureInfo.CurrentCulture" />. The result may vary
    /// depending on the calendar system (e.g. Gregorian, Hebrew, Hijri).
    /// </para>
    /// </remarks>
    public static int DaysInYear(this DateOnly date) => date.DaysInYear((Calendar?)null);

    /// <summary>
    /// Returns the number of days in the calendar year of the specified <see cref="DateOnly" />, using the supplied or
    /// current calendar.
    /// </summary>
    /// <param name="date">The date value whose year is used to determine the result.</param>
    /// <param name="calendar">
    /// An optional <see cref="Calendar" /> used to evaluate the result. If <see langword="null" />, the calendar of
    /// <see cref="CultureInfo.CurrentCulture" /> is used.
    /// </param>
    /// <returns>
    /// The number of days in the year of <paramref name="date" />, based on the supplied or fallback calendar.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Use this overload when you want to explicitly calculate based on a specific calendar system (e.g.
    /// <see cref="GregorianCalendar" />, <see cref="HebrewCalendar" />). If <paramref name="calendar" /> is
    /// <see langword="null" />, the calendar of <see cref="CultureInfo.CurrentCulture" /> is used.
    /// </para>
    /// </remarks>
    public static int DaysInYear(this DateOnly date, Calendar? calendar) => (calendar ?? CultureInfo.CurrentCulture.Calendar).GetDaysInYear(date.Year);
}
