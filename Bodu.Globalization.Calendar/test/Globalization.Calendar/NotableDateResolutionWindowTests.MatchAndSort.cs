// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateResolutionWindowTests.MatchAndSort.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Immutable;
using SysGlobal = System.Globalization;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies the remaining branches of <see cref="NotableDateResolutionWindow" /> — the inside-range
/// <see cref="NotableDateResolutionWindow.Contains" /> path, the calendar-mismatch and territory-mismatch arms of
/// <c>MatchesContext</c>, the name-mismatch <c>continue</c> in <see cref="NotableDateResolutionWindow.ResolveByName" />,
/// and the <c>CalendarType?.FullName</c> sort key in the internal <c>Sort</c> helper.
/// </summary>
[TestClass]
public sealed class NotableDateResolutionWindowMatchAndSortTests
{
	/// <summary>
	/// Verifies that <see cref="NotableDateResolutionWindow.Contains" /> returns <see langword="true" /> for a date
	/// that lies strictly between the configured <c>StartDate</c> and <c>EndDate</c>.
	/// </summary>
	[TestMethod]
	public void Contains_WhenDateIsBetweenStartAndEnd_ShouldReturnTrue()
	{
		NotableDateResolutionWindow window = new(
			new DateTime(2024, 1, 1),
			new DateTime(2024, 12, 31));

		Assert.IsTrue(window.Contains(new DateTime(2024, 6, 15)));
	}

	/// <summary>
	/// Verifies that <see cref="NotableDateResolutionWindow.Contains" /> returns <see langword="false" /> for a date
	/// strictly before the configured <c>StartDate</c>.
	/// </summary>
	[TestMethod]
	public void Contains_WhenDateIsBeforeStart_ShouldReturnFalse()
	{
		NotableDateResolutionWindow window = new(
			new DateTime(2024, 6, 1),
			new DateTime(2024, 6, 30));

		Assert.IsFalse(window.Contains(new DateTime(2024, 5, 31)));
	}

	/// <summary>
	/// Verifies that <see cref="NotableDateResolutionWindow.Contains" /> returns <see langword="false" /> for a date
	/// strictly after the configured <c>EndDate</c>.
	/// </summary>
	[TestMethod]
	public void Contains_WhenDateIsAfterEnd_ShouldReturnFalse()
	{
		NotableDateResolutionWindow window = new(
			new DateTime(2024, 6, 1),
			new DateTime(2024, 6, 30));

		Assert.IsFalse(window.Contains(new DateTime(2024, 7, 1)));
	}

	/// <summary>
	/// Verifies that <see cref="NotableDateResolutionWindow.ResolveByName" /> iterates past stored dates whose name
	/// does not match before returning the matching entry — exercising the <c>continue</c> branch in the foreach.
	/// </summary>
	[TestMethod]
	public void ResolveByName_WhenLeadingStoredDatesDoNotMatchName_ShouldSkipAndReturnLaterMatch()
	{
		NotableDateResolutionWindow window = new(
			new DateTime(2024, 1, 1),
			new DateTime(2024, 12, 31));

		// Two entries share the year; ResolveByName must skip the first (name mismatch) and return the second.
		window.AddBlocker(BuildNotable("Skip Me", new DateTime(2024, 1, 10)));
		window.AddBlocker(BuildNotable("Target", new DateTime(2024, 6, 1)));

		Assert.AreEqual(new DateTime(2024, 6, 1), window.ResolveByName("Target", 2024, null, null));
	}

	/// <summary>
	/// Verifies that <see cref="NotableDateResolutionWindow.ResolveByName" /> rejects a stored entry whose calendar
	/// type differs from the requested calendar context — exercising the calendar-mismatch arm of
	/// <c>MatchesContext</c>.
	/// </summary>
	[TestMethod]
	public void ResolveByName_WhenStoredCalendarDiffersFromRequest_ShouldReturnNull()
	{
		NotableDateResolutionWindow window = new(
			new DateTime(2024, 1, 1),
			new DateTime(2024, 12, 31));

		window.AddBlocker(BuildNotable(
			"Rosh Hashanah",
			new DateTime(2024, 10, 3),
			calendarType: typeof(SysGlobal.HebrewCalendar)));

		Assert.IsNull(window.ResolveByName(
			"Rosh Hashanah",
			2024,
			territoryCode: null,
			calendarType: typeof(SysGlobal.GregorianCalendar)));
	}

	/// <summary>
	/// Verifies that <see cref="NotableDateResolutionWindow.ResolveByName" /> rejects a stored entry when the
	/// requested territory string cannot be parsed by <see cref="TerritoryCode" />, surfacing as a no-match (the
	/// requested-side <c>TryParse</c> false-branch of <c>MatchesContext</c>).
	/// </summary>
	[TestMethod]
	public void ResolveByName_WhenRequestedTerritoryIsMalformed_ShouldReturnNull()
	{
		NotableDateResolutionWindow window = new(
			new DateTime(2024, 1, 1),
			new DateTime(2024, 12, 31));

		window.AddBlocker(BuildNotable("Holiday", new DateTime(2024, 6, 10), territoryCode: "AU"));

		Assert.IsNull(window.ResolveByName("Holiday", 2024, territoryCode: "lowercase-only", calendarType: null));
	}

	/// <summary>
	/// Verifies that <see cref="NotableDateResolutionWindow.ResolveByName" /> rejects a stored entry when its own
	/// territory string cannot be parsed by <see cref="TerritoryCode" /> (the stored-side <c>TryParse</c> false-branch
	/// of <c>MatchesContext</c>).
	/// </summary>
	[TestMethod]
	public void ResolveByName_WhenStoredTerritoryIsMalformed_ShouldReturnNull()
	{
		NotableDateResolutionWindow window = new(
			new DateTime(2024, 1, 1),
			new DateTime(2024, 12, 31));

		// The stored entry has a territory code that does not match the TerritoryCode pattern (no uppercase
		// ISO 3166 component); the parse on the stored side fails, MatchesContext returns false, and the lookup
		// returns null.
		window.AddBlocker(BuildNotable("Holiday", new DateTime(2024, 6, 10), territoryCode: "lowercase-only"));

		Assert.IsNull(window.ResolveByName("Holiday", 2024, territoryCode: "AU", calendarType: null));
	}

	/// <summary>
	/// Verifies that <see cref="NotableDateResolutionWindow.OutputDates" /> applies the <c>CalendarType?.FullName</c>
	/// fallback sort key after date, name, and territory ties — the only branch through the inner
	/// <c>ThenBy(date => date.CalendarType?.FullName, ...)</c> clause.
	/// </summary>
	[TestMethod]
	public void OutputDates_WhenEntriesTieOnAllPriorKeys_ShouldOrderByCalendarTypeFullName()
	{
		NotableDateResolutionWindow window = new(
			new DateTime(2024, 1, 1),
			new DateTime(2024, 12, 31));

		// Identical name, date, and territory; differ only on CalendarType, so the inner ThenBy(CalendarType?.FullName)
		// is the deciding clause. HebrewCalendar.FullName sorts before JulianCalendar.FullName alphabetically by
		// System.Globalization.HebrewCalendar < System.Globalization.JulianCalendar.
		window.AddAdjusted(BuildNotable(
			"Conflict",
			new DateTime(2024, 6, 1),
			territoryCode: "AU",
			calendarType: typeof(SysGlobal.JulianCalendar)));
		window.AddAdjusted(BuildNotable(
			"Conflict",
			new DateTime(2024, 6, 1),
			territoryCode: "AU",
			calendarType: typeof(SysGlobal.HebrewCalendar)));

		IReadOnlyList<NotableDate> emitted = window.OutputDates;

		Assert.AreEqual(2, emitted.Count);
		Assert.AreEqual(typeof(SysGlobal.HebrewCalendar), emitted[0].CalendarType);
		Assert.AreEqual(typeof(SysGlobal.JulianCalendar), emitted[1].CalendarType);
	}

	/// <summary>
	/// Verifies that <see cref="NotableDateResolutionWindow.OutputDates" /> places a calendar-neutral entry
	/// (<c>CalendarType == null</c>) before an entry with a non-null calendar type, because <c>CalendarType?.FullName</c>
	/// is <see langword="null" /> and the default ordinal-string comparer puts <see langword="null" /> first.
	/// </summary>
	[TestMethod]
	public void OutputDates_WhenOnlyCalendarTypeDiffers_ShouldPlaceNullCalendarBeforeNonNull()
	{
		NotableDateResolutionWindow window = new(
			new DateTime(2024, 1, 1),
			new DateTime(2024, 12, 31));

		window.AddAdjusted(BuildNotable(
			"Conflict",
			new DateTime(2024, 6, 1),
			territoryCode: "AU",
			calendarType: typeof(SysGlobal.HebrewCalendar)));
		window.AddAdjusted(BuildNotable(
			"Conflict",
			new DateTime(2024, 6, 1),
			territoryCode: "AU",
			calendarType: null));

		IReadOnlyList<NotableDate> emitted = window.OutputDates;

		Assert.AreEqual(2, emitted.Count);
		Assert.IsNull(emitted[0].CalendarType);
		Assert.AreEqual(typeof(SysGlobal.HebrewCalendar), emitted[1].CalendarType);
	}

	private static NotableDate BuildNotable(
		string name,
		DateTime date,
		string? territoryCode = null,
		Type? calendarType = null) =>
		new()
		{
			Name = name,
			Date = date.Date,
			Category = NotableDateCategory.Holiday,
			IsNonWorkingDay = false,
			TerritoryCode = territoryCode,
			CalendarType = calendarType,
			Tags = ImmutableHashSet<string>.Empty,
		};
}
