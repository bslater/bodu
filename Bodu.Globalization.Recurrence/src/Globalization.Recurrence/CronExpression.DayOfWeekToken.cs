// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CronExpression.DayOfWeekToken.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Extensions;

namespace Bodu.Globalization.Recurrence;

public sealed partial class CronExpression
{
    /// <summary>
    /// Represents a Quartz token that stands for the whole day-of-week field, and decides which days of a month it
    /// selects.
    /// </summary>
    /// <param name="Day">The day of the week the token selects.</param>
    /// <param name="Ordinal">
    /// Which of the month's days falling on <paramref name="Day" /> the token selects: <see cref="WeekOrdinal.First" />
    /// to <see cref="WeekOrdinal.Fifth" /> for <c>d#k</c>, or <see cref="WeekOrdinal.Last" /> for <c>dL</c>.
    /// </param>
    /// <remarks>
    /// The record's value equality is what <see cref="CronExpression.Equals(CronExpression)" /> compares. The weekday
    /// is a <see cref="DayOfWeek" />, so the cron weekday seven, Sunday, is stored as <see cref="DayOfWeek.Sunday" />.
    /// </remarks>
    private readonly record struct DayOfWeekToken(DayOfWeek Day, WeekOrdinal Ordinal)
    {
        /// <summary>
        /// Determines whether the token selects a day.
        /// </summary>
        /// <param name="day">An instant within the day.</param>
        /// <returns>
        /// <see langword="true" /> when the token selects the day; otherwise <see langword="false" />.
        /// </returns>
        /// <remarks>
        /// A fifth weekday that a month does not have selects nothing in that month.
        /// </remarks>
        internal bool Matches(DateTime day) =>
            day.IsNthDateOfWeekInMonth(Day, Ordinal);

        /// <summary>
        /// Returns the token's canonical text.
        /// </summary>
        /// <returns>The text, with the weekday as a number, Sunday as zero.</returns>
        internal string Format() =>
            Ordinal == WeekOrdinal.Last
                ? string.Create(CultureInfo.InvariantCulture, $"{(int)Day}L")
                : string.Create(CultureInfo.InvariantCulture, $"{(int)Day}#{(int)Ordinal}");
    }
}
