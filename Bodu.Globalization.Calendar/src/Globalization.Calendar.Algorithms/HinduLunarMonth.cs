// ---------------------------------------------------------------------------------------------------------------
// <copyright file="HinduLunarMonth.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Algorithms;

/// <summary>
/// Identifies a month in the Hindu lunisolar calendar by its conventional Sanskrit name.
/// </summary>
/// <remarks>
/// <para>
/// Hindu months are lunar months (beginning at the new moon in the Amanta / new-moon reckoning, or at the full moon in
/// the Purnimanta / full-moon reckoning used in North India). The months are numbered here in the order they fall in
/// the Gregorian calendar year, starting from Chaitra (March/April). The actual Gregorian date of a month's start
/// varies year to year because the Hindu lunisolar calendar adds an intercalary (adhika) month approximately every 32
/// months to keep pace with the solar year.
/// </para>
/// <para>
/// The correspondence between Hindu months and Gregorian months is approximate.
/// <see cref="HinduLunarNotableDateAlgorithm" /> resolves the actual Gregorian date using astronomical calculation.
/// </para>
/// </remarks>
public enum HinduLunarMonth
{
    /// <summary>
    /// The first lunar month (approximately March/April). Associated with the Chaitra Navaratri.
    /// </summary>
    Chaitra = 1,

    /// <summary>
    /// The second lunar month (approximately April/May).
    /// </summary>
    Vaisakha = 2,

    /// <summary>
    /// The third lunar month (approximately May/June).
    /// </summary>
    Jyeshtha = 3,

    /// <summary>
    /// The fourth lunar month (approximately June/July).
    /// </summary>
    Ashadha = 4,

    /// <summary>
    /// The fifth lunar month (approximately July/August). Associated with Raksha Bandhan.
    /// </summary>
    Shravana = 5,

    /// <summary>
    /// The sixth lunar month (approximately August/September). Associated with Janmashtami and Ganesh Chaturthi.
    /// </summary>
    Bhadrapada = 6,

    /// <summary>
    /// The seventh lunar month (approximately September/October). Associated with Navaratri and Dussehra.
    /// </summary>
    Ashvin = 7,

    /// <summary>
    /// The eighth lunar month (approximately October/November). Associated with Diwali.
    /// </summary>
    Kartik = 8,

    /// <summary>
    /// The ninth lunar month (approximately November/December).
    /// </summary>
    Margashirsha = 9,

    /// <summary>
    /// The tenth lunar month (approximately December/January).
    /// </summary>
    Pausha = 10,

    /// <summary>
    /// The eleventh lunar month (approximately January/February).
    /// </summary>
    Magha = 11,

    /// <summary>
    /// The twelfth lunar month (approximately February/March). Associated with Holi (Purnima of Phalguna).
    /// </summary>
    Phalguna = 12,
}
