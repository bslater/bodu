// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateRuleMerger.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Immutable;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Applies a <see cref="NotableDateRuleUseDirective" /> to an inherited <see cref="NotableDateRule" />, producing the
/// merged rule that flows through the flatten pipeline.
/// </summary>
/// <remarks>
/// <para>
/// Extracted from <see cref="XmlResourceNotableDateRuleProvider" /> so the merge algorithm can be tested in isolation
/// without bootstrapping an assembly loader. The algorithm is purely functional — it does not read any external state —
/// and matches the contract documented on <see cref="NotableDateRuleOverrideBody" />:
/// </para>
/// <list type="number">
/// <item>
/// <description>
/// Scalar fields: override body wins over flat <c>&lt;Use&gt;</c> attributes; flat attributes win over the inherited
/// value.
/// </description>
/// </item>
/// <item>
/// <description>
/// Name: override body's name wins, then the flat <c>as</c> attribute's local name, then the inherited name.
/// </description>
/// </item>
/// <item>
/// <description>
/// Tags: additive union with the inherited set (set semantics, case-insensitive duplicates coalesced). <c>clearTags</c>
/// discards the inherited baseline first.
/// </description>
/// </item>
/// <item>
/// <description>
/// Adjustments: merge by <see cref="ObservanceAdjustment.Key" /> — matching keys replace in place, new keys append.
/// <c>clearAdjustments</c> discards the inherited baseline first.
/// </description>
/// </item>
/// <item>
/// <description>
/// Strategy: replaces wholesale when the override body declares one, otherwise inherited.
/// </description>
/// </item>
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
        NotableDateRuleOverrideBody? body = directive.OverrideBody;

        NotableDateRule merged = source with
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
            Tags = MergeTags(source.Tags, body?.Tags ?? [], directive.ClearTags),
            Adjustments = MergeAdjustments(source.Adjustments, body?.Adjustments ?? [], directive.ClearAdjustments),
        };

        if (body?.Strategy is { } overrideStrategy)
        {
            merged = ApplyStrategyOverride(merged, overrideStrategy, body);
        }

        return merged;
    }

    /// <summary>
    /// Resolves the effective name for the merged rule, applying body name → flat local name → source name precedence.
    /// </summary>
    /// <param name="sourceName">The inherited rule name.</param>
    /// <param name="flatLocalName">The <c>as</c> attribute on the <c>&lt;Use&gt;</c> directive, if any.</param>
    /// <param name="bodyName">The name declared inside the override body, if any.</param>
    /// <returns>The resolved name.</returns>
    private static string ResolveName(string sourceName, string? flatLocalName, string? bodyName) =>
        !string.IsNullOrWhiteSpace(bodyName) ? bodyName : !string.IsNullOrWhiteSpace(flatLocalName) ? flatLocalName! : sourceName;

    /// <summary>
    /// Merges the override tag list into the inherited tag set, optionally clearing the inherited baseline first.
    /// </summary>
    /// <param name="inherited">The tag set from the source rule.</param>
    /// <param name="overrideTags">Additional tags declared in the override body.</param>
    /// <param name="clearTags">
    /// When <see langword="true" />, the inherited tags are discarded before adding override tags.
    /// </param>
    /// <returns>The merged tag set.</returns>
    private static ImmutableHashSet<string> MergeTags(
        ImmutableHashSet<string> inherited,
        ImmutableArray<string> overrideTags,
        bool clearTags)
    {
        ImmutableHashSet<string> baseline = clearTags
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

    /// <summary>
    /// Merges the override adjustments into the inherited adjustment list. Matching keys replace their inherited
    /// counterpart in place; non-matching keys are appended. The inherited baseline may be cleared first when
    /// <paramref name="clearAdjustments" /> is <see langword="true" />.
    /// </summary>
    /// <param name="inherited">The adjustment list from the source rule.</param>
    /// <param name="overrideAdjustments">Adjustments declared in the override body.</param>
    /// <param name="clearAdjustments">
    /// When <see langword="true" />, the inherited adjustments are discarded before merging.
    /// </param>
    /// <returns>The merged adjustment list.</returns>
    private static ImmutableArray<ObservanceAdjustment> MergeAdjustments(
        ImmutableArray<ObservanceAdjustment> inherited,
        ImmutableArray<ObservanceAdjustment> overrideAdjustments,
        bool clearAdjustments)
    {
        ImmutableArray<ObservanceAdjustment> baseline = clearAdjustments || inherited.IsDefault
            ? []
            : inherited;

        if (overrideAdjustments.IsDefaultOrEmpty)
            return baseline;

        var builder = baseline.ToBuilder();

        foreach (ObservanceAdjustment overrideAdjustment in overrideAdjustments)
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

    /// <summary>
    /// Finds the index of the adjustment whose <see cref="ObservanceAdjustment.Key" /> matches <paramref name="key" />
    /// (case-insensitive), or returns <c>-1</c> if not found.
    /// </summary>
    /// <param name="builder">The mutable adjustment list being searched.</param>
    /// <param name="key">The key to locate.</param>
    /// <returns>The zero-based index, or <c>-1</c> when no match is found.</returns>
    private static int IndexOfKey(ImmutableArray<ObservanceAdjustment>.Builder builder, string key)
    {
        for (var i = 0; i < builder.Count; i++)
        {
            if (string.Equals(builder[i].Key, key, StringComparison.OrdinalIgnoreCase))
                return i;
        }

        return -1;
    }

    /// <summary>
    /// Replaces all strategy-specific fields on <paramref name="rule" /> with the values from <paramref name="body" />,
    /// clearing fields that are irrelevant to the new strategy.
    /// </summary>
    /// <param name="rule">The rule whose strategy fields are being replaced.</param>
    /// <param name="strategy">The new resolution strategy.</param>
    /// <param name="body">The override body supplying the replacement field values.</param>
    /// <returns>A new rule with the strategy and its associated fields replaced.</returns>
    private static NotableDateRule ApplyStrategyOverride(NotableDateRule rule, DateResolutionStrategy strategy, NotableDateRuleOverrideBody body) =>
        strategy switch
        {
            DateResolutionStrategy.Fixed => rule with
            {
                Strategy = strategy,
                Month = body.Month,
                Day = body.Day,
                SkipLeapMonth = body.SkipLeapMonth,
                SweepCalendarYears = body.SweepCalendarYears,
                CalendarMonthAlias = body.CalendarMonthAlias,
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
