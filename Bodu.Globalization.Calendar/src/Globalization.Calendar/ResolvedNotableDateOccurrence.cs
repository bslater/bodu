// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ResolvedNotableDateOccurrence.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Represents a base notable-date occurrence resolved from a rule before observance adjustments are applied.
/// </summary>
/// <remarks>
/// <para>
/// This type is used internally by <see cref="NotableDateService" /> to separate base-date resolution from adjustment
/// evaluation. That separation allows all base non-working dates for a generated year to be visible before any
/// substitute or conditional adjustment is evaluated.
/// </para>
/// </remarks>
/// <param name="Rule">The originating rule.</param>
/// <param name="AnchorDate">The unadjusted anchor date resolved from the rule.</param>
/// <param name="TerritoryCode">
/// The expanded territory code for this occurrence, or <see langword="null" /> for global.
/// </param>
/// <param name="BaseDate">The materialized unadjusted notable date.</param>
internal sealed record ResolvedNotableDateOccurrence(
    NotableDateRule Rule,
    DateTime AnchorDate,
    string? TerritoryCode,
    NotableDate BaseDate);