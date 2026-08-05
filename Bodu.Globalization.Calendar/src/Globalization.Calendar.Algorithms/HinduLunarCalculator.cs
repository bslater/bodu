// ---------------------------------------------------------------------------------------------------------------
// <copyright file="HinduLunarCalculator.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Algorithms;

/// <summary>
/// Approximates the Gregorian date of a Hindu lunisolar festival from the amanta lunar month, fortnight, and lunar day
/// (tithi) it falls on, using the new- and full-moon series of <see cref="LunarPhaseCalculator" /> anchored to the
/// sun's sidereal sign.
/// </summary>
/// <remarks>
/// <para>
/// The amanta month a new moon begins is identified by the sidereal zodiac sign the sun occupies at that new moon, so a
/// festival's lunation is selected by solar position rather than by a fixed Gregorian search month. A leap month
/// (adhika maasa) — two consecutive new moons in the same sign — is detected and the festival is taken in the true
/// (nija) month, so intercalary years resolve correctly.
/// </para>
/// <para>
/// Although the supported keys are Hindu in origin, the same panchanga arithmetic resolves the lunisolar dates of
/// observances in other Indian-origin traditions that fall on a shared tithi, such as the Buddhist Asalha Puja (Asadha
/// purnima) and the Jain Maun Agiyaras (Margashirsha shukla ekadashi).
/// </para>
/// <para>
/// Results are accurate to within a day or two for the modern era. The Lahiri ayanamsa and the mean tithi length are
/// approximations, so a festival whose tithi begins very near sunrise can differ by a day from a published panchanga.
/// Festivals fixed by a nakshatra rather than a tithi (for example Onam) are not modelled here.
/// </para>
/// </remarks>
internal static class HinduLunarCalculator
{
    /// <summary>The mean length of a tithi (one thirtieth of a synodic month), in days.</summary>
    private const double TithiDays = 29.530588861 / 30.0;

    /// <summary>The festival coordinates keyed by algorithm key: the amanta lunar month (1 = Chaitra to 12 = Phalguna), the offset in tithis from that month's new moon, and whether the festival is the full moon (Purnima) of the month.</summary>
    /// <remarks>
    /// <para>
    /// Festivals named in the purnimanta (northern) convention for the dark fortnight are expressed here in the
    /// equivalent amanta month: a purnimanta "Month X dark fortnight" is the amanta month <c>X - 1</c>. The offset for
    /// a bright-fortnight (shukla) tithi <c>T</c> is <c>T - 1</c>; for a dark-fortnight (krishna) tithi <c>T</c> it is
    /// <c>14 + T</c>, so the closing new moon (krishna 15) is offset twenty-nine.
    /// </para>
    /// </remarks>
    private static readonly Dictionary<string, (int Month, int Offset, bool Purnima)> s_festivals = new(StringComparer.Ordinal)
    {
        ["ram-navami"] = (1, 8, false),        // Chaitra shukla 9.
        ["raksha-bandhan"] = (5, 0, true),     // Shravana purnima.
        ["janmashtami"] = (5, 22, false),      // Amanta Shravana krishna 8 (purnimanta Bhadrapada).
        ["ganesh-chaturthi"] = (6, 3, false),  // Bhadrapada shukla 4.
        ["navaratri"] = (7, 0, false),         // Ashvin shukla 1.
        ["dussehra"] = (7, 9, false),          // Ashvin shukla 10.
        ["karva-chauth"] = (7, 18, false),     // Amanta Ashvin krishna 4 (purnimanta Kartik).
        ["diwali"] = (8, 0, false),            // Kartik new moon (purnimanta Kartik krishna 15 / amavasya).
        ["vasant-panchami"] = (11, 4, false),  // Magha shukla 5.
        ["maha-shivaratri"] = (11, 28, false), // Amanta Magha krishna 14 (purnimanta Phalguna).
        ["holi"] = (12, 0, true),              // Phalguna purnima.

        // Lunisolar coordinates shared with other Indian-origin traditions, resolved by the same panchanga arithmetic.
        ["asalha-puja"] = (4, 0, true),        // Asadha purnima (Buddhist Asalha Puja / Dhamma Day).
        ["maun-agiyaras"] = (9, 10, false),    // Margashirsha shukla 11 (Jain Maun Agiyaras / Maun Ekadashi).
    };

    /// <summary>
    /// Gets the algorithm keys of every recognized Hindu lunisolar festival.
    /// </summary>
    /// <value>The recognized festival keys.</value>
    public static IReadOnlyCollection<string> FestivalKeys =>
        s_festivals.Keys;

    /// <summary>
    /// Computes the approximate Gregorian date of a recognized festival for the supplied year.
    /// </summary>
    /// <param name="key">The festival algorithm key.</param>
    /// <param name="year">The Gregorian year.</param>
    /// <returns>The festival date, or <see langword="null" /> when the key is unknown or no date is found.</returns>
    public static DateOnly? Resolve(string key, int year)
    {
        if (!s_festivals.TryGetValue(key, out (int Month, int Offset, bool Purnima) festival))
            return null;

        return Compute(festival.Month, festival.Offset, festival.Purnima, year);
    }

    /// <summary>
    /// Computes a festival by locating the new moon that begins its amanta month and adding the festival's tithi
    /// offset.
    /// </summary>
    /// <param name="amantaMonth">The amanta lunar month, 1 (Chaitra) to 12 (Phalguna).</param>
    /// <param name="offsetTithis">The offset in tithis from the month's new moon.</param>
    /// <param name="purnima">Whether the festival is the full moon of the month.</param>
    /// <param name="year">The Gregorian year the festival should fall in.</param>
    /// <returns>The festival date, or <see langword="null" /> when no matching lunation is found.</returns>
    private static DateOnly? Compute(int amantaMonth, int offsetTithis, bool purnima, int year)
    {
        // The sun is in this sidereal sign at the new moon that begins the month (Chaitra new moon sun in Pisces).
        int targetSign = (((amantaMonth - 2) % 12) + 12) % 12;

        List<(DateOnly NewMoon, int Sign)> newMoons = GatherNewMoons(year);

        if (SelectFestival(newMoons, targetSign, offsetTithis, purnima, year) is DateOnly festival)
            return festival;

        // A new moon falling on the civil day of the solar ingress can read the previous sign when evaluated at the
        // start of its date even though the conjunction instant follows the ingress, making the month appear to
        // vanish (Magha 2029). Re-evaluate such boundary days at the following midnight before giving up.
        List<(DateOnly NewMoon, int Sign)> shifted = new(newMoons.Count);
        foreach ((DateOnly newMoon, int sign) in newMoons)
        {
            int nextDaySign = SiderealSunSign(newMoon.AddDays(1));
            shifted.Add((newMoon, sign == targetSign || nextDaySign == targetSign ? targetSign : sign));
        }

        return SelectFestival(shifted, targetSign, offsetTithis, purnima, year);
    }

    /// <summary>
    /// Selects the festival date from the sign-classified new moons, skipping an intercalary occurrence.
    /// </summary>
    /// <param name="newMoons">The chronological new moons paired with their solar signs.</param>
    /// <param name="targetSign">The sidereal sign identifying the amanta month's new moon.</param>
    /// <param name="offsetTithis">The offset in tithis from the month's new moon.</param>
    /// <param name="purnima">Whether the festival is the full moon of the month.</param>
    /// <param name="year">The Gregorian year the festival should fall in.</param>
    /// <returns>The festival date, or <see langword="null" /> when no matching lunation lands in the year.</returns>
    private static DateOnly? SelectFestival(List<(DateOnly NewMoon, int Sign)> newMoons, int targetSign, int offsetTithis, bool purnima, int year)
    {
        for (int i = 0; i < newMoons.Count; i++)
        {
            if (newMoons[i].Sign != targetSign)
                continue;

            // Two consecutive new moons in the same sign make the first an intercalary (adhika) month; the festival
            // falls in the second (nija) month, so skip the adhika occurrence.
            if (i + 1 < newMoons.Count && newMoons[i + 1].Sign == targetSign)
                continue;

            DateOnly festival = purnima
                ? LunarPhaseCalculator.FullMoonOnOrAfter(newMoons[i].NewMoon) ?? newMoons[i].NewMoon
                : newMoons[i].NewMoon.AddDays((int)Math.Round(offsetTithis * TithiDays));

            if (festival.Year == year)
                return festival;
        }

        return null;
    }

    /// <summary>
    /// Gathers the new moons spanning the requested Gregorian year, each paired with the sidereal zodiac sign the sun
    /// occupies at that new moon.
    /// </summary>
    /// <param name="year">
    /// The Gregorian year, scanned with a margin either side to cover festivals near the boundaries.
    /// </param>
    /// <returns>The chronological new moons and their solar signs.</returns>
    /// <remarks>
    /// The sign is evaluated at the conjunction instant, not at the start of the civil day: a new moon falling on a
    /// sidereal ingress day otherwise reads the pre-ingress sign, which pairs it with its neighbouring month and
    /// fabricates a phantom adhika month one lunation early (Chaitra 1991/2010, Kartika 2009).
    /// </remarks>
    private static List<(DateOnly NewMoon, int Sign)> GatherNewMoons(int year)
    {
        List<(DateOnly NewMoon, int Sign)> newMoons = new();

        DateOnly cursor = new(Math.Max(1, year - 1), 11, 1);
        DateOnly limit = new(Math.Min(9999, year + 1), 3, 1);

        while (cursor < limit)
        {
            if (LunarPhaseCalculator.NewMoonJulianDayOnOrAfter(cursor) is not double julianDay)
                break;

            DateOnly newMoon = LunarPhaseCalculator.DateOfJulianDay(julianDay);
            newMoons.Add((newMoon, SiderealSunSign(newMoon, julianDay)));
            cursor = newMoon.AddDays(1);
        }

        return newMoons;
    }

    /// <summary>
    /// Returns the sidereal zodiac sign of the sun on the supplied date, applying the Lahiri ayanamsa to the tropical
    /// longitude.
    /// </summary>
    /// <param name="date">The date to evaluate.</param>
    /// <returns>The zero-based sidereal sign, 0 (Aries) to 11 (Pisces).</returns>
    private static int SiderealSunSign(DateOnly date) =>
        SiderealSign(SolarTermCalculator.SunTropicalLongitude(date), date.Year);

    /// <summary>
    /// Returns the sidereal zodiac sign of the sun at the supplied Julian Day instant, applying the Lahiri ayanamsa to
    /// the tropical longitude.
    /// </summary>
    /// <param name="date">The calendar date containing the instant, used for the ayanamsa epoch.</param>
    /// <param name="julianDay">The Julian Day to evaluate, including the fractional time of day.</param>
    /// <returns>The zero-based sidereal sign, 0 (Aries) to 11 (Pisces).</returns>
    private static int SiderealSunSign(DateOnly date, double julianDay) =>
        SiderealSign(SolarTermCalculator.SunTropicalLongitude(julianDay), date.Year);

    /// <summary>
    /// Converts a tropical solar longitude to its sidereal sign under the Lahiri ayanamsa for the supplied epoch year.
    /// </summary>
    /// <param name="tropicalLongitude">The tropical ecliptic longitude in degrees.</param>
    /// <param name="year">The Gregorian year fixing the ayanamsa value.</param>
    /// <returns>The zero-based sidereal sign, 0 (Aries) to 11 (Pisces).</returns>
    private static int SiderealSign(double tropicalLongitude, int year)
    {
        double ayanamsa = 23.85 + (0.0139666 * (year - 2000));
        double sidereal = (((tropicalLongitude - ayanamsa) % 360.0) + 360.0) % 360.0;

        return (int)(sidereal / 30.0) % 12;
    }
}
