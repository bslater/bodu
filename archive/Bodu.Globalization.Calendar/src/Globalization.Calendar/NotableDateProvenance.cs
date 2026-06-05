// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateProvenance.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Identifies the layer a resolved <see cref="NotableDate" /> originated from, used as the strongest tie-breaker when
/// arbitrating same-day collisions.
/// </summary>
/// <remarks>
/// Higher values indicate stronger precedence: a <see cref="RuntimeOverride" /> occurrence outranks a
/// <see cref="Local" /> one, which outranks an <see cref="Imported" /> one.
/// </remarks>
public enum NotableDateProvenance
{
    /// <summary>
    /// The occurrence originates from a rule imported into the active resource set from another resource.
    /// </summary>
    Imported = 0,

    /// <summary>
    /// The occurrence originates from a rule declared by the active rule providers (the common case).
    /// </summary>
    Local = 1,

    /// <summary>
    /// The occurrence originates from a runtime override addition layered over the base rule set.
    /// </summary>
    RuntimeOverride = 2,
}
