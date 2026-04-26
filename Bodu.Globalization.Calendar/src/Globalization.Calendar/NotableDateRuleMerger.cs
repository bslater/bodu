// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateRuleMerger.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Immutable;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Applies a <see cref="NotableDateRuleUseDirective" /> to an inherited <see cref="NotableDateRule" />, producing the merged rule
/// that flows through the flatten pipeline.
/// </summary>
/// <remarks>
/// <para>
/// Extracted from <see cref="XmlResourceNotableDateRuleProvider" /> so the merge algorithm can be tested in isolation without
/// bootstrapping an assembly loader. The algorithm is purely functional — it does not read any external state — and matches the
/// contract documented on <see cref="NotableDateRuleOverrideBody" />:
/// </para>
/// <list type="number">
/// <item><description>Scalar fields: override body wins over flat <c>&lt;Use&gt;</c> attributes; flat attributes win over the inherited value.</description></item>
/// <item><description>Name: override body's name wins, then the flat <c>as</c> attribute's local name, then the inherited name.</description></item>
/// <item><description>Tags: additive union with the inherited set (set semantics, case-insensitive duplicates coalesced). <c>clearTags</c> discards the inherited baseline first.</description></item>
/// <item><description>Adjustments: merge by <see cref="ObservanceAdjustment.Key" /> — matching keys replace in place, new keys append. <c>clearAdjustments</c> discards the inherited baseline first.</description></item>
/// <item><description>Strategy: replaces wholesale when the override body declares one, otherwise inherited.</description></item>
/// </list>
/// </remarks>
internal static class NotableDateRuleMerger
{
	/// <summary>
	/// Returns a copy of <paramref name="source" /> with every override from <paramref name="directive" /> applied.
	/// </summary>
	/// <param name="source">The inherited rule being re-used.</param>
	/// <param name="directive">The cherry-pick directive specifying the override.</param>
	/// <returns>The merged rule.</returns>
	public static NotableDateRule Apply(NotableDateRule source, NotableDateRuleUseDirective directive)
	{
		var body = directive.OverrideBody;

		var merged = source with
		{
			Name = ResolveName(source.Name, directive.LocalName, body?.Name),
			RuleName = body?.RuleName ?? source.RuleName,
			Category = body?.Category ?? directive.Category ?? source.Category,
			TerritoryCode = body?.TerritoryCode ?? directive.TerritoryCode ?? source.TerritoryCode,
			IsNonWorkingDay = body?.IsNonWorkingDay ?? directive.IsNonWorkingDay ?? source.IsNonWorkingDay,
			FirstYear = body?.FirstYear ?? directive.FirstYear ?? source.FirstYear,
			LastYear = body?.LastYear ?? directive.LastYear ?? source.LastYear,
			OccurrenceYears = body?.OccurrenceYears ?? directive.OccurrenceYears ?? source.OccurrenceYears,
			DurationDays = body?.DurationDays ?? directive.DurationDays ?? source.DurationDays,
			Priority = body?.Priority ?? directive.Priority ?? source.Priority,
			Comment = body?.Comment ?? directive.Comment ?? source.Comment,
			CalendarType = body?.CalendarType ?? source.CalendarType,
			Tags = MergeTags(source.Tags, body?.Tags ?? ImmutableArray<string>.Empty, directive.ClearTags),
			Adjustments = MergeAdjustments(source.Adjustments, body?.Adjustments ?? ImmutableArray<ObservanceAdjustment>.Empty, directive.ClearAdjustments),
		};

		if (body?.Strategy is { } overrideStrategy)
		{
			merged = ApplyStrategyOverride(merged, overrideStrategy, body);
		}

		return merged;
	}

	private static string ResolveName(string sourceName, string? flatLocalName, string? bodyName)
	{
		if (!string.IsNullOrWhiteSpace(bodyName))
			return bodyName!;
		if (!string.IsNullOrWhiteSpace(flatLocalName))
			return flatLocalName!;
		return sourceName;
	}

	private static ImmutableHashSet<string> MergeTags(
		ImmutableHashSet<string> inherited,
		ImmutableArray<string> overrideTags,
		bool clearTags)
	{
		var baseline = clearTags
			? ImmutableHashSet.Create<string>(StringComparer.OrdinalIgnoreCase)
			: inherited;

		if (overrideTags.IsDefaultOrEmpty)
			return baseline;

		var builder = baseline.ToBuilder();
		foreach (var tag in overrideTags)
		{
			if (!string.IsNullOrWhiteSpace(tag))
				builder.Add(tag);
		}

		return builder.ToImmutable();
	}

	private static ImmutableArray<ObservanceAdjustment> MergeAdjustments(
		ImmutableArray<ObservanceAdjustment> inherited,
		ImmutableArray<ObservanceAdjustment> overrideAdjustments,
		bool clearAdjustments)
	{
		var baseline = clearAdjustments || inherited.IsDefault
			? ImmutableArray<ObservanceAdjustment>.Empty
			: inherited;

		if (overrideAdjustments.IsDefaultOrEmpty)
			return baseline;

		var builder = baseline.ToBuilder();

		foreach (var overrideAdjustment in overrideAdjustments)
		{
			var existingIndex = IndexOfKey(builder, overrideAdjustment.Key);
			if (existingIndex >= 0)
			{
				builder[existingIndex] = overrideAdjustment;
			}
			else
			{
				builder.Add(overrideAdjustment);
			}
		}

		return builder.ToImmutable();
	}

	private static int IndexOfKey(ImmutableArray<ObservanceAdjustment>.Builder builder, string key)
	{
		for (int i = 0; i < builder.Count; i++)
		{
			if (string.Equals(builder[i].Key, key, StringComparison.OrdinalIgnoreCase))
				return i;
		}

		return -1;
	}

	private static NotableDateRule ApplyStrategyOverride(NotableDateRule rule, DateResolutionStrategy strategy, NotableDateRuleOverrideBody body) =>
		strategy switch
		{
			DateResolutionStrategy.Fixed => rule with
			{
				Strategy = strategy,
				Month = body.Month,
				Day = body.Day,
				DayOfWeek = null,
				WeekOrdinal = null,
				AnchorRuleName = null,
				OffsetDays = null,
				AlgorithmKey = null,
				AlgorithmType = null,
				AlgorithmMonth = null,
				AlgorithmDay = null,
			},
			DateResolutionStrategy.DayOfWeekInMonth => rule with
			{
				Strategy = strategy,
				Month = body.Month,
				Day = null,
				DayOfWeek = body.DayOfWeek,
				WeekOrdinal = body.WeekOrdinal,
				AnchorRuleName = null,
				OffsetDays = null,
				AlgorithmKey = null,
				AlgorithmType = null,
				AlgorithmMonth = null,
				AlgorithmDay = null,
			},
			DateResolutionStrategy.OffsetFromAnchor => rule with
			{
				Strategy = strategy,
				Month = null,
				Day = null,
				DayOfWeek = null,
				WeekOrdinal = null,
				AnchorRuleName = body.AnchorRuleName,
				OffsetDays = body.OffsetDays,
				AlgorithmKey = null,
				AlgorithmType = null,
				AlgorithmMonth = null,
				AlgorithmDay = null,
			},
			DateResolutionStrategy.Algorithm => rule with
			{
				Strategy = strategy,
				Month = null,
				Day = null,
				DayOfWeek = null,
				WeekOrdinal = null,
				AnchorRuleName = null,
				OffsetDays = null,
				AlgorithmKey = body.AlgorithmKey,
				AlgorithmType = body.AlgorithmType,
				AlgorithmMonth = body.AlgorithmMonth,
				AlgorithmDay = body.AlgorithmDay,
			},
			_ => rule,
		};
}
