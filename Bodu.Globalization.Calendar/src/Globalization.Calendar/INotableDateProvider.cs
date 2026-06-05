// ---------------------------------------------------------------------------------------------------------------
// <copyright file="INotableDateProvider.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Contributes fully-formed notable-date occurrences to a <see cref="NotableDateService" /> in code, for sources that
/// cannot be expressed as authored resource rules.
/// </summary>
/// <remarks>
/// <para>
/// A provider returns finished <see cref="NotableDate" /> occurrences — date and all metadata — for a requested window
/// and territory, complementing the resource-authored rules. It is the code-first counterpart to authoring a rule, and
/// is registered with the service through the provider-aware constructor.
/// </para>
/// <para>
/// Provider occurrences are <b>terminal</b>: the service emits them as supplied (after intersecting them with the
/// requested range and applying any query filter), so they do <b>not</b> pass through adjustment policies, declarative
/// overrides, territory specificity, or the occupied-day interaction that resource rules participate in. They do take
/// part in the final date ordering and the resource's same-day collision policy alongside resource occurrences. A
/// provider that needs observed-date shifting must compute it itself.
/// </para>
/// </remarks>
public interface INotableDateProvider
{
    /// <summary>
    /// Produces the notable-date occurrences that fall within the requested range for the territory.
    /// </summary>
    /// <param name="range">The inclusive range being resolved.</param>
    /// <param name="territory">The requested territory code.</param>
    /// <returns>
    /// The occurrences the provider contributes; an empty sequence when it has none. Occurrences whose span does not
    /// intersect <paramref name="range" /> are ignored by the service.
    /// </returns>
    IEnumerable<NotableDate> GetNotableDates(DateRange range, string territory);
}
