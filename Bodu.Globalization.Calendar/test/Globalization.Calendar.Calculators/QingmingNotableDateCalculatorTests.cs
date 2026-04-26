// ---------------------------------------------------------------------------------------------------------------
// <copyright file="QingmingNotableDateCalculatorTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Calculators;

/// <summary>
/// Verifies the correctness and boundary behaviour of <see cref="QingmingNotableDateCalculator" />.
/// </summary>
[TestClass]
public sealed class QingmingNotableDateCalculatorTests
{
	private readonly QingmingNotableDateCalculator _calculator = new();

	/// <summary>
	/// Verifies that requesting Qingming with a year below one throws <see cref="ArgumentOutOfRangeException" />.
	/// </summary>
	[DataRow(0)]
	[DataRow(-1)]
	[DataRow(int.MinValue)]
	[TestMethod]
	public void GetDate_WhenYearLessThanOne_ShouldThrowArgumentOutOfRangeException(int year)
	{
		var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
		{
			_ = _calculator.GetDate(year);
		});

		Assert.AreEqual("year", ex.ParamName);
	}

	/// <summary>
	/// Verifies that Qingming returns known correct dates for years in the range 2020–2026. All known dates fall on
	/// 4 or 5 April; the algorithm is expected to be accurate to within one calendar day.
	/// </summary>
	[DataRow(2020, 4, 4)]
	[DataRow(2021, 4, 5)]
	[DataRow(2022, 4, 5)]
	[DataRow(2023, 4, 5)]
	[DataRow(2024, 4, 4)]
	[DataRow(2025, 4, 4)]
	[DataRow(2026, 4, 5)]
	[TestMethod]
	public void GetDate_WhenGivenKnownYears_ShouldReturnExpectedDate(int year, int expectedMonth, int expectedDay)
	{
		DateTime? result = _calculator.GetDate(year);

		Assert.IsNotNull(result);
		Assert.AreEqual(new DateTime(year, expectedMonth, expectedDay), result!.Value,
			$"Qingming {year}: expected {expectedMonth:D2}/{expectedDay:D2}, got {result.Value:yyyy-MM-dd}");
	}

	/// <summary>
	/// Verifies that for every year in the range 1901–2100 the result falls on either 4 or 5 April.
	/// </summary>
	[TestMethod]
	public void GetDate_WhenIteratingSupportedRange_ShouldAlwaysFallOnApril4Or5()
	{
		for (int year = 1901; year <= 2100; year++)
		{
			DateTime? result = _calculator.GetDate(year);

			Assert.IsNotNull(result, $"GetDate returned null for year {year}.");
			Assert.AreEqual(4, result!.Value.Month, $"Expected April for year {year}.");
			Assert.IsTrue(result.Value.Day is 4 or 5,
				$"Expected day 4 or 5 for year {year}, got {result.Value.Day}.");
		}
	}

	/// <summary>
	/// Verifies that the returned <see cref="DateTime.Kind" /> is always <see cref="DateTimeKind.Unspecified" />.
	/// </summary>
	[TestMethod]
	public void GetDate_WhenCalendarIsNull_ShouldReturnUnspecifiedKind()
	{
		DateTime? result = _calculator.GetDate(2024);

		Assert.IsNotNull(result);
		Assert.AreEqual(DateTimeKind.Unspecified, result!.Value.Kind);
	}
}
