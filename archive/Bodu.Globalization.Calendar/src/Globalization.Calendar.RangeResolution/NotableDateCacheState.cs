// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateCacheState.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.RangeResolution;

/// <summary>
/// Identifies the materialization role of a <see cref="NotableDateCacheEntry" /> within the chronological
/// range-resolution pipeline.
/// </summary>
/// <remarks>
/// <para>
/// The role distinguishes entries that represent a real rule occurrence (and are therefore eligible for emission) from
/// entries materialized purely to support adjustment or offset-anchor resolution. Whether a <see cref="Candidate" />
/// entry actually appears in the output is decided independently at emission time by intersecting its base and adjusted
/// dates with the requested window under the active <see cref="ObservedDateMode" /> — so the answer for a given day is
/// independent of the query-window width.
/// </para>
/// </remarks>
internal enum NotableDateCacheState
{
    /// <summary>
    /// The entry exists only as context for adjustment or offset-anchor resolution (for example, an on-demand fringe
    /// anchor materialized so a dependent offset rule can read it). It is never emitted to the caller.
    /// </summary>
    ContextOnly = 0,

    /// <summary>
    /// The entry represents a real rule occurrence and is eligible for emission. Whether it is emitted, and whether the
    /// base or adjusted form is used, is determined at emission time from the requested window and observed-date mode.
    /// </summary>
    Candidate,
}
