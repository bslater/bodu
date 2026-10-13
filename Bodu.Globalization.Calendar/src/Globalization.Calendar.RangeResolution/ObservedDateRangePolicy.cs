// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ObservedDateRangePolicy.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.RangeResolution;

/// <summary>
/// Specifies which span of a resolved occurrence decides whether a date-range query includes it: the emitted span, the
/// actual span, or either.
/// </summary>
/// <remarks>
/// <para>
/// The emitted span runs from <see cref="NotableDate.Date" /> through <see cref="NotableDate.EndDate" />; the actual
/// span runs from <see cref="NotableDate.ActualDate" /> through the same duration. The occurrence a query returns is
/// always the emitted one, whichever span included it.
/// </para>
/// <para>
/// The policy applies to <see cref="INotableDateService.Resolve(DateRange, string)" /> and everything built on it: the
/// by-year and single-day queries, the working-day extensions, which under
/// <see cref="ActualOccurrenceControlsInclusion" /> treat a moved holiday's actual day, not its observed day, as the
/// day it makes non-working, and the caching decorator. Collision groups stay keyed by the emitted date, so an
/// occurrence that loses its emitted day's collision is absent whichever span would include it, and the actual and
/// observed occurrences of an <see cref="EmissionMode.ActualAndObserved" /> policy, which share their actual date,
/// enter and leave an actual-controlled window together.
/// </para>
/// <para>
/// A binary pack whose policy is not <see cref="ObservedOccurrenceControlsInclusion" /> is written at format version 3,
/// so a reader from before the policy was applied rejects it rather than resolving it by the emitted span.
/// </para>
/// </remarks>
/// <seealso cref="ResolutionPolicy" /> <seealso href="../guides/calendar/identity-and-resolution.html">Rule identity,
/// priority, and observed-date resolution (guide)</seealso>
public enum ObservedDateRangePolicy
{
    /// <summary>
    /// The emitted (observed) span controls inclusion: an occurrence is included when any day from its emitted date
    /// through its last day falls within the window. This is the default.
    /// </summary>
    ObservedOccurrenceControlsInclusion = 0,

    /// <summary>
    /// The actual span controls inclusion: an occurrence is included when any day from its originally calculated
    /// (actual) date through its duration falls within the window.
    /// </summary>
    ActualOccurrenceControlsInclusion,

    /// <summary>
    /// Either span controls inclusion: an occurrence is included when its emitted span or its actual span meets the
    /// window.
    /// </summary>
    BothOccurrencesControlInclusion,
}
