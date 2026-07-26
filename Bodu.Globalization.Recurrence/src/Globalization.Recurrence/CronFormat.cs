// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CronFormat.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Recurrence;

/// <summary>
/// Identifies the field layout of a <see cref="CronExpression" />.
/// </summary>
public enum CronFormat
{
    /// <summary>
    /// The five-field Vixie layout: minute, hour, day-of-month, month, day-of-week.
    /// </summary>
    Standard,

    /// <summary>
    /// The six-field layout that prepends a seconds field: second, minute, hour, day-of-month, month, day-of-week.
    /// </summary>
    WithSeconds,
}
