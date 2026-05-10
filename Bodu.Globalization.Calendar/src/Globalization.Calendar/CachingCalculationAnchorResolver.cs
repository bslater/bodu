// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CachingCalculationAnchorResolver.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Concurrent;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Resolves and caches calculation anchors for the lifetime of a notable-date service instance.
/// </summary>
/// <remarks>
/// <para>
/// This resolver caches internal calculation anchors such as <c>Easter Sunday</c> by rule name and year. The cached value
/// is reused by dependent rules such as <c>Start of Lent</c>, <c>Palm Sunday</c>, <c>Good Friday</c>, and
/// <c>Easter Monday</c>.
/// </para>
/// <para>
/// Cached anchors are not emitted as notable dates. They are internal calculation facts. A cached anchor becomes visible to
/// callers only when a corresponding <see cref="NotableDateRule" /> is materialised into a <see cref="NotableDate" />.
/// </para>
/// </remarks>
internal sealed class CachingCalculationAnchorResolver : ICalculationAnchorResolver
{
    private readonly ConcurrentDictionary<CalculationAnchorCacheKey, Lazy<DateTime?>> _cache = new();
    private readonly NotableDateRuleResolver _ruleResolver;
    private readonly IReadOnlyDictionary<string, NotableDateRule> _rulesByName;

    /// <summary>
    /// Initialises a new instance of the <see cref="CachingCalculationAnchorResolver" /> class.
    /// </summary>
    /// <param name="rules">The rules available for anchor resolution.</param>
    /// <param name="ruleResolver">The resolver used to calculate rule anchor dates.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="rules" /> or <paramref name="ruleResolver" /> is <see langword="null" />.
    /// </exception>
    public CachingCalculationAnchorResolver(
        IReadOnlyList<NotableDateRule> rules,
        NotableDateRuleResolver ruleResolver)
    {
        ArgumentNullException.ThrowIfNull(rules);
        ArgumentNullException.ThrowIfNull(ruleResolver);

        Dictionary<string, NotableDateRule> rulesByName = new(StringComparer.OrdinalIgnoreCase);

        foreach (NotableDateRule rule in rules)
        {
            if (string.IsNullOrWhiteSpace(rule.Name))
                continue;

            // Match NotableDateRuleResolver's current behaviour: duplicate names collapse with the last rule winning.
            rulesByName[rule.Name] = rule;
        }

        _rulesByName = rulesByName;
        _ruleResolver = ruleResolver;
    }

    /// <inheritdoc />
    public DateTime? Resolve(string anchorRuleName, int year)
    {
        if (string.IsNullOrWhiteSpace(anchorRuleName))
            throw new ArgumentException("The anchor rule name cannot be null, empty, or white-space.", nameof(anchorRuleName));

        CalculationAnchorCacheKey key = new(anchorRuleName, year);

        Lazy<DateTime?> lazy = _cache.GetOrAdd(
            key,
            static (cacheKey, state) => new Lazy<DateTime?>(
                () => state.ResolveCore(cacheKey),
                LazyThreadSafetyMode.ExecutionAndPublication),
            this);

        try
        {
            return lazy.Value;
        }
        catch
        {
            // Do not permanently cache configuration/algorithm failures. If the caller corrects the provider set in a future
            // service instance, that instance should resolve independently; within this instance, retrying remains possible.
            _cache.TryRemove(key, out _);
            throw;
        }
    }

    /// <summary>
    /// Resolves the uncached anchor value for the specified key.
    /// </summary>
    /// <param name="key">The calculation-anchor cache key.</param>
    /// <returns>
    /// The resolved anchor date, or <see langword="null" /> when the anchor rule does not apply in the requested year.
    /// </returns>
    /// <exception cref="InvalidOperationException">The named anchor rule cannot be found.</exception>
    private DateTime? ResolveCore(CalculationAnchorCacheKey key)
    {
        if (!_rulesByName.TryGetValue(key.AnchorRuleName, out NotableDateRule? rule))
        {
            throw new InvalidOperationException(
                $"The calculation anchor rule '{key.AnchorRuleName}' could not be found.");
        }

        return _ruleResolver.ResolveAnchorDate(rule, key.Year);
    }
}