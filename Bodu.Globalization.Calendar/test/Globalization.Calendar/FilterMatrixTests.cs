// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FilterMatrixTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies every single-factory <see cref="NotableDateFilter" /> predicate against directly constructed
/// <see cref="NotableDate" /> occurrences using the v2 single-stage <see cref="NotableDateFilter.Matches" /> surface,
/// together with the factory argument validation.
/// </summary>
/// <remarks>
/// <para>
/// The v1 filter exposed a two-stage gate (rule-level <c>IsRuleEligible</c> and date-level <c>IsMatch</c>); the v2
/// filter is single-stage and is evaluated against a resolved occurrence, so the v1 rule-gate and date-gate rows
/// collapse into a single <see cref="NotableDateFilter.Matches" /> assertion. The v2 string predicates
/// (<see cref="NotableDateFilter.WithTag" />, <see cref="NotableDateFilter.WithName" />,
/// <see cref="NotableDateFilter.WithId" />, and their multi-valued variants) match with
/// <see cref="System.StringComparison.Ordinal" /> rather than the v1 case-insensitive comparison.
/// </para>
/// </remarks>
[TestClass]
public sealed partial class FilterMatrixTests
{
    /// <summary>
    /// Builds a resolved occurrence with the supplied characteristics for direct filter evaluation.
    /// </summary>
    /// <param name="displayName">The display name.</param>
    /// <param name="notableDateId">The notable-date concept id.</param>
    /// <param name="category">The category.</param>
    /// <param name="isNonWorkingDay">Whether the occurrence is a non-working day.</param>
    /// <param name="isObserved">Whether the emitted date was adjusted from the calculated date.</param>
    /// <param name="tags">The tags carried by the occurrence.</param>
    /// <param name="date">The emitted date.</param>
    /// <param name="durationDays">The span length in days.</param>
    /// <returns>The constructed occurrence.</returns>
    private static NotableDate Occurrence(
        string displayName = "Test",
        string notableDateId = "test",
        NotableDateCategory category = NotableDateCategory.PublicHoliday,
        bool isNonWorkingDay = false,
        bool isObserved = false,
        string[]? tags = null,
        DateOnly? date = null,
        int durationDays = 1) =>
        new(
            Date: date ?? new DateOnly(2024, 1, 1),
            ActualDate: date ?? new DateOnly(2024, 1, 1),
            IsObserved: isObserved,
            Identity: new NotableDateRuleIdentity("res", notableDateId, "rule"),
            DisplayName: displayName,
            TerritoryCode: "XX",
            Category: category,
            Priority: 0,
            DurationDays: durationDays,
            IsNonWorkingDay: isNonWorkingDay,
            Tags: tags ?? [],
            AdjustmentPolicyId: null,
            AdjustmentReason: null);
}
