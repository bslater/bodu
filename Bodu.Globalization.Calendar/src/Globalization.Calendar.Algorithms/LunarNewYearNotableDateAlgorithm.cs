// ---------------------------------------------------------------------------------------------------------------
// <copyright file="LunarNewYearNotableDateAlgorithm.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using SysGlobal = System.Globalization;

namespace Bodu.Globalization.Calendar.Algorithms;

/// <summary>
/// Provides a algorithm for determining the date of Lunar New Year for a given year.
/// </summary>
/// <remarks>
/// Lunar New Year marks the beginning of the new year based on the traditional Chinese lunisolar calendar. It typically falls between
/// January 21 and February 20 in the Gregorian calendar. This algorithm uses the <see cref="SysGlobal.ChineseLunisolarCalendar" /> to
/// determine the date and maps the result to the specified calendar system.
/// </remarks>
public sealed class LunarNewYearNotableDateAlgorithm
	: INotableDateAlgorithm
{
	private static readonly SysGlobal.ChineseLunisolarCalendar ChineseCalendar = new SysGlobal.ChineseLunisolarCalendar();

	/// <summary>
	/// Computes the date of Lunar New Year for the specified year and calendar system.
	/// </summary>
	/// <param name="year">
	/// The year for which to calculate Lunar New Year. Must be greater than or equal to 1. Only years between 1901 and 2100 are supported.
	/// </param>
	/// <param name="calendar">
	/// An optional <see cref="SysGlobal.Calendar" /> instance representing the calendar system to use. If <c>null</c>, a
	/// <see cref="SysGlobal.GregorianCalendar" /> is assumed.
	/// </param>
	/// <returns>
	/// A <see cref="DateTime" /> representing the date of Lunar New Year in the specified calendar system, or <c>null</c> if the year
	/// is outside the supported range. The returned <see cref="DateTime.Kind" /> is always <see cref="DateTimeKind.Unspecified" />.
	/// </returns>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="year" /> is less than 1.</exception>
	/// <exception cref="NotSupportedException">Thrown if the specified <paramref name="calendar" /> type is unsupported.</exception>
	public DateTime? GetDate(int year, SysGlobal.Calendar? calendar = null)
	{
		if (year < 1)
			throw new ArgumentOutOfRangeException(nameof(year), "Year must be greater than or equal to 1.");

		// ChineseLunisolarCalendar's MaxSupportedDateTime reports 28 January 2101 because the
		// lunar year starting in 2100 extends into early 2101, but ToDateTime(year, 1, 1, …)
		// for year 2101 is not itself a supported era year and throws. Cap the guard at the
		// last *valid* lunar year — MaxSupportedDateTime.Year - 1 — so callers get a clean
		// null rather than an exception.
		if (year < ChineseCalendar.MinSupportedDateTime.Year || year >= ChineseCalendar.MaxSupportedDateTime.Year)
			return null;

		SysGlobal.Calendar targetCalendar = calendar ?? new SysGlobal.GregorianCalendar();

		DateTime lunarNewYear = ChineseCalendar.ToDateTime(year, 1, 1, 0, 0, 0, 0);

		if (targetCalendar.GetType() == typeof(SysGlobal.GregorianCalendar))
		{
			// Explicitly create a DateTime with Unspecified kind to avoid S6562 warning
			return DateTime.SpecifyKind(lunarNewYear.Date, DateTimeKind.Unspecified);
		}

		return new DateTime(
			targetCalendar.GetYear(lunarNewYear),
			targetCalendar.GetMonth(lunarNewYear),
			targetCalendar.GetDayOfMonth(lunarNewYear),
			0, 0, 0, 0,
			targetCalendar,
			DateTimeKind.Unspecified
		);
	}
}
