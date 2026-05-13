// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateResolutionAdjustmentProcessorTests.EmissionContract.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Immutable;
using Bodu.Extensions;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies the emission contract of <see cref="NotableDateResolutionAdjustmentProcessor.ApplyAdjustments" />:
/// no-op adjustments (when the trigger fails or the moved date equals the anchor), the projection-based
/// <c>ShouldEmitAdjustedDate</c> branch, and the unsupported-projection guard.
/// </summary>
[TestClass]
public sealed class NotableDateResolutionAdjustmentProcessorEmissionContractTests
{
	/// <summary>
	/// Verifies that an <see cref="AdjustmentTrigger.IfWeekend" /> adjustment whose anchor falls on a weekday does not
	/// fire — the window emits the original occurrence only, with no extra entry tagged as adjusted.
	/// </summary>
	[TestMethod]
	public void ApplyAdjustments_WhenAdjustmentTriggerDoesNotFire_ShouldNotEmitAdjustedDate()
	{
		// 25 December 2024 falls on a Wednesday — IfWeekend should not activate.
		NotableDateRule rule = FixedWithAdjustment(
			"Christmas Day",
			month: 12,
			day: 25,
			trigger: AdjustmentTrigger.IfWeekend,
			action: AdjustmentAction.MoveToNextNonWorkingDay);

		ResolvedNotableDateOccurrence occurrence = Occurrence(rule, new DateTime(2024, 12, 25));

		NotableDateResolutionWindow window = new(new DateTime(2024, 12, 20), new DateTime(2024, 12, 31));
		window.AddBase(occurrence);

		NotableDateResolutionAdjustmentProcessor processor = new(CalendarWeekendDefinition.SaturdaySunday);
		processor.ApplyAdjustments(
			window,
			Request(new DateTime(2024, 12, 20), new DateTime(2024, 12, 31)),
			new[] { occurrence });

		IReadOnlyList<NotableDate> emitted = window.OutputDates;

		Assert.AreEqual(1, emitted.Count);
		Assert.IsFalse(emitted[0].WasAdjusted);
	}

	/// <summary>
	/// Verifies that an <see cref="AdjustmentAction.AddDays" /> adjustment with an offset of zero produces an adjusted
	/// date identical to the anchor and is suppressed because it would duplicate the base occurrence.
	/// </summary>
	[TestMethod]
	public void ApplyAdjustments_WhenAdjustedDateEqualsAnchor_ShouldNotEmitDuplicate()
	{
		NotableDateRule rule = new()
		{
			Name = "Holiday",
			Strategy = DateResolutionStrategy.Fixed,
			Category = NotableDateCategory.Holiday,
			Month = 6,
			Day = 15,
			IsNonWorkingDay = true,
			Adjustments = ImmutableArray.Create(new ObservanceAdjustment
			{
				Key = "no-shift",
				Trigger = AdjustmentTrigger.Always,
				Action = AdjustmentAction.AddDays,
				OffsetDays = 0,
			}),
		};

		ResolvedNotableDateOccurrence occurrence = Occurrence(rule, new DateTime(2024, 6, 15));

		NotableDateResolutionWindow window = new(new DateTime(2024, 6, 1), new DateTime(2024, 6, 30));
		window.AddBase(occurrence);

		NotableDateResolutionAdjustmentProcessor processor = new(CalendarWeekendDefinition.SaturdaySunday);
		processor.ApplyAdjustments(
			window,
			Request(new DateTime(2024, 6, 1), new DateTime(2024, 6, 30)),
			new[] { occurrence });

		IReadOnlyList<NotableDate> emitted = window.OutputDates;

		Assert.AreEqual(1, emitted.Count);
		Assert.IsFalse(emitted[0].WasAdjusted);
	}

	/// <summary>
	/// Verifies that under <see cref="NotableDateResolutionProjection.AnchorDate" /> semantics, an adjusted date that
	/// falls outside the request window is still emitted because the anchor itself is inside the window.
	/// </summary>
	[TestMethod]
	public void ApplyAdjustments_WhenAnchorProjectionAndAdjustedDateFallsOutsideWindow_ShouldStillEmitAdjusted()
	{
		// 31 December 2022 (Saturday) → MoveToNextNonWorkingDay lands on Monday 2 January 2023, outside the December window.
		NotableDateRule rule = FixedWithAdjustment(
			"Year-End",
			month: 12,
			day: 31,
			trigger: AdjustmentTrigger.IfWeekend,
			action: AdjustmentAction.MoveToNextNonWorkingDay);

		ResolvedNotableDateOccurrence occurrence = Occurrence(rule, new DateTime(2022, 12, 31));

		NotableDateResolutionWindow window = new(new DateTime(2022, 12, 1), new DateTime(2022, 12, 31));
		window.AddBase(occurrence);

		NotableDateResolutionAdjustmentProcessor processor = new(CalendarWeekendDefinition.SaturdaySunday);
		processor.ApplyAdjustments(
			window,
			new NotableDateResolutionRequest(
				new DateTime(2022, 12, 1),
				new DateTime(2022, 12, 31),
				NotableDateResolutionProjection.AnchorDate),
			new[] { occurrence });

		IReadOnlyList<NotableDate> emitted = window.OutputDates;

		Assert.IsTrue(emitted.Any(d => d.WasAdjusted && d.Date == new DateTime(2023, 1, 2)));
	}

	/// <summary>
	/// Verifies that under <see cref="NotableDateResolutionProjection.ObservedDate" /> semantics, an adjusted date that
	/// falls outside the request window is suppressed from emission (it is still recorded as a blocker).
	/// </summary>
	[TestMethod]
	public void ApplyAdjustments_WhenObservedProjectionAndAdjustedDateFallsOutsideWindow_ShouldNotEmitAdjusted()
	{
		NotableDateRule rule = FixedWithAdjustment(
			"Year-End",
			month: 12,
			day: 31,
			trigger: AdjustmentTrigger.IfWeekend,
			action: AdjustmentAction.MoveToNextNonWorkingDay);

		ResolvedNotableDateOccurrence occurrence = Occurrence(rule, new DateTime(2022, 12, 31));

		NotableDateResolutionWindow window = new(new DateTime(2022, 12, 1), new DateTime(2022, 12, 31));
		window.AddBase(occurrence);

		NotableDateResolutionAdjustmentProcessor processor = new(CalendarWeekendDefinition.SaturdaySunday);
		processor.ApplyAdjustments(
			window,
			Request(new DateTime(2022, 12, 1), new DateTime(2022, 12, 31)),
			new[] { occurrence });

		IReadOnlyList<NotableDate> emitted = window.OutputDates;

		// Only the base anchor (Saturday 31 Dec) should be in the output; the substitute lies in January.
		Assert.IsFalse(emitted.Any(d => d.WasAdjusted));
	}

	/// <summary>
	/// Verifies that constructing the processor with <see cref="CalendarWeekendDefinition.Custom" /> but no provider
	/// throws <see cref="ArgumentNullException" />.
	/// </summary>
	[TestMethod]
	public void Constructor_WhenCustomWeekendDefinitionAndProviderIsNull_ShouldThrowArgumentNullException()
	{
		ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
		{
			_ = new NotableDateResolutionAdjustmentProcessor(CalendarWeekendDefinition.Custom);
		});

		Assert.AreEqual("weekendProvider", ex.ParamName);
	}

	/// <summary>
	/// Verifies that calling <see cref="NotableDateResolutionAdjustmentProcessor.ApplyAdjustments" /> with an
	/// unsupported projection enum value throws <see cref="ArgumentOutOfRangeException" /> from the projection switch.
	/// </summary>
	[TestMethod]
	public void ApplyAdjustments_WhenProjectionIsUnsupported_ShouldThrowArgumentOutOfRangeException()
	{
		NotableDateRule rule = FixedWithAdjustment(
			"Holiday",
			month: 1,
			day: 1,
			trigger: AdjustmentTrigger.Always,
			action: AdjustmentAction.AddDays,
			offsetDays: 1);

		ResolvedNotableDateOccurrence occurrence = Occurrence(rule, new DateTime(2024, 1, 1));

		NotableDateResolutionWindow window = new(new DateTime(2024, 1, 1), new DateTime(2024, 1, 31));
		window.AddBase(occurrence);

		NotableDateResolutionAdjustmentProcessor processor = new(CalendarWeekendDefinition.SaturdaySunday);

		Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
		{
			processor.ApplyAdjustments(
				window,
				new NotableDateResolutionRequest(
					new DateTime(2024, 1, 1),
					new DateTime(2024, 1, 31),
					(NotableDateResolutionProjection)999),
				new[] { occurrence });
		});
	}

	/// <summary>
	/// Verifies that the adjustment evaluator records an <see cref="AdjustmentReason" /> referencing the original anchor
	/// even when the adjustment outcome equals the anchor date plus a non-zero offset.
	/// </summary>
	[TestMethod]
	public void ApplyAdjustments_WhenAddDaysOffsetMovesDate_ShouldEmitAdjustedWithExpectedReason()
	{
		NotableDateRule rule = FixedWithAdjustment(
			"Holiday",
			month: 6,
			day: 15,
			trigger: AdjustmentTrigger.Always,
			action: AdjustmentAction.AddDays,
			offsetDays: 3);

		ResolvedNotableDateOccurrence occurrence = Occurrence(rule, new DateTime(2024, 6, 15));

		NotableDateResolutionWindow window = new(new DateTime(2024, 6, 1), new DateTime(2024, 6, 30));
		window.AddBase(occurrence);

		NotableDateResolutionAdjustmentProcessor processor = new(CalendarWeekendDefinition.SaturdaySunday);
		processor.ApplyAdjustments(
			window,
			Request(new DateTime(2024, 6, 1), new DateTime(2024, 6, 30)),
			new[] { occurrence });

		NotableDate adjusted = window.OutputDates.Single(d => d.WasAdjusted);

		Assert.AreEqual(new DateTime(2024, 6, 18), adjusted.Date);
		Assert.IsNotNull(adjusted.AdjustmentReason);
		Assert.AreEqual(new DateTime(2024, 6, 15), adjusted.AdjustmentReason!.OriginalDate);
		Assert.AreEqual(AdjustmentAction.AddDays, adjusted.AdjustmentReason.Action);
	}

	private static NotableDateResolutionRequest Request(DateTime startDate, DateTime endDate) =>
		new(startDate, endDate, NotableDateResolutionProjection.ObservedDate);

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
