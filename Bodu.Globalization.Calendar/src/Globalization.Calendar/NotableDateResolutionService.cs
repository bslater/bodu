// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateResolutionService.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Provides an internal service facade for the chronological notable-date resolution pipeline.
/// </summary>
/// <remarks>
/// <para>
/// This service is intentionally separate from <see cref="NotableDateService" /> while the revised architecture is being
/// validated. It owns only the new resolution pipeline and avoids the legacy year-cache and recursive generation model.
/// </para>
/// <para>
/// The service currently materialises base occurrences through <see cref="NotableDateResolutionEngine" />. Observance
/// adjustments and dynamic window expansion are expected to be added in later slices.
/// </para>
/// </remarks>
internal sealed class NotableDateResolutionService
{
    private readonly IReadOnlyList<NotableDateRule> effectiveRules;
    private readonly INotableDateResolutionEngine resolutionEngine;
    private readonly INotableDateCollisionResolver? collisionResolver;

    /// <summary>
    /// Initializes a new instance of the <see cref="NotableDateResolutionService" /> class.
    /// </summary>
    /// <param name="ruleProviders">The rule providers used to load notable-date rules.</param>
    /// <param name="algorithmRegistry">The optional algorithm registry used to resolve algorithm-backed rules.</param>
    /// <param name="collisionResolver">The optional collision resolver applied to emitted dates.</param>
    /// <exception cref="ArgumentNullException"><paramref name="ruleProviders" /> is <see langword="null" />.</exception>
    public NotableDateResolutionService(
        IEnumerable<INotableDateRuleProvider> ruleProviders,
        INotableDateAlgorithmRegistry? algorithmRegistry = null,
        INotableDateCollisionResolver? collisionResolver = null)
    {
        ArgumentNullException.ThrowIfNull(ruleProviders);

        this.effectiveRules = LoadRules(ruleProviders);
        this.collisionResolver = collisionResolver;

        NotableDateRuleResolver ruleResolver = new(this.effectiveRules, algorithmRegistry);
        ICalculationAnchorResolver calculationAnchors = new CachingCalculationAnchorResolver(this.effectiveRules, ruleResolver);
        AnchorRelativeRuleIndex anchorRelativeRules = new(this.effectiveRules);
        INotableDateRuleOccurrenceResolver occurrenceResolver = new NotableDateRuleOccurrenceResolver(
            this.effectiveRules,
            ruleResolver,
            calculationAnchors,
            anchorRelativeRules);

        this.resolutionEngine = new NotableDateResolutionEngine(occurrenceResolver);
    }

    /// <summary>
    /// Gets the effective rules loaded by this resolution service.
    /// </summary>
    internal IReadOnlyList<NotableDateRule> EffectiveRules => effectiveRules;

    /// <summary>
    /// Resolves notable dates for the specified request.
    /// </summary>
    /// <param name="request">The resolution request.</param>
    /// <returns>The resolved notable dates.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="request" /> is <see langword="null" />.</exception>
    public IReadOnlyList<NotableDate> Resolve(NotableDateResolutionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        IReadOnlyList<NotableDate> resolved = resolutionEngine.Resolve(request);

        return ApplyCollisionResolution(resolved);
    }

    /// <summary>
    /// Loads rules from the configured providers.
    /// </summary>
    /// <param name="ruleProviders">The rule providers.</param>
    /// <returns>The loaded rules.</returns>
    private static IReadOnlyList<NotableDateRule> LoadRules(IEnumerable<INotableDateRuleProvider> ruleProviders)
    {
        List<NotableDateRule> rules = new();

        foreach (INotableDateRuleProvider provider in ruleProviders)
        {
            ArgumentNullException.ThrowIfNull(provider, nameof(ruleProviders));

            rules.AddRange(provider.LoadRules());
        }

        return rules;
    }

    /// <summary>
    /// Applies optional collision resolution and returns deterministic output ordering.
    /// </summary>
    /// <param name="dates">The dates to process.</param>
    /// <returns>The collision-resolved dates.</returns>
    private IReadOnlyList<NotableDate> ApplyCollisionResolution(IReadOnlyList<NotableDate> dates)
    {
        if (collisionResolver is null)
            return Sort(dates);

        List<NotableDate> resolved = new();

        foreach (IGrouping<DateTime, NotableDate> group in dates.GroupBy(date => date.Date.Date).OrderBy(group => group.Key))
        {
            IReadOnlyList<NotableDate>? groupResult = collisionResolver.Resolve(group.Key, group.ToList());

            if (groupResult is null)
                continue;

            resolved.AddRange(groupResult);
        }

        return Sort(resolved);
    }

    /// <summary>
    /// Sorts notable dates in deterministic chronological order.
    /// </summary>
    /// <param name="dates">The dates to sort.</param>
    /// <returns>The sorted dates.</returns>
    private static IReadOnlyList<NotableDate> Sort(IEnumerable<NotableDate> dates) =>
        dates
            .OrderBy(date => date.Date)
            .ThenBy(date => date.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(date => date.TerritoryCode, StringComparer.OrdinalIgnoreCase)
            .ThenBy(date => date.CalendarType?.FullName, StringComparer.OrdinalIgnoreCase)
            .ToList();
}