// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDate.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.V2;

/// <summary>
/// Represents a single resolved notable-date occurrence, carrying both the emitted date and the source metadata needed
/// for diagnostics and auditability.
/// </summary>
/// <param name="Date">The emitted occurrence date (the observed date when an adjustment applied).</param>
/// <param name="ActualDate">The originally calculated occurrence date.</param>
/// <param name="IsObserved">
/// Whether the emitted date differs from the calculated date because an adjustment applied.
/// </param>
/// <param name="Identity">The full identity of the rule that produced the occurrence.</param>
/// <param name="DisplayName">The display name of the notable-date concept.</param>
/// <param name="TerritoryCode">The territory the occurrence was resolved for.</param>
/// <param name="Category">The effective category of the rule.</param>
/// <param name="DurationDays">The number of days the occurrence spans, inclusive of the first day.</param>
/// <param name="IsNonWorkingDay">Whether the occurrence is a non-working day.</param>
/// <param name="Tags">The tags declared by the rule that produced the occurrence.</param>
/// <param name="AdjustmentPolicyId">
/// The identifier of the adjustment policy that produced the observed date, if any.
/// </param>
/// <param name="AdjustmentReason">The reason recorded by the adjustment policy, if any.</param>
public sealed record NotableDate(
    DateOnly Date,
    DateOnly? ActualDate,
    bool IsObserved,
    NotableDateRuleIdentity Identity,
    string DisplayName,
    string TerritoryCode,
    NotableDateCategory Category,
    int DurationDays,
    bool IsNonWorkingDay,
    IReadOnlyList<string> Tags,
    string? AdjustmentPolicyId,
    string? AdjustmentReason)
{
    /// <summary>
    /// Gets the identifier of the notable-date concept that produced the occurrence.
    /// </summary>
    /// <returns>The notable-date id from <see cref="Identity" />.</returns>
    public string NotableDateId =>
        this.Identity.NotableDateId;

    /// <summary>
    /// Gets the identifier of the rule that produced the occurrence.
    /// </summary>
    /// <returns>The rule id from <see cref="Identity" />.</returns>
    public string RuleId =>
        this.Identity.RuleId;

    /// <summary>
    /// Gets the last day the occurrence spans.
    /// </summary>
    /// <returns>
    /// The inclusive end date, equal to <see cref="Date" /> for a single-day occurrence and
    /// <c><see cref="Date" /> + <see cref="DurationDays" /> - 1</c> for a multi-day span.
    /// </returns>
    public DateOnly EndDate =>
        this.Date.AddDays(Math.Max(1, this.DurationDays) - 1);
}
