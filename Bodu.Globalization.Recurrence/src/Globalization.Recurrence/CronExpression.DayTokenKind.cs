// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CronExpression.DayTokenKind.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Recurrence;

public sealed partial class CronExpression
{
    /// <summary>
    /// Identifies the Quartz token a day field holds in place of a list of values.
    /// </summary>
    private enum DayTokenKind
    {
        /// <summary>
        /// The field holds values rather than a token.
        /// </summary>
        None,

        /// <summary>
        /// <c>L</c> or <c>L-n</c>: the last day of the month, or the day n days before it.
        /// </summary>
        LastDayOfMonth,

        /// <summary>
        /// <c>LW</c> or <c>L-nW</c>: the weekday nearest the last day of the month, or the day n days before it.
        /// </summary>
        WeekdayNearestLastDay,

        /// <summary>
        /// <c>nW</c>: the weekday nearest the n-th day of the month, without leaving the month.
        /// </summary>
        WeekdayNearestDay,

        /// <summary>
        /// <c>dL</c>: the last day of the month that falls on weekday d.
        /// </summary>
        LastWeekdayOfMonth,

        /// <summary>
        /// <c>d#k</c>: the k-th day of the month that falls on weekday d.
        /// </summary>
        NthWeekdayOfMonth,
    }
}
