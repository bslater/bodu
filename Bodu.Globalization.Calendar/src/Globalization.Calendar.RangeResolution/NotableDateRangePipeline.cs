// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateRangePipeline.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using BoduExt = Bodu.Extensions;

namespace Bodu.Globalization.Calendar.RangeResolution;

/// <summary>
/// Orchestrates the chronological range-resolution pipeline: planning, tiered occurrence materialization, observance adjustment,
/// and emission ordering.
/// </summary>
/// <remarks>
/// <para>
/// The pipeline is the prototype replacement for <see cref="NotableDateResolutionEngine" /> and
/// <see cref="NotableDateResolutionAdjustmentProcessor" />. It is intentionally implemented as a single class so the four tiers and
/// the adjustment phase are visible together; production code may decompose this into separate processors.
/// </para>
/// <para>
/// Tiered processing order (each tier reads from the cache populated by earlier tiers):
/// </para>
/// <list type="number">
///   <item><description><see cref="RuleTier.Fixed" /> — direct fixed-date and day-of-week-in-month rules.</description></item>
///   <item><description><see cref="RuleTier.OffsetFromFixed" /> — offset rules whose root anchor is a fixed-date rule.</description></item>
///   <item><description><see cref="RuleTier.Algorithmic" /> — algorithm-backed anchors, computed once per (anchor name, year).</description></item>
///   <item><description><see cref="RuleTier.OffsetFromAlgorithmic" /> — offset rules whose root anchor is algorithmic (for example <c>Start of Lent</c> = <c>Easter Sunday − 46</c>).</description></item>
/// </list>
/// </remarks>
internal sealed class NotableDateRangePipeline
{
    private readonly RuleStaticAnalysis _analysis;
    private readonly NotableDateRuleResolver _ruleResolver;
    private readonly NotableDateRangePlanner _planner;
    private readonly BoduExt.CalendarWeekendDefinition _weekendDefinition;
    private readonly BoduExt.IWeekendDefinitionProvider? _weekendProvider;
    private readonly IAdjustmentHandlerRegistry? _handlerRegistry;
    private readonly IReadOnlyList<RuleRemoval> _overrideRemovals;

    /// <summary>
    /// Initializes a new instance of the <see cref="NotableDateRangePipeline" /> class.
    /// </summary>
    /// <param name="analysis">The static rule analysis.</param>
    /// <param name="ruleResolver">The resolver used to compute fixed and algorithmic anchor dates.</param>
    /// <param name="weekendDefinition">The weekend definition used during adjustment evaluation.</param>
    /// <param name="weekendProvider">The optional custom weekend provider.</param>
    /// <param name="handlerRegistry">The optional custom adjustment handler registry.</param>
    /// <param name="overrideRemovals">The override removals consulted when materializing rules. Each entry suppresses a rule for matching years and territories.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="analysis" /> or <paramref name="ruleResolver" /> is <see langword="null" />.
    /// </exception>
    public NotableDateRangePipeline(
        RuleStaticAnalysis analysis,
        NotableDateRuleResolver ruleResolver,
        BoduExt.CalendarWeekendDefinition weekendDefinition,
        BoduExt.IWeekendDefinitionProvider? weekendProvider = null,
        IAdjustmentHandlerRegistry? handlerRegistry = null,
        IReadOnlyList<RuleRemoval>? overrideRemovals = null)
    {
        _analysis = analysis ?? throw new ArgumentNullException(nameof(analysis));
        _ruleResolver = ruleResolver ?? throw new ArgumentNullException(nameof(ruleResolver));
        _planner = new NotableDateRangePlanner(analysis);
        _weekendDefinition = weekendDefinition;
        _weekendProvider = weekendProvider;
        _handlerRegistry = handlerRegistry;
        _overrideRemovals = overrideRemovals ?? [];
    }

    /// <summary>
    /// Resolves notable dates whose observed date intersects the supplied request window.
    /// </summary>
    /// <param name="request">The range request.</param>
    /// <returns>The resolved notable dates, ordered by observed date.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="request" /> is <see langword="null" />.</exception>
    /// <remarks>
    /// <para>
    /// Resolution runs in two passes followed by an adjustment phase:
    /// </para>
    /// <list type="number">
    ///   <item><description><b>Main pass</b> — every eligible rule is materialized for the civil years that the request range spans. Rules whose resolved date falls inside the request window enter the cache in <see cref="NotableDateCacheState.InWindow" />; those just outside enter as <see cref="NotableDateCacheState.Computed" /> for adjustment context.</description></item>
    ///   <item><description><b>Fringe pass</b> — for each adjacent civil year the request window touches inside the planner's fringe distance, every <see cref="RuleTier.Fixed" /> rule with at least one adjustment is materialized when its resolved date falls inside the fringe window. This catches cross-year roll-overs such as <c>31 Dec</c> rolling forward to <c>3 Jan</c> without a global reach expansion.</description></item>
    ///   <item><description><b>Adjustment phase</b> — observance adjustments are applied using the cache as the non-working day context. Adjusted dates that intersect the request promote the entry to <see cref="NotableDateCacheState.Adjusted" /> and supersede the base on emission.</description></item>
    /// </list>
    /// </remarks>
    public IReadOnlyList<NotableDate> Resolve(NotableDateRangeRequest request)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));

        NotableDateRangePlan plan = _planner.Plan(request);
        NotableDateRangeResolutionCache cache = new();

        // Pass 1 — main: rules whose resolved date for years the request spans may intersect (or directly feed) the window.

        // Tier 1: Fixed and DayOfWeekInMonth.
        foreach (RuleStaticProfile profile in plan.EligibleRules)
        {
            if (profile.Tier != RuleTier.Fixed) continue;
            ProcessDirect(profile, plan, cache);
        }

        // Tier 2: OffsetFromFixed — anchor already computed in Tier 1.
        foreach (RuleStaticProfile profile in plan.EligibleRules)
        {
            if (profile.Tier != RuleTier.OffsetFromFixed) continue;
            ProcessOffsetFromCached(profile, plan, cache);
        }

        // Tier 3: Algorithmic anchors — compute exactly the demanded years (request years ∪ fringe years).
        ProcessAlgorithmicAnchors(plan, cache);

        // Tier 4: OffsetFromAlgorithmic — anchor available in the cache from Tier 3.
        foreach (RuleStaticProfile profile in plan.EligibleRules)
        {
            if (profile.Tier != RuleTier.OffsetFromAlgorithmic) continue;
            ProcessOffsetFromCached(profile, plan, cache);
        }

        // Pass 2 — fringe: scan adjacent year boundaries for Tier 1 rules with adjustments that may roll into the window.
        ProcessFringePass(plan, cache);

        // Adjustment phase — uses the cache as non-working day context.
        ApplyAdjustments(plan, cache);

        return BuildEmissionList(plan, cache);
    }

    /// <summary>
    /// Materializes adjacent-year rules whose observance adjustment chain or multi-day duration may project an emission into the
    /// requested window. Covers two fringe-relevant categories: rules with at least one <see cref="ObservanceAdjustment" /> and
    /// rules with <see cref="NotableDateRule.DurationDays" /> greater than one.
    /// </summary>
    /// <param name="plan">The active resolution plan.</param>
    /// <param name="cache">The shared cache being populated.</param>
    /// <remarks>
    /// <para>
    /// The fringe pass handles three concrete classes of cross-year contribution:
    /// </para>
    /// <list type="bullet">
    ///   <item><description><b>Tier 1 (Fixed) with adjustment</b> — for example, a <c>31 Dec</c> holiday whose <see cref="AdjustmentAction.MoveToNextNonWorkingDay" /> rolls forward into the new year.</description></item>
    ///   <item><description><b>Tier 2 (OffsetFromFixed) with adjustment</b> — for example, <c>"Day after Christmas"</c> with a weekend roll-forward. The rule's root anchor is materialized on-demand for the fringe year if Pass 1 did not load it.</description></item>
    ///   <item><description><b>Tier 1 (Fixed) with multi-day duration</b> — for example, a seven-day festival starting <c>30 Dec</c> whose span reaches into early January.</description></item>
    /// </list>
    /// <para>
    /// Algorithmic and offset-from-algorithmic tiers are already covered by the main pass — the planner unions fringe years into
    /// <see cref="NotableDateRangePlan.GetAnchorYears" /> so Tier 3 / Tier 4 read those years directly.
    /// </para>
    /// <para>
    /// Per-rule filtering uses each profile's <see cref="RuleStaticProfile.MinObservedReach" /> /
    /// <see cref="RuleStaticProfile.MaxObservedReach" /> envelope rather than the planner-wide fringe window, so a rule with a
    /// large adjustment shift (for example, <see cref="AdjustmentAction.AddDays" /> = 60) is correctly admitted while a rule with
    /// a small reach is not over-scanned.
    /// </para>
    /// </remarks>
    private void ProcessFringePass(NotableDateRangePlan plan, NotableDateRangeResolutionCache cache)
    {
        if (plan.FringeYears.Count == 0) return;

        foreach (RuleStaticProfile profile in plan.EligibleRules)
        {
            // Algorithmic and OffsetFromAlgorithmic are covered by the main pass via plan.GetAnchorYears.
            if (profile.Tier == RuleTier.Algorithmic || profile.Tier == RuleTier.OffsetFromAlgorithmic) continue;

            // Skip rules that cannot contribute through the fringe — neither an adjustment nor a multi-day span.
            var hasAdjustments = !profile.Rule.Adjustments.IsDefaultOrEmpty;
            var hasMultiDaySpan = profile.Rule.DurationDays > 1;
            if (!hasAdjustments && !hasMultiDaySpan) continue;

            foreach (var year in plan.FringeYears)
            {
                if (!NotableDateRuleResolver.IsApplicable(profile.Rule, year))
                    continue;

                DateTime? anchor = ResolveFringeAnchor(profile, year, plan, cache);
                if (anchor is null) continue;

                // Per-rule emission envelope: [anchor + MinObservedReach, anchor + MaxObservedReach]. Includes both observance
                // adjustment shifts and multi-day duration. Skip when the envelope cannot intersect the request window.
                DateTime potentialStart = SafeAddDays(anchor.Value.Date, profile.MinObservedReach);
                DateTime potentialEnd = SafeAddDays(anchor.Value.Date, profile.MaxObservedReach);

                if (potentialStart > plan.Request.EndDate || potentialEnd < plan.Request.StartDate)
                    continue;

                AddEntries(profile, year, anchor.Value, plan, cache);
            }
        }
    }

    /// <summary>
    /// Resolves the anchor date of a fringe-year rule. Tier 1 rules use the rule resolver directly; Tier 2 rules read the root
    /// anchor from the cache, materializing it on-demand when the main pass did not process it for this year.
    /// </summary>
    /// <param name="profile">The rule profile being materialized.</param>
    /// <param name="year">The fringe-year being processed.</param>
    /// <param name="plan">The active resolution plan.</param>
    /// <param name="cache">The shared cache being populated.</param>
    /// <returns>The resolved anchor date, or <see langword="null" /> when the rule does not apply or the anchor is unavailable.</returns>
    private DateTime? ResolveFringeAnchor(
        RuleStaticProfile profile,
        int year,
        NotableDateRangePlan plan,
        NotableDateRangeResolutionCache cache)
    {
        if (profile.Tier == RuleTier.Fixed)
        {
            try { return _ruleResolver.ResolveAnchorDate(profile.Rule, year); }
            catch (InvalidOperationException) { return null; }
        }

        if (profile.Tier != RuleTier.OffsetFromFixed) return null;
        if (string.IsNullOrWhiteSpace(profile.RootAnchorRuleName)) return null;

        DateTime? rootAnchor = cache.ResolveAnchor(profile.RootAnchorRuleName!, year);

        if (rootAnchor is null)
        {
            // On-demand: the main pass only materializes Tier 1 rules for candidate years, so the root anchor of a Tier 2 fringe
            // rule may be missing for the fringe year. Materialize it here so this offset rule (and any sibling Tier 2 rules in
            // the fringe pass that share the same root) can read it. The anchor enters the cache as Computed unless its own
            // resolved date independently lands in the request window.
            if (!_analysis.TryGetProfile(profile.RootAnchorRuleName!, out RuleStaticProfile rootProfile)) return null;
            if (rootProfile.Tier != RuleTier.Fixed) return null;
            if (!NotableDateRuleResolver.IsApplicable(rootProfile.Rule, year)) return null;

            DateTime? rootDate;
            try { rootDate = _ruleResolver.ResolveAnchorDate(rootProfile.Rule, year); }
            catch (InvalidOperationException) { return null; }

            if (rootDate is null) return null;

            AddEntries(rootProfile, year, rootDate.Value, plan, cache);
            rootAnchor = rootDate;
        }

        try { return rootAnchor.Value.Date.AddDays(profile.OffsetFromRoot); }
        catch (ArgumentOutOfRangeException) { return null; }
    }

    /// <summary>
    /// Adds days to a date, clamping to the supported <see cref="DateTime" /> range.
    /// </summary>
    /// <param name="date">The source date.</param>
    /// <param name="days">The number of days to add (may be negative).</param>
    /// <returns>The resulting date, clamped to the supported range.</returns>
    private static DateTime SafeAddDays(DateTime date, int days)
    {
        DateTime value = date.Date;

        if (days < 0 && value <= DateTime.MinValue.AddDays(-days)) return DateTime.MinValue.Date;
        if (days > 0 && value >= DateTime.MaxValue.AddDays(-days)) return DateTime.MaxValue.Date;

        return value.AddDays(days);
    }

    /// <summary>
    /// Materializes Tier 1 (Fixed / DayOfWeekInMonth) rules across every candidate year and territory.
    /// </summary>
    /// <param name="profile">The rule profile to materialize.</param>
    /// <param name="plan">The active resolution plan.</param>
    /// <param name="cache">The shared cache being populated.</param>
    private void ProcessDirect(RuleStaticProfile profile, NotableDateRangePlan plan, NotableDateRangeResolutionCache cache)
    {
        foreach (var year in plan.CandidateYears)
        {
            if (!NotableDateRuleResolver.IsApplicable(profile.Rule, year))
                continue;

            DateTime? anchor;
            try
            {
                anchor = _ruleResolver.ResolveAnchorDate(profile.Rule, year);
            }
            catch (InvalidOperationException)
            {
                continue;
            }

            if (anchor is null) continue;

            AddEntries(profile, year, anchor.Value, plan, cache);
        }
    }

    /// <summary>
    /// Materializes Tier 3 algorithmic rules for the years demanded by <see cref="NotableDateRangePlan.GetAnchorYears" />.
    /// </summary>
    /// <param name="plan">The active resolution plan.</param>
    /// <param name="cache">The shared cache being populated.</param>
    private void ProcessAlgorithmicAnchors(NotableDateRangePlan plan, NotableDateRangeResolutionCache cache)
    {
        foreach (var anchorName in plan.RequiredAnchorNames())
        {
            if (!_analysis.TryGetProfile(anchorName, out RuleStaticProfile profile)) continue;
            if (profile.Tier != RuleTier.Algorithmic) continue;

            IReadOnlyList<int> years = plan.GetAnchorYears(anchorName);
            foreach (var year in years)
            {
                if (!NotableDateRuleResolver.IsApplicable(profile.Rule, year))
                    continue;

                DateTime? anchor;
                try
                {
                    anchor = _ruleResolver.ResolveAnchorDate(profile.Rule, year);
                }
                catch (InvalidOperationException)
                {
                    continue;
                }

                if (anchor is null) continue;

                AddEntries(profile, year, anchor.Value, plan, cache);
            }
        }
    }

    /// <summary>
    /// Materializes a Tier 2 or Tier 4 offset rule by reading its root anchor from the cache and applying the static
    /// <see cref="RuleStaticProfile.OffsetFromRoot" />.
    /// </summary>
    /// <param name="profile">The offset rule profile.</param>
    /// <param name="plan">The active resolution plan.</param>
    /// <param name="cache">The shared cache being populated.</param>
    private void ProcessOffsetFromCached(RuleStaticProfile profile, NotableDateRangePlan plan, NotableDateRangeResolutionCache cache)
    {
        if (string.IsNullOrWhiteSpace(profile.RootAnchorRuleName)) return;

        IEnumerable<int> years = profile.Tier == RuleTier.OffsetFromAlgorithmic
            ? plan.GetAnchorYears(profile.RootAnchorRuleName!)
            : plan.CandidateYears;

        foreach (var year in years)
        {
            if (!NotableDateRuleResolver.IsApplicable(profile.Rule, year))
                continue;

            DateTime? anchor = cache.ResolveAnchor(profile.RootAnchorRuleName!, year);
            if (anchor is null) continue;

            DateTime occurrence;
            try
            {
                occurrence = anchor.Value.Date.AddDays(profile.OffsetFromRoot);
            }
            catch (ArgumentOutOfRangeException)
            {
                continue;
            }

            AddEntries(profile, year, occurrence, plan, cache);
        }
    }

    /// <summary>
    /// Builds and adds cache entries for the supplied rule, anchor year, and anchor date, expanding the rule's authored territory
    /// list into one entry per territory that matches the request context. Entries suppressed by an override
    /// <see cref="RuleRemoval" /> for the supplied year and territory are skipped before reaching the cache.
    /// </summary>
    /// <param name="profile">The rule profile being materialized.</param>
    /// <param name="year">The anchor year.</param>
    /// <param name="anchorDate">The resolved anchor date.</param>
    /// <param name="plan">The active resolution plan.</param>
    /// <param name="cache">The shared cache being populated.</param>
    private void AddEntries(
        RuleStaticProfile profile,
        int year,
        DateTime anchorDate,
        NotableDateRangePlan plan,
        NotableDateRangeResolutionCache cache)
    {
        foreach (var territory in EnumerateApplicableTerritories(profile.Rule, plan.Request.TerritoryCode))
        {
            if (IsRemovedByOverride(profile.Rule, year, territory))
                continue;

            NotableDate notable = BuildNotableDate(profile.Rule, anchorDate, territory, adjustmentReason: null);

            NotableDateCacheState state = NotableDateCacheState.Computed;
            if (Intersects(plan.Request.StartDate, plan.Request.EndDate, notable.Date, notable.EndDate))
            {
                if (plan.Request.Filter is null || plan.Request.Filter.IsMatch(notable))
                    state = NotableDateCacheState.InWindow;
            }

            NotableDateCacheEntry entry = new(profile, year, notable, state);
            cache.Add(entry);
        }
    }

    /// <summary>
    /// Applies observance adjustments to every cache entry that has at least one configured adjustment.
    /// </summary>
    /// <param name="plan">The active resolution plan.</param>
    /// <param name="cache">The shared cache.</param>
    private void ApplyAdjustments(NotableDateRangePlan plan, NotableDateRangeResolutionCache cache)
    {
        NotableDateAdjuster adjuster = new(
            IsWeekend,
            (date, territory, calendar) => IsNonWorkingDay(cache, date, territory, calendar),
            _weekendDefinition,
            _weekendProvider,
            _handlerRegistry,
            (name, year, territory, calendar) => cache.ResolveObservedByName(name, year, territory, calendar));

        // Snapshot first — entries can be mutated as adjustments are applied.
        var snapshot = cache.Entries.ToList();

        foreach (NotableDateCacheEntry entry in snapshot)
        {
            if (entry.Rule.Adjustments.IsDefaultOrEmpty) continue;

            foreach (ObservanceAdjustment adjustment in entry.Rule.Adjustments.OrderBy(a => a.Priority))
            {
                if (!NotableDateAdjuster.IsInScope(adjustment, entry.AnchorYear, entry.BaseNotable.TerritoryCode, entry.Rule.CalendarType))
                    continue;

                AdjustmentApplyResult result = adjuster.Apply(
                    adjustment,
                    entry.Rule,
                    entry.BaseNotable.Date,
                    entry.BaseNotable.TerritoryCode,
                    entry.Rule.CalendarType);

                if (!result.Activated || result.AdjustedDate.Date == entry.BaseNotable.Date.Date)
                    continue;

                var emittedTerritory = !string.IsNullOrEmpty(adjustment.TerritoryCode)
                    ? adjustment.TerritoryCode
                    : entry.BaseNotable.TerritoryCode;

                var isNonWorking = result.IsNonWorkingOverride ?? entry.Rule.IsNonWorkingDay ?? false;
                AdjustmentReason reason = new(entry.BaseNotable.Date, result.Trigger, result.Action, result.HandlerKey);
                NotableDate adjusted = BuildNotableDate(entry.Rule, result.AdjustedDate, emittedTerritory, reason, isNonWorking);

                entry.Adjusted = adjusted;

                var adjustedIntersects = Intersects(plan.Request.StartDate, plan.Request.EndDate, adjusted.Date, adjusted.EndDate);
                var filterMatch = plan.Request.Filter is null || plan.Request.Filter.IsMatch(adjusted);

                // Always promote to Adjusted when the adjusted date lands inside the request window. The emission step
                // independently checks whether the base date also intersects, so we never lose the base when both are visible.
                if (adjustedIntersects && filterMatch)
                    entry.State = NotableDateCacheState.Adjusted;
                else if (entry.State == NotableDateCacheState.Computed)
                    entry.State = NotableDateCacheState.AdjustedBlocker;
            }
        }
    }

    /// <summary>
    /// Builds the final emission list from emissable cache entries.
    /// </summary>
    /// <param name="plan">The active resolution plan.</param>
    /// <param name="cache">The shared cache.</param>
    /// <returns>The emission list, ordered by observed date and rule name.</returns>
    /// <remarks>
    /// <para>
    /// Emission policy: an observance adjustment replaces the base anchor — when an entry's <see cref="NotableDateCacheState" /> is
    /// <see cref="NotableDateCacheState.Adjusted" /> only the <see cref="NotableDateCacheEntry.Adjusted" /> form is emitted, never
    /// the underlying anchor. This guarantees that the emitted count equals the notable-date count for the requested window.
    /// </para>
    /// </remarks>
    private static IReadOnlyList<NotableDate> BuildEmissionList(NotableDateRangePlan plan, NotableDateRangeResolutionCache cache)
    {
        List<NotableDate> emitted = [];

        foreach (NotableDateCacheEntry entry in cache.EmissableEntries())
        {
            if (entry.State == NotableDateCacheState.Adjusted && entry.Adjusted is not null)
            {
                // Adjusted form supersedes the base — the original anchor is recorded on the adjusted date's AdjustmentReason
                // rather than emitted as a separate entry.
                emitted.Add(entry.Adjusted);
                continue;
            }

            if (entry.State == NotableDateCacheState.InWindow)
                emitted.Add(entry.BaseNotable);
        }

        // Defensive: never emit a notable date whose span lies outside the requested window. The state transitions enforce this on
        // their own, but the explicit guard catches any future regression in the state machine.
        return [.. emitted
            .Where(n => Intersects(plan.Request.StartDate, plan.Request.EndDate, n.Date, n.EndDate))
            .OrderBy(n => n.Date)
            .ThenBy(n => n.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(n => n.TerritoryCode, StringComparer.OrdinalIgnoreCase)];
    }

    /// <summary>
    /// Determines whether the supplied date is a non-working day from the cache's perspective. Falls back to weekend evaluation.
    /// </summary>
    /// <param name="cache">The active cache.</param>
    /// <param name="date">The date to test.</param>
    /// <param name="territoryCode">The territory context.</param>
    /// <param name="calendarType">The calendar context.</param>
    /// <returns><see langword="true" /> when the date is non-working in context.</returns>
    private bool IsNonWorkingDay(NotableDateRangeResolutionCache cache, DateTime date, string? territoryCode, Type? calendarType)
    {
        if (IsWeekend(date)) return true;
        return cache.IsNonWorkingDay(date, territoryCode, calendarType);
    }

    /// <summary>
    /// Determines whether the supplied date falls on a weekend under the configured weekend definition.
    /// </summary>
    /// <param name="date">The date to test.</param>
    /// <returns><see langword="true" /> when the date is a weekend; otherwise, <see langword="false" />.</returns>
    private bool IsWeekend(DateTime date) =>
        BoduExt.DateTimeExtensions.IsWeekend(date, _weekendDefinition, _weekendProvider);

    /// <summary>
    /// Constructs a <see cref="NotableDate" /> from a rule, its resolved date, and any observance-adjustment metadata.
    /// </summary>
    /// <param name="rule">The originating notable-date rule.</param>
    /// <param name="date">The resolved observed date for the rule.</param>
    /// <param name="territory">The territory code, or <see langword="null" />.</param>
    /// <param name="adjustmentReason">The adjustment reason, or <see langword="null" /> when the date is the base anchor.</param>
    /// <param name="isNonWorkingOverride">An optional override for the non-working flag.</param>
    /// <returns>The constructed <see cref="NotableDate" />.</returns>
    private static NotableDate BuildNotableDate(
        NotableDateRule rule,
        DateTime date,
        string? territory,
        AdjustmentReason? adjustmentReason,
        bool? isNonWorkingOverride = null) =>
        new()
        {
            Date = date.Date,
            Name = rule.Name,
            Category = rule.Category,
            DurationDays = Math.Max(1, rule.DurationDays),
            IsNonWorkingDay = isNonWorkingOverride ?? rule.IsNonWorkingDay ?? false,
            CalendarType = rule.CalendarType,
            TerritoryCode = territory,
            Tags = rule.Tags,
            Comment = rule.Comment,
            AdjustmentReason = adjustmentReason,
        };

    /// <summary>
    /// Splits a comma-separated territory list and returns the entries that overlap the requested territory under parent / child
    /// containment, or yields a single <see langword="null" /> when the rule is territory-neutral.
    /// </summary>
    /// <param name="rule">The rule whose territory list is being expanded.</param>
    /// <param name="requestedTerritory">The requested territory, or <see langword="null" />.</param>
    /// <returns>The applicable territory codes.</returns>
    private static IEnumerable<string?> EnumerateApplicableTerritories(NotableDateRule rule, string? requestedTerritory)
    {
        if (string.IsNullOrEmpty(rule.TerritoryCode))
        {
            yield return null;
            yield break;
        }

        TerritoryCode? requested = null;
        if (!string.IsNullOrEmpty(requestedTerritory) &&
            TerritoryCode.TryParse(requestedTerritory, out TerritoryCode parsed))
        {
            requested = parsed;
        }

        foreach (TerritoryCode owned in TerritoryCode.ParseList(rule.TerritoryCode))
        {
            if (requested is null || owned.Contains(requested.Value) || requested.Value.Contains(owned))
                yield return owned.ToString();
        }
    }

    /// <summary>
    /// Determines whether two inclusive date spans intersect.
    /// </summary>
    /// <param name="leftStart">The first span's start date.</param>
    /// <param name="leftEnd">The first span's end date.</param>
    /// <param name="rightStart">The second span's start date.</param>
    /// <param name="rightEnd">The second span's end date.</param>
    /// <returns><see langword="true" /> when the spans intersect.</returns>
    private static bool Intersects(DateTime leftStart, DateTime leftEnd, DateTime rightStart, DateTime rightEnd) =>
        rightStart.Date <= leftEnd.Date && rightEnd.Date >= leftStart.Date;

    /// <summary>
    /// Determines whether any configured <see cref="RuleRemoval" /> suppresses the supplied rule for the supplied civil year
    /// and territory context. Mirrors the suppression semantics applied by the legacy
    /// <c>NotableDateService.GenerateYear</c> path so override removals behave consistently across both pipelines.
    /// </summary>
    /// <param name="rule">The rule under consideration.</param>
    /// <param name="year">The civil year being materialized.</param>
    /// <param name="territory">The territory the entry would be tagged with, or <see langword="null" /> for territory-neutral.</param>
    /// <returns><see langword="true" /> when at least one configured removal matches; otherwise, <see langword="false" />.</returns>
    private bool IsRemovedByOverride(NotableDateRule rule, int year, string? territory)
    {
        foreach (RuleRemoval removal in _overrideRemovals)
        {
            if (!string.Equals(removal.RuleName, rule.Name, StringComparison.OrdinalIgnoreCase))
                continue;

            if (removal.FromYear is { } from && year < from) continue;
            if (removal.ToYear is { } to && year > to) continue;

            if (!string.IsNullOrEmpty(removal.TerritoryCode))
            {
                if (string.IsNullOrEmpty(territory)) continue;
                if (!TerritoryCode.TryParse(removal.TerritoryCode, out TerritoryCode removalScope)) continue;
                if (!TerritoryCode.TryParse(territory, out TerritoryCode actual)) continue;
                if (!removalScope.Contains(actual)) continue;
            }

            return true;
        }

        return false;
    }
}
