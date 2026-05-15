// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateOnlyExtensions.IsWeekday.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public static partial class DateOnlyExtensions
{
    /// <summary>
    /// Determines whether the specified <see cref="DateOnly"/> falls on a weekday, using the default <see cref="WorkingDaysOfWeek.MondayToFriday"/> rule.
    /// </summary>
    /// <param name="date">The date value to evaluate.</param>
    /// <returns><see langword="true"/> if <paramref name="date"/> does not fall on Saturday or Sunday; otherwise, <see langword="false"/>.</returns>
    /// <remarks>
    /// <para>A weekday is any day selected by the working-week pattern. This overload uses <see cref="WorkingDaysOfWeek.MondayToFriday"/> and no custom provider.</para>
    /// </remarks>
    public static bool IsWeekday(this DateOnly date) => !IsWeekend(date, WorkingDaysOfWeek.MondayToFriday, null);

    /// <summary>
    /// Determines whether the specified <see cref="DateOnly"/> falls on a weekday, using the supplied <see cref="WorkingDaysOfWeek"/> and an optional custom <paramref name="provider"/>.
    /// </summary>
    /// <param name="date">The date value to evaluate.</param>
    /// <param name="workingWeek">The <see cref="WorkingDaysOfWeek"/> that determines which days are treated as working days.</param>
    /// <param name="provider">An optional <see cref="IWeekendDefinitionProvider"/> that supplies custom weekend logic when <paramref name="workingWeek"/> is <see cref="WorkingDaysOfWeek.Custom"/>.</param>
    /// <returns><see langword="true"/> if <paramref name="date"/> is not a weekend under the supplied rule or provider; otherwise, <see langword="false"/>.</returns>
    /// <remarks>
    /// <para>The method evaluates whether the <see cref="DateOnly.DayOfWeek"/> of <paramref name="date"/> is included in the working-week pattern supplied by <paramref name="workingWeek"/> and optionally refined by <paramref name="provider"/>.</para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="workingWeek"/> is not a defined value of the <see cref="WorkingDaysOfWeek"/> enumeration,
    /// -or- <paramref name="workingWeek"/> is <see cref="WorkingDaysOfWeek.Custom"/> and <paramref name="provider"/> is <see langword="null"/>.
    /// </exception>
    public static bool IsWeekday(this DateOnly date, WorkingDaysOfWeek workingWeek, IWeekendDefinitionProvider? provider = null) => !IsWeekend(date, workingWeek, provider);
}
