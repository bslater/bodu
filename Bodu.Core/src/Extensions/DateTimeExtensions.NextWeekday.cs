// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateTimeExtensions.NextWeekday.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public static partial class DateTimeExtensions
{
    /// <summary>
    /// Returns a new <see cref="DateTime" /> representing the next calendar weekday after the specified
    /// <paramref name="dateTime" />, based on the supplied <paramref name="workingWeek" /> pattern.
    /// </summary>
    /// <param name="dateTime">The starting date and time value from which to search forward.</param>
    /// <param name="workingWeek">
    /// The <see cref="WorkingDaysOfWeek" /> that determines which days are treated as working days.
    /// </param>
    /// <returns>
    /// An object whose value is set to the first calendar day after <paramref name="dateTime" /> that is a working day
    /// under the specified <paramref name="workingWeek" /> rule, with the original time-of-day and
    /// <see cref="DateTime.Kind" /> preserved.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The method evaluates each successive day until it finds one that is selected as a working day by the specified
    /// rule. The original <paramref name="dateTime" /> is never returned, even if it already falls on a working day.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="workingWeek" /> is not a defined value of the <see cref="WorkingDaysOfWeek" />
    /// enumeration.
    /// </exception>
    public static DateTime NextWeekday(this DateTime dateTime, WorkingDaysOfWeek workingWeek)
    {
        ThrowHelper.ThrowIfEnumValueIsUndefined(workingWeek);

        var ticks = dateTime.Ticks;
        do
        {
            ticks += TicksPerDay;
        }
        while (IsWeekend(GetDayOfWeekFromTicks(ticks), workingWeek));

        return new DateTime(ticks, dateTime.Kind);
    }

    /// <summary>
    /// Returns a new <see cref="DateTime" /> representing the next calendar weekday after the specified
    /// <paramref name="dateTime" />, using the supplied <paramref name="workingWeek" /> pattern and an optional custom
    /// <paramref name="provider" />.
    /// </summary>
    /// <param name="dateTime">The starting date and time value from which to search forward.</param>
    /// <param name="workingWeek">
    /// The <see cref="WorkingDaysOfWeek" /> that determines which days are treated as working days.
    /// </param>
    /// <param name="provider">
    /// An optional <see cref="IWeekendDefinitionProvider" /> that supplies custom weekend logic when
    /// <paramref name="workingWeek" /> is <see cref="WorkingDaysOfWeek.Custom" />. If <see langword="null" />, the
    /// default behavior for the supplied <paramref name="workingWeek" /> applies.
    /// </param>
    /// <returns>
    /// An object whose value is set to the first calendar day after <paramref name="dateTime" /> that is a working day
    /// under the specified <paramref name="workingWeek" /> rule and the logic of <paramref name="provider" />, with the
    /// original time-of-day and <see cref="DateTime.Kind" /> preserved.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The method evaluates each successive day following <paramref name="dateTime" /> until it finds one that is a
    /// working day, either by the supplied <paramref name="workingWeek" /> pattern or by the custom logic of
    /// <paramref name="provider" />.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="workingWeek" /> is not a defined value of the <see cref="WorkingDaysOfWeek" />
    /// enumeration.
    /// </exception>
    public static DateTime NextWeekday(this DateTime dateTime, WorkingDaysOfWeek workingWeek, IWeekendDefinitionProvider? provider)
    {
        ThrowHelper.ThrowIfEnumValueIsUndefined(workingWeek);

        var ticks = dateTime.Ticks;
        do
        {
            ticks += TicksPerDay;
        }
        while (IsWeekend(GetDayOfWeekFromTicks(ticks), workingWeek, provider));

        return new DateTime(ticks, dateTime.Kind);
    }

    /// <summary>
    /// Returns a new <see cref="DateTime" /> representing the next day after <paramref name="dateTime" /> whose
    /// <see cref="DayOfWeek" /> is selected in the supplied <paramref name="workingWeek" />.
    /// </summary>
    /// <param name="dateTime">The starting date and time value from which to search forward.</param>
    /// <param name="workingWeek">
    /// The working-week pattern that determines which days are considered working days.
    /// </param>
    /// <returns>
    /// The first calendar day strictly after <paramref name="dateTime" /> whose day-of-week is selected in
    /// <paramref name="workingWeek" />, with the original time-of-day and <see cref="DateTime.Kind" /> preserved.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The walk is bounded by the seven distinct <see cref="DayOfWeek" /> values; when <paramref name="workingWeek" />
    /// has no days selected (<see cref="WeekPattern.Empty" />) the method will overrun <see cref="DateTime.MaxValue" />
    /// rather than loop indefinitely. Callers must ensure the supplied pattern selects at least one day.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="workingWeek" /> is <see cref="WeekPattern.Empty" />.
    /// </exception>
    public static DateTime NextWeekday(this DateTime dateTime, WeekPattern workingWeek)
    {
        if (workingWeek.Count == 0) throw new ArgumentOutOfRangeException(nameof(workingWeek), ResourceStrings.Arg_OutOfRange_WorkingWeekEmpty);

        var ticks = dateTime.Ticks;
        do
        {
            ticks += TicksPerDay;
        }
        while (!workingWeek.Contains(GetDayOfWeekFromTicks(ticks)));

        return new DateTime(ticks, dateTime.Kind);
    }
}
