// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ObservedDateRangePolicy.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.RangeResolution;

/// <summary>
/// Determines which emitted occurrence controls whether a resolved notable date is included by a date-range query.
/// </summary>
/// <remarks>
/// <para>
/// The recommended default is <see cref="ObservedOccurrenceControlsInclusion" />, which guarantees that a single-day
/// query and a range query return consistent results: an occurrence is included when its emitted (observed) date falls
/// inside the requested window.
/// </para>
/// </remarks>
public enum ObservedDateRangePolicy
{
    /// <summary>
    /// The emitted (observed) occurrence date controls inclusion.
    /// </summary>
    ObservedOccurrenceControlsInclusion = 0,

    /// <summary>
    /// The originally calculated (actual) occurrence date controls inclusion.
    /// </summary>
    ActualOccurrenceControlsInclusion,

    /// <summary>
    /// Either the actual or the observed occurrence date controls inclusion.
    /// </summary>
    BothOccurrencesControlInclusion,
}
