// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TehranEquinoxCalculator.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Algorithms;

/// <summary>
/// Computes Nowruz — the first day of the Solar Hijri year — from the true vernal-equinox instant at the Tehran
/// standard meridian, following the official Iranian calendar rule.
/// </summary>
/// <remarks>
/// <para>
/// The rule: Farvardin 1 falls on the civil day of the vernal equinox when the equinox instant occurs before apparent
/// solar noon at the 52.5°E standard meridian (the meridian of Iran Standard Time, UTC+03:30); otherwise it falls on
/// the following day. Apparent noon is derived from mean noon by the equation of time, which shifts the boundary about
/// eight minutes later in late March.
/// </para>
/// <para>
/// The equinox instant comes from the Meeus chapter 27 series in dynamical time and is reduced to Universal Time with a
/// ΔT estimate (Espenak–Meeus polynomial expressions), so the result tracks the observationally anchored civil calendar
/// rather than an arithmetic intercalation cycle. This is the opt-in astronomical variant behind the
/// <c>tehran-nowruz</c> algorithm key; the tabular resources remain the default.
/// </para>
/// </remarks>
internal static class TehranEquinoxCalculator
{
    /// <summary>The UTC offset, in hours, of the Iranian standard meridian at 52.5°E (Iran Standard Time).</summary>
    private const double TehranMeridianUtcOffsetHours = 3.5;

    /// <summary>The number of minutes from midnight to mean solar noon.</summary>
    private const double MeanNoonMinutes = 720.0;

    /// <summary>
    /// Returns the Gregorian date of Nowruz (Farvardin 1) whose vernal equinox falls in the supplied Gregorian year.
    /// </summary>
    /// <param name="year">The Gregorian year.</param>
    /// <returns>The Nowruz date, or <see langword="null" /> when the year is outside the supported range.</returns>
    public static DateOnly? Nowruz(int year)
    {
        // The ΔT expressions and the Meeus series are both fitted around the modern era; outside this window the
        // boundary decision degrades below civil-calendar reliability, so the algorithm declines rather than guesses.
        if (year is < 1800 or > 2200)
            return null;

        DateTime dynamical = SolarTermCalculator.VernalEquinoxDynamicalInstant(year);
        DateTime universal = dynamical.AddSeconds(-DeltaTSeconds(year));
        DateTime local = universal.AddHours(TehranMeridianUtcOffsetHours);

        DateOnly equinoxDay = DateOnly.FromDateTime(local);
        double apparentNoonMinutes = MeanNoonMinutes - EquationOfTimeMinutes(equinoxDay);

        return local.TimeOfDay.TotalMinutes < apparentNoonMinutes ? equinoxDay : equinoxDay.AddDays(1);
    }

    /// <summary>
    /// Estimates ΔT (dynamical time minus Universal Time), in seconds, for the supplied year.
    /// </summary>
    /// <param name="year">The Gregorian year.</param>
    /// <returns>The ΔT estimate in seconds.</returns>
    /// <remarks>
    /// <para>
    /// Uses the Espenak–Meeus polynomial expressions for 1986–2005 and 2005–2050, and the long-term parabola
    /// <c>−20 + 32u²</c> (with <c>u</c> in centuries from 1820) elsewhere. The boundary decision is only sensitive to
    /// ΔT when the equinox falls within ΔT of apparent noon, so coarse accuracy outside the fitted ranges suffices.
    /// </para>
    /// </remarks>
    private static double DeltaTSeconds(int year)
    {
        if (year is >= 1986 and < 2005)
        {
            double t = year - 2000;
            return 63.86 + (0.3345 * t) - (0.060374 * t * t) + (0.0017275 * t * t * t)
                + (0.000651814 * t * t * t * t) + (0.00002373599 * t * t * t * t * t);
        }

        if (year is >= 2005 and <= 2050)
        {
            double t = year - 2000;
            return 62.92 + (0.32217 * t) + (0.005589 * t * t);
        }

        double u = (year - 1820) / 100.0;
        return -20.0 + (32.0 * u * u);
    }

    /// <summary>
    /// Returns the equation of time — apparent minus mean solar time — in minutes for the supplied date, using the
    /// Spencer harmonic fit.
    /// </summary>
    /// <param name="date">The date to evaluate.</param>
    /// <returns>The equation of time in minutes; negative when apparent noon falls after mean noon.</returns>
    private static double EquationOfTimeMinutes(DateOnly date)
    {
        double gamma = 2.0 * Math.PI * (date.DayOfYear - 1) / (DateTime.IsLeapYear(date.Year) ? 366.0 : 365.0);

        return 229.18 * (0.000075
            + (0.001868 * Math.Cos(gamma))
            - (0.032077 * Math.Sin(gamma))
            - (0.014615 * Math.Cos(2.0 * gamma))
            - (0.040849 * Math.Sin(2.0 * gamma)));
    }
}
