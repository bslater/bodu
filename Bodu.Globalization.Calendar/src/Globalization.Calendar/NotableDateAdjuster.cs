// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateAdjuster.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Extensions;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Evaluates an <see cref="ObservanceAdjustment" /> against a calculated date and applies the configured action when the trigger
/// activates.
/// </summary>
/// <remarks>
/// <para>
/// The adjuster centralises every evaluation concern that the previous implementation duplicated across <c>NotableDateService</c> and
/// the partial <c>NotableDateAdjuster</c>: the trigger condition, the rule's territory and calendar scope, the effective year window,
/// and the action dispatch. This means that callers can apply an adjustment in isolation (for example from inside a custom handler)
/// without losing any of those guards.
/// </para>
/// <para>
/// The adjuster also implements every previously-stubbed <see cref="AdjustmentTrigger" /> and <see cref="AdjustmentAction" /> value,
/// including <see cref="AdjustmentAction.MoveToNextNonWorkingDay" />, <see cref="AdjustmentAction.ReplaceWithNamedDate" />, and the
/// custom handler dispatch path.
/// </para>
/// </remarks>
internal sealed class NotableDateAdjuster
{
	private readonly Func<DateTime, bool> _isWeekend;
	private readonly Func<DateTime, string?, Type?, bool> _isNonWorkingDay;
	private readonly CalendarWeekendDefinition _weekendDefinition;
	private readonly IWeekendDefinitionProvider? _weekendProvider;
	private readonly IAdjustmentHandlerRegistry? _handlerRegistry;
	private readonly Func<string, int, string?, Type?, DateTime?>? _resolveByName;

	/// <summary>
	/// Initialises a new instance of the <see cref="NotableDateAdjuster" /> class.
	/// </summary>
	/// <param name="isWeekend">A predicate for weekend evaluation.</param>
	/// <param name="isNonWorkingDay">A predicate for non-working-day evaluation, scoped by territory and calendar.</param>
	/// <param name="weekendDefinition">The configured weekend definition.</param>
	/// <param name="weekendProvider">An optional custom weekend provider.</param>
	/// <param name="handlerRegistry">An optional registry of custom <see cref="IAdjustmentHandler" /> instances.</param>
	/// <param name="resolveByName">An optional callback used by <see cref="AdjustmentAction.ReplaceWithNamedDate" /> to look up another rule's resolved date for the same year.</param>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="isWeekend" /> or <paramref name="isNonWorkingDay" /> is <see langword="null" />.</exception>
	public NotableDateAdjuster(
		Func<DateTime, bool> isWeekend,
		Func<DateTime, string?, Type?, bool> isNonWorkingDay,
		CalendarWeekendDefinition weekendDefinition,
		IWeekendDefinitionProvider? weekendProvider,
		IAdjustmentHandlerRegistry? handlerRegistry = null,
		Func<string, int, string?, Type?, DateTime?>? resolveByName = null)
	{
		_isWeekend = isWeekend ?? throw new ArgumentNullException(nameof(isWeekend));
		_isNonWorkingDay = isNonWorkingDay ?? throw new ArgumentNullException(nameof(isNonWorkingDay));
		_weekendDefinition = weekendDefinition;
		_weekendProvider = weekendProvider;
		_handlerRegistry = handlerRegistry;
		_resolveByName = resolveByName;
	}

	/// <summary>
	/// Determines whether the supplied adjustment is in scope for the supplied context.
	/// </summary>
	/// <param name="adjustment">The adjustment.</param>
	/// <param name="year">The year being resolved.</param>
	/// <param name="territoryCode">The territory currently being resolved, if any.</param>
	/// <param name="calendarType">The calendar currently being resolved, if any.</param>
	/// <returns><see langword="true" /> if the adjustment may activate; otherwise <see langword="false" />.</returns>
	public static bool IsInScope(ObservanceAdjustment adjustment, int year, string? territoryCode, Type? calendarType)
	{
		if (adjustment is null) throw new ArgumentNullException(nameof(adjustment));

		if (adjustment.EffectiveFromYear is { } from && year < from) return false;
		if (adjustment.EffectiveToYear is { } to && year > to) return false;

		if (adjustment.CalendarType is not null && calendarType is not null && adjustment.CalendarType != calendarType)
			return false;

		if (!string.IsNullOrEmpty(adjustment.TerritoryCode) && !string.IsNullOrEmpty(territoryCode))
		{
			if (!TerritoryCode.TryParse(territoryCode, out var requested))
				return false;

			bool matched = false;
			foreach (var scoped in TerritoryCode.ParseList(adjustment.TerritoryCode))
			{
				if (scoped.Contains(requested))
				{
					matched = true;
					break;
				}
			}

			if (!matched) return false;
		}

		return true;
	}

	/// <summary>
	/// Applies the supplied adjustment to the supplied date.
	/// </summary>
	/// <param name="adjustment">The adjustment to evaluate. Must not be <see langword="null" />.</param>
	/// <param name="rule">The originating rule, supplied for diagnostics and custom handlers. Must not be <see langword="null" />.</param>
	/// <param name="originalDate">The currently resolved date.</param>
	/// <param name="territoryCode">The territory currently being resolved, if any.</param>
	/// <param name="calendarType">The calendar currently being resolved, if any.</param>
	/// <returns>An <see cref="AdjustmentApplyResult" /> describing whether the adjustment activated and what date it produced.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="adjustment" /> or <paramref name="rule" /> is <see langword="null" />.</exception>
	public AdjustmentApplyResult Apply(
		ObservanceAdjustment adjustment,
		NotableDateRule rule,
		DateTime originalDate,
		string? territoryCode = null,
		Type? calendarType = null)
	{
		if (adjustment is null) throw new ArgumentNullException(nameof(adjustment));
		if (rule is null) throw new ArgumentNullException(nameof(rule));

		if (!IsInScope(adjustment, originalDate.Year, territoryCode, calendarType))
			return AdjustmentApplyResult.NotActivated(originalDate);

		// Custom triggers always go through the handler registry: the handler decides both activation and the resulting date.
		if (adjustment.Trigger == AdjustmentTrigger.Custom)
			return ApplyCustomHandler(adjustment, rule, originalDate, territoryCode, calendarType);

		if (!EvaluateTrigger(adjustment, originalDate, territoryCode, calendarType))
			return AdjustmentApplyResult.NotActivated(originalDate);

		return ApplyAction(adjustment, rule, originalDate, territoryCode, calendarType);
	}

	private bool EvaluateTrigger(ObservanceAdjustment adjustment, DateTime original, string? territoryCode, Type? calendarType)
	{
		switch (adjustment.Trigger)
		{
			case AdjustmentTrigger.Always:
				return true;

			case AdjustmentTrigger.IfWeekend:
				return _isWeekend(original);

			case AdjustmentTrigger.IfWeekday:
				return !_isWeekend(original);

			case AdjustmentTrigger.IfNonWorkingDay:
				return _isNonWorkingDay(original, territoryCode, calendarType);

			case AdjustmentTrigger.IfLeapYear:
				return DateTime.IsLeapYear(original.Year);

			case AdjustmentTrigger.IfDayOfWeek:
				return adjustment.DayOfWeek.HasValue && original.DayOfWeek == adjustment.DayOfWeek.Value;

			case AdjustmentTrigger.IfBeforeFixedDate:
				return adjustment.ComparisonDate is { } before && original < ProjectComparisonDate(before, original.Year);

			case AdjustmentTrigger.IfAfterFixedDate:
				return adjustment.ComparisonDate is { } after && original > ProjectComparisonDate(after, original.Year);

			case AdjustmentTrigger.IfNthOccurrenceInMonth:
				return adjustment.WeekOrdinal is { } ord && original.OrdinalWeekOfMonth() == ord;

			default:
				return false;
		}
	}

	private AdjustmentApplyResult ApplyAction(
		ObservanceAdjustment adjustment,
		NotableDateRule rule,
		DateTime original,
		string? territoryCode,
		Type? calendarType)
	{
		DateTime adjusted = adjustment.Action switch
		{
			AdjustmentAction.None => original,
			AdjustmentAction.AddDays => original.AddDays(adjustment.OffsetDays),
			AdjustmentAction.MoveToNextWeekday => original.NextWeekday(_weekendDefinition, _weekendProvider),
			AdjustmentAction.MoveToPreviousWeekday => original.PreviousWeekday(_weekendDefinition, _weekendProvider),
			AdjustmentAction.MoveToNextNonWorkingDay => MoveToNextNonWorkingDay(original, territoryCode, calendarType),
			AdjustmentAction.ReplaceWithNamedDate => ResolveReplacement(adjustment, original, territoryCode, calendarType),
			AdjustmentAction.Custom => ApplyCustomHandler(adjustment, rule, original, territoryCode, calendarType).AdjustedDate,
			_ => original,
		};

		return new AdjustmentApplyResult(true, adjusted, adjustment.Trigger, adjustment.Action, adjustment.HandlerKey, adjustment.IsNonWorkingDay);
	}

	private AdjustmentApplyResult ApplyCustomHandler(
		ObservanceAdjustment adjustment,
		NotableDateRule rule,
		DateTime original,
		string? territoryCode,
		Type? calendarType)
	{
		if (_handlerRegistry is null
			|| string.IsNullOrWhiteSpace(adjustment.HandlerKey)
			|| !_handlerRegistry.TryGet(adjustment.HandlerKey!, out var handler))
		{
			return AdjustmentApplyResult.NotActivated(original);
		}

		var context = new AdjustmentHandlerContext(original, adjustment, rule, territoryCode, calendarType);
		var result = handler.Apply(context);

		if (!result.Activated)
			return AdjustmentApplyResult.NotActivated(original);

		return new AdjustmentApplyResult(
			true,
			result.AdjustedDate,
			adjustment.Trigger,
			adjustment.Action,
			adjustment.HandlerKey,
			result.IsNonWorkingOverride ?? adjustment.IsNonWorkingDay);
	}

	private DateTime MoveToNextNonWorkingDay(DateTime original, string? territoryCode, Type? calendarType)
	{
		// Walk forward at most 31 days looking for the next day that is *not* flagged non-working — i.e. the next "open" day.
		// Reading "MoveToNextNonWorkingDay" literally: we move to the next day that is itself a non-working day.
		// The original enum doc clarifies it skips to the following non-working day, used to avoid stacking observances.
		DateTime cursor = original.AddDays(1);
		for (int i = 0; i < 366; i++, cursor = cursor.AddDays(1))
		{
			if (_isNonWorkingDay(cursor, territoryCode, calendarType))
				return cursor;
		}

		return original;
	}

	private DateTime ResolveReplacement(ObservanceAdjustment adjustment, DateTime original, string? territoryCode, Type? calendarType)
	{
		if (string.IsNullOrWhiteSpace(adjustment.TargetRuleName) || _resolveByName is null)
			return original;

		var resolved = _resolveByName(adjustment.TargetRuleName!, original.Year, territoryCode, calendarType);
		return resolved ?? original;
	}

	private static DateTime ProjectComparisonDate(DateTime comparison, int year)
	{
		// Authors specify a month/day; we project it onto the active year so rules remain stable across years.
		try
		{
			return new DateTime(year, comparison.Month, comparison.Day, 0, 0, 0, DateTimeKind.Unspecified);
		}
		catch (ArgumentOutOfRangeException)
		{
			// Falls through for 29 February in a non-leap year — treat as 28 February.
			return new DateTime(year, comparison.Month, Math.Min(comparison.Day, 28), 0, 0, 0, DateTimeKind.Unspecified);
		}
	}
}

/// <summary>
/// Captures the outcome of an <see cref="NotableDateAdjuster.Apply" /> invocation.
/// </summary>
/// <param name="Activated">Whether the adjustment fired.</param>
/// <param name="AdjustedDate">The resulting date. Equal to the original date when <paramref name="Activated" /> is <see langword="false" />.</param>
/// <param name="Trigger">The trigger that produced the result, when activated.</param>
/// <param name="Action">The action that produced the result, when activated.</param>
/// <param name="HandlerKey">The custom handler key invoked, when applicable.</param>
/// <param name="IsNonWorkingOverride">An optional override for the resulting date's non-working flag.</param>
internal readonly record struct AdjustmentApplyResult(
	bool Activated,
	DateTime AdjustedDate,
	AdjustmentTrigger Trigger = default,
	AdjustmentAction Action = default,
	string? HandlerKey = null,
	bool? IsNonWorkingOverride = null)
{
	/// <summary>
	/// Creates a result indicating that the adjustment did not activate.
	/// </summary>
	/// <param name="originalDate">The unchanged date.</param>
	/// <returns>An inactive result.</returns>
	public static AdjustmentApplyResult NotActivated(DateTime originalDate) => new(false, originalDate);
}
