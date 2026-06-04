// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateService.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.V2;

/// <summary>
/// Resolves notable-date occurrences from a loaded <see cref="NotableDateResource" /> for a requested territory and day
/// or date range.
/// </summary>
/// <remarks>
/// <para>
/// Resolution runs in two phases. The first phase calculates every actual occurrence purely and seeds an occupied-day
/// set with the actual dates of non-working occurrences. The second phase places observed dates in an explicit
/// precedence order — earliest actual date, then higher priority, then stable identity — so that a substitute that opts
/// in to <see cref="AdjustmentPolicy.SkipNonWorkingDates" /> advances past days already claimed by other holidays.
/// </para>
/// <para>
/// Inclusion is decided by the emitted (observed) date, so a single-day query and a range query covering the same dates
/// return consistent results. To capture occurrences whose actual date lies just outside the requested window but whose
/// observed date falls inside it, the service scans one civil year either side of the window.
/// </para>
/// </remarks>
public sealed class NotableDateService : INotableDateService
{
    /// <summary>
    /// The loaded resource the service draws occurrences from.
    /// </summary>
    private readonly NotableDateResource _resource;

    /// <summary>
    /// The custom algorithm registry, or <see langword="null" /> when only built-in algorithms are available.
    /// </summary>
    private readonly INotableDateAlgorithmRegistry? _algorithms;

    /// <summary>
    /// The custom same-day collision resolver, consulted when the policy is <see cref="CollisionPolicy.Custom" />.
    /// </summary>
    private readonly INotableDateCollisionResolver? _collisionResolver;

    /// <summary>
    /// Initializes a new instance of the <see cref="NotableDateService" /> class.
    /// </summary>
    /// <param name="resource">The loaded resource the service draws occurrences from.</param>
    /// <exception cref="ArgumentNullException"><paramref name="resource" /> is <see langword="null" />.</exception>
    public NotableDateService(NotableDateResource resource)
        : this(resource, null, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NotableDateService" /> class with a custom algorithm registry.
    /// </summary>
    /// <param name="resource">The loaded resource the service draws occurrences from.</param>
    /// <param name="algorithms">The custom algorithm registry, or <see langword="null" /> for built-ins only.</param>
    /// <exception cref="ArgumentNullException"><paramref name="resource" /> is <see langword="null" />.</exception>
    public NotableDateService(NotableDateResource resource, INotableDateAlgorithmRegistry? algorithms)
        : this(resource, algorithms, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NotableDateService" /> class with a custom algorithm registry and
    /// same-day collision resolver.
    /// </summary>
    /// <param name="resource">The loaded resource the service draws occurrences from.</param>
    /// <param name="algorithms">The custom algorithm registry, or <see langword="null" /> for built-ins only.</param>
    /// <param name="collisionResolver">
    /// The collision resolver consulted when the resource's same-day collision policy is
    /// <see cref="CollisionPolicy.Custom" />, or <see langword="null" />.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="resource" /> is <see langword="null" />.</exception>
    public NotableDateService(NotableDateResource resource, INotableDateAlgorithmRegistry? algorithms, INotableDateCollisionResolver? collisionResolver)
    {
        ThrowHelper.ThrowIfNull(resource);

        this._resource = resource;
        this._algorithms = algorithms;
        this._collisionResolver = collisionResolver;
    }

    /// <inheritdoc />
    public IReadOnlyList<NotableDate> Resolve(DateOnly date, string territory) =>
        this.Resolve(new DateRange(date, date), territory);

    /// <inheritdoc />
    /// <exception cref="ArgumentNullException"><paramref name="territory" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The range start is later than its end.</exception>
    public IReadOnlyList<NotableDate> Resolve(DateRange range, string territory)
    {
        ThrowHelper.ThrowIfNull(territory);
        ThrowHelper.ThrowIfGreaterThan(range.StartDate, range.EndDate);

        int firstYear = Math.Max(1, range.StartDate.Year - 1);
        int lastYear = Math.Min(9999, range.EndDate.Year + 1);

        HashSet<DateOnly> occupied = new();
        List<ResolutionCandidate> candidates = this.GatherCandidates(territory, firstYear, lastYear, occupied);

        candidates.Sort(CompareForPlacement);

        List<NotableDate> results = new();
        foreach (ResolutionCandidate candidate in candidates)
            EmitCandidate(results, candidate, territory, occupied, range);

        List<NotableDate> ordered = results
            .OrderBy(r => r.Date)
            .ThenBy(r => r.NotableDateId, StringComparer.Ordinal)
            .ThenBy(r => r.RuleId, StringComparer.Ordinal)
            .ToList();

        return this.ApplySameDayCollisionPolicy(ordered);
    }

    /// <summary>
    /// Applies the resource's same-day collision policy to occurrences sharing an emitted date, keeping all of them,
    /// the highest-priority occurrence(s), the highest-category-then-priority occurrence(s), or whatever a custom
    /// resolver selects.
    /// </summary>
    /// <param name="ordered">The date-ordered occurrences.</param>
    /// <returns>The occurrences surviving the policy, preserving order.</returns>
    private IReadOnlyList<NotableDate> ApplySameDayCollisionPolicy(List<NotableDate> ordered)
    {
        CollisionPolicy policy = this._resource.ResolutionPolicy.SameDayCollisionPolicy;
        if (policy == CollisionPolicy.KeepAll || ordered.Count < 2)
            return ordered;

        bool higherWins = this._resource.ResolutionPolicy.PriorityDirection == PriorityDirection.HigherWins;
        List<NotableDate> kept = new();

        int index = 0;
        while (index < ordered.Count)
        {
            DateOnly date = ordered[index].Date;
            int end = index;
            while (end < ordered.Count && ordered[end].Date == date)
                end++;

            if (end - index == 1)
            {
                kept.Add(ordered[index]);
            }
            else
            {
                kept.AddRange(ResolveCollision(date, ordered.GetRange(index, end - index), policy, higherWins));
            }

            index = end;
        }

        return kept;
    }

    /// <summary>
    /// Selects the occurrences to keep from a single same-day collision group.
    /// </summary>
    /// <param name="date">The emitted date the occurrences share.</param>
    /// <param name="group">The colliding occurrences.</param>
    /// <param name="policy">The same-day collision policy.</param>
    /// <param name="higherWins">Whether a higher priority value wins.</param>
    /// <returns>The kept occurrences.</returns>
    private IReadOnlyList<NotableDate> ResolveCollision(DateOnly date, List<NotableDate> group, CollisionPolicy policy, bool higherWins)
    {
        switch (policy)
        {
            case CollisionPolicy.HighestPriorityOnly:
                return KeepBestPriority(group, higherWins);

            case CollisionPolicy.CategoryPriority:
            {
                int bestRank = group.Max(n => CategoryRank(n.Category));
                List<NotableDate> topCategory = group.Where(n => CategoryRank(n.Category) == bestRank).ToList();
                return KeepBestPriority(topCategory, higherWins);
            }

            case CollisionPolicy.Custom:
                return this._collisionResolver?.Resolve(date, group) ?? group;

            default:
                return group;
        }
    }

    /// <summary>
    /// Keeps the occurrences whose priority is the best in the group for the configured direction.
    /// </summary>
    /// <param name="group">The occurrences to filter.</param>
    /// <param name="higherWins">Whether a higher priority value wins.</param>
    /// <returns>The best-priority occurrences.</returns>
    private static List<NotableDate> KeepBestPriority(List<NotableDate> group, bool higherWins)
    {
        int best = higherWins ? group.Max(n => n.Priority) : group.Min(n => n.Priority);
        return group.Where(n => n.Priority == best).ToList();
    }

    /// <summary>
    /// Returns the collision-precedence rank of a category, where a higher rank wins a category collision.
    /// </summary>
    /// <param name="category">The category to rank.</param>
    /// <returns>The precedence rank.</returns>
    private static int CategoryRank(NotableDateCategory category) =>
        category switch
        {
            NotableDateCategory.PublicHoliday => 11,
            NotableDateCategory.BankHoliday => 10,
            NotableDateCategory.Remembrance => 9,
            NotableDateCategory.Religious => 8,
            NotableDateCategory.Civic => 7,
            NotableDateCategory.Seasonal => 6,
            NotableDateCategory.Cultural => 5,
            NotableDateCategory.School => 4,
            NotableDateCategory.Regional => 3,
            NotableDateCategory.Observance => 2,
            NotableDateCategory.Other => 1,
            _ => 0,
        };

    /// <inheritdoc />
    /// <exception cref="ArgumentNullException">
    /// <paramref name="territory" /> or <paramref name="filter" /> is <see langword="null" />.
    /// </exception>
    public IReadOnlyList<NotableDate> Resolve(DateOnly date, string territory, NotableDateFilter filter) =>
        this.Resolve(new DateRange(date, date), territory, filter);

    /// <inheritdoc />
    /// <exception cref="ArgumentNullException">
    /// <paramref name="territory" /> or <paramref name="filter" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">The range start is later than its end.</exception>
    public IReadOnlyList<NotableDate> Resolve(DateRange range, string territory, NotableDateFilter filter)
    {
        ThrowHelper.ThrowIfNull(filter);

        return this.Resolve(range, territory).Where(filter.Matches).ToArray();
    }

    /// <summary>
    /// Phase one: calculates every applicable actual occurrence and seeds the occupied-day set with the actual dates of
    /// non-working occurrences.
    /// </summary>
    /// <param name="territory">The requested territory code.</param>
    /// <param name="firstYear">The first civil year to scan.</param>
    /// <param name="lastYear">The last civil year to scan.</param>
    /// <param name="occupied">The occupied-day set to seed.</param>
    /// <returns>The calculated candidates.</returns>
    private List<ResolutionCandidate> GatherCandidates(string territory, int firstYear, int lastYear, HashSet<DateOnly> occupied)
    {
        StrategyResolutionContext context = new(this._resource, this._algorithms);
        List<ResolutionCandidate> candidates = new();

        foreach (NotableDateDefinition definition in this._resource.NotableDates)
        {
            for (int year = firstYear; year <= lastYear; year++)
            {
                // Within a concept, the most-specific territory match wins: a narrower rule (for example AU-WA)
                // shadows a broader same-concept rule (AU) for that territory and year.
                List<NotableDateRule> applicable = new();
                int maxSpecificity = -1;
                foreach (NotableDateRule rule in definition.Rules)
                {
                    if (!rule.Applicability.AppliesTo(territory, year))
                        continue;

                    applicable.Add(rule);
                    maxSpecificity = Math.Max(maxSpecificity, rule.Applicability.MatchSpecificity(territory));
                }

                foreach (NotableDateRule rule in applicable)
                {
                    if (rule.Applicability.MatchSpecificity(territory) != maxSpecificity)
                        continue;

                    NotableDateCategory category = rule.Category ?? definition.Category;
                    NotableDateRuleIdentity identity = this._resource.GetIdentity(definition, rule);
                    bool nonWorking = rule.NonWorking ?? definition.DefaultNonWorkingDay;
                    int durationDays = Math.Max(1, rule.DurationDays ?? definition.DefaultDurationDays);

                    IReadOnlyList<string> tags = rule.Tags.Count > 0 ? rule.Tags : definition.Tags;

                    foreach (DateOnly baseDate in EnumerateBaseDates(rule.Strategy, year, context))
                    {
                        AdjustmentPolicy? policy = this.SelectAdjustmentPolicy(definition, rule, category, baseDate, territory);
                        candidates.Add(new ResolutionCandidate(identity, definition.DisplayName, category, baseDate, policy, rule.Priority, nonWorking, durationDays, tags));

                        if (nonWorking)
                            occupied.Add(baseDate);
                    }
                }
            }
        }

        return candidates;
    }

    /// <summary>
    /// Enumerates the calculated occurrences a strategy produces for a year: every occurrence of a fixed-date strategy
    /// (a short Islamic month and day can recur twice in one Gregorian year) and the single occurrence of every other
    /// strategy.
    /// </summary>
    /// <param name="strategy">The strategy to evaluate.</param>
    /// <param name="year">The Gregorian year to calculate against.</param>
    /// <param name="context">The resolution context for offset references.</param>
    /// <returns>The calculated occurrences for the year.</returns>
    private static IEnumerable<DateOnly> EnumerateBaseDates(IDateCalculationStrategy strategy, int year, StrategyResolutionContext context)
    {
        if (strategy is FixedDateStrategy fixedStrategy)
            return fixedStrategy.CalculateAll(year, context);

        return strategy.Calculate(year, context) is DateOnly date ? new[] { date } : Array.Empty<DateOnly>();
    }

    /// <summary>
    /// Orders candidates for placement: earliest actual date first, then higher priority, then stable identity.
    /// Earliest first ensures an earlier holiday claims a contested day before a later one resolves its substitute.
    /// </summary>
    /// <param name="left">The first candidate.</param>
    /// <param name="right">The second candidate.</param>
    /// <returns>A signed comparison result.</returns>
    private static int CompareForPlacement(ResolutionCandidate left, ResolutionCandidate right)
    {
        int byDate = left.BaseDate.CompareTo(right.BaseDate);
        if (byDate != 0)
            return byDate;

        int byPriority = right.Priority.CompareTo(left.Priority);
        if (byPriority != 0)
            return byPriority;

        int byNotableDate = string.CompareOrdinal(left.Identity.NotableDateId, right.Identity.NotableDateId);
        return byNotableDate != 0 ? byNotableDate : string.CompareOrdinal(left.Identity.RuleId, right.Identity.RuleId);
    }

    /// <summary>
    /// Phase two: computes the observed date for a candidate against the occupied-day set and emits occurrences per the
    /// winning policy's emission mode, updating the occupied-day set with any claimed observed date.
    /// </summary>
    /// <param name="results">The accumulating result list.</param>
    /// <param name="candidate">The candidate being placed.</param>
    /// <param name="territory">The requested territory code.</param>
    /// <param name="occupied">The occupied-day set, updated as observed dates are claimed.</param>
    /// <param name="range">The inclusive range that controls emission inclusion.</param>
    private static void EmitCandidate(
        List<NotableDate> results,
        ResolutionCandidate candidate,
        string territory,
        HashSet<DateOnly> occupied,
        DateRange range)
    {
        if (candidate.Policy is not AdjustmentPolicy policy)
        {
            AddIfInRange(results, candidate.BaseDate, candidate.BaseDate, false, candidate, territory, null, null, range);
            return;
        }

        DateOnly observed = policy.ApplyAction(candidate.BaseDate, occupied.Contains);
        string reason = policy.Reason ?? string.Empty;

        switch (policy.Emission)
        {
            case EmissionMode.ActualOnly:
                AddIfInRange(results, candidate.BaseDate, candidate.BaseDate, false, candidate, territory, null, null, range);
                break;

            case EmissionMode.ObservedOnly:
                AddIfInRange(results, observed, candidate.BaseDate, true, candidate, territory, policy.Id, reason, range);
                Claim(occupied, observed, candidate);
                break;

            case EmissionMode.ActualAndObserved:
            case EmissionMode.ObservedAsAdditional:
                AddIfInRange(results, candidate.BaseDate, candidate.BaseDate, false, candidate, territory, null, null, range);
                AddIfInRange(results, observed, candidate.BaseDate, true, candidate, territory, policy.Id, reason, range);
                Claim(occupied, observed, candidate);
                break;

            case EmissionMode.Suppress:
                break;

            default:
                break;
        }
    }

    /// <summary>
    /// Claims an observed date in the occupied-day set when the candidate is a non-working occurrence.
    /// </summary>
    /// <param name="occupied">The occupied-day set.</param>
    /// <param name="observed">The observed date to claim.</param>
    /// <param name="candidate">The candidate that produced the observed date.</param>
    private static void Claim(HashSet<DateOnly> occupied, DateOnly observed, ResolutionCandidate candidate)
    {
        if (candidate.NonWorking)
            occupied.Add(observed);
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
            .FirstOrDefault(p => p.IsTriggered(baseDate));
    }

    /// <summary>
    /// Adds a resolved occurrence to the result list when its emitted date falls within the requested window.
    /// </summary>
    /// <param name="results">The accumulating result list.</param>
    /// <param name="emitted">The emitted occurrence date.</param>
    /// <param name="actual">The calculated occurrence date.</param>
    /// <param name="isObserved">Whether the emitted date differs from the actual date.</param>
    /// <param name="candidate">The candidate that produced the occurrence.</param>
    /// <param name="territory">The requested territory code.</param>
    /// <param name="adjustmentPolicyId">
    /// The id of the adjustment policy that produced the observed date, if any.
    /// </param>
    /// <param name="reason">The reason recorded by the adjustment policy, if any.</param>
    /// <param name="range">The inclusive range that controls inclusion.</param>
    /// <remarks>
    /// <para>
    /// A multi-day occurrence is included when any day of its span intersects the requested window, so a single-day
    /// query for a day inside a multi-day holiday returns it.
    /// </para>
    /// </remarks>
    private static void AddIfInRange(
        List<NotableDate> results,
        DateOnly emitted,
        DateOnly actual,
        bool isObserved,
        ResolutionCandidate candidate,
        string territory,
        string? adjustmentPolicyId,
        string? reason,
        DateRange range)
    {
        int spanEndDayNumber = emitted.DayNumber + Math.Max(1, candidate.DurationDays) - 1;
        if (emitted > range.EndDate || spanEndDayNumber < range.StartDate.DayNumber)
            return;

        results.Add(new NotableDate(
            emitted,
            actual,
            isObserved,
            candidate.Identity,
            candidate.DisplayName,
            territory,
            candidate.Category,
            candidate.Priority,
            candidate.DurationDays,
            candidate.NonWorking,
            candidate.Tags,
            adjustmentPolicyId,
            string.IsNullOrEmpty(reason) ? null : reason));
    }
}
