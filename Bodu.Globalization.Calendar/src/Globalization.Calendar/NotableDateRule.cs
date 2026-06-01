// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateRule.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Immutable;
using Bodu.Extensions;
using SysGlobal = System.Globalization;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Specifies how a notable date is identified, calculated, scoped, and adjusted within a calendar system.
/// </summary>
/// <remarks>
/// <para>
/// A <see cref="NotableDateRule" /> is the authored description of a notable date — a recipe that the
/// <see cref="NotableDateRuleResolver" /> turns into one or more concrete <see cref="NotableDate" /> instances per
/// year. It captures every authoring concern in one place: the resolution strategy and its arguments, temporal and
/// recurrence bounds, regional and calendar scoping, observance adjustments, multi-day spans, tags, and an optional
/// priority used to resolve overlaps with other rules.
/// </para>
/// <para>
/// The <see cref="Strategy" /> property selects one of four resolution strategies:
/// </para>
/// <list type="bullet">
/// <item>
/// <description>
/// <see cref="DateResolutionStrategy.Fixed" /> — resolves to an explicit month and day each year.
/// </description>
/// </item>
/// <item>
/// <description>
/// <see cref="DateResolutionStrategy.DayOfWeekInMonth" /> — resolves to the <em>n</em>th occurrence of a weekday within
/// a month (for example, the second Monday of October).
/// </description>
/// </item>
/// <item>
/// <description>
/// <see cref="DateResolutionStrategy.Algorithm" /> — delegates to a registered <see cref="INotableDateAlgorithm" />
/// identified by <see cref="AlgorithmKey" /> (for example, Easter or Lunar New Year).
/// </description>
/// </item>
/// <item>
/// <description>
/// <see cref="DateResolutionStrategy.OffsetFromAnchor" /> — resolves by adding <see cref="OffsetDays" /> to the date
/// produced by another named rule.
/// </description>
/// </item>
/// </list>
/// <para>
/// This type replaces the earlier <c>NotableDateDefinition</c>. The new name reads more naturally alongside
/// <see cref="NotableDate" />: a <em>rule</em> produces <em>dates</em>.
/// </para>
/// </remarks>
/// <example>
/// <para>
/// A fixed public holiday for Australia with a weekend roll-forward adjustment:
/// </para>
/// <code>
///<![CDATA[
/// NotableDateRule australiaDay = new NotableDateRule
/// {
///     Name = "Australia Day",
///     Strategy = DateResolutionStrategy.Fixed,
///     Category = NotableDateCategory.Public,
///     Month = 1,
///     Day = 26,
///     TerritoryCode = "AU",
///     IsNonWorkingDay = true,
///     Tags = ImmutableArray.Create("Federal"),
///     Adjustments = ImmutableArray.Create(new ObservanceAdjustment
///     {
///         Key = "weekend-roll",
///         Trigger = AdjustmentTrigger.IfWeekend,
///         Action = AdjustmentAction.MoveToNextMonday,
///         IsNonWorkingDay = true,
///     }),
/// };
///
/// Algorithm-based rule (Easter Monday — offset from Easter Sunday):
/// NotableDateRule easterMonday = new NotableDateRule
/// {
///     Name = "Easter Monday",
///     Strategy = DateResolutionStrategy.OffsetFromAnchor,
///     Category = NotableDateCategory.Public,
///     AnchorRuleName = "Easter Sunday",
///     OffsetDays = 1,
///     IsNonWorkingDay = true,
/// };
///]]>
/// </code>
/// </example>
public sealed record NotableDateRule
{
#if NET7_0_OR_GREATER

    /// <summary>
    /// Gets the unique name of the notable date this rule produces (e.g. <c>"Christmas Day"</c>, <c>"Anzac Day"</c>).
    /// </summary>
    /// <returns>The canonical notable date title. Required at construction; never <see langword="null" />.</returns>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the resolution strategy that determines how the anchor date is calculated for each year.
    /// </summary>
    /// <returns>One of the defined <see cref="DateResolutionStrategy" /> values. Required at construction.</returns>
    public required DateResolutionStrategy Strategy { get; init; }

    /// <summary>
    /// Gets the primary category for the produced notable date. Use <see cref="Tags" /> for additional, non-exclusive
    /// classification.
    /// </summary>
    /// <returns>One of the defined <see cref="NotableDateCategory" /> values. Required at construction.</returns>
    public required NotableDateCategory Category { get; init; }
#else

	/// <summary>
	/// Gets the unique name of the notable date this rule produces.
	/// </summary>
	/// <returns>The canonical notable date title. Defaults to <see cref="string.Empty" /> when not assigned.</returns>
	public string Name { get; init; } = string.Empty;

	/// <summary>
	/// Gets the resolution strategy that determines how the anchor date is calculated for each year.
	/// </summary>
	/// <returns>One of the defined <see cref="DateResolutionStrategy" /> values.</returns>
	public DateResolutionStrategy Strategy { get; init; }

	/// <summary>
	/// Gets the primary category for the produced notable date.
	/// </summary>
	/// <returns>One of the defined <see cref="NotableDateCategory" /> values.</returns>
	public NotableDateCategory Category { get; init; }
#endif

    /// <summary>
    /// Gets the optional rule-level identifier authored on the <c>&lt;Rule&gt;</c> element's <c>name</c> attribute.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Distinct from <see cref="Name" /> — which is the canonical notable date title shared by every rule that produces
    /// it (for example, <c>"King's Birthday"</c>) — <see cref="RuleName" /> identifies the specific rule variant within
    /// that notable date. Authors use it to target a particular rule for inheritance or override when a notable date is
    /// described by more than one rule (era splits, subdivision-specific strategies, alternate calendars).
    /// </para>
    /// <para>
    /// A <c>&lt;Use&gt;</c> directive's nested override body addresses a source rule by <see cref="RuleName" /> when
    /// the source notable date contains multiple rules; if no <see cref="RuleName" /> is specified the override applies
    /// to every inherited rule sharing the same <see cref="Name" />.
    /// </para>
    /// </remarks>
    /// <returns>The rule-level identifier, or <see langword="null" /> when the rule was authored without one.</returns>
    public string? RuleName { get; init; }

    /// <summary>
    /// Gets the inclusive first year the rule is applicable, or <see langword="null" /> for no lower bound.
    /// </summary>
    /// <returns>
    /// A four-digit civil year, or <see langword="null" /> when the rule has no earliest applicability bound.
    /// </returns>
    public int? FirstYear { get; init; }

    /// <summary>
    /// Gets the inclusive last year the rule is applicable, or <see langword="null" /> for no upper bound.
    /// </summary>
    /// <returns>
    /// A four-digit civil year, or <see langword="null" /> when the rule has no final applicability bound.
    /// </returns>
    public int? LastYear { get; init; }

    /// <summary>
    /// Gets the recurrence interval in years. When set, the rule resolves only in years where
    /// <c>(year - FirstYear) % OccurrenceYears == 0</c>.
    /// </summary>
    /// <remarks>
    /// Useful for quadrennial events such as Olympic Games or fixed-term elections. <see cref="FirstYear" /> defines
    /// the cadence origin; if it is not set, the cadence is anchored at year zero.
    /// </remarks>
    /// <returns>
    /// A positive cadence in years, or <see langword="null" /> when the rule resolves every year within its bounds.
    /// </returns>
    public int? OccurrenceYears { get; init; }

    /// <summary>
    /// Gets the <see cref="System.Globalization.Calendar" />-derived CLR type the rule's <see cref="Month" /> and
    /// <see cref="Day" /> are authored against. The resolver activates this type via its parameterless constructor and
    /// uses it to project the authored (month, day) tuple to a Gregorian <see cref="DateTime" />.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When <see langword="null" />, the rule is interpreted in the Gregorian calendar and the resolver constructs the
    /// date directly via <c>new DateTime(year, Month, Day)</c> without any calendar conversion.
    /// </para>
    /// <para>
    /// When set to a BCL calendar type — for example <see cref="SysGlobal.HijriCalendar" />,
    /// <see cref="SysGlobal.UmAlQuraCalendar" />, <see cref="SysGlobal.HebrewCalendar" />,
    /// <see cref="SysGlobal.PersianCalendar" />, or <see cref="SysGlobal.ChineseLunisolarCalendar" /> — the resolver
    /// instantiates the calendar once per resolution and calls
    /// <see cref="System.Globalization.Calendar.ToDateTime(int, int, int, int, int, int, int)" /> on the projected
    /// (calendar-year, month, day) tuple. The chosen calendar year depends on <see cref="SweepCalendarYears" /> and on
    /// whether <see cref="SkipLeapMonth" /> is set for lunisolar calendars.
    /// </para>
    /// <para>
    /// The type must declare a public parameterless constructor; configurable calendar variants (e.g.
    /// <see cref="SysGlobal.HijriCalendar.HijriAdjustment" />) use their default settings. For Saudi-aligned dates that
    /// diverge from the tabular Hijri default, use <see cref="SysGlobal.UmAlQuraCalendar" /> rather than adjusting
    /// <see cref="SysGlobal.HijriCalendar" />.
    /// </para>
    /// <para>
    /// See the <c>Working with non-Gregorian calendars</c> DocFX article for a per-calendar reference of which flags
    /// must accompany <see cref="CalendarType" /> on a Fixed rule.
    /// </para>
    /// </remarks>
    /// <returns>
    /// The CLR <see cref="Type" /> of the authoring calendar, or <see langword="null" /> to indicate the Gregorian
    /// calendar.
    /// </returns>
    public Type? CalendarType { get; init; }

    /// <summary>
    /// Gets the comma-separated list of territory codes that scope the rule, or <see langword="null" /> for global
    /// applicability.
    /// </summary>
    /// <returns>
    /// A comma-separated ISO 3166-style territory list, or <see langword="null" /> when the rule applies to every
    /// territory.
    /// </returns>
    public string? TerritoryCode { get; init; }

    /// <summary>
    /// Gets a value indicating whether the produced notable date should be flagged as a non-working day.
    /// </summary>
    /// <returns>
    /// <see langword="true" /> when the resolved <see cref="NotableDate" /> is non-working; <see langword="false" />
    /// when working; <see langword="null" /> to defer to the calendar default.
    /// </returns>
    public bool? IsNonWorkingDay { get; init; }

    /// <summary>
    /// Gets the duration of the notable date in days. Defaults to one (a single-day event). Multi-day spans (Hanukkah,
    /// Ramadan, festival weeks) use values greater than one.
    /// </summary>
    /// <returns>A positive day count. The default is <c>1</c>.</returns>
    public int DurationDays { get; init; } = 1;

    /// <summary>
    /// Gets the priority used to break ties when multiple rules resolve to the same date. Lower values win.
    /// </summary>
    /// <returns>An integer priority. The default is <c>100</c>.</returns>
    public int Priority { get; init; } = 100;

    /// <summary>
    /// Gets a set of optional tags providing non-exclusive classification (e.g. <c>"Christian"</c>, <c>"Public"</c>,
    /// <c>"Federal"</c>).
    /// </summary>
    /// <returns>
    /// An immutable hash set of tag values. Empty when no tags are authored; never <see langword="null" />.
    /// </returns>
    public ImmutableHashSet<string> Tags { get; init; } = [];

    // --- Fixed strategy fields ---------------------------------------------------------------

    /// <summary>
    /// Gets the day of the month for <see cref="DateResolutionStrategy.Fixed" /> and
    /// <see cref="DateResolutionStrategy.DayOfWeekInMonth" />.
    /// </summary>
    /// <returns>
    /// A day-of-month in the range <c>1</c>..<c>31</c>, or <see langword="null" /> when the strategy does not require
    /// it.
    /// </returns>
    public int? Day { get; init; }

    /// <summary>
    /// Gets the month of the year for <see cref="DateResolutionStrategy.Fixed" /> and
    /// <see cref="DateResolutionStrategy.DayOfWeekInMonth" />.
    /// </summary>
    /// <returns>
    /// A month number in the range <c>1</c>..<c>12</c>, or <see langword="null" /> when the strategy does not require
    /// it.
    /// </returns>
    public int? Month { get; init; }

    /// <summary>
    /// Gets a value indicating whether the intercalary (leap) month should be skipped when resolving this
    /// <see cref="DateResolutionStrategy.Fixed" /> rule against a lunisolar calendar.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When <see langword="true" /> and <see cref="CalendarType" /> is a lunisolar calendar such as
    /// <see cref="SysGlobal.ChineseLunisolarCalendar" />, the resolver maps the conventional ordinal
    /// <see cref="Month" /> to the calendar's consecutive month numbering by advancing past any intercalary leap month
    /// that precedes it in the same year. This ensures that festivals defined against a conventional month number — for
    /// example, the Dragon Boat Festival on the fifth day of the fifth lunar month — resolve to the correct Gregorian
    /// date in leap lunar years.
    /// </para>
    /// </remarks>
    /// <returns>
    /// <see langword="true" /> to advance past intercalary months when ordinal mapping; otherwise
    /// <see langword="false" />.
    /// </returns>
    public bool SkipLeapMonth { get; init; }

    /// <summary>
    /// Gets a value indicating whether the resolver should sweep the two candidate calendar years that overlap the
    /// requested Gregorian year when resolving this <see cref="DateResolutionStrategy.Fixed" /> rule against a
    /// non-Gregorian <see cref="CalendarType" />.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Required for every calendar whose year numbering does not match the Gregorian year — that is, for every
    /// supported <see cref="CalendarType" /> other than <see cref="SysGlobal.GregorianCalendar" />. The resolver
    /// determines the calendar year that contains 1 January of the requested Gregorian year, then evaluates the
    /// authored (<see cref="Month" /> or <see cref="CalendarMonthAlias" />, <see cref="Day" />) tuple against that
    /// calendar year and the next, returning the first candidate whose projected Gregorian year matches the request.
    /// When both candidate calendar years contain a valid occurrence in the requested Gregorian year — possible only
    /// for fast-drifting lunar calendars such as Hijri / Umm al-Qura, where the same month can fall in the same
    /// Gregorian year twice approximately every 33 years — the chronologically earlier occurrence is returned.
    /// </para>
    /// <para>
    /// <b>When sweep is required, by calendar:</b>
    /// </para>
    /// <list type="bullet">
    /// <item>
    /// <description>
    /// <see cref="SysGlobal.HijriCalendar" /> and <see cref="SysGlobal.UmAlQuraCalendar" /> — required. The lunar year
    /// is ~11 days shorter than the Gregorian year, so the same Hijri month can fall in different Gregorian years for
    /// adjacent Hijri years.
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// <see cref="SysGlobal.HebrewCalendar" /> — required. The lunisolar year starts in September or October (Rosh
    /// Hashanah), so Hebrew months from Tishri through Elul straddle two Gregorian years.
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// <see cref="SysGlobal.PersianCalendar" /> — required. Although the Persian year starts on ~20 March (close to a
    /// Gregorian quarter boundary), Persian year numbers are offset by ~621 from Gregorian year numbers. Without the
    /// sweep, the resolver would interpret the requested Gregorian year as the Persian year and project into the wrong
    /// era.
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// <see cref="SysGlobal.ChineseLunisolarCalendar" /> — uses the sibling <see cref="SkipLeapMonth" /> path rather
    /// than the sweep; the calendar's year numbering already aligns with the Gregorian year for the new-year date, but
    /// ordinal lunar months need leap-month skipping.
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// <see cref="SysGlobal.GregorianCalendar" /> (or <see cref="CalendarType" /> = <see langword="null" />) — ignored.
    /// Gregorian rules construct the date directly from <see cref="Month" /> and <see cref="Day" /> without sweep
    /// involvement.
    /// </description>
    /// </item>
    /// </list>
    /// <para>
    /// Setting this flag on a Fixed rule without a <see cref="CalendarType" /> is a no-op: the resolver only routes
    /// through the sweep when <see cref="CalendarType" /> resolves to a non-null calendar instance.
    /// </para>
    /// </remarks>
    /// <returns>
    /// <see langword="true" /> to evaluate both candidate calendar years; otherwise <see langword="false" />.
    /// </returns>
    public bool SweepCalendarYears { get; init; }

    /// <summary>
    /// Gets the calendar-specific month alias for months whose position depends on whether the calendar year is a leap
    /// year. Used with <see cref="SweepCalendarYears" /> when <see cref="CalendarType" /> is
    /// <see cref="SysGlobal.HebrewCalendar" />.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Accepted values: <c>LastAdar</c>, <c>Nisan</c>, <c>Iyar</c>, <c>Sivan</c>, <c>Tammuz</c>, <c>Av</c>, <c>Elul</c>
    /// . When this property is set, <see cref="Month" /> is <see langword="null" />. Simple Hebrew months with a fixed
    /// calendar position (Tishri through AdarII) are stored directly in <see cref="Month" />.
    /// </para>
    /// </remarks>
    /// <returns>
    /// The textual month alias, or <see langword="null" /> when the rule does not require alias resolution.
    /// </returns>
    public string? CalendarMonthAlias { get; init; }

    // --- DayOfWeekInMonth strategy fields ----------------------------------------------------

    /// <summary>
    /// Gets the weekday for <see cref="DateResolutionStrategy.DayOfWeekInMonth" /> (e.g. the <em>Monday</em> in "first
    /// Monday of October").
    /// </summary>
    /// <returns>
    /// A <see cref="System.DayOfWeek" /> value, or <see langword="null" /> when the strategy does not require it.
    /// </returns>
    public DayOfWeek? DayOfWeek { get; init; }

    /// <summary>
    /// Gets the ordinal position of the weekday within the month for
    /// <see cref="DateResolutionStrategy.DayOfWeekInMonth" />.
    /// </summary>
    /// <returns>
    /// One of the defined <see cref="WeekOfMonthOrdinal" /> values, or <see langword="null" /> when the strategy does
    /// not require it.
    /// </returns>
    public WeekOfMonthOrdinal? WeekOrdinal { get; init; }

    // --- OffsetFromAnchor strategy fields ----------------------------------------------------

    /// <summary>
    /// Gets the name of another <see cref="NotableDateRule" /> used as the anchor for
    /// <see cref="DateResolutionStrategy.OffsetFromAnchor" />.
    /// </summary>
    /// <returns>
    /// The anchor rule's <see cref="Name" />, or <see langword="null" /> when the strategy does not require an anchor.
    /// </returns>
    public string? AnchorRuleName { get; init; }

    /// <summary>
    /// Gets the integer day offset applied to the anchor for <see cref="DateResolutionStrategy.OffsetFromAnchor" />.
    /// </summary>
    /// <returns>
    /// A signed day offset (positive moves forward, negative moves backward), or <see langword="null" /> when the
    /// strategy does not require it.
    /// </returns>
    public int? OffsetDays { get; init; }

    // --- Algorithm strategy fields ----------------------------------------------------------

    /// <summary>
    /// Gets the registry key used to look up an <see cref="INotableDateAlgorithm" /> for
    /// <see cref="DateResolutionStrategy.Algorithm" />.
    /// </summary>
    /// <returns>
    /// The algorithm registry key, or <see langword="null" /> when the rule resolves via <see cref="AlgorithmType" />
    /// instead.
    /// </returns>
    public string? AlgorithmKey { get; init; }

    /// <summary>
    /// Gets the optional CLR <see cref="Type" /> implementing <see cref="INotableDateAlgorithm" /> used as a fallback
    /// when <see cref="AlgorithmKey" /> is not registered. Provided for backward compatibility with rules authored
    /// against the previous <c>NotableDateAlgorithmType</c> field.
    /// </summary>
    /// <returns>
    /// The CLR <see cref="Type" /> of the algorithm, or <see langword="null" /> when the rule resolves via
    /// <see cref="AlgorithmKey" />.
    /// </returns>
    public Type? AlgorithmType { get; init; }

    /// <summary>
    /// Gets the optional month token passed to the algorithm constructor when the rule's <see cref="AlgorithmType" />
    /// exposes a two-argument <c>(month, day)</c> constructor. Used to author calendar-specific algorithms (Hebrew,
    /// Hijri, Hindu lunar) directly from XML by naming the algorithm class and supplying the month/day pair as
    /// constructor arguments.
    /// </summary>
    /// <remarks>
    /// The string is parsed against the first constructor parameter's type at resolution time: an enum parameter is
    /// parsed case-insensitively, an <see cref="int" /> parameter accepts a numeric token, and a <see cref="string" />
    /// parameter receives the raw value. Unused when the rule resolves through an <see cref="AlgorithmKey" />
    /// registered on an <see cref="INotableDateAlgorithmRegistry" />.
    /// </remarks>
    /// <returns>
    /// The raw month token to forward to the algorithm constructor, or <see langword="null" /> when no month argument
    /// is supplied.
    /// </returns>
    public string? AlgorithmMonth { get; init; }

    /// <summary>
    /// Gets the optional day-of-month value passed to the algorithm constructor alongside <see cref="AlgorithmMonth" />
    /// when the rule's <see cref="AlgorithmType" /> exposes a two-argument <c>(month, day)</c> constructor.
    /// </summary>
    /// <returns>
    /// The day-of-month value to forward to the algorithm constructor, or <see langword="null" /> when no day argument
    /// is supplied.
    /// </returns>
    public int? AlgorithmDay { get; init; }

    // --- Adjustments + metadata --------------------------------------------------------------

    /// <summary>
    /// Gets the observance adjustments evaluated against the resolved date. Defaults to an empty array (never
    /// default-uninitialized).
    /// </summary>
    /// <returns>
    /// An immutable array of <see cref="ObservanceAdjustment" /> entries in evaluation order. Empty when no adjustments
    /// are authored.
    /// </returns>
    public ImmutableArray<ObservanceAdjustment> Adjustments { get; init; } = [];

    /// <summary>
    /// Gets an optional human-readable comment describing the rule's intent or provenance.
    /// </summary>
    /// <returns>The free-form comment text, or <see langword="null" /> when no comment was authored.</returns>
    public string? Comment { get; init; }
}
