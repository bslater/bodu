// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateRuleResolverTests.HebrewAlias.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using SysGlobal = System.Globalization;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies the Hebrew month alias resolution path of <see cref="NotableDateRuleResolver" />. Aliases whose
/// integer index depends on whether the Hebrew year is a leap year are exercised through
/// <see cref="DateResolutionStrategy.Fixed" /> rules authored against <see cref="SysGlobal.HebrewCalendar" />.
/// </summary>
[TestClass]
public sealed class NotableDateRuleResolverHebrewAliasTests
{
	/// <summary>
	/// Verifies that the <c>Nisan</c> alias resolves to a valid Gregorian date in <see cref="SysGlobal.HebrewCalendar" />.
	/// In a common Hebrew year Nisan is month 7; in a leap Hebrew year it shifts to month 8.
	/// </summary>
	[TestMethod]
	public void ResolveAnchorDate_WhenAliasIsNisan_ShouldResolveWithinRequestedYear()
	{
		NotableDateRule rule = new()
		{
			Name = "Passover",
			Strategy = DateResolutionStrategy.Fixed,
			Category = NotableDateCategory.Religious,
			Day = 15,
			CalendarMonthAlias = "Nisan",
			CalendarType = typeof(SysGlobal.HebrewCalendar),
			SweepCalendarYears = true,
		};

		NotableDateRuleResolver resolver = new(new[] { rule });

		DateTime? date = resolver.ResolveAnchorDate(rule, 2026);

		Assert.IsNotNull(date);
		Assert.AreEqual(2026, date!.Value.Year);

		SysGlobal.HebrewCalendar hebrew = new();
		Assert.AreEqual(15, hebrew.GetDayOfMonth(date.Value));

		var monthsInYear = hebrew.GetMonthsInYear(hebrew.GetYear(date.Value));
		var isLeap = monthsInYear == 13;
		Assert.AreEqual(isLeap ? 8 : 7, hebrew.GetMonth(date.Value));
	}

	/// <summary>
	/// Verifies that the <c>Av</c> alias resolves to a Gregorian date inside the requested civil year.
	/// </summary>
	[TestMethod]
	public void ResolveAnchorDate_WhenAliasIsAv_ShouldResolveWithinRequestedYear()
	{
		NotableDateRule rule = new()
		{
			Name = "Tisha B'Av",
			Strategy = DateResolutionStrategy.Fixed,
			Category = NotableDateCategory.Religious,
			Day = 9,
			CalendarMonthAlias = "Av",
			CalendarType = typeof(SysGlobal.HebrewCalendar),
			SweepCalendarYears = true,
		};

		NotableDateRuleResolver resolver = new(new[] { rule });

		DateTime? date = resolver.ResolveAnchorDate(rule, 2026);

		Assert.IsNotNull(date);
		Assert.AreEqual(2026, date!.Value.Year);
	}

	/// <summary>
	/// Verifies that the <c>LastAdar</c> alias resolves successfully — in a common year it maps onto month 6, in a leap
	/// year it maps onto month 7.
	/// </summary>
	[TestMethod]
	public void ResolveAnchorDate_WhenAliasIsLastAdar_ShouldResolveSuccessfully()
	{
		NotableDateRule rule = new()
		{
			Name = "Last Adar Marker",
			Strategy = DateResolutionStrategy.Fixed,
			Category = NotableDateCategory.Religious,
			Day = 14,
			CalendarMonthAlias = "LastAdar",
			CalendarType = typeof(SysGlobal.HebrewCalendar),
			SweepCalendarYears = true,
		};

		NotableDateRuleResolver resolver = new(new[] { rule });

		DateTime? date = resolver.ResolveAnchorDate(rule, 2026);

		Assert.IsNotNull(date);
	}

	/// <summary>
	/// Verifies that an unrecognised alias resolves to <see langword="null" />.
	/// </summary>
	[TestMethod]
	public void ResolveAnchorDate_WhenAliasIsUnknown_ShouldReturnNull()
	{
		NotableDateRule rule = new()
		{
			Name = "Unknown Alias",
			Strategy = DateResolutionStrategy.Fixed,
			Category = NotableDateCategory.Religious,
			Day = 1,
			CalendarMonthAlias = "Smarchadar",
			CalendarType = typeof(SysGlobal.HebrewCalendar),
			SweepCalendarYears = true,
		};

		NotableDateRuleResolver resolver = new(new[] { rule });

		DateTime? date = resolver.ResolveAnchorDate(rule, 2026);

		Assert.IsNull(date);
	}

	/// <summary>
	/// Verifies that the simple Hebrew month <c>Heshvan</c> resolves consistently (it has a fixed position regardless of
	/// leap-year shape) when authored as an explicit integer rather than via the alias slot.
	/// </summary>
	[TestMethod]
	public void ResolveAnchorDate_WhenSimpleHebrewMonthAuthoredAsInteger_ShouldResolveWithinRequestedYear()
	{
		NotableDateRule rule = new()
		{
			Name = "Heshvan Observance",
			Strategy = DateResolutionStrategy.Fixed,
			Category = NotableDateCategory.Religious,
			Month = 2,
			Day = 7,
			CalendarType = typeof(SysGlobal.HebrewCalendar),
			SweepCalendarYears = true,
		};

		NotableDateRuleResolver resolver = new(new[] { rule });

		DateTime? date = resolver.ResolveAnchorDate(rule, 2026);

		Assert.IsNotNull(date);
		Assert.AreEqual(2026, date!.Value.Year);
	}
}
