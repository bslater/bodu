// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateResolutionProjection.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Specifies how a resolved notable-date window should be projected for a caller.
/// </summary>
internal enum NotableDateResolutionProjection
{
    /// <summary>
    /// Returns occurrences whose original rule anchor date intersects the requested window.
    /// </summary>
    AnchorDate,

    /// <summary>
    /// Returns occurrences whose resolved observed date span intersects the requested window.
    /// </summary>
    ObservedDate,
}