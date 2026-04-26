// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ChineseLunisolarNotableDateCalculatorTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using SysGlob = System.Globalization;

namespace Bodu.Globalization.Calendar.Calculators;

/// <summary>
/// Verifies the correctness and boundary behaviour of <see cref="ChineseLunisolarNotableDateCalculator" />.
/// </summary>
[TestClass]
public sealed class ChineseLunisolarNotableDateCalculatorTests
{
	/// <summary>
	/// Verifies that a lunar month less than 1 or greater than 13 throws <see cref="ArgumentOutOfRangeException" />.
	/// </summary>
	[DataRow(0)]
	[DataRow(14)]
	[DataRow(int.MinValue)]
	[TestMethod]
	public void Constructor_WhenLunarMonthOutOfRange_ShouldThrowArgumentOutOfRangeException(int lunarMonth)
	{
		var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
		{
			_ = new ChineseLunisolarNotableDateCalculator(lunarMonth, 1);
		});

		Assert.AreEqual("lunarMonth", ex.ParamName);
	}

	/// <summary>
	/// Verifies that a lunar day less than 1 or greater than 30 throws <see cref="ArgumentOutOfRangeException" />.
	/// </summary>
	[DataRow(0)]
	[DataRow(31)]
	[TestMethod]
	public void Constructor_WhenLunarDayOutOfRange_ShouldThrowArgumentOutOfRangeException(int lunarDay)
	{
		var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
		{
			_ = new ChineseLunisolarNotableDateCalculator(1, lunarDay);
		});

		Assert.AreEqual("lunarDay", ex.ParamName);
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
		var sut = new ChineseLunisolarNotableDateCalculator(5, 5);

		var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
		{
			_ = sut.GetDate(year);
		});

		Assert.AreEqual("year", ex.ParamName);
	}

	/// <summary>
	/// Verifies that years outside the supported ChineseLunisolarCalendar range return <see langword="null" />.
	/// </summary>
	[DataRow(1)]
	[DataRow(1900)]
	[DataRow(2101)]
	[DataRow(9999)]
	[TestMethod]
	public void GetDate_WhenYearOutsideSupportedRange_ShouldReturnNull(int year)
	{
		var sut = new ChineseLunisolarNotableDateCalculator(5, 5);

		Assert.IsNull(sut.GetDate(year));
	}

	/// <summary>
	/// Verifies that the Lantern Festival (month 1, day 15) returns known correct dates.
	/// </summary>
	[DataRow(2020, 2, 8)]
	[DataRow(2022, 2, 15)]
	[DataRow(2023, 2, 5)]
	[DataRow(2024, 2, 24)]
	[TestMethod]
	public void GetDate_WhenLanternFestival_ShouldReturnExpectedDate(int year, int expectedMonth, int expectedDay)
	{
		var sut = new ChineseLunisolarNotableDateCalculator(1, 15);

		DateTime? result = sut.GetDate(year);

		Assert.IsNotNull(result);
		Assert.AreEqual(new DateTime(year, expectedMonth, expectedDay), result!.Value);
		Assert.AreEqual(DateTimeKind.Unspecified, result.Value.Kind);
	}

	/// <summary>
	/// Verifies that the Dragon Boat Festival (month 5, day 5) returns known correct dates.
	/// </summary>
	[DataRow(2022, 6, 3)]
	[DataRow(2023, 6, 22)]
	[DataRow(2024, 6, 10)]
	[TestMethod]
	public void GetDate_WhenDragonBoatFestival_ShouldReturnExpectedDate(int year, int expectedMonth, int expectedDay)
	{
		var sut = new ChineseLunisolarNotableDateCalculator(5, 5);

		DateTime? result = sut.GetDate(year);

		Assert.IsNotNull(result);
		Assert.AreEqual(new DateTime(year, expectedMonth, expectedDay), result!.Value);
	}

	/// <summary>
	/// Verifies that the Mid-Autumn Festival (month 8, day 15) returns known correct dates.
	/// </summary>
	[DataRow(2022, 9, 10)]
	[DataRow(2023, 9, 29)]
	[DataRow(2024, 9, 17)]
	[TestMethod]
	public void GetDate_WhenMidAutumnFestival_ShouldReturnExpectedDate(int year, int expectedMonth, int expectedDay)
	{
		var sut = new ChineseLunisolarNotableDateCalculator(8, 15);

		DateTime? result = sut.GetDate(year);

		Assert.IsNotNull(result);
		Assert.AreEqual(new DateTime(year, expectedMonth, expectedDay), result!.Value);
	}

	/// <summary>
	/// Verifies that the Double Ninth Festival (month 9, day 9) returns known correct dates.
	/// </summary>
	[DataRow(2022, 10, 4)]
	[DataRow(2023, 10, 23)]
	[DataRow(2024, 10, 11)]
	[TestMethod]
	public void GetDate_WhenDoubleNinthFestival_ShouldReturnExpectedDate(int year, int expectedMonth, int expectedDay)
	{
		var sut = new ChineseLunisolarNotableDateCalculator(9, 9);

		DateTime? result = sut.GetDate(year);

		Assert.IsNotNull(result);
		Assert.AreEqual(new DateTime(year, expectedMonth, expectedDay), result!.Value);
	}

	/// <summary>
	/// Verifies that the returned <see cref="DateTime.Kind" /> is always <see cref="DateTimeKind.Unspecified" />.
	/// </summary>
	[TestMethod]
	public void GetDate_WhenCalendarIsNull_ShouldReturnUnspecifiedKind()
	{
		var sut = new ChineseLunisolarNotableDateCalculator(8, 15);

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
		var sut = new ChineseLunisolarNotableDateCalculator(8, 15);

		DateTime? withNull = sut.GetDate(2024, calendar: null);
		DateTime? withGregorian = sut.GetDate(2024, new SysGlob.GregorianCalendar());

		Assert.IsNotNull(withNull);
		Assert.IsNotNull(withGregorian);
		Assert.AreEqual(withNull!.Value, withGregorian!.Value);
	}
}
