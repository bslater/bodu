// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateTimeExtensions.IsWeekday.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public static partial class DateTimeExtensions
{
    /// <summary>
    /// Determines whether the specified <see cref="DateTime" /> falls on a weekday, using the default
    /// <see cref="WorkingDaysOfWeek.MondayToFriday" /> rule.
    /// </summary>
    /// <param name="dateTime">The date and time value to evaluate.</param>
    /// <returns>
    /// <see langword="true" /> if <paramref name="dateTime" /> does not fall on Saturday or Sunday; otherwise,
    /// <see langword="false" />.
    /// </returns>
    /// <remarks>
    /// <para>
    /// A weekday is any day selected by the working-week pattern. This overload uses
    /// <see cref="WorkingDaysOfWeek.MondayToFriday" /> and no custom provider.
    /// </para>
    /// </remarks>
    public static bool IsWeekday(this DateTime dateTime) => !IsWeekend(dateTime, WorkingDaysOfWeek.MondayToFriday, null);

    /// <summary>
    /// Determines whether the specified <see cref="DateTime" /> falls on a weekday, using the supplied
    /// <see cref="WorkingDaysOfWeek" /> and an optional custom <paramref name="provider" />.
    /// </summary>
    /// <param name="dateTime">The date and time value to evaluate.</param>
    /// <param name="workingWeek">
    /// The <see cref="WorkingDaysOfWeek" /> that determines which days are treated as working days.
    /// </param>
    /// <param name="provider">
    /// An optional <see cref="IWeekendDefinitionProvider" /> that supplies custom weekend logic when
    /// <paramref name="workingWeek" /> is <see cref="WorkingDaysOfWeek.Custom" />.
    /// </param>
    /// <returns>
    /// <see langword="true" /> if <paramref name="dateTime" /> is not a weekend under the supplied rule or provider;
    /// otherwise, <see langword="false" />.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The method evaluates whether the <see cref="DateTime.DayOfWeek" /> of <paramref name="dateTime" /> is included
    /// in the working-week pattern supplied by <paramref name="workingWeek" /> and optionally refined by
    /// <paramref name="provider" />.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="workingWeek" /> is not a defined value of the <see cref="WorkingDaysOfWeek" />
    /// enumeration, -or- <paramref name="workingWeek" /> is <see cref="WorkingDaysOfWeek.Custom" /> and
    /// <paramref name="provider" /> is <see langword="null" />.
    /// </exception>
    public static bool IsWeekday(this DateTime dateTime, WorkingDaysOfWeek workingWeek, IWeekendDefinitionProvider? provider = null) => !IsWeekend(dateTime, workingWeek, provider);

    /// <summary>
    /// Determines whether the specified <see cref="DayOfWeek" /> is considered a weekday, using the supplied
    /// <see cref="WorkingDaysOfWeek" /> and an optional custom <paramref name="provider" />.
    /// </summary>
    /// <param name="dayOfWeek">The <see cref="DayOfWeek" /> value to evaluate.</param>
    /// <param name="workingWeek">
    /// The <see cref="WorkingDaysOfWeek" /> that determines which days are treated as working days.
    /// </param>
    /// <param name="provider">
    /// An optional <see cref="IWeekendDefinitionProvider" /> that supplies custom weekend logic when
    /// <paramref name="workingWeek" /> is <see cref="WorkingDaysOfWeek.Custom" />.
    /// </param>
    /// <returns>
    /// <see langword="true" /> if <paramref name="dayOfWeek" /> is not a weekend under the supplied rule or provider;
    /// otherwise, <see langword="false" />.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method is equivalent to <c>!IsWeekend(dayOfWeek, workingWeek, provider)</c>.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="workingWeek" /> is not a defined value of the <see cref="WorkingDaysOfWeek" />
    /// enumeration, -or- <paramref name="workingWeek" /> is <see cref="WorkingDaysOfWeek.Custom" /> and
    /// <paramref name="provider" /> is <see langword="null" />.
    /// </exception>
    public static bool IsWeekday(DayOfWeek dayOfWeek, WorkingDaysOfWeek workingWeek, IWeekendDefinitionProvider? provider = null) => !IsWeekend(dayOfWeek, workingWeek, provider);
}
