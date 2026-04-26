// ---------------------------------------------------------------------------------------------------------------
// <copyright file="HebrewMonth.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Algorithms;

/// <summary>
/// Identifies a month in the Hebrew (Jewish) lunisolar calendar using its conventional Jewish name.
/// </summary>
/// <remarks>
/// <para>
/// The Hebrew calendar numbers months beginning with Tishri (the civil new year). In a standard (non-leap) year there are twelve
/// months; in a leap year a thirteenth month — Adar II — is intercalated after Adar I, shifting the month numbers of all subsequent
/// months by one. The values in this enumeration use stable semantic identifiers that are independent of this shift; the
/// <see cref="HebrewNotableDateAlgorithm" /> resolves them to the correct <see cref="System.Globalization.HebrewCalendar" /> month
/// number at runtime.
/// </para>
/// <para>
/// Use <see cref="LastAdar" /> when the intended date follows the convention of the last Adar in the year — for example, Purim
/// (14 Adar) is always observed in the last Adar regardless of whether the year is a leap year.
/// </para>
/// </remarks>
public enum HebrewMonth
{
	/// <summary>The first month of the Hebrew civil year (September/October in the Gregorian calendar).</summary>
	Tishri = 1,

	/// <summary>The second month (October/November).</summary>
	Heshvan = 2,

	/// <summary>The third month (November/December).</summary>
	Kislev = 3,

	/// <summary>The fourth month (December/January).</summary>
	Tevet = 4,

	/// <summary>The fifth month (January/February).</summary>
	Shevat = 5,

	/// <summary>
	/// The sixth month. In a non-leap year this is simply Adar; in a leap year it is Adar I (the first of the two Adar months). Use
	/// <see cref="LastAdar" /> to target the last Adar in both leap and non-leap years.
	/// </summary>
	AdarI = 6,

	/// <summary>
	/// The second Adar month that exists only in Hebrew leap years (February/March). Using this value in a non-leap year causes
	/// <see cref="HebrewNotableDateAlgorithm.GetDate" /> to return <see langword="null" />.
	/// </summary>
	AdarII = 7,

	/// <summary>
	/// The last Adar of the year regardless of whether the year is a leap year. In a non-leap year this resolves to Adar (month 6);
	/// in a leap year it resolves to Adar II (month 7). Use this value for observances such as Purim that are always celebrated in
	/// the final Adar.
	/// </summary>
	LastAdar = 100,

	/// <summary>The seventh month in a non-leap year, eighth in a leap year (March/April).</summary>
	Nisan = 200,

	/// <summary>The eighth month in a non-leap year, ninth in a leap year (April/May).</summary>
	Iyar = 201,

	/// <summary>The ninth month in a non-leap year, tenth in a leap year (May/June).</summary>
	Sivan = 202,

	/// <summary>The tenth month in a non-leap year, eleventh in a leap year (June/July).</summary>
	Tammuz = 203,

	/// <summary>The eleventh month in a non-leap year, twelfth in a leap year (July/August).</summary>
	Av = 204,

	/// <summary>The twelfth month in a non-leap year, thirteenth in a leap year (August/September). The last month of the Hebrew year.</summary>
	Elul = 205,
}
