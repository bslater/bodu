// --------------------------------------------------------------------------------------------------------------- //
// <copyright file="DateTimeExtensions.NthDayOfWeekInMonth.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Extensions;

public static partial class DateTimeExtensions
{
    /// <summary>
    /// Returns a new <see cref="DateTime"/> representing the specified ordinal occurrence of a <see cref="DayOfWeek"/> within the given
    /// <paramref name="month"/> and <paramref name="year"/>.
    /// </summary>
    /// <param name="year">
    /// The calendar year to evaluate. Must be between the year values of <see cref="DateTime.MinValue"/> and
    /// <see cref="DateTime.MaxValue"/>, inclusive.
    /// </param>
    /// <param name="month">The calendar month to evaluate. Must be an integer from 1 (January) to 12 (December), inclusive.</param>
    /// <param name="dayOfWeek">
    /// The <see cref="DayOfWeek"/> to locate within the specified month. For example, <see cref="DayOfWeek.Tuesday"/> will return the nth
    /// Tuesday as determined by <paramref name="ordinal"/>.
    /// </param>
    /// <param name="ordinal">
    /// The ordinal occurrence to return. Valid values include <see cref="WeekOfMonthOrdinal.First"/>,
    /// <see cref="WeekOfMonthOrdinal.Second"/>, <see cref="WeekOfMonthOrdinal.Third"/>, <see cref="WeekOfMonthOrdinal.Fourth"/>,
    /// <see cref="WeekOfMonthOrdinal.Fifth"/>, and <see cref="WeekOfMonthOrdinal.Last"/>.
    /// <para><b>Note:</b><see cref="WeekOfMonthOrdinal.Fifth"/> is valid only in months where five matching weekdays occur.</para>
    /// </param>
    /// <returns>
    /// A new <see cref="DateTime"/> with the time set to midnight and <see cref="DateTimeKind.Unspecified"/>, representing the requested
    /// occurrence of <paramref name="dayOfWeek"/> in the specified <paramref name="month"/> and <paramref name="year"/>.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if:
    /// <list type="bullet">
    /// <item><paramref name="year"/> is outside the supported range for <see cref="DateTime"/> values.</item>
    /// <item><paramref name="month"/> is not between 1 and 12.</item>
    /// <item><paramref name="dayOfWeek"/> is not a valid <see cref="DayOfWeek"/> value.</item>
    /// <item><paramref name="ordinal"/> is not a valid <see cref="WeekOfMonthOrdinal"/> value.</item>
    /// <item><paramref name="ordinal"/> refers to a day that does not occur in the given month (e.g., fifth Thursday in February).</item>
    /// </list>
    /// </exception>
    /// <remarks>
    /// <para>
    /// For <see cref="WeekOfMonthOrdinal.Last"/>, the method searches backward from the end of the month for the final matching <paramref name="dayOfWeek"/>.
    /// </para>
    /// <para>
    /// For other ordinal values, the method finds the first matching weekday and adds a multiple of 7 days to locate the requested occurrence.
    /// </para>
    /// </remarks>
    public static DateTime GetNthDayOfWeekInMonth(int year, int month, DayOfWeek dayOfWeek, WeekOfMonthOrdinal ordinal)
    {
        ThrowHelper.ThrowIfOutOfRange(year, MinYear, MaxYear);
        ThrowHelper.ThrowIfOutOfRange(month, 1, 12);
        ThrowHelper.ThrowIfEnumValueIsUndefined(dayOfWeek);
        ThrowHelper.ThrowIfEnumValueIsUndefined(ordinal);

        switch (ordinal)
        {
            case Extensions.WeekOfMonthOrdinal.First:
                return new DateTime(GetFirstDayOfWeekInMonthTicks(year, month, dayOfWeek), DateTimeKind.Unspecified);

            case Extensions.WeekOfMonthOrdinal.Last:
                return new DateTime(GetLastDayOfWeekInMonthAsTicks(year, month, dayOfWeek), DateTimeKind.Unspecified);

            default:
                var result = new DateTime(GetFirstDayOfWeekInMonthTicks(year, month, dayOfWeek) + (((int)ordinal - 1) * TicksPerWeek), DateTimeKind.Unspecified);

                if (result.Month != month)
                    throw new ArgumentOutOfRangeException(
                        nameof(ordinal),
                        string.Format(ResourceStrings.Arg_Invalid_OrdinalDoesNotExistForMonth, ordinal, dayOfWeek, $"{GetMonthName(month)} {year:0000}"));

                return result;
        }
    }

    /// <summary>
    /// Returns a new <see cref="DateTime"/> representing the specified ordinal occurrence of a <see cref="DayOfWeek"/> within the same
    /// calendar month and year as the given <paramref name="dateTime"/>.
    /// </summary>
    /// <param name="dateTime">
    /// The reference <see cref="DateTime"/>. Only the month and year components are used; the day component is ignored.
    /// </param>
    /// <param name="dayOfWeek">
    /// The <see cref="DayOfWeek"/> to locate within the month. For example, <see cref="DayOfWeek.Monday"/> will return the nth Monday as
    /// defined by <paramref name="ordinal"/>.
    /// </param>
    /// <param name="ordinal">
    /// The ordinal occurrence to return. Valid values include <see cref="WeekOfMonthOrdinal.First"/>,
    /// <see cref="WeekOfMonthOrdinal.Second"/>, <see cref="WeekOfMonthOrdinal.Third"/>, <see cref="WeekOfMonthOrdinal.Fourth"/>,
    /// <see cref="WeekOfMonthOrdinal.Fifth"/>, and <see cref="WeekOfMonthOrdinal.Last"/>.
    /// <para><b>Note:</b><see cref="WeekOfMonthOrdinal.Fifth"/> is valid only in months where five matching weekdays occur.</para>
    /// </param>
    /// <returns>
    /// A new <see cref="DateTime"/> set to midnight and having the same <see cref="DateTime.Kind"/> as <paramref name="dateTime"/>,
    /// representing the requested occurrence of <paramref name="dayOfWeek"/> within the same month and year as <paramref name="dateTime"/>.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="dayOfWeek"/> or <paramref name="ordinal"/> is not a defined enumeration value, or if the requested
    /// <paramref name="ordinal"/> does not exist within the month.
    /// </exception>
    /// <remarks>
    /// <para>
    /// For <see cref="WeekOfMonthOrdinal.Last"/>, the method returns the final matching <paramref name="dayOfWeek"/> in the month. For
    /// all other values, the method locates the first matching weekday and offsets by a multiple of 7 days to reach the desired ordinal.
    /// </para>
    /// </remarks>
    public static DateTime NthDayOfWeekInMonth(this DateTime dateTime, DayOfWeek dayOfWeek, WeekOfMonthOrdinal ordinal)
    {
        ThrowHelper.ThrowIfEnumValueIsUndefined(dayOfWeek);
        ThrowHelper.ThrowIfEnumValueIsUndefined(ordinal);

        switch (ordinal)
        {
            case Extensions.WeekOfMonthOrdinal.First:
                return new DateTime(GetFirstDayOfWeekInMonthTicks(dateTime, dayOfWeek), dateTime.Kind);

            case Extensions.WeekOfMonthOrdinal.Last:
                return dateTime.LastDayOfWeekInMonth(dayOfWeek);

            default:
                var result = new DateTime(GetFirstDayOfWeekInMonthTicks(dateTime, dayOfWeek) + (((int)ordinal - 1) * TicksPerWeek), dateTime.Kind);

                if (result.Month != dateTime.Month)
                    throw new ArgumentOutOfRangeException(
                        nameof(ordinal),
                        string.Format(ResourceStrings.Arg_Invalid_OrdinalDoesNotExistForMonth, ordinal, dayOfWeek, dateTime.ToString("MMMM yyyy")));

                return result;
        }
    }
}
