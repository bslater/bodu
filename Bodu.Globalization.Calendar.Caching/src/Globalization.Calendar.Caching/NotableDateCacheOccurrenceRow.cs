// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateCacheOccurrenceRow.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Caching;

/// <summary>
/// The persisted, flattened form of a single <see cref="NotableDate" /> occurrence, carrying its associating year and
/// resource version alongside every field needed to reconstruct the record.
/// </summary>
/// <remarks>
/// The rule identity is flattened to its three string components and the category to its enum name, so the row is a
/// flat table of scalars that both the TOML and JSON serializers round-trip natively. Nullable fields are omitted when
/// unset and deserialize back to <see langword="null" />.
/// </remarks>
public sealed class NotableDateCacheOccurrenceRow
{
    /// <summary>
    /// Gets or sets the civil year that associates the occurrence with its cached year entry.
    /// </summary>
    /// <value>The civil year.</value>
    public int Year { get; set; }

    /// <summary>
    /// Gets or sets the resource version that associates the occurrence with its cached year entry.
    /// </summary>
    /// <value>The resource version token, or <see langword="null" /> in a malformed file.</value>
    public string? Version { get; set; }

    /// <summary>
    /// Gets or sets the emitted occurrence date.
    /// </summary>
    /// <value>The emitted (observed) date.</value>
    public DateOnly Date { get; set; }

    /// <summary>
    /// Gets or sets the originally calculated occurrence date.
    /// </summary>
    /// <value>The calculated date, or <see langword="null" /> when unset.</value>
    public DateOnly? ActualDate { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the emitted date differs from the calculated date.
    /// </summary>
    /// <value><see langword="true" /> when an adjustment applied.</value>
    public bool IsObserved { get; set; }

    /// <summary>
    /// Gets or sets the resource identifier component of the rule identity.
    /// </summary>
    /// <value>The resource identifier.</value>
    public string? ResourceId { get; set; }

    /// <summary>
    /// Gets or sets the notable-date identifier component of the rule identity.
    /// </summary>
    /// <value>The notable-date identifier.</value>
    public string? NotableDateId { get; set; }

    /// <summary>
    /// Gets or sets the rule identifier component of the rule identity.
    /// </summary>
    /// <value>The rule identifier.</value>
    public string? RuleId { get; set; }

    /// <summary>
    /// Gets or sets the display name of the notable-date concept.
    /// </summary>
    /// <value>The display name.</value>
    public string? DisplayName { get; set; }

    /// <summary>
    /// Gets or sets the territory the occurrence was resolved for.
    /// </summary>
    /// <value>The territory code.</value>
    public string? TerritoryCode { get; set; }

    /// <summary>
    /// Gets or sets the effective category name of the rule.
    /// </summary>
    /// <value>The <see cref="NotableDateCategory" /> name, or <see langword="null" /> in a malformed file.</value>
    public string? Category { get; set; }

    /// <summary>
    /// Gets or sets the selection priority of the rule.
    /// </summary>
    /// <value>The priority.</value>
    public int Priority { get; set; }

    /// <summary>
    /// Gets or sets the number of days the occurrence spans, inclusive of the first day.
    /// </summary>
    /// <value>The duration in days.</value>
    public int DurationDays { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the occurrence is a non-working day.
    /// </summary>
    /// <value><see langword="true" /> when the occurrence is a non-working day.</value>
    public bool IsNonWorkingDay { get; set; }

    /// <summary>
    /// Gets or sets the tags declared by the rule.
    /// </summary>
    /// <value>The tags.</value>
    public List<string> Tags { get; set; } = new();

    /// <summary>
    /// Gets or sets the identifier of the adjustment policy that produced the observed date.
    /// </summary>
    /// <value>The adjustment policy identifier, or <see langword="null" /> when none applied.</value>
    public string? AdjustmentPolicyId { get; set; }

    /// <summary>
    /// Gets or sets the reason recorded by the adjustment policy.
    /// </summary>
    /// <value>The adjustment reason, or <see langword="null" /> when none applied.</value>
    public string? AdjustmentReason { get; set; }
}
