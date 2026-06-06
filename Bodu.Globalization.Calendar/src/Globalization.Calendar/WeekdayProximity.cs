// ---------------------------------------------------------------------------------------------------------------
// <copyright file="WeekdayProximity.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Identifies the direction and inclusivity used when seeking a weekday relative to an anchor date.
/// </summary>
/// <seealso cref="WeekOrdinal" /> <seealso href="../guides/calendar/rule-authoring.html">Authoring notable date rules
/// (guide)</seealso>
public enum WeekdayProximity
{
    /// <summary>
    /// The first matching weekday strictly before the anchor.
    /// </summary>
    Before = 0,

    /// <summary>
    /// The anchor itself when it matches, otherwise the first matching weekday before it.
    /// </summary>
    OnOrBefore,

    /// <summary>
    /// The matching weekday closest to the anchor in either direction.
    /// </summary>
    Nearest,

    /// <summary>
    /// The anchor itself when it matches, otherwise the first matching weekday after it.
    /// </summary>
    OnOrAfter,

    /// <summary>
    /// The first matching weekday strictly after the anchor.
    /// </summary>
    After,
}
