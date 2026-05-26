// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RangeResolutionKat.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Test.Kat;

/// <summary>
/// Represents a known-answer test row for a calendar range-resolution scenario: a date range, a territory
/// and calendar identifier, the rule names to evaluate, and the expected actual-date and optional
/// observed-date sequences the resolver must produce.
/// </summary>
/// <param name="Name">The short label that identifies the row in failure diagnostics.</param>
/// <param name="Start">The inclusive start of the query range.</param>
/// <param name="End">The inclusive end of the query range.</param>
/// <param name="Territory">The territory code passed to the resolver.</param>
/// <param name="Calendar">The calendar identifier.</param>
/// <param name="RuleNames">The rule names to evaluate in the range.</param>
/// <param name="ExpectedDates">The expected actual dates, ordered chronologically.</param>
/// <param name="ExpectedObservedDates">The expected observed dates when an adjustment rule applies. <see langword="null" /> when no adjustments are expected; otherwise matches <see cref="ExpectedDates" /> in length.</param>
public sealed record RangeResolutionKat(
    string Name,
    DateOnly Start,
    DateOnly End,
    string Territory,
    string Calendar,
    string[] RuleNames,
    DateOnly[] ExpectedDates,
    DateOnly[]? ExpectedObservedDates = null) : IKat;
