// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateRuleIdentity.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Represents the full, stable identity of a calculation rule: the owning resource, the notable-date concept, and the
/// rule itself.
/// </summary>
/// <param name="ResourceId">The identifier of the resource that declares the rule.</param>
/// <param name="NotableDateId">The identifier of the notable-date concept that owns the rule.</param>
/// <param name="RuleId">The identifier of the rule within its notable-date concept.</param>
/// <remarks>
/// <para>
/// Identity is the single key used for lookup, override targeting, and result attribution. Display names are never used
/// as identity, so two rules that merely share a display name (for example a national and a regional Constitution Day)
/// remain distinct, and an override that targets one rule never collapses its siblings.
/// </para>
/// </remarks>
public readonly record struct NotableDateRuleIdentity(string ResourceId, string NotableDateId, string RuleId)
{
    /// <summary>
    /// Returns the canonical string form of the identity, joining its components with a forward slash.
    /// </summary>
    /// <returns>The identity rendered as <c>resourceId/notableDateId/ruleId</c>.</returns>
    public override string ToString() =>
        $"{ResourceId}/{NotableDateId}/{RuleId}";
}
