// ---------------------------------------------------------------------------------------------------------------
// <copyright file="WorkingDaysOfWeek.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

/// <summary>
/// Represents a named pattern of working days within a seven-day week.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="WorkingDaysOfWeek" /> enumerates the commonly observed working-week patterns across global regions.
/// Each named value maps to a canonical <see cref="WeekPattern" /> preset and is interchangeable with the bitmask
/// representation through the conversion extension methods on <see cref="Bodu.Extensions.WorkingDaysOfWeekExtensions" />.
/// </para>
/// <para>
/// Use a named value when the working week is one of the listed patterns; use <see cref="Custom" /> together with a
/// caller-supplied <see cref="WeekPattern" /> when the working week is non-standard (for example a four-day week or
/// an unevenly distributed schedule).
/// </para>
/// </remarks>
public enum WorkingDaysOfWeek
{
    /// <summary>
    /// Indicates a Monday through to Friday working week (Western standard).
    /// </summary>
    MondayToFriday = 0,

    /// <summary>
    /// Indicates a Monday through to Saturday working week.
    /// </summary>
    MondayToSaturday = 1,

    /// <summary>
    /// Indicates a Monday through to Thursday, and Saturday working week.
    /// </summary>
    MondayToThursdayAndSaturday = 2,

    /// <summary>
    /// Indicates a Saturday through to Thursday working week (e.g. parts of the Middle East).
    /// </summary>
    SaturdayToThursday = 3,

    /// <summary>
    /// Indicates a Saturday through to Wednesday working week.
    /// </summary>
    SaturdayToWednesday = 4,

    /// <summary>
    /// Indicates a Sunday through to Friday working week.
    /// </summary>
    SundayToFriday = 5,

    /// <summary>
    /// Indicates a Sunday through to Thursday working week (e.g. Israel, parts of the Middle East).
    /// </summary>
    SundayToThursday = 6,

    /// <summary>
    /// Indicates a non-standard working week whose pattern is supplied externally via a <see cref="WeekPattern" />.
    /// </summary>
    Custom = 7,
}
