// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateResolutionAdjustmentProcessorTests.WindowExpansion.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Immutable;
using Bodu.Extensions;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies the forward / backward window-expansion paths in the <c>EnsureKnown</c> closure of
/// <see cref="NotableDateResolutionAdjustmentProcessor.ApplyAdjustments" />. These tests exercise the
/// closure's two cross-window branches by setting up adjustment chains that walk past the supplied
/// resolution-window bounds while looking for a non-working day.
/// </summary>
[TestClass]
public sealed class NotableDateResolutionAdjustmentProcessorWindowExpansionTests
{
	/// <summary>
	/// Verifies that an adjustment whose chain walks forward past the request <c>EndDate</c> triggers the
	/// forward-expansion branch of <c>EnsureKnown</c>: the resolution window expands to include the substitute
	/// date, the substitute is emitted, and the materialised-date set is updated without an exception.
	/// </summary>
	[TestMethod]
	public void ApplyAdjustments_WhenSubstituteWalkExtendsPastRequestEnd_ShouldExpandKnownWindowForward()
	{
		// 31 Dec 2022 (Saturday) → MoveToNextNonWorkingDay walks Sun 1 Jan → Mon 2 Jan.
		NotableDateRule rule = FixedWithAdjustment(
			"Year-End Holiday",
			month: 12,
			day: 31,
			trigger: AdjustmentTrigger.IfWeekend,
			action: AdjustmentAction.MoveToNextNonWorkingDay);

		ResolvedNotableDateOccurrence occurrence = Occurrence(rule, new DateTime(2022, 12, 31));

		NotableDateResolutionWindow window = new(
			new DateTime(2022, 12, 30),
			new DateTime(2022, 12, 31));
		window.AddBase(occurrence);

		NotableDateResolutionAdjustmentProcessor processor = new(CalendarWeekendDefinition.SaturdaySunday);
		processor.ApplyAdjustments(
			window,
			new NotableDateResolutionRequest(
				new DateTime(2022, 12, 30),
				new DateTime(2022, 12, 31),
				NotableDateResolutionProjection.AnchorDate),
			new[] { occurrence });

		// AnchorDate projection emits the adjusted date even though it landed beyond the request end.
		Assert.IsTrue(window.OutputDates.Any(d => d.WasAdjusted && d.Date.Year == 2023),
			"Expected the substitute date in 2023 to be emitted under AnchorDate projection.");

		// The window must have expanded forward past the original end.
		Assert.IsTrue(window.EndDate >= new DateTime(2023, 1, 2),
			$"Expected the window to expand forward past 2 Jan 2023, got {window.EndDate:yyyy-MM-dd}.");
	}

	/// <summary>
	/// Verifies that an adjustment whose chain walks backward past the request <c>StartDate</c> triggers the
	/// backward-expansion branch of <c>EnsureKnown</c>: the resolution window expands backward, the substitute is
	/// emitted, and processing completes without an exception.
	/// </summary>
	[TestMethod]
	public void ApplyAdjustments_WhenSubstituteWalkExtendsBeforeRequestStart_ShouldExpandKnownWindowBackward()
	{
		// 1 Jan 2022 (Saturday) → MoveToPreviousWeekday walks Fri 31 Dec 2021.
		NotableDateRule rule = FixedWithAdjustment(
			"New Year's Day Backward",
			month: 1,
			day: 1,
			trigger: AdjustmentTrigger.IfWeekend,
			action: AdjustmentAction.MoveToPreviousWeekday);

		ResolvedNotableDateOccurrence occurrence = Occurrence(rule, new DateTime(2022, 1, 1));

		NotableDateResolutionWindow window = new(
			new DateTime(2022, 1, 1),
			new DateTime(2022, 1, 5));
		window.AddBase(occurrence);

		NotableDateResolutionAdjustmentProcessor processor = new(CalendarWeekendDefinition.SaturdaySunday);
		processor.ApplyAdjustments(
			window,
			new NotableDateResolutionRequest(
				new DateTime(2022, 1, 1),
				new DateTime(2022, 1, 5),
				NotableDateResolutionProjection.AnchorDate),
			new[] { occurrence });

		// AnchorDate projection emits the adjusted date even though it landed before the request start.
		Assert.IsTrue(window.OutputDates.Any(d => d.WasAdjusted && d.Date.Year == 2021),
			"Expected the substitute date in 2021 to be emitted under AnchorDate projection.");

		// The window must have expanded backward past the original start.
		Assert.IsTrue(window.StartDate <= new DateTime(2021, 12, 31),
			$"Expected the window to expand backward past 31 Dec 2021, got {window.StartDate:yyyy-MM-dd}.");
	}

	/// <summary>
	/// Verifies that a chained substitute landing exactly on the same day as the original anchor is suppressed (the
	/// early-return inside <c>ApplyOccurrenceAdjustments</c>), and the materialised-date set contains the anchor
	/// without an extra entry being added by <c>EnsureKnown</c>.
	/// </summary>
	[TestMethod]
	public void ApplyAdjustments_WhenAlreadyMaterialisedDateInsideWindow_ShouldNotEmitAdditionalAdjustedDate()
	{
		NotableDateRule rule = FixedWithAdjustment(
			"Same-Day No-Op",
			month: 6,
			day: 15,
			trigger: AdjustmentTrigger.Always,
			action: AdjustmentAction.AddDays,
			offsetDays: 0);

		ResolvedNotableDateOccurrence occurrence = Occurrence(rule, new DateTime(2024, 6, 15));

		NotableDateResolutionWindow window = new(new DateTime(2024, 6, 1), new DateTime(2024, 6, 30));
		window.AddBase(occurrence);

		NotableDateResolutionAdjustmentProcessor processor = new(CalendarWeekendDefinition.SaturdaySunday);
		processor.ApplyAdjustments(
			window,
			new NotableDateResolutionRequest(
				new DateTime(2024, 6, 1),
				new DateTime(2024, 6, 30),
				NotableDateResolutionProjection.ObservedDate),
			new[] { occurrence });

		Assert.AreEqual(1, window.OutputDates.Count);
		Assert.IsFalse(window.OutputDates[0].WasAdjusted);
	}

	private static NotableDateRule FixedWithAdjustment(
		string name,
		int month,
		int day,
		AdjustmentTrigger trigger,
		AdjustmentAction action,
		int offsetDays = 0) =>
		new()
		{
			Name = name,
			Strategy = DateResolutionStrategy.Fixed,
			Category = NotableDateCategory.Holiday,
			Month = month,
			Day = day,
			IsNonWorkingDay = true,
			Adjustments = ImmutableArray.Create(new ObservanceAdjustment
			{
				Key = "test-adjustment",
				Trigger = trigger,
				Action = action,
				OffsetDays = offsetDays,
				IsNonWorkingDay = true,
			}),
		};

	private static ResolvedNotableDateOccurrence Occurrence(NotableDateRule rule, DateTime date)
	{
		NotableDate notable = new()
		{
			Name = rule.Name,
			Date = date.Date,
			Category = rule.Category,
			DurationDays = Math.Max(1, rule.DurationDays),
			IsNonWorkingDay = rule.IsNonWorkingDay ?? false,
			CalendarType = rule.CalendarType,
			TerritoryCode = rule.TerritoryCode,
			Tags = rule.Tags,
			Comment = rule.Comment,
		};

		return new ResolvedNotableDateOccurrence(
			rule,
			date.Date,
			rule.TerritoryCode,
			notable);
	}
}
