// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateResource.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Represents a fully loaded and validated notable-date document resource: its resolution policy, reusable adjustment
/// policies, and notable-date concepts.
/// </summary>
/// <remarks>
/// <para>
/// A resource is immutable once loaded. Imports and override operations are applied during loading, so the concepts and
/// rules exposed here are the effective set used by the resolver.
/// </para>
/// <para>
/// <strong>How obtained.</strong> A resource is normally produced by <see cref="NotableDateResourceLoader" />, a data
/// pack factory, or <c>NotableDateDocumentBuilder.Build</c>, then handed to a <see cref="NotableDateService" />. The
/// public constructor exists for advanced code-first assembly; most consumers never call it directly.
/// </para>
/// </remarks>
/// <seealso cref="NotableDateResourceLoader" /> <seealso cref="NotableDateService" />
/// <seealso href="../guides/calendar/building-the-service.html">Building and extending the service (guide)</seealso>
public sealed class NotableDateResource
{
    /// <summary>
    /// The reusable adjustment policies indexed by their identifier for O(1) lookup, first-wins on duplicates.
    /// </summary>
    private readonly Dictionary<string, AdjustmentPolicy> _adjustmentPoliciesById;

    /// <summary>
    /// The notable-date concepts indexed by their identifier for O(1) lookup, first-wins on duplicates.
    /// </summary>
    private readonly Dictionary<string, NotableDateDefinition> _definitionsById;

    /// <summary>
    /// Initializes a new instance of the <see cref="NotableDateResource" /> class.
    /// </summary>
    /// <param name="resourceId">The stable identifier of the resource.</param>
    /// <param name="schemaVersion">The schema version declared by the resource.</param>
    /// <param name="resolutionPolicy">The resource-level resolution policy.</param>
    /// <param name="adjustmentPolicies">The reusable adjustment policies declared by the resource.</param>
    /// <param name="notableDates">The notable-date concepts declared by the resource.</param>
    /// <exception cref="ArgumentNullException">Any argument is <see langword="null" />.</exception>
    public NotableDateResource(
        string resourceId,
        string schemaVersion,
        ResolutionPolicy resolutionPolicy,
        IEnumerable<AdjustmentPolicy> adjustmentPolicies,
        IEnumerable<NotableDateDefinition> notableDates)
    {
        ThrowHelper.ThrowIfNull(resourceId);
        ThrowHelper.ThrowIfNull(schemaVersion);
        ThrowHelper.ThrowIfNull(resolutionPolicy);
        ThrowHelper.ThrowIfNull(adjustmentPolicies);
        ThrowHelper.ThrowIfNull(notableDates);

        ResourceId = resourceId;
        SchemaVersion = schemaVersion;
        ResolutionPolicy = resolutionPolicy;
        AdjustmentPolicies = [.. adjustmentPolicies];
        NotableDates = [.. notableDates];

        // Index the immutable collections once so per-query lookups are O(1) rather than linear scans. Both indices are
        // first-wins on a duplicate identifier, matching the historical linear-search behaviour these lookups replace;
        // duplicate identifiers are themselves reported as errors by the validator during loading.
        _adjustmentPoliciesById = new Dictionary<string, AdjustmentPolicy>(AdjustmentPolicies.Count, StringComparer.Ordinal);
        foreach (AdjustmentPolicy policy in AdjustmentPolicies)
            _adjustmentPoliciesById.TryAdd(policy.Id, policy);

        _definitionsById = new Dictionary<string, NotableDateDefinition>(NotableDates.Count, StringComparer.Ordinal);
        foreach (NotableDateDefinition definition in NotableDates)
            _definitionsById.TryAdd(definition.Id, definition);
    }

    /// <summary>
    /// Gets the stable identifier of the resource.
    /// </summary>
    /// <returns>The resource id.</returns>
    public string ResourceId { get; }

    /// <summary>
    /// Gets the schema version declared by the resource.
    /// </summary>
    /// <returns>The schema version string.</returns>
    public string SchemaVersion { get; }

    /// <summary>
    /// Gets the resource-level resolution policy.
    /// </summary>
    /// <returns>The <see cref="ResolutionPolicy" />.</returns>
    public ResolutionPolicy ResolutionPolicy { get; }

    /// <summary>
    /// Gets the reusable adjustment policies declared by the resource.
    /// </summary>
    /// <returns>The adjustment policies; empty when none are declared.</returns>
    public IReadOnlyList<AdjustmentPolicy> AdjustmentPolicies { get; }

    /// <summary>
    /// Gets the notable-date concepts declared by the resource.
    /// </summary>
    /// <returns>The notable-date concepts.</returns>
    public IReadOnlyList<NotableDateDefinition> NotableDates { get; }

    /// <summary>
    /// Gets the total number of rules across every notable-date concept in the resource.
    /// </summary>
    /// <returns>The aggregate rule count.</returns>
    public int RuleCount =>
        NotableDates.Sum(d => d.Rules.Count);

    /// <summary>
    /// Builds the full identity of a rule belonging to this resource.
    /// </summary>
    /// <param name="definition">The notable-date concept that owns the rule.</param>
    /// <param name="rule">The rule whose identity is required.</param>
    /// <returns>The composite <see cref="NotableDateRuleIdentity" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="definition" /> or <paramref name="rule" /> is <see langword="null" />.
    /// </exception>
    public NotableDateRuleIdentity GetIdentity(NotableDateDefinition definition, NotableDateRule rule)
    {
        ThrowHelper.ThrowIfNull(definition);
        ThrowHelper.ThrowIfNull(rule);

        return new NotableDateRuleIdentity(ResourceId, definition.Id, rule.Id);
    }

    /// <summary>
    /// Finds the reusable adjustment policy with the supplied identifier.
    /// </summary>
    /// <param name="id">The policy identifier to locate.</param>
    /// <returns>The matching <see cref="AdjustmentPolicy" />, or <see langword="null" /> when none exists.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="id" /> is <see langword="null" />.</exception>
    public AdjustmentPolicy? FindAdjustmentPolicy(string id)
    {
        ThrowHelper.ThrowIfNull(id);

        return _adjustmentPoliciesById.TryGetValue(id, out AdjustmentPolicy? policy) ? policy : null;
    }

    /// <summary>
    /// Finds the notable-date concept with the supplied identifier.
    /// </summary>
    /// <param name="id">The concept identifier to locate.</param>
    /// <returns>
    /// The matching <see cref="NotableDateDefinition" />, or <see langword="null" /> when none exists.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="id" /> is <see langword="null" />.</exception>
    public NotableDateDefinition? FindDefinition(string id)
    {
        ThrowHelper.ThrowIfNull(id);

        return _definitionsById.TryGetValue(id, out NotableDateDefinition? definition) ? definition : null;
    }
}
