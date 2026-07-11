// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IMultiOccurrenceCalculation.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Algorithms;

/// <summary>
/// Provides an opt-in extension of <see cref="IDateCalculationStrategy" /> for single-date strategies that can produce
/// more than one occurrence within a single Gregorian year.
/// </summary>
/// <remarks>
/// This lets the resolution engine enumerate every occurrence of such a strategy without a concrete-type check. A
/// non-Gregorian <see cref="FixedDateStrategy" /> is the canonical case: a short lunar month and day can recur twice in
/// one Gregorian year.
/// </remarks>
internal interface IMultiOccurrenceCalculation
{
    /// <summary>
    /// Calculates every occurrence of the strategy within the supplied Gregorian year.
    /// </summary>
    /// <param name="year">The Gregorian year to calculate against.</param>
    /// <param name="context">The resolution context for referential lookups.</param>
    /// <returns>The occurrences in ascending chronological order; an empty list when there is none.</returns>
    IReadOnlyList<DateOnly> CalculateAll(int year, StrategyResolutionContext context);
}
