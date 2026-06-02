// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateRuleReference.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Represents a reference to a <see cref="NotableDateRule" /> used by offset anchors, replacement-target adjustments,
/// removals, and nested-override targeting.
/// </summary>
/// <param name="Name">The canonical notable-date name to match. Required.</param>
/// <param name="RuleName">An optional rule-level variant filter, or <see langword="null" /> to match any variant.</param>
/// <param name="TerritoryCode">An optional territory filter, or <see langword="null" /> to match any territory.</param>
/// <param name="CalendarType">An optional calendar-type filter, or <see langword="null" /> to match any calendar.</param>
/// <remarks>
/// <para>
/// A reference is a <em>partial selector</em>, not an equality key: every component except <see cref="Name" /> is an
/// optional filter applied during resolution. A reference that supplies only <see cref="Name" /> resolves uniquely when
/// a single candidate exists, and otherwise disambiguates by preferring a candidate that shares the requesting context's
/// territory and calendar (see <see cref="NotableDateRuleIndex" />). When no single candidate can be selected the
/// resolution is reported as ambiguous so callers can surface a precise diagnostic rather than binding to an arbitrary
/// variant.
/// </para>
/// </remarks>
internal readonly record struct NotableDateRuleReference(
    string Name,
    string? RuleName = null,
    string? TerritoryCode = null,
    Type? CalendarType = null)
{
    /// <summary>
    /// Creates a reference that matches solely on canonical <paramref name="name" />, leaving every other component
    /// unconstrained.
    /// </summary>
    /// <param name="name">The canonical notable-date name to match.</param>
    /// <returns>A name-only <see cref="NotableDateRuleReference" />.</returns>
    public static NotableDateRuleReference ForName(string name) =>
        new(name);
}
