// ---------------------------------------------------------------------------------------------------------------
// <copyright file="InvalidDayOfMonthBehavior.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Algorithms;

/// <summary>
/// Specifies how a monthly day-of-month recurrence handles a month that does not contain the requested day, such as the
/// 31st in February.
/// </summary>
/// <seealso cref="MonthlyDayRecurrenceStrategy" />
public enum InvalidDayOfMonthBehavior
{
    /// <summary>
    /// A month that does not contain the requested day emits no occurrence.
    /// </summary>
    Skip = 0,

    /// <summary>
    /// A month that does not contain the requested day emits its last calendar day instead.
    /// </summary>
    UseLastDayOfMonth,
}
