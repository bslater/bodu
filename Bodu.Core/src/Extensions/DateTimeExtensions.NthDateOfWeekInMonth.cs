// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateTimeExtensions.NthDateOfWeekInMonth.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu.Extensions;

public static partial class DateTimeExtensions
{
    /// <summary>
    /// Returns a new <see cref="DateTime"/> representing the specified ordinal occurrence of a <see cref="DayOfWeek"/> within the same calendar month and year as the specified <paramref name="dateTime"/>.
    /// </summary>
    /// <param name="dateTime">The date and time value whose month and year are used to determine the result. The day component is ignored.</param>
    /// <param name="dayOfWeek">The <see cref="DayOfWeek"/> to locate within the month. For example, <see cref="DayOfWeek.Monday"/> returns the nth Monday.</param>
    /// <param name="ordinal">The ordinal occurrence to return. Valid values are <see cref="WeekOfMonthOrdinal.First"/>, <see cref="WeekOfMonthOrdinal.Second"/>, <see cref="WeekOfMonthOrdinal.Third"/>, <see cref="WeekOfMonthOrdinal.Fourth"/>, <see cref="WeekOfMonthOrdinal.Fifth"/>, and <see cref="WeekOfMonthOrdinal.Last"/>. <see cref="WeekOfMonthOrdinal.Fifth"/> is valid only in months where five matching weekdays occur.</param>
    /// <returns>An object whose value is set to midnight (00:00:00) on the requested occurrence of <paramref name="dayOfWeek"/> within the same calendar month and year as <paramref name="dateTime"/>, with the original <see cref="DateTime.Kind"/> preserved.</returns>
    /// <remarks>
    /// <para>For <see cref="WeekOfMonthOrdinal.Last"/>, the method returns the final matching <paramref name="dayOfWeek"/> in the month. For other ordinal values, the method locates the first matching weekday and offsets by a multiple of seven days to reach the desired ordinal.</para>
    /// <para>The returned value has its time component normalised to midnight (00:00:00), and the original <see cref="DateTime.Kind"/> is retained.</para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="dayOfWeek"/> is not a defined value of the <see cref="DayOfWeek"/> enumeration,
    /// -or- <paramref name="ordinal"/> is not a defined value of the <see cref="WeekOfMonthOrdinal"/> enumeration,
    /// -or- the requested <paramref name="ordinal"/> does not occur within the month (for example, a fifth Thursday in February).
    /// </exception>
    public static DateTime NthDateOfWeekInMonth(this DateTime dateTime, DayOfWeek dayOfWeek, WeekOfMonthOrdinal ordinal)
    {
        ThrowHelper.ThrowIfEnumValueIsUndefined(dayOfWeek);
        ThrowHelper.ThrowIfEnumValueIsUndefined(ordinal);

        switch (ordinal)
        {
            case Extensions.WeekOfMonthOrdinal.First:
                return new DateTime(GetFirstDayOfWeekInMonthTicks(dateTime, dayOfWeek), dateTime.Kind);

            case Extensions.WeekOfMonthOrdinal.Last:
                return dateTime.LastDateOfWeekInMonth(dayOfWeek);

            default:
                var result = new DateTime(GetFirstDayOfWeekInMonthTicks(dateTime, dayOfWeek) + (((int)ordinal - 1) * TicksPerWeek), dateTime.Kind);

                if (result.Month != dateTime.Month)
                    throw new ArgumentOutOfRangeException(
                        nameof(ordinal),
                        string.Format(ResourceStrings.Arg_Invalid_OrdinalDoesNotExistForMonth, ordinal, dayOfWeek, dateTime.ToString("MMMM yyyy")));

                return result;
        }
    }

    /// <summary>
    /// Returns a new <see cref="DateTime"/> representing the specified ordinal occurrence of a <see cref="DayOfWeek"/> within the given calendar <paramref name="month"/> and <paramref name="year"/>.
    /// </summary>
    /// <param name="year">The calendar year of the result. Must be between the <c>Year</c> property values of <see cref="DateTime.MinValue"/> and <see cref="DateTime.MaxValue"/>, inclusive.</param>
    /// <param name="month">The calendar month of the result. Must be between 1 and 12, inclusive, where 1 represents January and 12 represents December.</param>
    /// <param name="dayOfWeek">The <see cref="DayOfWeek"/> to locate within the month. For example, <see cref="DayOfWeek.Tuesday"/> returns the nth Tuesday.</param>
    /// <param name="ordinal">The ordinal occurrence to return. Valid values are <see cref="WeekOfMonthOrdinal.First"/>, <see cref="WeekOfMonthOrdinal.Second"/>, <see cref="WeekOfMonthOrdinal.Third"/>, <see cref="WeekOfMonthOrdinal.Fourth"/>, <see cref="WeekOfMonthOrdinal.Fifth"/>, and <see cref="WeekOfMonthOrdinal.Last"/>. <see cref="WeekOfMonthOrdinal.Fifth"/> is valid only in months where five matching weekdays occur.</param>
    /// <returns>An object whose value is set to midnight (00:00:00) on the requested occurrence of <paramref name="dayOfWeek"/> within the specified <paramref name="year"/> and <paramref name="month"/>, using <see cref="DateTimeKind.Unspecified"/>.</returns>
    /// <remarks>
    /// <para>For <see cref="WeekOfMonthOrdinal.Last"/>, the method returns the final matching <paramref name="dayOfWeek"/> in the month. For other ordinal values, the method locates the first matching weekday and offsets by a multiple of seven days to reach the desired ordinal.</para>
    /// <para>The returned value is normalised to midnight (00:00:00) and uses <see cref="DateTimeKind.Unspecified"/>.</para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="year"/> is less than the <c>Year</c> of <see cref="DateTime.MinValue"/> or greater than that of <see cref="DateTime.MaxValue"/>,
    /// -or- <paramref name="month"/> is less than 1 or greater than 12,
    /// -or- <paramref name="dayOfWeek"/> is not a defined value of the <see cref="DayOfWeek"/> enumeration,
    /// -or- <paramref name="ordinal"/> is not a defined value of the <see cref="WeekOfMonthOrdinal"/> enumeration,
    /// -or- the requested <paramref name="ordinal"/> does not occur within the month (for example, a fifth Thursday in February).
    /// </exception>
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
}
