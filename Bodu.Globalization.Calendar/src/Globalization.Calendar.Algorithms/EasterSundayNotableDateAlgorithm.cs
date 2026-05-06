// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EasterSundayNotableDateAlgorithm.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Concurrent;

namespace Bodu.Globalization.Calendar.Algorithms;

/// <summary>
/// Provides Easter Sunday date calculations based on the given year and calendar system.
/// </summary>
/// <remarks>
/// This implementation uses the Gregorian calendar for years &gt;= 1583 and the Julian calendar otherwise. Results are cached for
/// performance. Use this algorithm to retrieve the date of Easter Sunday.
/// </remarks>
public sealed class EasterSundayNotableDateAlgorithm
	: INotableDateAlgorithm
{
	/// <summary>Shared per-process cache of computed Easter dates, keyed by year and calendar identity string.</summary>
	private static readonly ConcurrentDictionary<(int year, string calendarId), DateTime> _easterCache = new();

	/// <inheritdoc />
	public DateTime? GetDate(int year, System.Globalization.Calendar? calendar)
	{
		ThrowHelper.ThrowIfLessThan(year, 1);

		return GetOrAddEasterSunday(year, calendar);
	}

	/// <summary>
	/// Computes Easter Sunday for the specified year and optional calendar.
	/// </summary>
	/// <param name="year">The year for which to compute Easter.</param>
	/// <param name="calendar">The calendar system (defaults to Gregorian).</param>
	/// <returns>A <see cref="DateTime" /> representing Easter Sunday.</returns>
	private static DateTime GetOrAddEasterSunday(int year, System.Globalization.Calendar? calendar)
	{
		var calendarId = calendar?.GetType().FullName ?? "Gregorian";
		var key = (year, calendarId);

		return _easterCache.GetOrAdd(key, _ =>
		{
			int month, day;
			if (year >= 1583 && calendar is not System.Globalization.JulianCalendar)
			{
				// Gregorian calendar algorithm (Computus)
				int a = year % 19;
				int b = year / 100;
				int c = year % 100;
				int d = b / 4;
				int e = b % 4;
				int f = (b + 8) / 25;
				int g = (b - f + 1) / 3;
				int h = (19 * a + b - d - g + 15) % 30;
				int i = c / 4;
				int k = c % 4;
				int l = (32 + 2 * e + 2 * i - h - k) % 7;
				int m = (a + 11 * h + 22 * l) / 451;
				month = (h + l - 7 * m + 114) / 31;
				day = (h + l - 7 * m + 114) % 31 + 1;
			}
			else
			{
				// Julian calendar algorithm
				int a = year % 4;
				int b = year % 7;
				int c = year % 19;
				int d = (19 * c + 15) % 30;
				int e = (2 * a + 4 * b - d + 34) % 7;
				int f = d + e + 114;
				month = f / 31;
				day = f % 31 + 1;
			}

			return calendar != null
				? calendar.ToDateTime(year, month, day, 0, 0, 0, 0)
				: new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Unspecified);
		});
	}
}
