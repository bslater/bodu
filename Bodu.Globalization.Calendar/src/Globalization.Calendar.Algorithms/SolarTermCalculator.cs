// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SolarTermCalculator.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Algorithms;

/// <summary>
/// Computes the March (vernal) and September (autumnal) equinoxes, and the derived Qingming solar term, using the
/// equinox/solstice series from Jean Meeus, <em>Astronomical Algorithms</em> (chapter 27).
/// </summary>
/// <remarks>
/// <para>
/// The instant is computed in Universal Time and shifted by a fixed local-time offset before the calendar date is
/// taken, because civil observances such as the Japanese equinox holidays and Qingming are fixed by the local date of
/// the astronomical event.
/// </para>
/// </remarks>
internal static class SolarTermCalculator
{
    /// <summary>The number of mean days the sun takes to traverse the fifteen degrees of ecliptic longitude between the vernal equinox and Qingming.</summary>
    private const double QingmingDegreeDays = 15.0 * 365.2422 / 360.0;

    /// <summary>The Julian Ephemeris Day of the J2000.0 epoch.</summary>
    private const double J2000JulianDay = 2451545.0;

    /// <summary>The J2000.0 epoch as a calendar instant (1 January 2000, 12:00 UT).</summary>
    private static readonly DateTime s_j2000Epoch = new(2000, 1, 1, 12, 0, 0, DateTimeKind.Unspecified);

    /// <summary>The periodic correction terms of Meeus table 27.c, as (amplitude, phase in degrees, rate in degrees per century).</summary>
    private static readonly (double Amplitude, double Phase, double Rate)[] s_correctionTerms =
    [
        (485, 324.96, 1934.136), (203, 337.23, 32964.467), (199, 342.08, 20.186), (182, 27.85, 445267.112),
        (156, 73.14, 45036.886), (136, 171.52, 22518.443), (77, 222.54, 65928.934), (74, 296.72, 3034.906),
        (70, 243.58, 9037.513), (58, 119.81, 33718.147), (52, 297.17, 150.678), (50, 21.02, 2281.226),
        (45, 247.54, 29929.562), (44, 325.15, 31555.956), (29, 60.93, 4443.417), (18, 155.12, 67555.328),
        (17, 288.79, 4562.452), (16, 198.04, 62894.029), (14, 199.76, 31557.381), (12, 95.39, 14577.848),
        (10, 69.15, 31556.017), (9, 293.38, 149.438), (9, 242.69, 9325.052), (8, 141.01, 31559.532),
        (7, 283.90, 23.000), (7, 306.92, 10977.079), (6, 233.02, 10447.357), (6, 262.55, 31441.677),
    ];

    /// <summary>
    /// Returns the local date of the March (vernal) equinox for the supplied year.
    /// </summary>
    /// <param name="year">The Gregorian year.</param>
    /// <param name="utcOffsetHours">The local-time offset from Universal Time, in hours.</param>
    /// <returns>The equinox date in the local time zone.</returns>
    public static DateOnly VernalEquinox(int year, double utcOffsetHours) =>
        JulianDayToLocalDate(ComputeEquinoxJulianDay(year, vernal: true), utcOffsetHours);

    /// <summary>
    /// Returns the local date of the September (autumnal) equinox for the supplied year.
    /// </summary>
    /// <param name="year">The Gregorian year.</param>
    /// <param name="utcOffsetHours">The local-time offset from Universal Time, in hours.</param>
    /// <returns>The equinox date in the local time zone.</returns>
    public static DateOnly AutumnalEquinox(int year, double utcOffsetHours) =>
        JulianDayToLocalDate(ComputeEquinoxJulianDay(year, vernal: false), utcOffsetHours);

    /// <summary>
    /// Returns the local date of Qingming (the solar term fifteen degrees of solar longitude past the vernal equinox).
    /// </summary>
    /// <param name="year">The Gregorian year.</param>
    /// <param name="utcOffsetHours">The local-time offset from Universal Time, in hours.</param>
    /// <returns>The Qingming date in the local time zone.</returns>
    public static DateOnly Qingming(int year, double utcOffsetHours) =>
        JulianDayToLocalDate(ComputeEquinoxJulianDay(year, vernal: true) + QingmingDegreeDays, utcOffsetHours);

    /// <summary>
    /// Returns the dynamical-time (Terrestrial Time) instant of the March (vernal) equinox for the supplied year.
    /// </summary>
    /// <param name="year">The Gregorian year.</param>
    /// <returns>The equinox instant in dynamical time, with an unspecified <see cref="DateTime.Kind" />.</returns>
    /// <remarks>
    /// <para>
    /// The Meeus series yields a Julian <em>Ephemeris</em> Day, so the returned instant is in dynamical time; callers
    /// needing Universal Time must subtract their own ΔT estimate before applying a time-zone offset.
    /// </para>
    /// </remarks>
    public static DateTime VernalEquinoxDynamicalInstant(int year) =>
        s_j2000Epoch.AddDays(ComputeEquinoxJulianDay(year, vernal: true) - J2000JulianDay);

    /// <summary>
    /// Returns the sun's apparent tropical ecliptic longitude at midnight Universal Time on the supplied date, using
    /// the low-precision solar series from Jean Meeus, <em>Astronomical Algorithms</em> (chapter 25).
    /// </summary>
    /// <param name="date">The date to evaluate the sun's position for.</param>
    /// <returns>The tropical ecliptic longitude in degrees, normalized to the range <c>[0, 360)</c>.</returns>
    public static double SunTropicalLongitude(DateOnly date) =>
        SunTropicalLongitude(1721425.5 + date.DayNumber);

    /// <summary>
    /// Returns the sun's apparent tropical ecliptic longitude at the supplied Julian Day instant, using the
    /// low-precision solar series from Jean Meeus, <em>Astronomical Algorithms</em> (chapter 25).
    /// </summary>
    /// <param name="julianDay">The Julian Day to evaluate the sun's position at, including any fractional time of day.</param>
    /// <returns>The tropical ecliptic longitude in degrees, normalized to the range <c>[0, 360)</c>.</returns>
    public static double SunTropicalLongitude(double julianDay)
    {
        double t = (julianDay - J2000JulianDay) / 36525.0;
        double meanLongitude = 280.46646 + (36000.76983 * t) + (0.0003032 * t * t);
        double meanAnomaly = DegreesToRadians(357.52911 + (35999.05029 * t) - (0.0001537 * t * t));

        double equationOfCentre =
            ((1.914602 - (0.004817 * t) - (0.000014 * t * t)) * Math.Sin(meanAnomaly))
            + ((0.019993 - (0.000101 * t)) * Math.Sin(2.0 * meanAnomaly))
            + (0.000289 * Math.Sin(3.0 * meanAnomaly));

        double longitude = (meanLongitude + equationOfCentre) % 360.0;
        return longitude < 0.0 ? longitude + 360.0 : longitude;
    }

    /// <summary>
    /// Computes the Julian Ephemeris Day of an equinox using the Meeus mean polynomial plus the periodic correction.
    /// </summary>
    /// <param name="year">The Gregorian year.</param>
    /// <param name="vernal">
    /// <see langword="true" /> for the March equinox; <see langword="false" /> for September.
    /// </param>
    /// <returns>The Julian Ephemeris Day of the equinox.</returns>
    private static double ComputeEquinoxJulianDay(int year, bool vernal)
    {
        double y = (year - 2000) / 1000.0;

        double jde0 = vernal
            ? 2451623.80984 + (365242.37404 * y) + (0.05169 * y * y) - (0.00411 * y * y * y) - (0.00057 * y * y * y * y)
            : 2451810.21715 + (365242.01767 * y) - (0.11575 * y * y) + (0.00337 * y * y * y) + (0.00078 * y * y * y * y);

        double t = (jde0 - J2000JulianDay) / 36525.0;
        double w = DegreesToRadians((35999.373 * t) - 2.47);
        double lambda = 1.0 + (0.0334 * Math.Cos(w)) + (0.0007 * Math.Cos(2.0 * w));

        double s = 0.0;
        foreach ((double amplitude, double phase, double rate) in s_correctionTerms)
            s += amplitude * Math.Cos(DegreesToRadians(phase + (rate * t)));

        return jde0 + (0.00001 * s / lambda);
    }

    /// <summary>
    /// Converts a Julian Ephemeris Day to a local calendar date by applying the time-zone offset before truncation.
    /// </summary>
    /// <param name="julianDay">The Julian Ephemeris Day, in Universal Time.</param>
    /// <param name="utcOffsetHours">The local-time offset from Universal Time, in hours.</param>
    /// <returns>The local date.</returns>
    private static DateOnly JulianDayToLocalDate(double julianDay, double utcOffsetHours) =>
        DateOnly.FromDateTime(s_j2000Epoch.AddDays(julianDay - J2000JulianDay).AddHours(utcOffsetHours));

    /// <summary>
    /// Converts an angle from degrees to radians.
    /// </summary>
    /// <param name="degrees">The angle in degrees.</param>
    /// <returns>The angle in radians.</returns>
    private static double DegreesToRadians(double degrees) =>
        degrees * (Math.PI / 180.0);
}
