// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CalendarWeekendDefinition.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

/// <summary>
/// Represents common weekend configurations used across global regions and cultures.
/// </summary>
public enum CalendarWeekendDefinition
{
    /// <summary>
    /// Saturday and Sunday are considered weekends (used in most Western countries).
    /// </summary>
    SaturdaySunday = 0,

    /// <summary>
    /// Friday and Saturday are weekends (used in many Middle Eastern countries).
    /// </summary>
    FridaySaturday = 1,

    /// <summary>
    /// Thursday and Friday are weekends (used in some Middle Eastern academic/institutional contexts).
    /// </summary>
    ThursdayFriday = 2,

    /// <summary>
    /// Friday only is considered the weekend (e.g., traditional Islamic week).
    /// </summary>
    FridayOnly = 3,

    /// <summary>
    /// Sunday only is considered the weekend (used historically and in some religious institutions).
    /// </summary>
    SundayOnly = 4,

    /// <summary>
    /// No standard weekend is defined (all days may be working or off depending on context).
    /// </summary>
    None = 5,

    /// <summary>
    /// Custom definition provided via an <see cref="IWeekendDefinitionProvider" />.
    /// </summary>
    Custom = 6,
}