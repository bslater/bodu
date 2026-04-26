// ---------------------------------------------------------------------------------------------------------------
// <copyright file="HebrewNotableDateCalculatorTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using SysGlob = System.Globalization;

namespace Bodu.Globalization.Calendar.Calculators;

/// <summary>
/// Verifies the correctness and boundary behaviour of <see cref="HebrewNotableDateCalculator" />.
/// </summary>
[TestClass]
public sealed class HebrewNotableDateCalculatorTests
{
	/// <summary>
	/// Verifies that an undefined <see cref="HebrewMonth" /> value throws <see cref="ArgumentException" />.
	/// </summary>
	[TestMethod]
	public void Constructor_WhenMonthIsUndefined_ShouldThrowArgumentException()
	{
		var ex = Assert.ThrowsExactly<ArgumentException>(() =>
		{
			_ = new HebrewNotableDateCalculator((HebrewMonth)99, 1);
		});

		Assert.AreEqual("month", ex.ParamName);
	}

	/// <summary>
	/// Verifies that a day outside the range 1–30 throws <see cref="ArgumentOutOfRangeException" />.
	/// </summary>
	[DataRow(0)]
	[DataRow(31)]
	[TestMethod]
	public void Constructor_WhenDayOutOfRange_ShouldThrowArgumentOutOfRangeException(int day)
	{
		var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
		{
			_ = new HebrewNotableDateCalculator(HebrewMonth.Tishri, day);
		});

		Assert.AreEqual("day", ex.ParamName);
	}

	/// <summary>
	/// Verifies that requesting a date with a Gregorian year below one throws <see cref="ArgumentOutOfRangeException" />.
	/// </summary>
	[DataRow(0)]
	[DataRow(-1)]
	[DataRow(int.MinValue)]
	[TestMethod]
	public void GetDate_WhenYearLessThanOne_ShouldThrowArgumentOutOfRangeException(int year)
	{
		var sut = new HebrewNotableDateCalculator(HebrewMonth.Tishri, 1);

		var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
		{
			_ = sut.GetDate(year);
		});

		Assert.AreEqual("year", ex.ParamName);
	}

	/// <summary>
	/// Verifies that Rosh Hashanah (1 Tishri) returns known correct Gregorian dates.
	/// </summary>
	[DataRow(2022, 9, 26)]
	[DataRow(2023, 9, 16)]
	[DataRow(2024, 10, 2)]
	[TestMethod]
	public void GetDate_WhenRoshHashanah_ShouldReturnExpectedDate(int year, int expectedMonth, int expectedDay)
	{
		var sut = new HebrewNotableDateCalculator(HebrewMonth.Tishri, 1);

		DateTime? result = sut.GetDate(year);

		Assert.IsNotNull(result);
		Assert.AreEqual(new DateTime(year, expectedMonth, expectedDay), result!.Value);
		Assert.AreEqual(DateTimeKind.Unspecified, result.Value.Kind);
	}

	/// <summary>
	/// Verifies that Yom Kippur (10 Tishri) returns known correct Gregorian dates.
	/// </summary>
	[DataRow(2022, 10, 5)]
	[DataRow(2023, 9, 25)]
	[DataRow(2024, 10, 11)]
	[TestMethod]
	public void GetDate_WhenYomKippur_ShouldReturnExpectedDate(int year, int expectedMonth, int expectedDay)
	{
		var sut = new HebrewNotableDateCalculator(HebrewMonth.Tishri, 10);

		DateTime? result = sut.GetDate(year);

		Assert.IsNotNull(result);
		Assert.AreEqual(new DateTime(year, expectedMonth, expectedDay), result!.Value);
	}

	/// <summary>
	/// Verifies that Passover (15 Nisan) returns known correct Gregorian dates.
	/// </summary>
	[DataRow(2022, 4, 16)]
	[DataRow(2023, 4, 6)]
	[DataRow(2024, 4, 22)]
	[TestMethod]
	public void GetDate_WhenPassover_ShouldReturnExpectedDate(int year, int expectedMonth, int expectedDay)
	{
		var sut = new HebrewNotableDateCalculator(HebrewMonth.Nisan, 15);

		DateTime? result = sut.GetDate(year);

		Assert.IsNotNull(result);
		Assert.AreEqual(new DateTime(year, expectedMonth, expectedDay), result!.Value);
	}

	/// <summary>
	/// Verifies that Hanukkah (25 Kislev) returns known correct Gregorian dates.
	/// </summary>
	[DataRow(2022, 12, 19)]
	[DataRow(2023, 12, 8)]
	[DataRow(2024, 12, 26)]
	[TestMethod]
	public void GetDate_WhenHanukkah_ShouldReturnExpectedDate(int year, int expectedMonth, int expectedDay)
	{
		var sut = new HebrewNotableDateCalculator(HebrewMonth.Kislev, 25);

		DateTime? result = sut.GetDate(year);

		Assert.IsNotNull(result);
		Assert.AreEqual(new DateTime(year, expectedMonth, expectedDay), result!.Value);
	}

	/// <summary>
	/// Verifies that Purim via <see cref="HebrewMonth.LastAdar" /> (14 of last Adar) returns known correct dates
	/// in both leap and non-leap Hebrew years.
	/// </summary>
	[DataRow(2022, 3, 17)]   // Hebrew 5782 — non-leap year
	[DataRow(2023, 3, 7)]    // Hebrew 5783 — non-leap year
	[DataRow(2024, 3, 24)]   // Hebrew 5784 — leap year (Adar II)
	[TestMethod]
	public void GetDate_WhenPurimViaLastAdar_ShouldReturnExpectedDate(int year, int expectedMonth, int expectedDay)
	{
		var sut = new HebrewNotableDateCalculator(HebrewMonth.LastAdar, 14);

		DateTime? result = sut.GetDate(year);

		Assert.IsNotNull(result);
		Assert.AreEqual(new DateTime(year, expectedMonth, expectedDay), result!.Value);
	}

	/// <summary>
	/// Verifies that requesting <see cref="HebrewMonth.AdarII" /> in a non-leap Hebrew year returns
	/// <see langword="null" /> rather than throwing.
	/// </summary>
	[TestMethod]
	public void GetDate_WhenAdarIIInNonLeapYear_ShouldReturnNull()
	{
		// Hebrew year 5782 (2021–2022) and 5783 (2022–2023) are non-leap years.
		var sut = new HebrewNotableDateCalculator(HebrewMonth.AdarII, 14);

		Assert.IsNull(sut.GetDate(2022));
		Assert.IsNull(sut.GetDate(2023));
	}

	/// <summary>
	/// Verifies that requesting <see cref="HebrewMonth.AdarII" /> in a leap Hebrew year returns a non-null result.
	/// </summary>
	[TestMethod]
	public void GetDate_WhenAdarIIInLeapYear_ShouldReturnNonNull()
	{
		// Hebrew year 5784 (2023–2024) is a leap year; Adar II exists.
		var sut = new HebrewNotableDateCalculator(HebrewMonth.AdarII, 14);

		Assert.IsNotNull(sut.GetDate(2024));
	}

	/// <summary>
	/// Verifies that the returned <see cref="DateTime.Kind" /> is always <see cref="DateTimeKind.Unspecified" />.
	/// </summary>
	[TestMethod]
	public void GetDate_WhenCalendarIsNull_ShouldReturnUnspecifiedKind()
	{
		var sut = new HebrewNotableDateCalculator(HebrewMonth.Tishri, 1);

		DateTime? result = sut.GetDate(2024);

		Assert.IsNotNull(result);
		Assert.AreEqual(DateTimeKind.Unspecified, result!.Value.Kind);
	}

	/// <summary>
	/// Verifies that supplying an explicit <see cref="SysGlob.GregorianCalendar" /> produces the same date as passing
	/// <see langword="null" />.
	/// </summary>
	[TestMethod]
	public void GetDate_WhenCalendarIsGregorian_ShouldMatchDefaultResult()
	{
		var sut = new HebrewNotableDateCalculator(HebrewMonth.Tishri, 1);

		DateTime? withNull = sut.GetDate(2024, calendar: null);
		DateTime? withGregorian = sut.GetDate(2024, new SysGlob.GregorianCalendar());

		Assert.IsNotNull(withNull);
		Assert.IsNotNull(withGregorian);
		Assert.AreEqual(withNull!.Value, withGregorian!.Value);
	}
}
