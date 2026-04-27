// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateRule.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Extensions;
using System.Collections.Immutable;
using SysGlobal = System.Globalization;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Specifies how a notable date is identified, calculated, scoped, and adjusted within a calendar system.
/// </summary>
/// <remarks>
/// <para>
/// A <see cref="NotableDateRule" /> is the authored description of a notable date — a recipe that the
/// <see cref="NotableDateRuleResolver" /> turns into one or more concrete <see cref="NotableDate" /> instances per year. It captures
/// every authoring concern in one place: the resolution strategy and its arguments, temporal and recurrence bounds, regional and
/// calendar scoping, observance adjustments, multi-day spans, tags, and an optional priority used to resolve overlaps with other
/// rules.
/// </para>
/// <para>
/// This type replaces the earlier <c>NotableDateDefinition</c>. The new name reads more naturally alongside <see cref="NotableDate" />:
/// a <em>rule</em> produces <em>dates</em>.
/// </para>
/// </remarks>
public sealed record NotableDateRule
{
#if NET7_0_OR_GREATER

	/// <summary>
	/// Gets the unique name of the notable date this rule produces (e.g. <c>"Christmas Day"</c>, <c>"Anzac Day"</c>).
	/// </summary>
	public required string Name { get; init; }

	/// <summary>
	/// Gets the resolution strategy that determines how the anchor date is calculated for each year.
	/// </summary>
	public required DateResolutionStrategy Strategy { get; init; }

	/// <summary>
	/// Gets the primary category for the produced notable date. Use <see cref="Tags" /> for additional, non-exclusive classification.
	/// </summary>
	public required NotableDateCategory Category { get; init; }
#else

	/// <summary>
	/// Gets the unique name of the notable date this rule produces.
	/// </summary>
	public string Name { get; init; } = string.Empty;

	/// <summary>
	/// Gets the resolution strategy that determines how the anchor date is calculated for each year.
	/// </summary>
	public DateResolutionStrategy Strategy { get; init; }

	/// <summary>
	/// Gets the primary category for the produced notable date.
	/// </summary>
	public NotableDateCategory Category { get; init; }
#endif

	/// <summary>
	/// Gets the optional rule-level identifier authored on the <c>&lt;Rule&gt;</c> element's <c>name</c> attribute.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Distinct from <see cref="Name" /> — which is the canonical notable date title shared by every rule that produces it (for
	/// example, <c>"King's Birthday"</c>) — <see cref="RuleName" /> identifies the specific rule variant within that notable date.
	/// Authors use it to target a particular rule for inheritance or override when a notable date is described by more than one
	/// rule (era splits, subdivision-specific strategies, alternate calendars).
	/// </para>
	/// <para>
	/// A <c>&lt;Use&gt;</c> directive's nested override body addresses a source rule by <see cref="RuleName" /> when the source
	/// notable date contains multiple rules; if no <see cref="RuleName" /> is specified the override applies to every inherited
	/// rule sharing the same <see cref="Name" />.
	/// </para>
	/// </remarks>
	/// <returns>The rule-level identifier, or <see langword="null" /> when the rule was authored without one.</returns>
	public string? RuleName { get; init; }

	/// <summary>
	/// Gets the inclusive first year the rule is applicable, or <see langword="null" /> for no lower bound.
	/// </summary>
	public int? FirstYear { get; init; }

	/// <summary>
	/// Gets the inclusive last year the rule is applicable, or <see langword="null" /> for no upper bound.
	/// </summary>
	public int? LastYear { get; init; }

	/// <summary>
	/// Gets the recurrence interval in years. When set, the rule resolves only in years where <c>(year - FirstYear) % OccurrenceYears == 0</c>.
	/// </summary>
	/// <remarks>
	/// Useful for quadrennial events such as Olympic Games or fixed-term elections. <see cref="FirstYear" /> defines the cadence
	/// origin; if it is not set, the cadence is anchored at year zero.
	/// </remarks>
	public int? OccurrenceYears { get; init; }

	/// <summary>
	/// Gets the calendar system the rule is authored against (e.g. <see cref="SysGlobal.GregorianCalendar" />).
	/// </summary>
	public Type? CalendarType { get; init; }

	/// <summary>
	/// Gets the comma-separated list of territory codes that scope the rule, or <see langword="null" /> for global applicability.
	/// </summary>
	public string? TerritoryCode { get; init; }

	/// <summary>
	/// Gets a value indicating whether the produced notable date should be flagged as a non-working day.
	/// </summary>
	public bool? IsNonWorkingDay { get; init; }

	/// <summary>
	/// Gets the duration of the notable date in days. Defaults to one (a single-day event). Multi-day spans (Hanukkah, Ramadan,
	/// festival weeks) use values greater than one.
	/// </summary>
	public int DurationDays { get; init; } = 1;

	/// <summary>
	/// Gets the priority used to break ties when multiple rules resolve to the same date. Lower values win.
	/// </summary>
	public int Priority { get; init; } = 100;

	/// <summary>
	/// Gets a set of optional tags providing non-exclusive classification (e.g. <c>"Christian"</c>, <c>"Public"</c>, <c>"Federal"</c>).
	/// </summary>
	public ImmutableHashSet<string> Tags { get; init; } = ImmutableHashSet<string>.Empty;

	// --- Fixed strategy fields ---------------------------------------------------------------

	/// <summary>
	/// Gets the day of the month for <see cref="DateResolutionStrategy.Fixed" /> and <see cref="DateResolutionStrategy.DayOfWeekInMonth" />.
	/// </summary>
	public int? Day { get; init; }

	/// <summary>
	/// Gets the month of the year for <see cref="DateResolutionStrategy.Fixed" /> and <see cref="DateResolutionStrategy.DayOfWeekInMonth" />.
	/// </summary>
	public int? Month { get; init; }

	/// <summary>
	/// Gets a value indicating whether the intercalary (leap) month should be skipped when resolving this
	/// <see cref="DateResolutionStrategy.Fixed" /> rule against a lunisolar calendar.
	/// </summary>
	/// <remarks>
	/// <para>
	/// When <see langword="true" /> and <see cref="CalendarType" /> is a lunisolar calendar such as
	/// <see cref="SysGlobal.ChineseLunisolarCalendar" />, the resolver maps the conventional ordinal
	/// <see cref="Month" /> to the calendar's consecutive month numbering by advancing past any intercalary
	/// leap month that precedes it in the same year. This ensures that festivals defined against a conventional
	/// month number — for example, the Dragon Boat Festival on the fifth day of the fifth lunar month —
	/// resolve to the correct Gregorian date in leap lunar years.
	/// </para>
	/// </remarks>
	public bool SkipLeapMonth { get; init; }

	/// <summary>
	/// Gets a value indicating whether the resolver should sweep the two overlapping calendar years when
	/// resolving this <see cref="DateResolutionStrategy.Fixed" /> rule.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Required for calendars whose year boundaries do not align with the Gregorian year — for example, the
	/// Islamic (Hijri) calendar (a purely lunar calendar approximately 11 days shorter than a Gregorian year)
	/// and the Hebrew calendar (a lunisolar calendar whose year begins in September or October). The resolver
	/// determines the calendar year that contains 1 January of the requested Gregorian year, then checks that
	/// year and the next, returning the first candidate whose Gregorian year matches.
	/// </para>
	/// </remarks>
	public bool SweepCalendarYears { get; init; }

	/// <summary>
	/// Gets the calendar-specific month alias for months whose position depends on whether the calendar year
	/// is a leap year. Used with <see cref="SweepCalendarYears" /> when <see cref="CalendarType" /> is
	/// <see cref="SysGlobal.HebrewCalendar" />.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Accepted values: <c>LastAdar</c>, <c>Nisan</c>, <c>Iyar</c>, <c>Sivan</c>, <c>Tammuz</c>, <c>Av</c>,
	/// <c>Elul</c>. When this property is set, <see cref="Month" /> is <see langword="null" />. Simple Hebrew
	/// months with a fixed calendar position (Tishri through AdarII) are stored directly in <see cref="Month" />.
	/// </para>
	/// </remarks>
	public string? CalendarMonthAlias { get; init; }

	// --- DayOfWeekInMonth strategy fields ----------------------------------------------------

	/// <summary>
	/// Gets the weekday for <see cref="DateResolutionStrategy.DayOfWeekInMonth" /> (e.g. the <em>Monday</em> in "first Monday of October").
	/// </summary>
	public DayOfWeek? DayOfWeek { get; init; }

	/// <summary>
	/// Gets the ordinal position of the weekday within the month for <see cref="DateResolutionStrategy.DayOfWeekInMonth" />.
	/// </summary>
	public WeekOfMonthOrdinal? WeekOrdinal { get; init; }

	// --- OffsetFromAnchor strategy fields ----------------------------------------------------

	/// <summary>
	/// Gets the name of another <see cref="NotableDateRule" /> used as the anchor for <see cref="DateResolutionStrategy.OffsetFromAnchor" />.
	/// </summary>
	public string? AnchorRuleName { get; init; }

	/// <summary>
	/// Gets the integer day offset applied to the anchor for <see cref="DateResolutionStrategy.OffsetFromAnchor" />.
	/// </summary>
	public int? OffsetDays { get; init; }

	// --- Algorithm strategy fields ----------------------------------------------------------

	/// <summary>
	/// Gets the registry key used to look up an <see cref="INotableDateAlgorithm" /> for <see cref="DateResolutionStrategy.Algorithm" />.
	/// </summary>
	public string? AlgorithmKey { get; init; }

	/// <summary>
	/// Gets the optional CLR <see cref="Type" /> implementing <see cref="INotableDateAlgorithm" /> used as a fallback when
	/// <see cref="AlgorithmKey" /> is not registered. Provided for backward compatibility with rules authored against the previous
	/// <c>NotableDateAlgorithmType</c> field.
	/// </summary>
	public Type? AlgorithmType { get; init; }

	/// <summary>
	/// Gets the optional month token passed to the algorithm constructor when the rule's <see cref="AlgorithmType" /> exposes a
	/// two-argument <c>(month, day)</c> constructor. Used to author calendar-specific algorithms (Hebrew, Hijri, Hindu lunar)
	/// directly from XML by naming the algorithm class and supplying the month/day pair as constructor arguments.
	/// </summary>
	/// <remarks>
	/// The string is parsed against the first constructor parameter's type at resolution time: an enum parameter is parsed
	/// case-insensitively, an <see cref="int" /> parameter accepts a numeric token, and a <see cref="string" /> parameter
	/// receives the raw value. Unused when the rule resolves through an <see cref="AlgorithmKey" /> registered on an
	/// <see cref="INotableDateAlgorithmRegistry" />.
	/// </remarks>
	public string? AlgorithmMonth { get; init; }

	/// <summary>
	/// Gets the optional day-of-month value passed to the algorithm constructor alongside <see cref="AlgorithmMonth" /> when the
	/// rule's <see cref="AlgorithmType" /> exposes a two-argument <c>(month, day)</c> constructor.
	/// </summary>
	public int? AlgorithmDay { get; init; }

	// --- Adjustments + metadata --------------------------------------------------------------

	/// <summary>
	/// Gets the observance adjustments evaluated against the resolved date. Defaults to an empty array (never default-uninitialised).
	/// </summary>
	public ImmutableArray<ObservanceAdjustment> Adjustments { get; init; } = ImmutableArray<ObservanceAdjustment>.Empty;

	/// <summary>
	/// Gets an optional human-readable comment describing the rule's intent or provenance.
	/// </summary>
	public string? Comment { get; init; }
}
