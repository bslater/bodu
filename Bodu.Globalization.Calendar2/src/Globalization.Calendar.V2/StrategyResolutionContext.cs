// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StrategyResolutionContext.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.V2;

/// <summary>
/// Provides a strategy with access to the surrounding resource so that referential strategies (such as
/// <see cref="OffsetFromRuleStrategy" />) can resolve the occurrence of another rule for the same year.
/// </summary>
/// <remarks>
/// <para>
/// The context maintains an in-progress set keyed by rule identity so that a circular reference between offset rules
/// resolves to <see langword="null" /> rather than recursing without end.
/// </para>
/// </remarks>
public sealed class StrategyResolutionContext
{
    /// <summary>
    /// The resource that referential strategies resolve against.
    /// </summary>
    private readonly NotableDateResource _resource;

    /// <summary>
    /// The identities currently being resolved, used to break circular references.
    /// </summary>
    private readonly HashSet<NotableDateRuleIdentity> _inProgress = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="StrategyResolutionContext" /> class.
    /// </summary>
    /// <param name="resource">The resource that referential strategies resolve against.</param>
    /// <exception cref="ArgumentNullException"><paramref name="resource" /> is <see langword="null" />.</exception>
    public StrategyResolutionContext(NotableDateResource resource)
        : this(resource, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="StrategyResolutionContext" /> class with a custom algorithm
    /// registry.
    /// </summary>
    /// <param name="resource">The resource that referential strategies resolve against.</param>
    /// <param name="algorithms">The custom algorithm registry, or <see langword="null" /> for built-ins only.</param>
    /// <exception cref="ArgumentNullException"><paramref name="resource" /> is <see langword="null" />.</exception>
    public StrategyResolutionContext(NotableDateResource resource, INotableDateAlgorithmRegistry? algorithms)
    {
        ThrowHelper.ThrowIfNull(resource);

        this._resource = resource;
        this.Algorithms = algorithms;
    }

    /// <summary>
    /// Gets the custom algorithm registry consulted for keys the built-in catalogue does not recognize.
    /// </summary>
    /// <returns>The registry, or <see langword="null" /> when only built-in algorithms are available.</returns>
    public INotableDateAlgorithmRegistry? Algorithms { get; }

    /// <summary>
    /// Resolves the occurrence of a referenced rule for the supplied Gregorian year.
    /// </summary>
    /// <param name="notableDateRef">The identifier of the referenced notable-date concept.</param>
    /// <param name="ruleRef">
    /// The identifier of the referenced rule, or <see langword="null" /> to use its sole rule.
    /// </param>
    /// <param name="year">The Gregorian year to resolve.</param>
    /// <returns>
    /// The referenced occurrence, or <see langword="null" /> when the reference cannot be resolved (missing, ambiguous,
    /// circular, or producing no occurrence).
    /// </returns>
    public DateOnly? ResolveReference(string notableDateRef, string? ruleRef, int year)
    {
        NotableDateDefinition? definition = null;
        foreach (NotableDateDefinition candidate in this._resource.NotableDates)
        {
            if (string.Equals(candidate.Id, notableDateRef, StringComparison.Ordinal))
            {
                definition = candidate;
                break;
            }
        }

        if (definition is null)
            return null;

        NotableDateRule? rule = SelectRule(definition, ruleRef);
        if (rule is null)
            return null;

        NotableDateRuleIdentity identity = this._resource.GetIdentity(definition, rule);
        if (!this._inProgress.Add(identity))
            return null;

        try
        {
            return rule.Strategy.Calculate(year, this);
        }
        finally
        {
            this._inProgress.Remove(identity);
        }
    }

    /// <summary>
    /// Selects the referenced rule, requiring an unambiguous match.
    /// </summary>
    /// <param name="definition">The referenced concept.</param>
    /// <param name="ruleRef">The requested rule identifier, or <see langword="null" /> to use the sole rule.</param>
    /// <returns>The matching rule, or <see langword="null" /> when missing or ambiguous.</returns>
    private static NotableDateRule? SelectRule(NotableDateDefinition definition, string? ruleRef)
    {
        if (!string.IsNullOrEmpty(ruleRef))
        {
            foreach (NotableDateRule candidate in definition.Rules)
            {
                if (string.Equals(candidate.Id, ruleRef, StringComparison.Ordinal))
                    return candidate;
            }

            return null;
        }

        return definition.Rules.Count == 1 ? definition.Rules[0] : null;
    }
}
