// ---------------------------------------------------------------------------------------------------------------
// <copyright file="QingmingNotableDateAlgorithm.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using SysGlobal = System.Globalization;

namespace Bodu.Globalization.Calendar.Algorithms;

/// <summary>
/// Provides an algorithm for determining the Gregorian date of the Qingming solar term (清明節) for a given year.
/// </summary>
/// <remarks>
/// <para>
/// Qingming (Pure Brightness) is the fifth solar term of the Chinese lunisolar calendar, defined as the moment when the sun's
/// ecliptic longitude reaches 15°. It falls approximately 15 days after the vernal equinox, typically on 4 or 5 April in the
/// Gregorian calendar.
/// </para>
/// <para>
/// This algorithm uses the vernal equinox formula from Jean Meeus, <em>Astronomical Algorithms</em> (2nd ed.), Chapter 27, with
/// the principal correction terms from Table 27.c, plus an offset of one full solar degree (15°) worth of the tropical year to
/// reach 15° of ecliptic longitude. The result is accurate to within one calendar day for years in the range 1901–2100.
/// </para>
/// <para>
/// Returned dates are expressed in the observer's local civil date. Because Qingming is defined in terms of Universal Time (UT)
/// astronomical events, observers in East Asia (UTC+8) may observe the festival on the previous Gregorian day in a small number
/// of years when the solar term transition occurs near local midnight. This edge case is not corrected by this algorithm.
/// </para>
/// </remarks>
public sealed class QingmingNotableDateAlgorithm
	: INotableDateAlgorithm
{
	/// <summary>
	/// The number of days corresponding to 15° of solar longitude (one solar term), computed as the tropical year length
	/// (365.2422 days) multiplied by 15/360.
	/// </summary>
	private const double DegreesToDays15 = 15.0 * 365.2422 / 360.0;

	/// <summary>The J2000.0 epoch expressed as a <see cref="DateTime" /> (2000-01-01T12:00:00 UT, kind unspecified), equal to JDE 2451545.0.</summary>
	private static readonly DateTime J2000Epoch = new DateTime(2000, 1, 1, 12, 0, 0, DateTimeKind.Unspecified);

	/// <summary>
	/// Computes the date of the Qingming solar term for the specified year.
	/// </summary>
	/// <param name="year">
	/// The year for which to calculate Qingming. Must be greater than or equal to 1. Accurate results are produced for years in the
	/// range 1901–2100; results outside this range become progressively less reliable.
	/// </param>
	/// <param name="calendar">
	/// An optional <see cref="SysGlobal.Calendar" /> instance representing the calendar system to use for the returned
	/// <see cref="DateTime" />. When <see langword="null" />, <see cref="SysGlobal.GregorianCalendar" /> is assumed.
	/// </param>
	/// <returns>
	/// A <see cref="DateTime" /> representing the date of Qingming in the specified calendar system. The returned
	/// <see cref="DateTime.Kind" /> is always <see cref="DateTimeKind.Unspecified" />.
	/// </returns>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="year" /> is less than 1.</exception>
	/// <exception cref="NotSupportedException">Thrown when the specified <paramref name="calendar" /> type is unsupported.</exception>
	public DateTime? GetDate(int year, SysGlobal.Calendar? calendar = null)
	{
		if (year < 1)
			throw new ArgumentOutOfRangeException(nameof(year), CalendarResourceStrings.ArgumentOutOfRangeException_YearOutOfRange);

		var equinoxJde = ComputeVernalEquinoxJde(year);
		var qingmingJde = equinoxJde + DegreesToDays15;

		DateTime rawUtc = JdeToDateTime(qingmingJde);
		var dateOnly = DateTime.SpecifyKind(rawUtc.Date, DateTimeKind.Unspecified);

		SysGlobal.Calendar targetCalendar = calendar ?? new SysGlobal.GregorianCalendar();

		if (targetCalendar.GetType() == typeof(SysGlobal.GregorianCalendar))
			return dateOnly;

		return new DateTime(
			targetCalendar.GetYear(dateOnly),
			targetCalendar.GetMonth(dateOnly),
			targetCalendar.GetDayOfMonth(dateOnly),
			0, 0, 0, 0,
			targetCalendar,
			DateTimeKind.Unspecified);
	}

	/// <summary>
	/// Computes the Julian Day Ephemeris (JDE) of the vernal equinox for the given Gregorian year using the Meeus polynomial
	/// (Table 27.a) and the 24-term correction (Table 27.c).
	/// </summary>
	/// <param name="year">The Gregorian year for which the vernal equinox JDE is computed.</param>
	/// <returns>The Julian Day Ephemeris of the vernal equinox in Terrestrial Dynamical Time.</returns>
	private static double ComputeVernalEquinoxJde(int year)
	{
		var Y = (year - 2000) / 1000.0;

		// Meeus Table 27.a — mean JDE of the March equinox.
		var jde0 = 2451623.80984
			+ 365242.37404 * Y
			+ 0.05169 * Y * Y
			- 0.00411 * Y * Y * Y
			- 0.00057 * Y * Y * Y * Y;

		// Meeus Table 27.c — periodic correction terms (units: 0.00001 day × coefficient).
		var T = (jde0 - 2451545.0) / 36525.0;
		var W = DegToRad(35999.373 * T - 2.47);
		var lambda = 1.0 + 0.0334 * Math.Cos(W) + 0.0007 * Math.Cos(2.0 * W);

		var S = 0.0;
		foreach ((var a, var b, var c) in s_correctionTerms)
			S += a * Math.Cos(DegToRad(b + c * T));

		return jde0 + (0.00001 * S / lambda);
	}

	/// <summary>Converts a Julian Day Ephemeris to a <see cref="DateTime" /> relative to the J2000 epoch.</summary>
	private static DateTime JdeToDateTime(double jde) =>
		J2000Epoch.AddDays(jde - 2451545.0);

	/// <summary>Converts degrees to radians.</summary>
	private static double DegToRad(double degrees) =>
		degrees * (Math.PI / 180.0);

	/// <summary>
	/// Periodic correction terms from Meeus Table 27.c for the March equinox. Each entry is
	/// (amplitude, phase [°], rate [°/Julian century]).
	/// </summary>
	private static readonly (double A, double B, double C)[] s_correctionTerms =
	[
		(485,  324.96, 1934.136),
		(203,  337.23, 32964.467),
		(199,  342.08, 20.186),
		(182,   27.85, 445267.112),
		(156,   73.14, 45036.886),
		(136,  171.52, 22518.443),
		( 77,  222.54, 65928.934),
		( 74,  296.72, 3034.906),
		( 70,  243.58, 9037.513),
		( 58,  119.81, 33718.147),
		( 52,  297.17, 150.678),
		( 50,   21.02, 2281.226),
		( 45,  247.54, 29929.562),
		( 44,  325.15, 31555.956),
		( 29,   60.93, 4443.417),
		( 18,  155.12, 67555.328),
		( 17,  288.79, 4562.452),
		( 16,  198.04, 62894.029),
		( 14,  199.76, 31557.381),
		( 12,   95.39, 14577.848),
		( 10,   69.15, 31556.017),
		(  9,  293.38, 149.438),
		(  9,  242.69, 9325.052),
		(  8,  141.01, 31559.532),
		(  7,  283.90, 23.000),
		(  7,  306.92, 10977.079),
		(  6,  233.02, 10447.357),
		(  6,  262.55, 31441.677),
	];
}
