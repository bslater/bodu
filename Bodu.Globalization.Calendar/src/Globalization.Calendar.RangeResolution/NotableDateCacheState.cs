// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateCacheState.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.RangeResolution;

/// <summary>
/// Identifies the current qualification state of a <see cref="NotableDateCacheEntry" /> within the chronological
/// range-resolution pipeline.
/// </summary>
/// <remarks>
/// <para>
/// The state distinguishes entries that are eligible for emission to the caller from those that are present only as
/// adjustment context. Adjustment chains may need to consult dates that lie outside the requested window — for example
/// a prior-year <c>31 December</c> non-working day used as a blocker for a new-year roll-forward — without those dates
/// appearing in the caller's output.
/// </para>
/// </remarks>
internal enum NotableDateCacheState
{
    /// <summary>
    /// The entry is computed and cached but its observed date does not intersect the requested window. The entry
    /// participates only as adjustment context (for example, as a non-working blocker).
    /// </summary>
    Computed = 0,

    /// <summary>
    /// The entry's base observed date intersects the requested window and is eligible for emission.
    /// </summary>
    InWindow,

    /// <summary>
    /// An observance adjustment was applied and the resulting adjusted date intersects the requested window. The
    /// adjusted date is eligible for emission.
    /// </summary>
    Adjusted,

    /// <summary>
    /// An observance adjustment was applied but the resulting adjusted date lies outside the requested window. The
    /// adjusted date is retained as adjustment context only.
    /// </summary>
    AdjustedBlocker,
}
