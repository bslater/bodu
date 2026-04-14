// --------------------------------------------------------------------------------------------------------------- //
// <copyright file="DateOnlyExtensions.NthDayOfWeekInMonth.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Extensions;

public static partial class DateOnlyExtensions
{
    /// <summary>
    /// Returns a <see cref="DateOnly"/> representing the Nth occurrence of the specified <see cref="DayOfWeek"/> in the same month
    /// and year as the given <paramref name="date"/>.
    /// </summary>
    /// <param name="date">The input <see cref="DateOnly"/> providing the month and year context (the day component is ignored).</param>
    /// <param name="dayOfWeek">The day of the week to locate within the month.</param>
    /// <param name="ordinal">
    /// The ordinal occurrence to locate, such as <see cref="WeekOfMonthOrdinal.First"/>, <see cref="WeekOfMonthOrdinal.Second"/>, or <see cref="WeekOfMonthOrdinal.Last"/>.
    /// <para>
    /// <b>Note:</b><see cref="WeekOfMonthOrdinal.Fifth"/> only applies when a fifth instance of the specified weekday exists in the month.
    /// </para>
    /// </param>
    /// <returns>
    /// A <see cref="DateOnly"/> representing the specified Nth occurrence of <paramref name="dayOfWeek"/> within the same month and
    /// year as <paramref name="date"/>.
    /// </returns>
    /// <remarks>
    /// <para>
    /// If <paramref name="ordinal"/> is <see cref="WeekOfMonthOrdinal.Last"/>, the method returns the final matching weekday in the
    /// month. For all other values, the result is calculated by counting from the first occurrence of the weekday in the month.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="dayOfWeek"/> or <paramref name="ordinal"/> is not a defined enumeration value, or if the specified
    /// occurrence does not exist within the month.
    /// </exception>
    public static DateOnly NthDayOfWeekInMonth(this DateOnly date, DayOfWeek dayOfWeek, WeekOfMonthOrdinal ordinal)
    {
        ThrowHelper.ThrowIfEnumValueIsUndefined(dayOfWeek);
        ThrowHelper.ThrowIfEnumValueIsUndefined(ordinal);

        switch (ordinal)
        {
            case Extensions.WeekOfMonthOrdinal.First:
                return date.FirstDayOfWeekInMonth(dayOfWeek);

            case Extensions.WeekOfMonthOrdinal.Last:
                return date.LastDayOfWeekInMonth(dayOfWeek);

            default:
                int dayNumber = DateTimeExtensions.GetDayNumberUnchecked(date.Year, date.Month, 1);
                DateOnly result = DateOnly.FromDayNumber(
                    dayNumber + (((int)dayOfWeek - (int)GetDayOfWeekFromDayNumber(dayNumber) + 7) % 7) + (((int)ordinal - 1) * 7));

                if (result.Month != date.Month)
                    throw new ArgumentOutOfRangeException(
                        nameof(ordinal),
                        string.Format(ResourceStrings.Arg_Invalid_OrdinalDoesNotExistForMonth, ordinal, dayOfWeek, date.ToString("MMMM yyyy")));

                return result;
        }
    }

    /// <summary>
    /// Calculates the date of the Nth occurrence of a specified day of the week within a given month and year.
    /// </summary>
    /// <param name="year">The year for which to calculate the date. Must be between the <see cref="DateOnly.MinValue"/> and <see cref="DateOnly.MaxValue"/>.</param>
    /// <param name="month">The month (1–12) for which to calculate the date.</param>
    /// <param name="dayOfWeek">The <see cref="DayOfWeek"/> to find within the specified month.</param>
    /// <param name="ordinal">
    /// The ordinal occurrence of the specified day of the week to return (e.g. First, Second, Third, Fourth, Last). If the specified
    /// ordinal does not occur in the given month (e.g., a fifth Monday in February), an <see cref="ArgumentOutOfRangeException"/> is thrown.
    /// </param>
    /// <returns>
    /// A <see cref="DateOnly"/> representing the Nth occurrence of the specified <paramref name="dayOfWeek"/> in the given
    /// <paramref name="month"/> and <paramref name="year"/>.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if the specified <paramref name="ordinal"/> does not correspond to a valid date in the given month.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown if the <paramref name="dayOfWeek"/> or <paramref name="ordinal"/> is not a defined enumeration value.
    /// </exception>
    /// <remarks>
    /// For <see cref="WeekOfMonthOrdinal.Last"/>, the last matching day of the week in the month is returned. For other ordinal
    /// values, the method starts from the first matching day and adds full weeks to locate the Nth occurrence.
    /// </remarks>
    public static DateOnly NthDayOfWeekInMonth(int year, int month, DayOfWeek dayOfWeek, WeekOfMonthOrdinal ordinal)
    {
        ThrowHelper.ThrowIfOutOfRange(year, DateOnly.MinValue.Year, DateOnly.MaxValue.Year);
        ThrowHelper.ThrowIfOutOfRange(month, 1, 12);
        ThrowHelper.ThrowIfEnumValueIsUndefined(dayOfWeek);
        ThrowHelper.ThrowIfEnumValueIsUndefined(ordinal);

        switch (ordinal)
        {
            case Extensions.WeekOfMonthOrdinal.First:
                return DateOnly.FromDayNumber(DateOnlyExtensions.GetFirstDayOfWeekInMonthDayNumber(year, month, dayOfWeek));

            case Extensions.WeekOfMonthOrdinal.Last:
                return DateOnly.FromDayNumber(DateOnlyExtensions.GetLastDayOfWeekInMonthDayNumber(year, month, dayOfWeek));

            default:
                DateOnly result = DateOnly.FromDayNumber(DateOnlyExtensions.GetFirstDayOfWeekInMonthDayNumber(year, month, dayOfWeek) + (((int)ordinal - 1) * 7));

                if (result.Month != month)
                    throw new ArgumentOutOfRangeException(
                        nameof(ordinal),
                        string.Format(ResourceStrings.Arg_Invalid_OrdinalDoesNotExistForMonth, ordinal, dayOfWeek, new DateOnly(year, month, 1).ToString("MMMM yyyy")));

                return result;
        }
    }
}