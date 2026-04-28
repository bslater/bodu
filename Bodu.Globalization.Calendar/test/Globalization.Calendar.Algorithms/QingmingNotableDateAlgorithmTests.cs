// ---------------------------------------------------------------------------------------------------------------
// <copyright file="QingmingNotableDateAlgorithmTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Algorithms;

/// <summary>
/// Verifies the correctness and boundary behaviour of <see cref="QingmingNotableDateAlgorithm" />.
/// </summary>
[TestClass]
public sealed class QingmingNotableDateAlgorithmTests
{
	private readonly QingmingNotableDateAlgorithm _algorithm = new();

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
			_ = _algorithm.GetDate(year);
		});

		Assert.AreEqual("year", ex.ParamName);
	}

	/// <summary>
	/// Verifies that Qingming falls within one calendar day of the published astronomical date for years in the range
	/// 2020–2026. The algorithm computes solar-term transitions in Universal Time; published dates in East Asia use
	/// CST (UTC+8) and may differ by one calendar day when the transition falls near local midnight.
	/// </summary>
	[DataRow(2020, 4, 4)]
	[DataRow(2021, 4, 5)]
	[DataRow(2022, 4, 5)]
	[DataRow(2023, 4, 5)]
	[DataRow(2024, 4, 4)]
	[DataRow(2025, 4, 4)]
	[DataRow(2026, 4, 5)]
	[TestMethod]
	public void GetDate_WhenGivenKnownYears_ShouldFallWithinOneDayOfExpectedDate(int year, int expectedMonth, int expectedDay)
	{
		DateTime? result = _algorithm.GetDate(year);
		DateTime expected = new DateTime(year, expectedMonth, expectedDay);

		Assert.IsNotNull(result);
		int dayDiff = Math.Abs((result!.Value - expected).Days);
		Assert.IsTrue(dayDiff <= 1,
			$"Qingming {year}: expected within ±1 day of {expected:yyyy-MM-dd}, got {result.Value:yyyy-MM-dd}");
	}

	/// <summary>
	/// Verifies that for every year in the range 1901–2100 the result falls between 3 and 6 April inclusive. Qingming
	/// typically falls on 4 or 5 April; the bounds of 3 and 6 account for the algorithm's stated ±1 day accuracy and
	/// the long-term secular drift of the solar year across the full range.
	/// </summary>
	[TestMethod]
	public void GetDate_WhenIteratingSupportedRange_ShouldAlwaysFallBetweenApril3And6()
	{
		for (int year = 1901; year <= 2100; year++)
		{
			DateTime? result = _algorithm.GetDate(year);

			Assert.IsNotNull(result, $"GetDate returned null for year {year}.");
			Assert.AreEqual(4, result!.Value.Month, $"Expected April for year {year}.");
			Assert.IsTrue(result.Value.Day is >= 3 and <= 6,
				$"Expected day 3–6 for year {year}, got {result.Value.Day}.");
		}
	}

	/// <summary>
	/// Verifies that the returned <see cref="DateTime.Kind" /> is always <see cref="DateTimeKind.Unspecified" />.
	/// </summary>
	[TestMethod]
	public void GetDate_WhenCalendarIsNull_ShouldReturnUnspecifiedKind()
	{
		DateTime? result = _algorithm.GetDate(2024);

		Assert.IsNotNull(result);
		Assert.AreEqual(DateTimeKind.Unspecified, result!.Value.Kind);
	}
}
