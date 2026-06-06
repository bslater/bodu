// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EmissionMode.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.RangeResolution;

/// <summary>
/// Controls which occurrences an adjustment policy emits when its trigger is active: the originally calculated (actual)
/// occurrence, the adjusted (observed) occurrence, both, or neither.
/// </summary>
/// <remarks>
/// <para>
/// The emission mode makes observed-date behaviour explicit and query-width independent: a single-day query and a range
/// query that both span the relevant dates return the same actual-versus-observed answer because inclusion is decided
/// by the emitted occurrence, not by the query shape.
/// </para>
/// </remarks>
/// <seealso cref="AdjustmentPolicy" /> <seealso href="../guides/calendar/adjustment-rules.html">Observance adjustment
/// rules (guide)</seealso>
public enum EmissionMode
{
    /// <summary>
    /// Emit only the originally calculated occurrence; the adjustment does not change which date is reported.
    /// </summary>
    ActualOnly = 0,

    /// <summary>
    /// Suppress the calculated occurrence and emit only the adjusted (observed) occurrence.
    /// </summary>
    ObservedOnly,

    /// <summary>
    /// Emit both the calculated occurrence and the adjusted (observed) occurrence.
    /// </summary>
    ActualAndObserved,

    /// <summary>
    /// Emit the calculated occurrence normally and emit the adjusted occurrence as an additional observance.
    /// </summary>
    /// <remarks>
    /// This value behaves identically to <see cref="ActualAndObserved" /> and is normalized to it when an
    /// <see cref="AdjustmentPolicy" /> is constructed. It is retained for document and binary compatibility only.
    /// </remarks>
    [Obsolete("Use ActualAndObserved; ObservedAsAdditional behaves identically and is normalized to it.")]
    ObservedAsAdditional,

    /// <summary>
    /// Emit neither occurrence when the adjustment applies.
    /// </summary>
    Suppress,
}
