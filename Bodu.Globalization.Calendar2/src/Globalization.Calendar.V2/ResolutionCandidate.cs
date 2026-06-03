// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ResolutionCandidate.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.V2;

/// <summary>
/// Represents a single calculated occurrence captured during the first resolution phase, before observed dates are
/// placed against the occupied-day set.
/// </summary>
/// <param name="Identity">The full identity of the rule that produced the occurrence.</param>
/// <param name="DisplayName">The display name of the notable-date concept.</param>
/// <param name="Category">The effective category of the rule.</param>
/// <param name="BaseDate">The calculated (actual) occurrence date.</param>
/// <param name="Policy">
/// The adjustment policy that fired for the occurrence, or <see langword="null" /> when none did.
/// </param>
/// <param name="Priority">The selection priority of the owning rule, used for placement precedence.</param>
/// <param name="NonWorking">Whether the occurrence is a non-working day that claims the days it occupies.</param>
/// <param name="DurationDays">The number of days the occurrence spans, inclusive of the first day.</param>
/// <param name="Tags">The tags declared by the rule that produced the occurrence.</param>
internal sealed record ResolutionCandidate(
    NotableDateRuleIdentity Identity,
    string DisplayName,
    NotableDateCategory Category,
    DateOnly BaseDate,
    AdjustmentPolicy? Policy,
    int Priority,
    bool NonWorking,
    int DurationDays,
    IReadOnlyList<string> Tags);
