// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateRuleIdentity.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Represents the composite identity that uniquely distinguishes a <see cref="NotableDateRule" /> within a resolved
/// service: its canonical <see cref="NotableDateRule.Name" />, optional rule-level <see cref="NotableDateRule.RuleName" />
/// variant, scoping <see cref="NotableDateRule.TerritoryCode" />, and <see cref="NotableDateRule.CalendarType" />.
/// </summary>
/// <param name="Name">The canonical notable-date name.</param>
/// <param name="RuleName">The rule-level variant identifier, or <see langword="null" /> when the rule has none.</param>
/// <param name="TerritoryCode">The scoping territory code, or <see langword="null" /> when the rule is global.</param>
/// <param name="CalendarType">The scoping calendar type, or <see langword="null" /> when the rule is calendar-agnostic.</param>
/// <remarks>
/// <para>
/// Identity is the single key used across resource flattening, override merging, resolver lookup, range caching, static
/// analysis, and removal evaluation, so that variants sharing a canonical <see cref="NotableDateRule.Name" /> — for
/// example Easter <c>western</c> and Easter <c>orthodox</c> — coexist as distinct rules rather than collapsing under a
/// name-only key.
/// </para>
/// <para>
/// Equality is case-insensitive for the string components and normalises <see cref="TerritoryCode" /> through
/// <see cref="Calendar.TerritoryCode" /> so that, for example, <c>au</c> and <c>AU</c> compare equal while <c>AU</c> and
/// <c>AU-NSW</c> remain distinct. Comma-separated multi-territory values are compared verbatim (case-insensitively),
/// matching the per-rule keying produced by the flatten pipeline. A <see langword="null" /> <see cref="CalendarType" />
/// forms its own bucket and never matches a non-<see langword="null" /> type.
/// </para>
/// </remarks>
internal readonly record struct NotableDateRuleIdentity(string Name, string? RuleName, string? TerritoryCode, Type? CalendarType)
{
    /// <summary>
    /// Derives the <see cref="NotableDateRuleIdentity" /> of the supplied rule.
    /// </summary>
    /// <param name="rule">The rule whose identity is required.</param>
    /// <returns>The composite identity of <paramref name="rule" />.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="rule" /> is <see langword="null" />.</exception>
    public static NotableDateRuleIdentity From(NotableDateRule rule)
    {
        ThrowHelper.ThrowIfNull(rule);

        return new NotableDateRuleIdentity(rule.Name, rule.RuleName, rule.TerritoryCode, rule.CalendarType);
    }

    /// <summary>
    /// Determines whether this identity equals <paramref name="other" /> using case-insensitive string comparison and
    /// territory normalisation.
    /// </summary>
    /// <param name="other">The identity to compare against.</param>
    /// <returns><see langword="true" /> if the two identities are equal; otherwise <see langword="false" />.</returns>
    public bool Equals(NotableDateRuleIdentity other) =>
        string.Equals(Name ?? string.Empty, other.Name ?? string.Empty, StringComparison.OrdinalIgnoreCase)
        && string.Equals(RuleName ?? string.Empty, other.RuleName ?? string.Empty, StringComparison.OrdinalIgnoreCase)
        && string.Equals(NormalizeTerritory(TerritoryCode), NormalizeTerritory(other.TerritoryCode), StringComparison.OrdinalIgnoreCase)
        && CalendarType == other.CalendarType;

    /// <summary>
    /// Returns the hash code combining the canonical name, variant name, normalised territory, and calendar type.
    /// </summary>
    /// <returns>The composite hash code, consistent with <see cref="Equals(NotableDateRuleIdentity)" />.</returns>
    public override int GetHashCode() =>
        HashCode.Combine(
            StringComparer.OrdinalIgnoreCase.GetHashCode(Name ?? string.Empty),
            StringComparer.OrdinalIgnoreCase.GetHashCode(RuleName ?? string.Empty),
            StringComparer.OrdinalIgnoreCase.GetHashCode(NormalizeTerritory(TerritoryCode)),
            CalendarType);

    /// <summary>
    /// Normalises a territory string to its canonical upper-case form when it parses as a single
    /// <see cref="Calendar.TerritoryCode" />, otherwise returns the trimmed value verbatim.
    /// </summary>
    /// <param name="territory">The raw territory value, or <see langword="null" />.</param>
    /// <returns>The normalised territory key; <see cref="string.Empty" /> when the value is absent.</returns>
    internal static string NormalizeTerritory(string? territory) =>
        string.IsNullOrWhiteSpace(territory)
            ? string.Empty
            : Calendar.TerritoryCode.TryParse(territory, out Calendar.TerritoryCode parsed) ? parsed.ToString() : territory.Trim();
}
