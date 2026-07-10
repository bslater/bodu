// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateOnlyExtensions.IsWeekend.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public static partial class NotableDateOnlyExtensions
{
    /// <summary>
    /// Determines whether the date falls outside the working week.
    /// </summary>
    /// <param name="date">The date to test.</param>
    /// <param name="workingWeek">The working-week pattern, or <see langword="null" /> for Monday to Friday.</param>
    /// <returns>
    /// <see langword="true" /> if the date is not a working-week day; otherwise <see langword="false" />.
    /// </returns>
    /// <example>
    /// <code language="csharp">
    ///<![CDATA[
    /// bool saturday = new DateOnly(2026, 1, 3).IsWeekend();   // true under the default Mon-Fri week
    ///
    /// // A Sunday-Thursday working week makes Friday the weekend instead.
    /// var sundayToThursday = new WeekPattern(
    ///     DayOfWeek.Sunday, DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday);
    /// bool friday = new DateOnly(2026, 1, 2).IsWeekend(sundayToThursday);   // true
    ///]]>
    /// </code>
    /// </example>
    public static bool IsWeekend(this DateOnly date, WeekPattern? workingWeek = null) =>
        !(workingWeek ?? WeekPattern.MondayToFriday).Contains(date.DayOfWeek);
}
