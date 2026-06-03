// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateResolver.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.V2;

/// <summary>
/// Resolves notable-date occurrences from a loaded <see cref="NotableDateResource" /> for a requested territory and
/// date or date range.
/// </summary>
/// <remarks>
/// <para>
/// The resolver applies territory and year filtering, fixed-date strategy calculation, and adjustment policies, then
/// emits occurrences according to each policy's emission mode. Inclusion is decided by the emitted (observed) date, so
/// a single-day query and a range query covering the same dates return consistent results.
/// </para>
/// <para>
/// To capture occurrences whose actual date lies just outside the requested window but whose observed date falls inside
/// it, the resolver scans one civil year either side of the window and filters by the emitted date.
/// </para>
/// </remarks>
public sealed class NotableDateResolver
{
    /// <summary>
    /// The loaded resource the resolver draws occurrences from.
    /// </summary>
    private readonly NotableDateResource _resource;

    /// <summary>
    /// Initializes a new instance of the <see cref="NotableDateResolver" /> class.
    /// </summary>
    /// <param name="resource">The loaded resource the resolver draws occurrences from.</param>
    /// <exception cref="ArgumentNullException"><paramref name="resource" /> is <see langword="null" />.</exception>
    public NotableDateResolver(NotableDateResource resource)
    {
        ThrowHelper.ThrowIfNull(resource);

        this._resource = resource;
    }

    /// <summary>
    /// Resolves the notable-date occurrences emitted on a single day for the requested territory.
    /// </summary>
    /// <param name="date">The day to resolve.</param>
    /// <param name="territory">The requested territory code.</param>
    /// <returns>
    /// The occurrences whose emitted date equals <paramref name="date" />; empty when there are none.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="territory" /> is <see langword="null" />.</exception>
    public IReadOnlyList<ResolvedNotableDate> Resolve(DateOnly date, string territory) =>
        this.Resolve(date, date, territory);

    /// <summary>
    /// Resolves the notable-date occurrences emitted within an inclusive date range for the requested territory.
    /// </summary>
    /// <param name="startInclusive">The first day of the range, inclusive.</param>
    /// <param name="endInclusive">The last day of the range, inclusive.</param>
    /// <param name="territory">The requested territory code.</param>
    /// <returns>
    /// The occurrences whose emitted date falls within the range, ordered by date then identity; empty when there are
    /// none.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="territory" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="startInclusive" /> is later than <paramref name="endInclusive" />.
    /// </exception>
    public IReadOnlyList<ResolvedNotableDate> Resolve(DateOnly startInclusive, DateOnly endInclusive, string territory)
    {
        ThrowHelper.ThrowIfNull(territory);
        ThrowHelper.ThrowIfGreaterThan(startInclusive, endInclusive);

        List<ResolvedNotableDate> results = new();
        StrategyResolutionContext context = new(this._resource);

        int firstYear = Math.Max(1, startInclusive.Year - 1);
        int lastYear = Math.Min(9999, endInclusive.Year + 1);

        foreach (NotableDateDefinition definition in this._resource.NotableDates)
        {
            foreach (NotableDateRule rule in definition.Rules)
            {
                NotableDateCategory category = rule.Category ?? definition.Category;
                NotableDateRuleIdentity identity = this._resource.GetIdentity(definition, rule);

                for (int year = firstYear; year <= lastYear; year++)
                {
                    if (!rule.Applicability.AppliesTo(territory, year))
                        continue;

                    if (rule.Strategy.Calculate(year, context) is not DateOnly baseDate)
                        continue;

                    this.EmitOccurrences(results, definition, rule, category, identity, baseDate, territory, startInclusive, endInclusive);
                }
            }
        }

        return results
            .OrderBy(r => r.Date)
            .ThenBy(r => r.NotableDateId, StringComparer.Ordinal)
            .ThenBy(r => r.RuleId, StringComparer.Ordinal)
            .ToArray();
    }

    /// <summary>
    /// Emits the occurrences produced by a single calculated base date, honouring the winning adjustment policy's
    /// emission mode and the requested window.
    /// </summary>
    /// <param name="results">The accumulating result list.</param>
    /// <param name="definition">The notable-date concept being resolved.</param>
    /// <param name="rule">The rule being resolved.</param>
    /// <param name="category">The effective category of the rule.</param>
    /// <param name="identity">The full identity of the rule.</param>
    /// <param name="baseDate">The calculated (actual) occurrence date.</param>
    /// <param name="territory">The requested territory code.</param>
    /// <param name="startInclusive">The first day of the range, inclusive.</param>
    /// <param name="endInclusive">The last day of the range, inclusive.</param>
    private void EmitOccurrences(
        List<ResolvedNotableDate> results,
        NotableDateDefinition definition,
        NotableDateRule rule,
        NotableDateCategory category,
        NotableDateRuleIdentity identity,
        DateOnly baseDate,
        string territory,
        DateOnly startInclusive,
        DateOnly endInclusive)
    {
        AdjustmentPolicy? winning = this.SelectAdjustmentPolicy(definition, rule, category, baseDate, territory);

        if (winning is null)
        {
            AddIfInRange(results, baseDate, baseDate, false, identity, definition.DisplayName, territory, category, null, null, startInclusive, endInclusive);
            return;
        }

        DateOnly observed = winning.Action.Apply(baseDate);
        string reason = winning.Emission.Reason ?? string.Empty;

        switch (winning.Emission.Mode)
        {
            case EmissionMode.ActualOnly:
                AddIfInRange(results, baseDate, baseDate, false, identity, definition.DisplayName, territory, category, null, null, startInclusive, endInclusive);
                break;

            case EmissionMode.ObservedOnly:
                AddIfInRange(results, observed, baseDate, true, identity, definition.DisplayName, territory, category, winning.Id, reason, startInclusive, endInclusive);
                break;

            case EmissionMode.ActualAndObserved:
            case EmissionMode.ObservedAsAdditional:
                AddIfInRange(results, baseDate, baseDate, false, identity, definition.DisplayName, territory, category, null, null, startInclusive, endInclusive);
                AddIfInRange(results, observed, baseDate, true, identity, definition.DisplayName, territory, category, winning.Id, reason, startInclusive, endInclusive);
                break;

            case EmissionMode.Suppress:
                break;

            default:
                break;
        }
    }

    /// <summary>
    /// Selects the adjustment policy that fires for the supplied base date, using ascending-priority, first-active-wins
    /// evaluation across the rule's scope-matching policy references.
    /// </summary>
    /// <param name="definition">The notable-date concept being resolved.</param>
    /// <param name="rule">The rule being resolved.</param>
    /// <param name="category">The effective category of the rule.</param>
    /// <param name="baseDate">The calculated (actual) occurrence date.</param>
    /// <param name="territory">The requested territory code.</param>
    /// <returns>The winning <see cref="AdjustmentPolicy" />, or <see langword="null" /> when none fires.</returns>
    private AdjustmentPolicy? SelectAdjustmentPolicy(
        NotableDateDefinition definition,
        NotableDateRule rule,
        NotableDateCategory category,
        DateOnly baseDate,
        string territory)
    {
        List<AdjustmentPolicy> candidates = new();

        foreach (string policyRef in rule.AdjustmentPolicyRefs)
        {
            AdjustmentPolicy? policy = this._resource.FindAdjustmentPolicy(policyRef);
            if (policy is null)
                continue;

            if (policy.Scope.Matches(territory, rule.Applicability.Calendar, category, definition.Id, rule.Id))
                candidates.Add(policy);
        }

        return candidates
            .OrderBy(p => p.Priority)
            .FirstOrDefault(p => p.Trigger.IsTriggered(baseDate));
    }

    /// <summary>
    /// Adds a resolved occurrence to the result list when its emitted date falls within the requested window.
    /// </summary>
    /// <param name="results">The accumulating result list.</param>
    /// <param name="emitted">The emitted occurrence date.</param>
    /// <param name="actual">The calculated occurrence date.</param>
    /// <param name="isObserved">Whether the emitted date differs from the actual date.</param>
    /// <param name="identity">The full identity of the rule.</param>
    /// <param name="displayName">The display name of the notable-date concept.</param>
    /// <param name="territory">The requested territory code.</param>
    /// <param name="category">The effective category of the rule.</param>
    /// <param name="adjustmentPolicyId">
    /// The id of the adjustment policy that produced the observed date, if any.
    /// </param>
    /// <param name="reason">The reason recorded by the adjustment policy, if any.</param>
    /// <param name="startInclusive">The first day of the range, inclusive.</param>
    /// <param name="endInclusive">The last day of the range, inclusive.</param>
    private static void AddIfInRange(
        List<ResolvedNotableDate> results,
        DateOnly emitted,
        DateOnly actual,
        bool isObserved,
        NotableDateRuleIdentity identity,
        string displayName,
        string territory,
        NotableDateCategory category,
        string? adjustmentPolicyId,
        string? reason,
        DateOnly startInclusive,
        DateOnly endInclusive)
    {
        if (emitted < startInclusive || emitted > endInclusive)
            return;

        results.Add(new ResolvedNotableDate(
            emitted,
            actual,
            isObserved,
            identity,
            displayName,
            territory,
            category,
            adjustmentPolicyId,
            string.IsNullOrEmpty(reason) ? null : reason));
    }
}
