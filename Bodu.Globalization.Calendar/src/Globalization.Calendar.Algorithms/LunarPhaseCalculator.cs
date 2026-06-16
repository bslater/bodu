// ---------------------------------------------------------------------------------------------------------------
// <copyright file="LunarPhaseCalculator.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Algorithms;

/// <summary>
/// Computes the dates of new and full moons using the truncated lunar-phase series from Jean Meeus, <em>Astronomical
/// Algorithms</em> (chapter 49).
/// </summary>
/// <remarks>
/// <para>
/// Dates are returned in Universal Time and are accurate to within a day for the modern era. The series is evaluated
/// for the lunation index nearest the search date and advanced until the computed phase falls on or after it.
/// </para>
/// </remarks>
internal static class LunarPhaseCalculator
{
    /// <summary>
    /// The mean length of a synodic month, in days.
    /// </summary>
    private const double SynodicMonth = 29.530588861;

    /// <summary>
    /// The Julian Ephemeris Day of the J2000.0 epoch.
    /// </summary>
    private const double J2000JulianDay = 2451545.0;

    /// <summary>
    /// The J2000.0 epoch as a calendar instant (1 January 2000, 12:00 UT).
    /// </summary>
    private static readonly DateTime s_j2000Epoch = new(2000, 1, 1, 12, 0, 0, DateTimeKind.Unspecified);

    /// <summary>
    /// Returns the date of the first full moon falling on or after the supplied date.
    /// </summary>
    /// <param name="notBefore">The earliest acceptable date.</param>
    /// <returns>The full-moon date, or <see langword="null" /> when none is found within the search window.</returns>
    public static DateOnly? FullMoonOnOrAfter(DateOnly notBefore) =>
        PhaseOnOrAfter(notBefore, fullMoon: true);

    /// <summary>
    /// Returns the date of the first new moon falling on or after the supplied date.
    /// </summary>
    /// <param name="notBefore">The earliest acceptable date.</param>
    /// <returns>The new-moon date, or <see langword="null" /> when none is found within the search window.</returns>
    public static DateOnly? NewMoonOnOrAfter(DateOnly notBefore) =>
        PhaseOnOrAfter(notBefore, fullMoon: false);

    /// <summary>
    /// Advances the lunation index from an initial estimate until the computed phase falls on or after the search date.
    /// </summary>
    /// <param name="notBefore">The earliest acceptable date.</param>
    /// <param name="fullMoon">
    /// <see langword="true" /> to find a full moon; <see langword="false" /> for a new moon.
    /// </param>
    /// <returns>The phase date, or <see langword="null" /> when none is found within the search window.</returns>
    private static DateOnly? PhaseOnOrAfter(DateOnly notBefore, bool fullMoon)
    {
        double k = EstimateK(notBefore.Year, notBefore.Month, fullMoon);

        for (int attempts = 0; attempts < 3; attempts++, k += 1.0)
        {
            DateOnly candidate = JulianDayToDate(ComputeLunarPhaseJulianDay(k, fullMoon));
            if (candidate >= notBefore)
                return candidate;
        }

        return null;
    }

    /// <summary>
    /// Estimates the lunation index for a given year and month, biased toward full moons when requested.
    /// </summary>
    /// <param name="year">The Gregorian year of the search date.</param>
    /// <param name="month">The one-based month of the search date.</param>
    /// <param name="fullMoon"><see langword="true" /> to bias toward full moons (k = n + 0.5).</param>
    /// <returns>The estimated lunation index.</returns>
    private static double EstimateK(int year, int month, bool fullMoon)
    {
        double approxYear = year + ((month - 1) / 12.0);
        double k = (approxYear - 2000.0) * 12.3685;

        return fullMoon ? Math.Floor(k) + 0.5 : Math.Round(k);
    }

    /// <summary>
    /// Evaluates the Meeus chapter-49 series for the Julian Ephemeris Day of the phase at lunation index
    /// <paramref name="k" />.
    /// </summary>
    /// <param name="k">The lunation index (integral for a new moon, half-integral for a full moon).</param>
    /// <param name="fullMoon">
    /// <see langword="true" /> to apply the full-moon corrections; otherwise the new-moon corrections.
    /// </param>
    /// <returns>The Julian Ephemeris Day of the phase.</returns>
    private static double ComputeLunarPhaseJulianDay(double k, bool fullMoon)
    {
        double t = k / 1236.85;
        double t2 = t * t;
        double t3 = t2 * t;
        double t4 = t2 * t2;

        double jde = 2451550.09766
            + (SynodicMonth * k)
            + (0.00015437 * t2)
            - (0.000000150 * t3)
            + (0.00000000073 * t4);

        double e = 1.0 - (0.002516 * t) - (0.0000074 * t2);
        double e2 = e * e;

        double m = DegreesToRadians(2.5534 + (29.10535670 * k) - (0.0000014 * t2) - (0.00000011 * t3));
        double mp = DegreesToRadians(201.5643 + (385.81693528 * k) + (0.0107582 * t2) + (0.00001238 * t3) - (0.000000058 * t4));
        double f = DegreesToRadians(160.7108 + (390.67050284 * k) - (0.0016118 * t2) - (0.00000227 * t3) + (0.000000011 * t4));
        double omega = DegreesToRadians(124.7746 - (1.56375588 * k) + (0.0020672 * t2) + (0.00000215 * t3));

        double correction = fullMoon
            ? (-0.40614 * Math.Sin(mp))
                + (0.17302 * e * Math.Sin(m))
                + (0.01614 * Math.Sin(2 * mp))
                + (0.01043 * Math.Sin(2 * f))
                + (0.00734 * e * Math.Sin(mp - m))
                - (0.00515 * e * Math.Sin(mp + m))
                + (0.00209 * e2 * Math.Sin(2 * m))
                - (0.00111 * Math.Sin(mp - (2 * f)))
                - (0.00057 * Math.Sin(mp + (2 * f)))
                + (0.00056 * e * Math.Sin((2 * mp) + m))
                - (0.00042 * Math.Sin(3 * mp))
                + (0.00042 * e * Math.Sin(m + (2 * f)))
                + (0.00038 * e * Math.Sin(m - (2 * f)))
                - (0.00024 * e * Math.Sin((2 * mp) - m))
                - (0.00017 * Math.Sin(omega))
                - (0.00007 * Math.Sin(mp + (2 * m)))
                + (0.00004 * Math.Sin((2 * mp) - (2 * f)))
                + (0.00004 * Math.Sin(3 * m))
                + (0.00003 * Math.Sin(mp + m - (2 * f)))
                + (0.00003 * Math.Sin((2 * mp) + (2 * f)))
                - (0.00003 * Math.Sin(mp + m + (2 * f)))
                + (0.00003 * Math.Sin(mp - m + (2 * f)))
                - (0.00002 * Math.Sin(mp - m - (2 * f)))
                - (0.00002 * Math.Sin((3 * mp) + m))
                + (0.00002 * Math.Sin(4 * mp))
            : (-0.40720 * Math.Sin(mp))
                + (0.17241 * e * Math.Sin(m))
                + (0.01608 * Math.Sin(2 * mp))
                + (0.01039 * Math.Sin(2 * f))
                + (0.00739 * e * Math.Sin(mp - m))
                - (0.00514 * e * Math.Sin(mp + m))
                + (0.00208 * e2 * Math.Sin(2 * m))
                - (0.00111 * Math.Sin(mp - (2 * f)))
                - (0.00057 * Math.Sin(mp + (2 * f)))
                + (0.00056 * e * Math.Sin((2 * mp) + m))
                - (0.00042 * Math.Sin(3 * mp))
                + (0.00042 * e * Math.Sin(m + (2 * f)))
                + (0.00038 * e * Math.Sin(m - (2 * f)))
                - (0.00024 * e * Math.Sin((2 * mp) - m))
                - (0.00017 * Math.Sin(omega))
                - (0.00007 * Math.Sin(mp + (2 * m)))
                + (0.00004 * Math.Sin((2 * mp) - (2 * f)))
                + (0.00004 * Math.Sin(3 * m))
                + (0.00003 * Math.Sin(mp + m - (2 * f)))
                + (0.00003 * Math.Sin((2 * mp) + (2 * f)))
                - (0.00003 * Math.Sin(mp + m + (2 * f)))
                + (0.00003 * Math.Sin(mp - m + (2 * f)))
                - (0.00002 * Math.Sin(mp - m - (2 * f)))
                - (0.00002 * Math.Sin((3 * mp) + m))
                + (0.00002 * Math.Sin(4 * mp));

        double w = (0.000325 * Math.Sin(DegreesToRadians(299.77 + (0.107408 * k) - (0.009173 * t2))))
            + (0.000165 * Math.Sin(DegreesToRadians(251.88 + (0.016321 * k))))
            + (0.000164 * Math.Sin(DegreesToRadians(251.83 + (26.651886 * k))))
            + (0.000126 * Math.Sin(DegreesToRadians(349.42 + (36.412478 * k))))
            + (0.000110 * Math.Sin(DegreesToRadians(84.66 + (18.206239 * k))))
            + (0.000062 * Math.Sin(DegreesToRadians(141.74 + (53.303771 * k))))
            + (0.000060 * Math.Sin(DegreesToRadians(207.14 + (2.453732 * k))))
            + (0.000056 * Math.Sin(DegreesToRadians(154.84 + (7.306860 * k))))
            + (0.000047 * Math.Sin(DegreesToRadians(34.52 + (27.261239 * k))))
            + (0.000042 * Math.Sin(DegreesToRadians(207.19 + (0.121824 * k))))
            + (0.000040 * Math.Sin(DegreesToRadians(291.34 + (1.844379 * k))))
            + (0.000037 * Math.Sin(DegreesToRadians(161.72 + (24.198154 * k))))
            + (0.000035 * Math.Sin(DegreesToRadians(239.56 + (25.513099 * k))))
            + (0.000023 * Math.Sin(DegreesToRadians(331.55 + (3.592518 * k))));

        return jde + correction + w;
    }

    /// <summary>
    /// Converts a Julian Ephemeris Day to its Universal-Time calendar date.
    /// </summary>
    /// <param name="julianDay">The Julian Ephemeris Day.</param>
    /// <returns>The corresponding date.</returns>
    private static DateOnly JulianDayToDate(double julianDay) =>
        DateOnly.FromDateTime(s_j2000Epoch.AddDays(julianDay - J2000JulianDay));

    /// <summary>
    /// Converts an angle from degrees to radians.
    /// </summary>
    /// <param name="degrees">The angle in degrees.</param>
    /// <returns>The angle in radians.</returns>
    private static double DegreesToRadians(double degrees) =>
        degrees * (Math.PI / 180.0);
}
