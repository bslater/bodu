// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateTimeExtensions.LastDayOfWeek.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;
using System.Globalization;
using System.Threading;
using Bodu.Globalization.Extensions;

namespace Bodu.Extensions;

public static partial class DateTimeExtensions
{
    /// <summary>
    /// Returns a new <see cref="DateTime"/> representing the last day of the week that contains the specified <paramref name="dateTime"/>, using the last day of the week defined by <see cref="CultureInfo.CurrentCulture"/>.
    /// </summary>
    /// <param name="dateTime">The date and time value used to determine the containing week.</param>
    /// <returns>An object whose value is set to midnight (00:00:00) on the culturally defined last day of the week containing <paramref name="dateTime"/>, with the original <see cref="DateTime.Kind"/> preserved.</returns>
    /// <remarks>
    /// <para>This overload uses <see cref="CultureInfo.CurrentCulture"/> to determine the last day of the week, inferred from <see cref="DateTimeFormatInfo.FirstDayOfWeek"/>.</para>
    /// <para>The returned value has its time component normalised to midnight (00:00:00), and the original <see cref="DateTime.Kind"/> is retained.</para>
    /// </remarks>
    public static DateTime LastDayOfWeek(this DateTime dateTime) => dateTime.LastDayOfWeek((CultureInfo?)null);

    /// <summary>
    /// Returns a new <see cref="DateTime"/> representing the last day of the week that contains the specified <paramref name="dateTime"/>, using the last day of the week defined by the supplied or current culture.
    /// </summary>
    /// <param name="dateTime">The date and time value used to determine the containing week.</param>
    /// <param name="culture">An optional <see cref="CultureInfo"/> that defines the first day of the week via <see cref="DateTimeFormatInfo.FirstDayOfWeek"/>. If <see langword="null"/>, <see cref="CultureInfo.CurrentCulture"/> is used.</param>
    /// <returns>An object whose value is set to midnight (00:00:00) on the culturally defined last day of the week containing <paramref name="dateTime"/>, with the original <see cref="DateTime.Kind"/> preserved.</returns>
    /// <remarks>
    /// <para>This method computes the day offset between <paramref name="dateTime"/> and the culture-specific last day of the week, adds that offset, and resets the time to midnight.</para>
    /// <para>The returned value has its time component normalised to midnight (00:00:00), and the original <see cref="DateTime.Kind"/> is retained.</para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the resulting date is earlier than <see cref="DateTime.MinValue"/> or later than <see cref="DateTime.MaxValue"/>.</exception>
    public static DateTime LastDayOfWeek(this DateTime dateTime, CultureInfo? culture)
    {
        culture ??= Thread.CurrentThread.CurrentCulture;
        DayOfWeek lastDayOfWeek = culture.DateTimeFormat.LastDayOfWeek();

        long baseTicks = TruncateToDateTicks(dateTime);
        long offsetTicks = dateTime.DayOfWeek == lastDayOfWeek
            ? 0
            : GetTicksUntilNextOrSameDayOfWeek(dateTime, lastDayOfWeek);

        long dateTicks = baseTicks + offsetTicks;

        if ((ulong)dateTicks > (ulong)DateTime.MaxValue.Ticks)
            throw new ArgumentOutOfRangeException(
                nameof(dateTime),
                string.Format(ResourceStrings.Arg_OutOfRange_ResultingValueOutOfRangeForType, nameof(DateTime)));

        return new DateTime(dateTicks, dateTime.Kind);
    }

    /// <summary>
    /// Returns a new <see cref="DateTime"/> representing the last day of the week that contains the specified <paramref name="dateTime"/>, using a start-of-week inferred from the specified <see cref="CalendarWeekendDefinition"/>.
    /// </summary>
    /// <param name="dateTime">The date and time value used to determine the containing week.</param>
    /// <param name="weekend">A <see cref="CalendarWeekendDefinition"/> used to infer the last day of the week. For example, <see cref="CalendarWeekendDefinition.SaturdaySunday"/> implies a Monday start (and therefore a Sunday end).</param>
    /// <returns>An object whose value is set to midnight (00:00:00) on the last day of the week containing <paramref name="dateTime"/>, with the original <see cref="DateTime.Kind"/> preserved.</returns>
    /// <remarks>
    /// <para>The method infers the start of the week based on the specified <paramref name="weekend"/> value, then calculates the last day as six days after the inferred start. If <see cref="CalendarWeekendDefinition.None"/> is supplied, the method defaults to using <see cref="DayOfWeek.Monday"/>.</para>
    /// <para>The returned value has its time component normalised to midnight (00:00:00), and the original <see cref="DateTime.Kind"/> is retained.</para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="weekend"/> is not a defined <see cref="CalendarWeekendDefinition"/> value,
    /// -or- the resulting date is earlier than <see cref="DateTime.MinValue"/> or later than <see cref="DateTime.MaxValue"/>.
    /// </exception>
    public static DateTime LastDayOfWeek(this DateTime dateTime, CalendarWeekendDefinition weekend)
    {
        ThrowHelper.ThrowIfEnumValueIsUndefined(weekend);
        DayOfWeek startOfWeek = GetWeekStartDay(weekend);
        var endOfWeek = (DayOfWeek)(((int)startOfWeek + 6) % 7);

        int offsetDays = ((int)endOfWeek - (int)dateTime.DayOfWeek + 7) % 7;
        long dateTicks = dateTime.Ticks + (offsetDays * TicksPerDay);

        if ((ulong)dateTicks > (ulong)DateTime.MaxValue.Ticks)
            throw new ArgumentOutOfRangeException(
                nameof(dateTime),
                string.Format(ResourceStrings.Arg_OutOfRange_ResultingValueOutOfRangeForType, nameof(DateTime)));

        return new DateTime(dateTicks, dateTime.Kind);
    }
}
