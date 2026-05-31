// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateOnlyExtensions.NextWeekday.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public static partial class DateOnlyExtensions
{
    /// <summary>
    /// Returns a new <see cref="DateOnly" /> representing the next calendar weekday after the specified
    /// <paramref name="date" />, based on the supplied <paramref name="workingWeek" /> pattern.
    /// </summary>
    /// <param name="date">The starting date value from which to search forward.</param>
    /// <param name="workingWeek">
    /// The <see cref="WorkingDaysOfWeek" /> that determines which days are treated as working days.
    /// </param>
    /// <returns>
    /// A <see cref="DateOnly" /> value set to the first calendar day after <paramref name="date" /> that is a working
    /// day under the specified <paramref name="workingWeek" /> rule.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The method evaluates each successive day until it finds one that is selected as a working day by the specified
    /// rule. The original <paramref name="date" /> is never returned, even if it already falls on a working day.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="workingWeek" /> is not a defined value of the <see cref="WorkingDaysOfWeek" />
    /// enumeration.
    /// </exception>
    public static DateOnly NextWeekday(this DateOnly date, WorkingDaysOfWeek workingWeek)
    {
        ThrowHelper.ThrowIfEnumValueIsUndefined(workingWeek);

        var dayNumber = date.DayNumber;
        do
        {
            dayNumber += 1;
        }
        while (DateTimeExtensions.IsWeekend(DateOnlyExtensions.GetDayOfWeekFromDayNumber(dayNumber), workingWeek));

        return DateOnly.FromDayNumber(dayNumber);
    }

    /// <summary>
    /// Returns a new <see cref="DateOnly" /> representing the next calendar weekday after the specified
    /// <paramref name="date" />, using the supplied <paramref name="workingWeek" /> pattern and an optional custom
    /// <paramref name="provider" />.
    /// </summary>
    /// <param name="date">The starting date value from which to search forward.</param>
    /// <param name="workingWeek">
    /// The <see cref="WorkingDaysOfWeek" /> that determines which days are treated as working days.
    /// </param>
    /// <param name="provider">
    /// An optional <see cref="IWeekendDefinitionProvider" /> that supplies custom weekend logic when
    /// <paramref name="workingWeek" /> is <see cref="WorkingDaysOfWeek.Custom" />. If <see langword="null" />, the
    /// default behavior for the supplied <paramref name="workingWeek" /> applies.
    /// </param>
    /// <returns>
    /// A <see cref="DateOnly" /> value set to the first calendar day after <paramref name="date" /> that is a working
    /// day under the specified <paramref name="workingWeek" /> rule and the logic of <paramref name="provider" />.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The method evaluates each successive day following <paramref name="date" /> until it finds one that is a working
    /// day, either by the supplied <paramref name="workingWeek" /> pattern or by the custom logic of
    /// <paramref name="provider" />.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="workingWeek" /> is not a defined value of the <see cref="WorkingDaysOfWeek" />
    /// enumeration.
    /// </exception>
    public static DateOnly NextWeekday(this DateOnly date, WorkingDaysOfWeek workingWeek, IWeekendDefinitionProvider? provider)
    {
        ThrowHelper.ThrowIfEnumValueIsUndefined(workingWeek);

        var dayNumber = date.DayNumber;
        do
        {
            dayNumber += 1;
        }
        while (DateTimeExtensions.IsWeekend(DateOnlyExtensions.GetDayOfWeekFromDayNumber(dayNumber), workingWeek, provider));

        return DateOnly.FromDayNumber(dayNumber);
    }

    /// <summary>
    /// Returns a new <see cref="DateOnly" /> representing the next day after <paramref name="date" /> whose
    /// <see cref="DayOfWeek" /> is selected in the supplied <paramref name="workingWeek" />.
    /// </summary>
    /// <param name="date">The starting date value from which to search forward.</param>
    /// <param name="workingWeek">
    /// The working-week pattern that determines which days are considered working days.
    /// </param>
    /// <returns>
    /// The first calendar day strictly after <paramref name="date" /> whose day-of-week is selected in
    /// <paramref name="workingWeek" />.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="workingWeek" /> is <see cref="WeekPattern.Empty" />.
    /// </exception>
    public static DateOnly NextWeekday(this DateOnly date, WeekPattern workingWeek)
    {
        if (workingWeek.Count == 0) throw new ArgumentOutOfRangeException(nameof(workingWeek), ResourceStrings.Arg_OutOfRange_WorkingWeekEmpty);

        var dayNumber = date.DayNumber;
        do
        {
            dayNumber += 1;
        }
        while (!workingWeek.Contains(DateOnlyExtensions.GetDayOfWeekFromDayNumber(dayNumber)));

        return DateOnly.FromDayNumber(dayNumber);
    }
}
