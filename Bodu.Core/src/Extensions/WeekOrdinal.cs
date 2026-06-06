// ---------------------------------------------------------------------------------------------------------------
// <copyright file="WeekOrdinal.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

/// <summary>
/// Specifies the ordinal position of a weekday within a given month. Commonly used in recurrence rules, such as "the
/// second Tuesday of the month".
/// </summary>
public enum WeekOrdinal
{
    /// <summary>
    /// Represents the first occurrence of a specific weekday in the month (e.g., the 1st Monday).
    /// </summary>
    First = 1,

    /// <summary>
    /// Represents the second occurrence of a specific weekday in the month (e.g., the 2nd Monday).
    /// </summary>
    Second = 2,

    /// <summary>
    /// Represents the third occurrence of a specific weekday in the month (e.g., the 3rd Monday).
    /// </summary>
    Third = 3,

    /// <summary>
    /// Represents the fourth occurrence of a specific weekday in the month (e.g., the 4th Monday).
    /// </summary>
    Fourth = 4,

    /// <summary>
    /// Represents the fifth occurrence of a specific weekday in the month (e.g., the 5th Monday).
    /// <note type="important">This is relatively rare and only occurs in months where five instances of the specified
    /// weekday exist.</note>
    /// </summary>
    Fifth = 5,

    /// <summary>
    /// Represents the last occurrence of a specific weekday in the month (e.g., the last Monday).
    /// </summary>
    Last = 6,
}
