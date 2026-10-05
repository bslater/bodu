// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RecurrenceFrequency.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Recurrence;

/// <summary>
/// Identifies the base period at which a <see cref="RecurrenceRule" /> repeats, corresponding to the RFC 5545
/// <c>FREQ</c> rule part.
/// </summary>
/// <remarks>
/// <para>
/// The frequency sets the unit that <see cref="RecurrenceRule.Interval" /> multiplies and that the <c>BY</c> rule parts
/// refine. For example, a <see cref="Weekly" /> rule with an interval of two and a <c>BYDAY</c> of Monday repeats on
/// Monday of every second week.
/// </para>
/// <para>
/// At the sub-daily frequencies <see cref="Secondly" />, <see cref="Minutely" />, and <see cref="Hourly" />, each
/// period is a single second, minute, or hour. <c>BYMINUTE</c> and <c>BYSECOND</c> expand an hourly period, and
/// <c>BYSECOND</c> a minutely one; the time parts a frequency does not expand, and the date parts, limit which periods
/// produce occurrences.
/// </para>
/// </remarks>
public enum RecurrenceFrequency
{
    /// <summary>
    /// The rule repeats every second (<c>FREQ=SECONDLY</c>).
    /// </summary>
    Secondly,

    /// <summary>
    /// The rule repeats every minute (<c>FREQ=MINUTELY</c>).
    /// </summary>
    Minutely,

    /// <summary>
    /// The rule repeats every hour (<c>FREQ=HOURLY</c>).
    /// </summary>
    Hourly,

    /// <summary>
    /// The rule repeats every day (<c>FREQ=DAILY</c>).
    /// </summary>
    Daily,

    /// <summary>
    /// The rule repeats every week (<c>FREQ=WEEKLY</c>).
    /// </summary>
    Weekly,

    /// <summary>
    /// The rule repeats every month (<c>FREQ=MONTHLY</c>).
    /// </summary>
    Monthly,

    /// <summary>
    /// The rule repeats every year (<c>FREQ=YEARLY</c>).
    /// </summary>
    Yearly,
}
