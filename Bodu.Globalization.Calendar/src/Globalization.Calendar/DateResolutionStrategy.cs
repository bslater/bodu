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
/// <para>
/// Each value corresponds to a distinct branch in the <see cref="NotableDateRuleResolver" /> and to a distinct strategy
/// child element in the XML schema (<c>Fixed</c>, <c>DayOfWeekInMonth</c>, <c>OffsetFromAnchor</c>,
/// <c>WeekdayNearDate</c>, <c>RelativeWeekdayInMonth</c>, <c>Algorithm</c>).
/// </para>
/// <para>
/// <b>Choosing a strategy.</b> Prefer the simplest strategy that matches how the date is <i>defined</i>, and avoid
/// <see cref="Algorithm" /> whenever a declarative strategy fits (an unresolved algorithm key yields no occurrence
/// silently). Work down this list and take the first match:
/// </para>
/// <list type="number">
/// <item>
/// <description>
/// Same month and day every year (Gregorian or another calendar system) — use <see cref="Fixed" />.
/// </description>
/// </item>
/// <item>
/// <description>
/// The <i>n</i>th or last occurrence of a weekday in a month, where that weekday <i>is</i> the result (third Monday,
/// last Monday, fourth Thursday) — use <see cref="DayOfWeekInMonth" />.
/// </description>
/// </item>
/// <item>
/// <description>
/// A fixed number of days from another date that is <i>itself modelled as a rule</i> (Easter Monday, the day after
/// Thanksgiving) — use <see cref="OffsetFromAnchor" /> so the date tracks its anchor rather than re-deriving it.
/// </description>
/// </item>
/// <item>
/// <description>
/// A weekday positioned on or after, on or before, or nearest to a <i>fixed calendar date</i> (the Saturday on or
/// after 20 June; the Wednesday before 23 November) — use <see cref="WeekdayNearDate" />.
/// </description>
/// </item>
/// <item>
/// <description>
/// A <i>different</i> weekday positioned on or after, on or before, or nearest to the <i>n</i>th weekday of a month,
/// where no anchor rule exists to offset from (the Tuesday after the first Monday in November) — use
/// <see cref="RelativeWeekdayInMonth" />.
/// </description>
/// </item>
/// <item>
/// <description>
/// None of the above — an astronomical, ecclesiastical, or lunisolar computation (Easter, Vesak, solstice-based
/// festivals) — use <see cref="Algorithm" /> with a registered <see cref="INotableDateAlgorithm" />.
/// </description>
/// </item>
/// </list>
/// <para>
/// <b>Disambiguating the weekday strategies.</b> The anchor type is the deciding factor:
/// <see cref="DayOfWeekInMonth" /> when the ordinal weekday is the answer; <see cref="WeekdayNearDate" /> when the
/// anchor is a fixed month and day; <see cref="RelativeWeekdayInMonth" /> when the anchor is an ordinal weekday but the
/// answer is a different weekday relative to it; and <see cref="OffsetFromAnchor" /> in preference to either of the
/// latter two whenever the anchor is already modelled as its own rule.
/// </para>
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
