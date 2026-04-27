// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateServiceTests.Filter.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Immutable;

namespace Bodu.Globalization.Calendar;

public sealed partial class NotableDateServiceTests
{
	private static NotableDateRule FixedWithTags(string name, int month, int day, NotableDateCategory category, bool nonWorking, ImmutableHashSet<string> tags) =>
		new()
		{
			Name = name,
			Strategy = DateResolutionStrategy.Fixed,
			Category = category,
			Month = month,
			Day = day,
			IsNonWorkingDay = nonWorking,
			Tags = tags,
		};

	// --------------------------------------------------------------------------------------
	// GetNotableDates(year, filter)
	// --------------------------------------------------------------------------------------

	/// <summary>
	/// Verifies that <see cref="INotableDateService.GetNotableDates(int, NotableDateFilter, string?, Type?)" /> returns only dates
	/// matching the category filter and excludes all others.
	/// </summary>
	[TestMethod]
	public void GetNotableDates_WithYearAndCategoryFilter_ShouldReturnOnlyMatchingCategory()
	{
		NotableDateService service = BuildService(
			Fixed("Holiday A", 1, 1, NotableDateCategory.Holiday),
			Fixed("Observance B", 3, 15, NotableDateCategory.Observance),
			Fixed("Holiday C", 12, 25, NotableDateCategory.Holiday));

		NotableDateFilter filter = NotableDateFilter.ForCategory(NotableDateCategory.Holiday);
		IReadOnlyList<NotableDate> results = service.GetNotableDates(2024, filter);

		Assert.AreEqual(2, results.Count);
		Assert.IsTrue(results.All(d => d.Category == NotableDateCategory.Holiday));
	}

	/// <summary>
	/// Verifies that <see cref="INotableDateService.GetNotableDates(int, NotableDateFilter, string?, Type?)" /> returns no dates
	/// when the filter matches no rules.
	/// </summary>
	[TestMethod]
	public void GetNotableDates_WithYearAndFilterMatchingNoRules_ShouldReturnEmptyList()
	{
		NotableDateService service = BuildService(
			Fixed("Holiday A", 1, 1, NotableDateCategory.Holiday),
			Fixed("Holiday B", 12, 25, NotableDateCategory.Holiday));

		NotableDateFilter filter = NotableDateFilter.ForCategory(NotableDateCategory.Cultural);
		IReadOnlyList<NotableDate> results = service.GetNotableDates(2024, filter);

		Assert.AreEqual(0, results.Count);
	}

	/// <summary>
	/// Verifies that <see cref="INotableDateService.GetNotableDates(int, NotableDateFilter, string?, Type?)" /> returns dates
	/// matching any of the supplied categories when an Or filter is used.
	/// </summary>
	[TestMethod]
	public void GetNotableDates_WithYearAndOrCategoryFilter_ShouldReturnBothMatchingCategories()
	{
		NotableDateService service = BuildService(
			Fixed("Holiday A", 1, 1, NotableDateCategory.Holiday),
			Fixed("Observance B", 3, 15, NotableDateCategory.Observance),
			Fixed("Cultural C", 6, 1, NotableDateCategory.Cultural));

		NotableDateFilter filter = NotableDateFilter.ForCategory(NotableDateCategory.Holiday)
			.Or(NotableDateFilter.ForCategory(NotableDateCategory.Observance));

		IReadOnlyList<NotableDate> results = service.GetNotableDates(2024, filter);

		Assert.AreEqual(2, results.Count);
		Assert.IsTrue(results.All(d => d.Category == NotableDateCategory.Holiday || d.Category == NotableDateCategory.Observance));
	}

	/// <summary>
	/// Verifies that <see cref="INotableDateService.GetNotableDates(int, NotableDateFilter, string?, Type?)" /> returns only dates
	/// matching all criteria when an And filter is used.
	/// </summary>
	[TestMethod]
	public void GetNotableDates_WithYearAndAndFilter_ShouldReturnIntersection()
	{
		NotableDateService service = BuildService(
			FixedWithTags("Holiday Public", 1, 1, NotableDateCategory.Holiday, nonWorking: true, ImmutableHashSet.Create("Public")),
			FixedWithTags("Holiday Private", 3, 15, NotableDateCategory.Holiday, nonWorking: false, ImmutableHashSet.Create("Regional")),
			Fixed("Observance", 6, 1, NotableDateCategory.Observance, nonWorking: true));

		NotableDateFilter filter = NotableDateFilter.ForCategory(NotableDateCategory.Holiday)
			.And(NotableDateFilter.IsNonWorkingDay());

		IReadOnlyList<NotableDate> results = service.GetNotableDates(2024, filter);

		Assert.AreEqual(1, results.Count);
		Assert.AreEqual("Holiday Public", results[0].Name);
	}

	/// <summary>
	/// Verifies that <see cref="INotableDateService.GetNotableDates(int, NotableDateFilter, string?, Type?)" /> returns results
	/// ordered by anchor date.
	/// </summary>
	[TestMethod]
	public void GetNotableDates_WithYearAndFilter_ShouldReturnResultsOrderedByDate()
	{
		NotableDateService service = BuildService(
			Fixed("Holiday C", 12, 25, NotableDateCategory.Holiday),
			Fixed("Holiday A", 1, 1, NotableDateCategory.Holiday),
			Fixed("Holiday B", 7, 4, NotableDateCategory.Holiday));

		NotableDateFilter filter = NotableDateFilter.ForCategory(NotableDateCategory.Holiday);
		IReadOnlyList<NotableDate> results = service.GetNotableDates(2024, filter);

		Assert.AreEqual(3, results.Count);
		Assert.IsTrue(results[0].Date <= results[1].Date && results[1].Date <= results[2].Date);
	}

	/// <summary>
	/// Verifies that <see cref="INotableDateService.GetNotableDates(int, NotableDateFilter, string?, Type?)" /> throws
	/// <see cref="ArgumentNullException" /> when the filter is <see langword="null" />.
	/// </summary>
	[TestMethod]
	public void GetNotableDates_WithYearAndNullFilter_ShouldThrowArgumentNullException()
	{
		NotableDateService service = BuildService(Fixed("Holiday A", 1, 1));

		Assert.ThrowsExactly<ArgumentNullException>(() =>
		{
			_ = service.GetNotableDates(2024, null!);
		});
	}

	/// <summary>
	/// Verifies that a filtered query does not affect subsequent unfiltered queries, confirming that the per-year cache remains
	/// intact and returns complete results after a filtered call.
	/// </summary>
	[TestMethod]
	public void GetNotableDates_WithYearAndFilter_ShouldNotPolluteCacheForUnfilteredQuery()
	{
		NotableDateService service = BuildService(
			Fixed("Holiday A", 1, 1, NotableDateCategory.Holiday),
			Fixed("Observance B", 6, 1, NotableDateCategory.Observance));

		// Filtered call — should only return holidays.
		IReadOnlyList<NotableDate> filtered = service.GetNotableDates(2024, NotableDateFilter.ForCategory(NotableDateCategory.Holiday));

		// Unfiltered call — should return all dates including observances.
		IReadOnlyList<NotableDate> unfiltered = service.GetNotableDates(2024);

		Assert.AreEqual(1, filtered.Count);
		Assert.AreEqual(2, unfiltered.Count);
	}

	// --------------------------------------------------------------------------------------
	// GetNotableDates(startDate, endDate, filter)
	// --------------------------------------------------------------------------------------

	/// <summary>
	/// Verifies that <see cref="INotableDateService.GetNotableDates(DateTime, DateTime, NotableDateFilter, string?, Type?)" />
	/// returns only dates within the range that also match the filter.
	/// </summary>
	[TestMethod]
	public void GetNotableDates_WithDateRangeAndCategoryFilter_ShouldReturnMatchingDatesInRange()
	{
		NotableDateService service = BuildService(
			Fixed("Holiday Jan", 1, 1, NotableDateCategory.Holiday),
			Fixed("Observance Mar", 3, 15, NotableDateCategory.Observance),
			Fixed("Holiday Dec", 12, 25, NotableDateCategory.Holiday));

		NotableDateFilter filter = NotableDateFilter.ForCategory(NotableDateCategory.Holiday);
		IReadOnlyList<NotableDate> results = service.GetNotableDates(
			new DateTime(2024, 1, 1), new DateTime(2024, 6, 30), filter);

		Assert.AreEqual(1, results.Count);
		Assert.AreEqual("Holiday Jan", results[0].Name);
	}

	/// <summary>
	/// Verifies that <see cref="INotableDateService.GetNotableDates(DateTime, DateTime, NotableDateFilter, string?, Type?)" />
	/// throws <see cref="ArgumentNullException" /> when the filter is <see langword="null" />.
	/// </summary>
	[TestMethod]
	public void GetNotableDates_WithDateRangeAndNullFilter_ShouldThrowArgumentNullException()
	{
		NotableDateService service = BuildService(Fixed("Holiday A", 1, 1));

		Assert.ThrowsExactly<ArgumentNullException>(() =>
		{
			_ = service.GetNotableDates(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31), null!);
		});
	}

	// --------------------------------------------------------------------------------------
	// GetNotableDates(date, filter)
	// --------------------------------------------------------------------------------------

	/// <summary>
	/// Verifies that <see cref="INotableDateService.GetNotableDates(DateTime, NotableDateFilter, string?, Type?)" /> returns the
	/// notable date on that day when the filter matches.
	/// </summary>
	[TestMethod]
	public void GetNotableDates_WithSingleDateAndMatchingFilter_ShouldReturnDate()
	{
		NotableDateService service = BuildService(
			Fixed("Holiday A", 1, 1, NotableDateCategory.Holiday),
			Fixed("Observance B", 1, 1, NotableDateCategory.Observance));

		NotableDateFilter filter = NotableDateFilter.ForCategory(NotableDateCategory.Holiday);
		IReadOnlyList<NotableDate> results = service.GetNotableDates(new DateTime(2024, 1, 1), filter);

		Assert.AreEqual(1, results.Count);
		Assert.AreEqual("Holiday A", results[0].Name);
	}

	/// <summary>
	/// Verifies that <see cref="INotableDateService.GetNotableDates(DateTime, NotableDateFilter, string?, Type?)" /> returns an
	/// empty list when the filter excludes all dates on that day.
	/// </summary>
	[TestMethod]
	public void GetNotableDates_WithSingleDateAndNonMatchingFilter_ShouldReturnEmptyList()
	{
		NotableDateService service = BuildService(
			Fixed("Observance B", 1, 1, NotableDateCategory.Observance));

		NotableDateFilter filter = NotableDateFilter.ForCategory(NotableDateCategory.Holiday);
		IReadOnlyList<NotableDate> results = service.GetNotableDates(new DateTime(2024, 1, 1), filter);

		Assert.AreEqual(0, results.Count);
	}

	/// <summary>
	/// Verifies that <see cref="INotableDateService.GetNotableDates(DateTime, NotableDateFilter, string?, Type?)" /> throws
	/// <see cref="ArgumentNullException" /> when the filter is <see langword="null" />.
	/// </summary>
	[TestMethod]
	public void GetNotableDates_WithSingleDateAndNullFilter_ShouldThrowArgumentNullException()
	{
		NotableDateService service = BuildService(Fixed("Holiday A", 1, 1));

		Assert.ThrowsExactly<ArgumentNullException>(() =>
		{
			_ = service.GetNotableDates(new DateTime(2024, 1, 1), null!);
		});
	}

	// --------------------------------------------------------------------------------------
	// Tag filter integration
	// --------------------------------------------------------------------------------------

	/// <summary>
	/// Verifies that <see cref="NotableDateFilter.WithTag" /> filters service results correctly, returning only dates whose rule
	/// carries the specified tag.
	/// </summary>
	[TestMethod]
	public void GetNotableDates_WithTagFilter_ShouldReturnOnlyTaggedDates()
	{
		NotableDateService service = BuildService(
			FixedWithTags("Public Holiday", 1, 1, NotableDateCategory.Holiday, nonWorking: true, ImmutableHashSet.Create("Public")),
			FixedWithTags("Regional Holiday", 6, 1, NotableDateCategory.Holiday, nonWorking: true, ImmutableHashSet.Create("Regional")));

		NotableDateFilter filter = NotableDateFilter.WithTag("Public");
		IReadOnlyList<NotableDate> results = service.GetNotableDates(2024, filter);

		Assert.AreEqual(1, results.Count);
		Assert.AreEqual("Public Holiday", results[0].Name);
	}

	// --------------------------------------------------------------------------------------
	// Name filter integration
	// --------------------------------------------------------------------------------------

	/// <summary>
	/// Verifies that <see cref="NotableDateFilter.WithAnyName" /> returns only the named dates from the service.
	/// </summary>
	[TestMethod]
	public void GetNotableDates_WithAnyNameFilter_ShouldReturnOnlyNamedDates()
	{
		NotableDateService service = BuildService(
			Fixed("New Year's Day", 1, 1),
			Fixed("Easter Sunday", 4, 1),
			Fixed("Christmas Day", 12, 25));

		NotableDateFilter filter = NotableDateFilter.WithAnyName("New Year's Day", "Christmas Day");
		IReadOnlyList<NotableDate> results = service.GetNotableDates(2024, filter);

		Assert.AreEqual(2, results.Count);
		Assert.IsTrue(results.All(d => d.Name == "New Year's Day" || d.Name == "Christmas Day"));
	}
}
