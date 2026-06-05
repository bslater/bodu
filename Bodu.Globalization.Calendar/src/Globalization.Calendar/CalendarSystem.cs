// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CalendarSystem.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Identifies the calendar system that a rule's strategy is expressed in.
/// </summary>
/// <remarks>
/// <para>
/// A <see cref="FixedDateStrategy" /> expressed in a non-Gregorian system interprets its month and day in that system,
/// then projects the occurrence onto the proleptic Gregorian calendar for resolution. Each value maps to a
/// <see cref="System.Globalization.Calendar" /> provided by the base class library.
/// </para>
/// </remarks>
public enum CalendarSystem
{
    /// <summary>
    /// The proleptic Gregorian calendar.
    /// </summary>
    Gregorian = 0,

    /// <summary>
    /// The tabular Islamic (Hijri) calendar (<see cref="System.Globalization.HijriCalendar" />).
    /// </summary>
    Hijri = 1,

    /// <summary>
    /// The Umm al-Qura Islamic calendar used in Saudi Arabia (<see cref="System.Globalization.UmAlQuraCalendar" />).
    /// </summary>
    UmmAlQura = 2,

    /// <summary>
    /// The Hebrew (Jewish) lunisolar calendar (<see cref="System.Globalization.HebrewCalendar" />).
    /// </summary>
    Hebrew = 3,

    /// <summary>
    /// The Persian (Solar Hijri) calendar (<see cref="System.Globalization.PersianCalendar" />).
    /// </summary>
    Persian = 4,

    /// <summary>
    /// The Chinese lunisolar calendar (<see cref="System.Globalization.ChineseLunisolarCalendar" />).
    /// </summary>
    ChineseLunisolar = 5,
}
