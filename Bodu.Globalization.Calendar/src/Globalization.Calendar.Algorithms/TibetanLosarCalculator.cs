// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TibetanLosarCalculator.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Algorithms;

/// <summary>
/// Approximates the Gregorian date of Tibetan New Year (Gyalpo Losar) — the first day of the first month of the Tibetan
/// lunisolar calendar — by selecting the new moon that sits closest to a fixed point in the solar year.
/// </summary>
/// <remarks>
/// <para>
/// Losar is the new moon of the Tibetan first month. The Phukpa (Phugpa) calendar places its leap months differently
/// from the Chinese and Indian calendars, so in some years Losar falls a full lunation later than the new moon a naive
/// "first new moon after a fixed date" rule would select. This calculator instead chooses, among the new moons of the
/// year, the one whose apparent solar longitude is nearest <see cref="LosarSolarLongitude" />, which tracks the Tibetan
/// first-month new moon across the leap-month divergences. The approximation reproduces the published Phukpa Gyalpo
/// Losar dates for the modern era (validated across 2023-2030, including the late-falling 2027 and 2030 years) to
/// within a day.
/// </para>
/// </remarks>
internal static class TibetanLosarCalculator
{
    /// <summary>The apparent tropical solar longitude, in degrees, that the Tibetan first-month new moon sits nearest. Chosen to reproduce the published Phukpa Gyalpo Losar dates for the modern era.</summary>
    private const double LosarSolarLongitude = 333.5;

    /// <summary>
    /// Computes the approximate Gregorian date of Tibetan New Year (Gyalpo Losar) for the supplied year.
    /// </summary>
    /// <param name="year">The Gregorian year.</param>
    /// <returns>
    /// The Losar date, or <see langword="null" /> when the year is out of range or no new moon is found.
    /// </returns>
    public static DateOnly? Losar(int year)
    {
        if (year is < 1 or > 9999)
            return null;

        // Losar always falls between early February and early March, so the candidate new moon is one of the two or
        // three that begin in the first quarter of the year; the one whose sun is nearest the target longitude is taken.
        DateOnly cursor = new(year, 1, 1);
        DateOnly limit = new(year, 4, 1);

        DateOnly? losar = null;
        double smallestDelta = double.MaxValue;

        while (cursor < limit)
        {
            if (LunarPhaseCalculator.NewMoonOnOrAfter(cursor) is not DateOnly newMoon || newMoon >= limit)
                break;

            double delta = AngularDistance(SolarTermCalculator.SunTropicalLongitude(newMoon), LosarSolarLongitude);
            if (delta < smallestDelta)
            {
                smallestDelta = delta;
                losar = newMoon;
            }

            cursor = newMoon.AddDays(1);
        }

        return losar;
    }

    /// <summary>
    /// Returns the absolute angular distance between two longitudes, in degrees, in the range <c>[0, 180]</c>.
    /// </summary>
    /// <param name="first">The first longitude in degrees.</param>
    /// <param name="second">The second longitude in degrees.</param>
    /// <returns>The smallest absolute separation between the two longitudes.</returns>
    private static double AngularDistance(double first, double second)
    {
        double delta = Math.Abs((first - second) % 360.0);
        return delta > 180.0 ? 360.0 - delta : delta;
    }
}
