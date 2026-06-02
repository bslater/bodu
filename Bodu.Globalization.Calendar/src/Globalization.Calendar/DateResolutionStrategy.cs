// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DateResolutionStrategy.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Identifies the strategy a <see cref="NotableDateRule" /> uses to resolve its anchor date for a given year.
/// </summary>
/// <remarks>
/// This enumeration replaces <c>NotableDateDefinitionType</c>. Each value corresponds to a distinct branch in the
/// <see cref="NotableDateRuleResolver" /> and to a distinct <c>&lt;Definition&gt;</c> child element in the XML schema.
/// </remarks>
public enum DateResolutionStrategy
{
    /// <summary>
    /// Resolved from a fixed month and day in the rule's <see cref="NotableDateRule.CalendarType" />. For Gregorian
    /// rules (the default, when <see cref="NotableDateRule.CalendarType" /> is <see langword="null" />) the date is
    /// identical every year. For rules authored against a non-Gregorian calendar — Hijri, Umm al-Qura, Hebrew, Persian,
    /// or Chinese lunisolar — the resolver projects the authored (month, day) tuple through the target calendar to a
    /// Gregorian date that varies each year, with <see cref="NotableDateRule.SweepCalendarYears" /> or
    /// <see cref="NotableDateRule.SkipLeapMonth" /> assistance as appropriate to the calendar family.
    /// </summary>
    Fixed = 0,

    /// <summary>
    /// Resolved as the n-th occurrence of a specified weekday within a specified month (e.g. the second Monday of
    /// March).
    /// </summary>
    DayOfWeekInMonth,

    /// <summary>
    /// Resolved by an algorithmic <see cref="INotableDateAlgorithm" /> implementation looked up via key in the
    /// algorithm registry.
    /// </summary>
    Algorithm,

    /// <summary>
    /// Resolved as a fixed integer day offset from another notable date rule referenced by name (e.g. Easter Monday =
    /// Easter Sunday + 1).
    /// </summary>
    OffsetFromAnchor,

    /// <summary>
    /// Resolved as the occurrence of a specified weekday positioned relative to a fixed reference month and day — the
    /// first such weekday on or after the reference, on or before it, or the nearest occurrence in either direction.
    /// Expresses holidays such as "the Saturday between 20 and 26 June" (Nordic Midsummer), "the Wednesday before 23
    /// November" (German Repentance Day), and "the Monday nearest to a given date" without a custom algorithm.
    /// </summary>
    WeekdayNearDate,

    /// <summary>
    /// Resolved as the occurrence of a target weekday positioned relative to the <em>n</em>th occurrence of an anchor
    /// weekday within a month — on or after it, on or before it, or nearest to it. The anchor is a
    /// <see cref="DayOfWeekInMonth" />-style ordinal weekday; the target is the
    /// <see cref="NotableDateRule.RelativeDayOfWeek" />. Expresses holidays such as "the Tuesday after the first Monday
    /// in November" (United States Election Day) without a custom algorithm.
    /// </summary>
    RelativeWeekdayInMonth,
}
