// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ObservedDateRangePolicy.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.RangeResolution;

/// <summary>
/// Specifies which occurrence date a resource asks to control whether a resolved notable date is included by a
/// date-range query. Reserved: the value is stored but not yet applied.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Reserved.</strong> The value is read from a document's <c>observedDateRangePolicy</c> attribute, kept on
/// <see cref="ResolutionPolicy.ObservedDateRangePolicy" />, and written to binary packs, but the service does not
/// consult it: every query includes an occurrence when its emitted span, <see cref="NotableDate.Date" /> through
/// <see cref="NotableDate.EndDate" />, intersects the requested window, which is the behaviour
/// <see cref="ObservedOccurrenceControlsInclusion" /> describes. To include a holiday by its actual date, have its
/// adjustment policy emit the actual occurrence too (<see cref="EmissionMode.ActualAndObserved" />). Implementing the
/// other values is tracked by <see href="https://github.com/bslater/bodu/issues/1119">issue #1119</see>.
/// </para>
/// </remarks>
/// <seealso cref="ResolutionPolicy" /> <seealso href="../guides/calendar/identity-and-resolution.html">Rule identity,
/// priority, and observed-date resolution (guide)</seealso>
public enum ObservedDateRangePolicy
{
    /// <summary>
    /// The emitted (observed) occurrence date controls inclusion. This is how every query behaves today.
    /// </summary>
    ObservedOccurrenceControlsInclusion = 0,

    /// <summary>
    /// The originally calculated (actual) occurrence date would control inclusion. Reserved: not yet applied.
    /// </summary>
    ActualOccurrenceControlsInclusion,

    /// <summary>
    /// Either the actual or the observed occurrence date would control inclusion. Reserved: not yet applied.
    /// </summary>
    BothOccurrencesControlInclusion,
}
