// ---------------------------------------------------------------------------------------------------------------
// <copyright file="LunarPhaseAlgorithm.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Algorithms;

/// <summary>
/// Provides internal astronomical helpers for computing new moon and full moon Julian Day Ephemerides (JDE).
/// </summary>
/// <remarks>
/// <para>
/// The algorithms are taken from Jean Meeus, <em>Astronomical Algorithms</em> (2nd ed.), Chapter 49. The accuracy is
/// approximately ±2 minutes of the lunar phase transition for years in the range 1900–2100, which translates to a date
/// accuracy of better than one calendar day.
/// </para>
/// <para>
/// All JDE values are in Terrestrial Dynamical Time (TDT). The difference between TDT and UTC (ΔT) is at most a few
/// minutes for years in the supported range and is neglected when rounding to the nearest calendar day.
/// </para>
/// </remarks>
internal static class LunarPhaseAlgorithm
{
    /// <summary>
    /// The J2000.0 epoch expressed as a <see cref="DateTime" /> (2000-01-01T12:00:00, kind unspecified), equal to JDE
    /// 2451545.0.
    /// </summary>
    private static readonly DateTime s_j2000Epoch = new(2000, 1, 1, 12, 0, 0, DateTimeKind.Unspecified);

    /// <summary>
    /// The mean synodic month length in days (IAU value), used to advance the lunation index between search attempts.
    /// </summary>
    private const double SynodicMonth = 29.530588861;

    /// <summary>
    /// Finds the first full moon that starts on or after <paramref name="notBefore" />.
    /// </summary>
    /// <param name="notBefore">The earliest acceptable date (inclusive).</param>
    /// <returns>The UTC date of the full moon, or <see langword="null" /> if none can be computed in range.</returns>
    internal static DateTime? GetFullMoonOnOrAfter(DateTime notBefore)
    {
        // Estimate the lunation index k for the notBefore date, biased toward full moons (k = n + 0.5).
        var k = EstimateK(notBefore.Year, notBefore.Month, fullMoon: true);

        for (var attempts = 0; attempts < 3; attempts++, k += 1.0)
        {
            var jde = ComputeLunarPhaseJde(k);
            DateTime candidate = JdeToDate(jde);

            if (candidate >= notBefore.Date)
                return candidate;
        }

        return null;
    }

    /// <summary>
    /// Finds the first new moon that starts on or after <paramref name="notBefore" />.
    /// </summary>
    /// <param name="notBefore">The earliest acceptable date (inclusive).</param>
    /// <returns>The UTC date of the new moon, or <see langword="null" /> if none can be computed in range.</returns>
    internal static DateTime? GetNewMoonOnOrAfter(DateTime notBefore)
    {
        var k = EstimateK(notBefore.Year, notBefore.Month, fullMoon: false);

        for (var attempts = 0; attempts < 3; attempts++, k += 1.0)
        {
            var jde = ComputeLunarPhaseJde(k);
            DateTime candidate = JdeToDate(jde);

            if (candidate >= notBefore.Date)
                return candidate;
        }

        return null;
    }

    /// <summary>
    /// Estimates the lunation index k near the given year and month. For full moons k is a half-integer (e.g. 0.5,
    /// 1.5); for new moons k is an integer.
    /// </summary>
    /// <param name="year">The Gregorian year used to anchor the estimate.</param>
    /// <param name="month">The Gregorian month (1–12) used to refine the estimate within the year.</param>
    /// <param name="fullMoon">
    /// <see langword="true" /> to bias toward a full-moon lunation index; <see langword="false" /> for a new-moon
    /// index.
    /// </param>
    /// <returns>The estimated lunation index k.</returns>
    private static double EstimateK(int year, int month, bool fullMoon)
    {
        var approxYear = year + (month - 1) / 12.0;
        var k = (approxYear - 2000.0) * 12.3685;
        k = fullMoon ? Math.Floor(k) + 0.5 : Math.Round(k);
        return k;
    }

    /// <summary>
    /// Computes the Julian Day Ephemeris of the lunar phase identified by <paramref name="k" /> using the Meeus Chapter
    /// 49 algorithm.
    /// </summary>
    /// <param name="k">
    /// Integer k for a new moon; k + 0.5 for a full moon; k + 0.25 / k + 0.75 for quarter moons.
    /// </param>
    /// <returns>The Julian Day Ephemeris (JDE) in Terrestrial Dynamical Time.</returns>
    private static double ComputeLunarPhaseJde(double k)
    {
        var T = k / 1236.85;
        var T2 = T * T;
        var T3 = T2 * T;
        var T4 = T2 * T2;

        // Mean JDE (Meeus eq. 49.1)
        var jde = 2451550.09766
            + SynodicMonth * k
            + 0.00015437 * T2
            - 0.000000150 * T3
            + 0.00000000073 * T4;

        var E = 1.0 - 0.002516 * T - 0.0000074 * T2;
        var E2 = E * E;

        // Mean anomalies and argument of latitude (degrees)
        var M = DegToRad(2.5534 + 29.10535670 * k - 0.0000014 * T2 - 0.00000011 * T3);
        var Mp = DegToRad(201.5643 + 385.81693528 * k + 0.0107582 * T2 + 0.00001238 * T3 - 0.000000058 * T4);
        var F = DegToRad(160.7108 + 390.67050284 * k - 0.0016118 * T2 - 0.00000227 * T3 + 0.000000011 * T4);
        var Om = DegToRad(124.7746 - 1.56375588 * k + 0.0020672 * T2 + 0.00000215 * T3);

        var isFullMoon = Math.Abs(k - Math.Floor(k) - 0.5) < 1e-9;

        if (isFullMoon)
        {
            // Full moon corrections (Meeus Table 49.b)
            jde += -0.40614 * Math.Sin(Mp)
                + 0.17302 * E * Math.Sin(M)
                + 0.01614 * Math.Sin(2 * Mp)
                + 0.01043 * Math.Sin(2 * F)
                + 0.00734 * E * Math.Sin(Mp - M)
                - 0.00515 * E * Math.Sin(Mp + M)
                + 0.00209 * E2 * Math.Sin(2 * M)
                - 0.00111 * Math.Sin(Mp - 2 * F)
                - 0.00057 * Math.Sin(Mp + 2 * F)
                + 0.00056 * E * Math.Sin(2 * Mp + M)
                - 0.00042 * Math.Sin(3 * Mp)
                + 0.00042 * E * Math.Sin(M + 2 * F)
                + 0.00038 * E * Math.Sin(M - 2 * F)
                - 0.00024 * E * Math.Sin(2 * Mp - M)
                - 0.00017 * Math.Sin(Om)
                - 0.00007 * Math.Sin(Mp + 2 * M)
                + 0.00004 * Math.Sin(2 * Mp - 2 * F)
                + 0.00004 * Math.Sin(3 * M)
                + 0.00003 * Math.Sin(Mp + M - 2 * F)
                + 0.00003 * Math.Sin(2 * Mp + 2 * F)
                - 0.00003 * Math.Sin(Mp + M + 2 * F)
                + 0.00003 * Math.Sin(Mp - M + 2 * F)
                - 0.00002 * Math.Sin(Mp - M - 2 * F)
                - 0.00002 * Math.Sin(3 * Mp + M)
                + 0.00002 * Math.Sin(4 * Mp);
        }
        else
        {
            // New moon corrections (Meeus Table 49.a)
            jde += -0.40720 * Math.Sin(Mp)
                + 0.17241 * E * Math.Sin(M)
                + 0.01608 * Math.Sin(2 * Mp)
                + 0.01039 * Math.Sin(2 * F)
                + 0.00739 * E * Math.Sin(Mp - M)
                - 0.00514 * E * Math.Sin(Mp + M)
                + 0.00208 * E2 * Math.Sin(2 * M)
                - 0.00111 * Math.Sin(Mp - 2 * F)
                - 0.00057 * Math.Sin(Mp + 2 * F)
                + 0.00056 * E * Math.Sin(2 * Mp + M)
                - 0.00042 * Math.Sin(3 * Mp)
                + 0.00042 * E * Math.Sin(M + 2 * F)
                + 0.00038 * E * Math.Sin(M - 2 * F)
                - 0.00024 * E * Math.Sin(2 * Mp - M)
                - 0.00017 * Math.Sin(Om)
                - 0.00007 * Math.Sin(Mp + 2 * M)
                + 0.00004 * Math.Sin(2 * Mp - 2 * F)
                + 0.00004 * Math.Sin(3 * M)
                + 0.00003 * Math.Sin(Mp + M - 2 * F)
                + 0.00003 * Math.Sin(2 * Mp + 2 * F)
                - 0.00003 * Math.Sin(Mp + M + 2 * F)
                + 0.00003 * Math.Sin(Mp - M + 2 * F)
                - 0.00002 * Math.Sin(Mp - M - 2 * F)
                - 0.00002 * Math.Sin(3 * Mp + M)
                + 0.00002 * Math.Sin(4 * Mp);
        }

        // Additional correction W (Meeus eq. 49.2)
        var W = 0.000325 * Math.Sin(DegToRad(299.77 + 0.107408 * k - 0.009173 * T2))
            + 0.000165 * Math.Sin(DegToRad(251.88 + 0.016321 * k))
            + 0.000164 * Math.Sin(DegToRad(251.83 + 26.651886 * k))
            + 0.000126 * Math.Sin(DegToRad(349.42 + 36.412478 * k))
            + 0.000110 * Math.Sin(DegToRad(84.66 + 18.206239 * k))
            + 0.000062 * Math.Sin(DegToRad(141.74 + 53.303771 * k))
            + 0.000060 * Math.Sin(DegToRad(207.14 + 2.453732 * k))
            + 0.000056 * Math.Sin(DegToRad(154.84 + 7.306860 * k))
            + 0.000047 * Math.Sin(DegToRad(34.52 + 27.261239 * k))
            + 0.000042 * Math.Sin(DegToRad(207.19 + 0.121824 * k))
            + 0.000040 * Math.Sin(DegToRad(291.34 + 1.844379 * k))
            + 0.000037 * Math.Sin(DegToRad(161.72 + 24.198154 * k))
            + 0.000035 * Math.Sin(DegToRad(239.56 + 25.513099 * k))
            + 0.000023 * Math.Sin(DegToRad(331.55 + 3.592518 * k));

        return jde + W;
    }

    /// <summary>
    /// Converts a Julian Day Ephemeris to a <see cref="DateTime" /> date.
    /// </summary>
    /// <param name="jde">The Julian Day Ephemeris in Terrestrial Dynamical Time.</param>
    /// <returns>
    /// The corresponding day-only <see cref="DateTime" /> with <see cref="DateTime.Kind" /> set to
    /// <see cref="DateTimeKind.Unspecified" />.
    /// </returns>
    private static DateTime JdeToDate(double jde)
    {
        DateTime raw = s_j2000Epoch.AddDays(jde - 2451545.0);
        return DateTime.SpecifyKind(raw.Date, DateTimeKind.Unspecified);
    }

    /// <summary>
    /// Converts an angle expressed in degrees to radians.
    /// </summary>
    /// <param name="degrees">The angle, in degrees.</param>
    /// <returns>The equivalent angle in radians.</returns>
    private static double DegToRad(double degrees) =>
        degrees * (Math.PI / 180.0);
}
