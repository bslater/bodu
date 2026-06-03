// ---------------------------------------------------------------------------------------------------------------
// <copyright file="HinduLunarCalculator.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.V2;

/// <summary>
/// Approximates the Gregorian date of a Hindu lunisolar festival from its lunar month, fortnight (paksha), and lunar
/// day (tithi), using the new- and full-moon series of <see cref="LunarPhaseCalculator" />.
/// </summary>
/// <remarks>
/// <para>
/// The result is accurate to within a day or two for the modern era and may diverge in intercalary (adhika maasa)
/// years. Only festivals whose lunar coordinates are independently verified are exposed by key; regionally ambiguous or
/// non-lunar festivals (for example Onam) are intentionally omitted rather than shipped with uncertain dates.
/// </para>
/// </remarks>
internal static class HinduLunarCalculator
{
    /// <summary>
    /// The mean length of a tithi (one thirtieth of a synodic month), in days.
    /// </summary>
    private const double TithiDays = 29.530588861 / 30.0;

    /// <summary>
    /// The verified festival coordinates, keyed by algorithm key: the lunar month's Gregorian search month, whether the
    /// festival falls in the dark (Krishna) fortnight, and the one-based tithi.
    /// </summary>
    private static readonly Dictionary<string, (int SearchMonth, bool Krishna, int Tithi)> s_festivals = new(StringComparer.Ordinal)
    {
        ["holi"] = (2, false, 15),       // Phalguna, Shukla 15 (Purnima).
        ["navaratri"] = (9, false, 1),   // Ashvin, Shukla 1.
        ["diwali"] = (10, true, 15),     // Kartik, Krishna 15 (Amavasya).
    };

    /// <summary>
    /// Determines whether the supplied algorithm key names a verified Hindu festival.
    /// </summary>
    /// <param name="key">The algorithm key.</param>
    /// <returns><see langword="true" /> if the festival is recognized; otherwise <see langword="false" />.</returns>
    public static bool IsFestivalKey(string key) =>
        s_festivals.ContainsKey(key);

    /// <summary>
    /// Computes the approximate Gregorian date of a recognized festival for the supplied year.
    /// </summary>
    /// <param name="key">The festival algorithm key.</param>
    /// <param name="year">The Gregorian year.</param>
    /// <returns>The festival date, or <see langword="null" /> when the key is unknown or no date is found.</returns>
    public static DateOnly? Resolve(string key, int year)
    {
        if (!s_festivals.TryGetValue(key, out (int SearchMonth, bool Krishna, int Tithi) festival))
            return null;

        return Compute(festival.SearchMonth, festival.Krishna, festival.Tithi, year);
    }

    /// <summary>
    /// Computes the date of a tithi within a lunar month seeded near the supplied Gregorian search month.
    /// </summary>
    /// <param name="searchMonth">The Gregorian month near which the lunar month's new moon falls.</param>
    /// <param name="krishna"><see langword="true" /> for the dark fortnight (counted from the full moon).</param>
    /// <param name="tithi">The one-based lunar day.</param>
    /// <param name="year">The Gregorian year.</param>
    /// <returns>The computed date, or <see langword="null" /> when no lunation is found.</returns>
    private static DateOnly? Compute(int searchMonth, bool krishna, int tithi, int year)
    {
        DateOnly? monthNewMoon = LunarPhaseCalculator.NewMoonOnOrAfter(new DateOnly(year, searchMonth, 1));
        if (monthNewMoon is null)
            return null;

        if (!krishna)
            return monthNewMoon.Value.AddDays((int)Math.Round((tithi - 1) * TithiDays));

        DateOnly fullMoonApprox = monthNewMoon.Value.AddDays((int)Math.Round((15 * TithiDays) - 2));
        DateOnly? fullMoon = LunarPhaseCalculator.FullMoonOnOrAfter(fullMoonApprox);
        if (fullMoon is null)
            return null;

        return fullMoon.Value.AddDays((int)Math.Round((tithi - 1) * TithiDays));
    }
}
