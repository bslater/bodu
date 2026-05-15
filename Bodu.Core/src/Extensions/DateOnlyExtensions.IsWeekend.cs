// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateOnlyExtensions.IsWeekend.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public static partial class DateOnlyExtensions
{
    /// <summary>
    /// Determines whether the specified <see cref="DateOnly"/> falls on a weekend, using the default <see cref="WorkingDaysOfWeek.MondayToFriday"/> rule.
    /// </summary>
    /// <param name="date">The date value to evaluate.</param>
    /// <returns><see langword="true"/> if <paramref name="date"/> falls on Saturday or Sunday; otherwise, <see langword="false"/>.</returns>
    /// <remarks>
    /// <para>This overload uses the standard working-week pattern (Monday through Friday), so Saturday and Sunday are treated as weekend days.</para>
    /// </remarks>
    public static bool IsWeekend(this DateOnly date) => DateTimeExtensions.IsWeekend(date.DayOfWeek, WorkingDaysOfWeek.MondayToFriday, null);

    /// <summary>
    /// Determines whether the specified <see cref="DateOnly"/> falls on a weekend, using the supplied <see cref="WorkingDaysOfWeek"/> and an optional custom <paramref name="provider"/>.
    /// </summary>
    /// <param name="date">The date value to evaluate.</param>
    /// <param name="workingWeek">The <see cref="WorkingDaysOfWeek"/> that determines which days are treated as working days. Any day not selected is treated as a weekend day.</param>
    /// <param name="provider">An optional <see cref="IWeekendDefinitionProvider"/> that supplies custom weekend logic when <paramref name="workingWeek"/> is <see cref="WorkingDaysOfWeek.Custom"/>.</param>
    /// <returns><see langword="true"/> if <paramref name="date"/> falls on a weekend day as defined by the supplied working-week or provider; otherwise, <see langword="false"/>.</returns>
    /// <remarks>
    /// <para>This method supports alternative working-week patterns used in different cultures and regions, such as Sunday-to-Thursday or Saturday-to-Wednesday.</para>
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="workingWeek"/> is not a defined value of the <see cref="WorkingDaysOfWeek"/> enumeration,
    /// -or- <paramref name="workingWeek"/> is <see cref="WorkingDaysOfWeek.Custom"/> and <paramref name="provider"/> is <see langword="null"/>.
    /// </exception>
    public static bool IsWeekend(this DateOnly date, WorkingDaysOfWeek workingWeek, IWeekendDefinitionProvider? provider = null) => DateTimeExtensions.IsWeekend(date.DayOfWeek, workingWeek, provider);
}
