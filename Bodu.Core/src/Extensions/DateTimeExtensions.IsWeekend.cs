// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateTimeExtensions.IsWeekend.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Extensions;

public static partial class DateTimeExtensions
{
    /// <summary>
    /// Determines whether the specified <see cref="DateTime" /> falls on a weekend, using the default
    /// <see cref="WorkingDaysOfWeek.MondayToFriday" /> rule.
    /// </summary>
    /// <param name="dateTime">The date and time value to evaluate.</param>
    /// <returns>
    /// <see langword="true" /> if <paramref name="dateTime" /> falls on Saturday or Sunday; otherwise,
    /// <see langword="false" />.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This overload uses the standard working-week pattern (Monday through Friday), so Saturday and Sunday are treated
    /// as weekend days.
    /// </para>
    /// </remarks>
    public static bool IsWeekend(this DateTime dateTime) => IsWeekend(dateTime.DayOfWeek, WorkingDaysOfWeek.MondayToFriday, null);

    /// <summary>
    /// Determines whether the specified <see cref="DateTime" /> falls on a weekend, using the supplied
    /// <see cref="WorkingDaysOfWeek" /> and an optional custom <paramref name="provider" />.
    /// </summary>
    /// <param name="dateTime">The date and time value to evaluate.</param>
    /// <param name="workingWeek">
    /// The <see cref="WorkingDaysOfWeek" /> that determines which days are treated as working days. Any day not
    /// selected is treated as a weekend day.
    /// </param>
    /// <param name="provider">
    /// An optional <see cref="IWeekendDefinitionProvider" /> that supplies custom weekend logic when
    /// <paramref name="workingWeek" /> is <see cref="WorkingDaysOfWeek.Custom" />.
    /// </param>
    /// <returns>
    /// <see langword="true" /> if <paramref name="dateTime" /> falls on a weekend day as defined by the supplied
    /// working-week or provider; otherwise, <see langword="false" />.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method supports alternative working-week patterns used in different cultures and regions, such as
    /// Sunday-to-Thursday or Saturday-to-Wednesday.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="workingWeek" /> is not a defined value of the <see cref="WorkingDaysOfWeek" />
    /// enumeration, -or- <paramref name="workingWeek" /> is <see cref="WorkingDaysOfWeek.Custom" /> and
    /// <paramref name="provider" /> is <see langword="null" />.
    /// </exception>
    public static bool IsWeekend(this DateTime dateTime, WorkingDaysOfWeek workingWeek, IWeekendDefinitionProvider? provider = null) => IsWeekend(dateTime.DayOfWeek, workingWeek, provider);

    /// <summary>
    /// Determines whether the specified <see cref="DayOfWeek" /> is considered a weekend day, using the supplied
    /// <see cref="WorkingDaysOfWeek" /> and an optional custom <paramref name="provider" />.
    /// </summary>
    /// <param name="dayOfWeek">The <see cref="DayOfWeek" /> value to evaluate.</param>
    /// <param name="workingWeek">
    /// The <see cref="WorkingDaysOfWeek" /> that determines which days are treated as working days. Any day not
    /// selected is treated as a weekend day.
    /// </param>
    /// <param name="provider">
    /// An optional <see cref="IWeekendDefinitionProvider" /> that supplies custom weekend logic when
    /// <paramref name="workingWeek" /> is <see cref="WorkingDaysOfWeek.Custom" />.
    /// </param>
    /// <returns>
    /// <see langword="true" /> if <paramref name="dayOfWeek" /> is considered a weekend day; otherwise,
    /// <see langword="false" />.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This overload supports custom weekend evaluation logic via <paramref name="provider" /> when
    /// <paramref name="workingWeek" /> is <see cref="WorkingDaysOfWeek.Custom" />. For all other values the result is
    /// derived from the canonical <see cref="WeekPattern" /> implied by <paramref name="workingWeek" />.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="dayOfWeek" /> is not a defined value of the <see cref="DayOfWeek" /> enumeration, -or-
    /// <paramref name="workingWeek" /> is not a defined value of the <see cref="WorkingDaysOfWeek" /> enumeration, -or-
    /// <paramref name="workingWeek" /> is <see cref="WorkingDaysOfWeek.Custom" /> and <paramref name="provider" /> is
    /// <see langword="null" />.
    /// </exception>
    public static bool IsWeekend(DayOfWeek dayOfWeek, WorkingDaysOfWeek workingWeek, IWeekendDefinitionProvider? provider = null)
    {
        ThrowHelper.ThrowIfEnumValueIsUndefined(dayOfWeek);

        return workingWeek == WorkingDaysOfWeek.Custom
            ? provider is null
                ? throw new ArgumentOutOfRangeException(
                    nameof(workingWeek),
                    string.Format(CultureInfo.CurrentCulture, ResourceStrings.Arg_OutOfRange_EnumValue, nameof(WorkingDaysOfWeek), workingWeek))
                : provider.IsWeekend(dayOfWeek)
            : !workingWeek.ToWeekPattern().Contains(dayOfWeek);
    }

    /// <summary>
    /// Resolves the effective week pattern for a working-week day-stepping walk, or <see langword="null" /> when
    /// <paramref name="workingWeek" /> is <see cref="WorkingDaysOfWeek.Custom" /> and <paramref name="provider" /> must
    /// be consulted per day instead.
    /// </summary>
    /// <param name="workingWeek">The working-week selector to resolve.</param>
    /// <param name="provider">
    /// The custom weekend provider, consulted only when <paramref name="workingWeek" /> is
    /// <see cref="WorkingDaysOfWeek.Custom" />.
    /// </param>
    /// <returns>
    /// The canonical <see cref="WeekPattern" /> for a named working week; <see langword="null" /> when the caller must
    /// evaluate <paramref name="provider" /> per day.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="workingWeek" /> is <see cref="WorkingDaysOfWeek.Custom" /> and <paramref name="provider" /> is
    /// <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// Hoists the working-week validation and pattern conversion that
    /// <see cref="IsWeekend(DayOfWeek, WorkingDaysOfWeek, IWeekendDefinitionProvider?)" /> would otherwise repeat on
    /// every stepped day of a <c>NextWeekday</c> / <c>PreviousWeekday</c> walk, preserving that method's exception
    /// contract for the Custom-without-provider case.
    /// </remarks>
    internal static WeekPattern? ResolveWorkingWeekPattern(WorkingDaysOfWeek workingWeek, IWeekendDefinitionProvider? provider)
    {
        if (workingWeek != WorkingDaysOfWeek.Custom)
            return workingWeek.ToWeekPattern();

        if (provider is null)
        {
            throw new ArgumentOutOfRangeException(
                nameof(workingWeek),
                string.Format(CultureInfo.CurrentCulture, ResourceStrings.Arg_OutOfRange_EnumValue, nameof(WorkingDaysOfWeek), workingWeek));
        }

        return null;
    }
}
