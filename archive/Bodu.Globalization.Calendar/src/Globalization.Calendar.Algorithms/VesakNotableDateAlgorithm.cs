// ---------------------------------------------------------------------------------------------------------------
// <copyright file="VesakNotableDateAlgorithm.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using SysGlobal = System.Globalization;

namespace Bodu.Globalization.Calendar.Algorithms;

/// <summary>
/// Provides an algorithm for determining the Gregorian date of Vesak (Buddha Day) for a given year.
/// </summary>
/// <remarks>
/// <para>
/// Vesak (also spelled Wesak; Thai: Visakha Bucha) is the most important Buddhist festival, commemorating the birth,
/// enlightenment, and parinirvana of the Buddha Gautama. In the Theravada tradition it is observed on the full moon of
/// the month of Vaisakha, which corresponds to the first full moon in May (or occasionally late April or early June).
/// </para>
/// <para>
/// This algorithm returns the first full moon that falls on or after 1 May of the given year. This matches the Thai
/// Visakha Bucha calculation and the United Nations' definition of Vesak, which is the most widely observed convention
/// globally. Traditions in Sri Lanka, Myanmar, and some other countries use slightly different rules; this algorithm
/// does not attempt to replicate them.
/// </para>
/// <para>
/// The full moon date is computed using the Meeus Chapter 49 algorithm via <see cref="LunarPhaseAlgorithm" />, accurate
/// to within one calendar day for years in the range 1900–2100.
/// </para>
/// </remarks>
public sealed class VesakNotableDateAlgorithm
    : INotableDateAlgorithm
{
    /// <summary>
    /// Computes the date of Vesak for the specified year.
    /// </summary>
    /// <param name="year">The year for which to calculate Vesak. Must be greater than or equal to 1.</param>
    /// <param name="calendar">
    /// An optional <see cref="SysGlobal.Calendar" /> instance representing the calendar system to use. When
    /// <see langword="null" />, <see cref="SysGlobal.GregorianCalendar" /> is assumed.
    /// </param>
    /// <returns>
    /// A <see cref="DateTime" /> representing the date of Vesak, or <see langword="null" /> if the date cannot be
    /// determined. The returned <see cref="DateTime.Kind" /> is always <see cref="DateTimeKind.Unspecified" />.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="year" /> is less than 1 or greater than 9999.
    /// </exception>
    /// <exception cref="NotSupportedException">
    /// Thrown when the specified <paramref name="calendar" /> type is unsupported.
    /// </exception>
    public DateTime? GetDate(int year, SysGlobal.Calendar? calendar = null)
    {
        CalendarThrowHelper.ThrowIfYearOutOfRange(year);

        DateTime? fullMoon = LunarPhaseAlgorithm.GetFullMoonOnOrAfter(new DateTime(year, 5, 1));
        return fullMoon is null ? null : ProjectToCalendar(fullMoon.Value, calendar);
    }

    /// <summary>
    /// Projects <paramref name="date" /> into the specified calendar system, or returns the Gregorian date unchanged
    /// when <paramref name="calendar" /> is <see langword="null" /> or a <see cref="SysGlobal.GregorianCalendar" />.
    /// </summary>
    /// <param name="date">The Gregorian date to project.</param>
    /// <param name="calendar">The target calendar system, or <see langword="null" /> for Gregorian.</param>
    /// <returns>
    /// The date expressed in <paramref name="calendar" />, with <see cref="DateTimeKind.Unspecified" /> kind.
    /// </returns>
    private static DateTime? ProjectToCalendar(DateTime date, SysGlobal.Calendar? calendar)
    {
        SysGlobal.Calendar targetCalendar = calendar ?? new SysGlobal.GregorianCalendar();

        return targetCalendar.GetType() == typeof(SysGlobal.GregorianCalendar)
            ? DateTime.SpecifyKind(date, DateTimeKind.Unspecified)
            : new DateTime(
            targetCalendar.GetYear(date),
            targetCalendar.GetMonth(date),
            targetCalendar.GetDayOfMonth(date),
            0, 0, 0, 0,
            targetCalendar,
            DateTimeKind.Unspecified);
    }
}
