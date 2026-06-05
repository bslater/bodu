// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IDateCalculationStrategy.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Algorithms;

/// <summary>
/// Defines the calculation contract for a single rule strategy: given a Gregorian year, produce the occurrence of the
/// notable date in that year, if any.
/// </summary>
/// <remarks>
/// <para>
/// The engine ships a strategy per authoring shape — among them <see cref="FixedDateStrategy" />,
/// <see cref="DayOfWeekInMonthStrategy" />, <see cref="RelativeWeekdayInMonthStrategy" />,
/// <see cref="WeekdayNearDateStrategy" />, <see cref="OffsetFromRuleStrategy" />, and
/// <see cref="AlgorithmDateStrategy" /> (which dispatches to a registered <see cref="INotableDateAlgorithm" />). A
/// strategy is selected per rule when a document is loaded; consumers extend the catalogue through
/// <see cref="INotableDateAlgorithm" /> rather than by implementing this interface directly.
/// </para>
/// </remarks>
public interface IDateCalculationStrategy
{
    /// <summary>
    /// Calculates the occurrence of the notable date in the supplied Gregorian year.
    /// </summary>
    /// <param name="year">The Gregorian year in which to calculate the occurrence.</param>
    /// <param name="context">The resolution context used by referential strategies to resolve other rules.</param>
    /// <returns>
    /// The calculated occurrence, or <see langword="null" /> when the strategy produces no occurrence in
    /// <paramref name="year" /> (for example a 29 February rule in a non-leap year).
    /// </returns>
    DateOnly? Calculate(int year, StrategyResolutionContext context);
}
