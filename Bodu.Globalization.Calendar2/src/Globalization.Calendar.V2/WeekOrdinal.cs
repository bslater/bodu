// ---------------------------------------------------------------------------------------------------------------
// <copyright file="WeekOrdinal.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.V2;

/// <summary>
/// Identifies which occurrence of a weekday within a month a strategy selects.
/// </summary>
public enum WeekOrdinal
{
    /// <summary>
    /// The first occurrence of the weekday in the month.
    /// </summary>
    First = 1,

    /// <summary>
    /// The second occurrence of the weekday in the month.
    /// </summary>
    Second = 2,

    /// <summary>
    /// The third occurrence of the weekday in the month.
    /// </summary>
    Third = 3,

    /// <summary>
    /// The fourth occurrence of the weekday in the month.
    /// </summary>
    Fourth = 4,

    /// <summary>
    /// The fifth occurrence of the weekday in the month, which does not exist in every month.
    /// </summary>
    Fifth = 5,

    /// <summary>
    /// The last occurrence of the weekday in the month, whether that is the fourth or the fifth.
    /// </summary>
    Last = 100,
}
