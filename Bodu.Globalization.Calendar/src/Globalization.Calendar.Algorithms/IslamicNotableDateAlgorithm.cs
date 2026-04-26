// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IslamicNotableDateAlgorithm.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using SysGlobal = System.Globalization;

namespace Bodu.Globalization.Calendar.Algorithms;

/// <summary>
/// Provides a algorithm for determining the Gregorian date of an observance defined by a fixed month and day in the Islamic Hijri
/// calendar.
/// </summary>
/// <remarks>
/// <para>
/// The Islamic (Hijri) calendar is a purely lunar calendar of 12 months. Because it is approximately 11 days shorter than the
/// Gregorian year, Islamic observances advance through all seasons over a 33-year cycle. A given Gregorian year may therefore contain
/// a particular Islamic observance zero, one, or — in rare cases near the cycle boundary — two times.
/// </para>
/// <para>
/// This algorithm uses <see cref="SysGlobal.HijriCalendar" />, which implements the tabular (algorithmic) Islamic calendar rather
/// than the observational calendar. Dates may therefore differ from actual moon-sighted dates by up to two days in some jurisdictions.
/// </para>
/// <para>
/// When an observance falls in the Gregorian year twice (approximately once every 33 years), the earlier occurrence is returned.
/// When an observance does not fall in the Gregorian year at all, <see langword="null" /> is returned.
/// </para>
/// </remarks>
public sealed class IslamicNotableDateAlgorithm
	: INotableDateAlgorithm
{
	private static readonly SysGlobal.HijriCalendar HijriCalendar = new SysGlobal.HijriCalendar();

	private readonly int _hijriMonth;
	private readonly int _hijriDay;

	/// <summary>
	/// Initialises a new instance of the <see cref="IslamicNotableDateAlgorithm" /> class for the given Hijri month and day.
	/// </summary>
	/// <param name="hijriMonth">The Hijri month number (1–12), where 1 = Muharram and 12 = Dhul Hijja.</param>
	/// <param name="hijriDay">The Hijri day within the month (1–30).</param>
	/// <exception cref="ArgumentOutOfRangeException">
	/// <paramref name="hijriMonth" /> is less than 1 or greater than 12, or <paramref name="hijriDay" /> is less than 1 or greater
	/// than 30.
	/// </exception>
	public IslamicNotableDateAlgorithm(int hijriMonth, int hijriDay)
	{
		if (hijriMonth < 1 || hijriMonth > 12)
			throw new ArgumentOutOfRangeException(nameof(hijriMonth), "Hijri month must be between 1 and 12.");
		if (hijriDay < 1 || hijriDay > 30)
			throw new ArgumentOutOfRangeException(nameof(hijriDay), "Hijri day must be between 1 and 30.");

		_hijriMonth = hijriMonth;
		_hijriDay = hijriDay;
	}

	/// <summary>
	/// Computes the Gregorian date corresponding to the configured Hijri month and day for the specified Gregorian year.
	/// </summary>
	/// <param name="year">
	/// The Gregorian year for which to calculate the date. Must be greater than or equal to 1.
	/// </param>
	/// <param name="calendar">
	/// An optional <see cref="SysGlobal.Calendar" /> instance representing the calendar system to use for the returned
	/// <see cref="DateTime" />. When <see langword="null" />, <see cref="SysGlobal.GregorianCalendar" /> is assumed.
	/// </param>
	/// <returns>
	/// A <see cref="DateTime" /> representing the observance date in the specified calendar system, or <see langword="null" /> if the
	/// observance does not fall in the given Gregorian year (which occurs approximately once every 33 years for any given Hijri date).
	/// The returned <see cref="DateTime.Kind" /> is always <see cref="DateTimeKind.Unspecified" />.
	/// </returns>
	/// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="year" /> is less than 1.</exception>
	/// <exception cref="NotSupportedException">Thrown when the specified <paramref name="calendar" /> type is unsupported.</exception>
	public DateTime? GetDate(int year, SysGlobal.Calendar? calendar = null)
	{
		if (year < 1)
			throw new ArgumentOutOfRangeException(nameof(year), "Year must be greater than or equal to 1.");

		SysGlobal.Calendar targetCalendar = calendar ?? new SysGlobal.GregorianCalendar();

		int hijriYearForJan1 = HijriCalendar.GetYear(new DateTime(year, 1, 1));

		for (int h = hijriYearForJan1; h <= hijriYearForJan1 + 1; h++)
		{
			int daysInMonth = HijriCalendar.GetDaysInMonth(h, _hijriMonth);
			if (_hijriDay > daysInMonth)
				continue;

			DateTime candidate;
			try
			{
				candidate = HijriCalendar.ToDateTime(h, _hijriMonth, _hijriDay, 0, 0, 0, 0);
			}
			catch (ArgumentOutOfRangeException)
			{
				continue;
			}

			if (candidate.Year != year)
				continue;

			if (targetCalendar.GetType() == typeof(SysGlobal.GregorianCalendar))
				return DateTime.SpecifyKind(candidate.Date, DateTimeKind.Unspecified);

			return new DateTime(
				targetCalendar.GetYear(candidate),
				targetCalendar.GetMonth(candidate),
				targetCalendar.GetDayOfMonth(candidate),
				0, 0, 0, 0,
				targetCalendar,
				DateTimeKind.Unspecified);
		}

		return null;
	}
}
