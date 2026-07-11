// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CalculatedEndDateDurationDefinition.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Represents an occurrence duration whose end date is calculated by a second date strategy, producing a span that can
/// vary in length from year to year and can cross a civil-year boundary.
/// </summary>
/// <param name="EndStrategy">The strategy that calculates the end anchor of the span.</param>
/// <param name="StartBoundary">Whether the start anchor is included in the span.</param>
/// <param name="EndBoundary">Whether the end anchor is included in the span.</param>
/// <param name="EndSelection">How the end anchor is selected relative to the start anchor.</param>
/// <remarks>
/// <para>
/// The end strategy is evaluated relative to each resolved start occurrence: for the start anchor's civil year, and,
/// when that yields no acceptable candidate, the immediately following civil year. This models patterns such as a
/// company year-end shutdown that begins the Friday before Boxing Day and ends the Monday after New Year's Day, where
/// the span length differs each year.
/// </para>
/// <para>
/// The effective start and end are derived by applying <see cref="StartBoundary" /> and <see cref="EndBoundary" /> to
/// the resolved anchors. When the resulting span is shorter than one day, or no acceptable end anchor exists in the
/// permitted two-year window, the occurrence is not emitted.
/// </para>
/// </remarks>
/// <seealso cref="NotableDateDurationDefinition" />
public sealed record CalculatedEndDateDurationDefinition(
    IDateCalculationStrategy EndStrategy,
    DateBoundary StartBoundary = DateBoundary.Inclusive,
    DateBoundary EndBoundary = DateBoundary.Inclusive,
    EndDateSelection EndSelection = EndDateSelection.FirstOnOrAfterStart)
    : NotableDateDurationDefinition;
