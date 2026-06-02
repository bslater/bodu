// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ObservedDateMode.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Controls how a <see cref="NotableDateService" /> emits the actual and observed forms of a notable date when an
/// <see cref="ObservanceAdjustment" /> shifts it from its calculated position (for example, rolling a Saturday holiday
/// to the following Monday).
/// </summary>
/// <remarks>
/// <para>
/// The mode is applied consistently regardless of the width of the query window, so the answer to "what notable date
/// applies on this day?" does not change when the caller asks for a single day versus a range that also includes the
/// observed substitute.
/// </para>
/// <para>
/// The default for <see cref="NotableDateService" /> is <see cref="ObservedOnly" />, which suits public-holiday style
/// services where the observed date is the operative one.
/// </para>
/// </remarks>
public enum ObservedDateMode
{
    /// <summary>
    /// Emit only the actual (calculated) date. The observed substitute produced by an adjustment is suppressed.
    /// </summary>
    ActualOnly,

    /// <summary>
    /// Emit only the observed date. When an adjustment activates, the adjusted occurrence supersedes the actual date,
    /// which is suppressed. This is the default.
    /// </summary>
    ObservedOnly,

    /// <summary>
    /// Emit both the actual date and the observed substitute as separate occurrences when an adjustment activates.
    /// </summary>
    ActualAndObserved,
}
