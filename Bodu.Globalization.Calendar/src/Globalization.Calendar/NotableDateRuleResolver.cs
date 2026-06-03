// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateRuleResolver.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Reflection;
using Bodu.Extensions;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Resolves the anchor date of a <see cref="NotableDateRule" /> for a specified year, dispatching to the appropriate
/// strategy and honoring temporal bounds, recurrence cadences, anchor look-ups, and algorithm registry resolution.
/// </summary>
/// <remarks>
/// <para>
/// This class replaces the earlier <c>NotableDateResolver</c>. It preserves the same surface area (
/// <see cref="ResolveAnchorDate" />) while finishing several previously incomplete behaviors:
/// <see cref="NotableDateRule.OccurrenceYears" /> is now honored; circular <c>OffsetFromAnchor</c> chains are detected
/// at every depth; algorithms are looked up via <see cref="INotableDateAlgorithmRegistry" /> with a CLR
/// <see cref="Type" /> fallback for backward compatibility.
/// </para>
/// </remarks>
internal sealed class NotableDateRuleResolver
{
    /// <summary>
    /// An optional algorithm registry consulted for <see cref="DateResolutionStrategy.Algorithm" /> rules.
    /// </summary>
    private readonly INotableDateAlgorithmRegistry? _algorithms;

    /// <summary>
    /// An identity-keyed index of every rule available for resolution, built once at construction. Resolves offset
    /// anchors by canonical name with deterministic disambiguation when several variants share a name.
    /// </summary>
    private readonly NotableDateRuleIndex _index;

    /// <summary>
    /// Initializes a new instance of the <see cref="NotableDateRuleResolver" /> class.
    /// </summary>
    /// <param name="rules">The rules available for resolution. Must not be <see langword="null" />.</param>
    /// <param name="algorithms">
    /// Optional algorithm registry consulted for <see cref="DateResolutionStrategy.Algorithm" /> rules.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="rules" /> is <see langword="null" />.
    /// </exception>
    public NotableDateRuleResolver(IReadOnlyList<NotableDateRule> rules, INotableDateAlgorithmRegistry? algorithms = null)
    {
        ThrowHelper.ThrowIfNull(rules);

        // Build an identity-keyed index once. Same-name variants (e.g. western and orthodox Easter) coexist and are
        // disambiguated by territory/calendar context when an offset anchor refers to them by name.
        _index = new NotableDateRuleIndex(rules);
        _algorithms = algorithms;
    }

    /// <summary>
    /// Determines whether the supplied rule applies to the supplied year, after consulting
    /// <see cref="NotableDateRule.FirstYear" />, <see cref="NotableDateRule.LastYear" />, and
    /// <see cref="NotableDateRule.OccurrenceYears" />.
    /// </summary>
    /// <param name="rule">The rule under test.</param>
    /// <param name="year">The target year.</param>
    /// <returns><see langword="true" /> if the rule applies; otherwise <see langword="false" />.</returns>
    public static bool IsApplicable(NotableDateRule rule, int year)
    {
        if (rule.FirstYear is { } first && year < first) return false;
        if (rule.LastYear is { } last && year > last) return false;

        if (rule.OccurrenceYears is { } interval && interval > 0)
        {
            var origin = rule.FirstYear ?? 0;
            if (((year - origin) % interval) != 0)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Resolves the anchor date for the supplied rule and year.
    /// </summary>
    /// <param name="rule">The rule to resolve. Must not be <see langword="null" />.</param>
    /// <param name="year">The target year.</param>
    /// <returns>
    /// The resolved anchor date, or <see langword="null" /> if the rule does not apply to the supplied year.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="rule" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when a circular <c>OffsetFromAnchor</c> chain is detected or a referenced anchor cannot be found.
    /// </exception>
    public DateTime? ResolveAnchorDate(NotableDateRule rule, int year)
    {
        ThrowHelper.ThrowIfNull(rule);

        return ResolveInternal(rule, year, []);
    }

    /// <summary>
    /// Resolves a <see cref="DateResolutionStrategy.Fixed" /> rule authored against a non-Gregorian calendar whose year
    /// boundaries do not align with the Gregorian year, by checking the two calendar years that overlap the requested
    /// Gregorian year and returning the candidate whose projection falls within that Gregorian year.
    /// </summary>
    /// <param name="rule">
    /// The rule to resolve. The rule's <see cref="NotableDateRule.Month" /> (or
    /// <see cref="NotableDateRule.CalendarMonthAlias" /> for Hebrew rules) supplies the target calendar month;
    /// <see cref="NotableDateRule.Day" /> supplies the day.
    /// </param>
    /// <param name="year">
    /// The requested Gregorian year. All candidates whose Gregorian year does not equal this value are discarded.
    /// </param>
    /// <param name="cal">
    /// The target calendar instance (typically <see cref="System.Globalization.HijriCalendar" />,
    /// <see cref="System.Globalization.UmAlQuraCalendar" />, <see cref="System.Globalization.HebrewCalendar" />, or
    /// <see cref="System.Globalization.PersianCalendar" />). Constructed once per resolution call by
    /// <see cref="ResolveInternal" /> via <see cref="Activator.CreateInstance(Type)" /> on the rule's
    /// <see cref="NotableDateRule.CalendarType" />.
    /// </param>
    /// <param name="day">The day-of-month value, interpreted in the target calendar's month numbering.</param>
    /// <returns>
    /// The matching Gregorian date when the (month, day) tuple in either candidate calendar year projects into
    /// <paramref name="year" />; otherwise <see langword="null" />. The returned date has
    /// <see cref="DateTimeKind.Unspecified" />.
    /// </returns>
    /// <remarks>
    /// <para>
    /// <b>Algorithm.</b> The sweep proceeds in four steps:
    /// </para>
    /// <list type="number">
    /// <item>
    /// <description>
    /// Determine the lower-bound calendar year — the calendar year in which 1 January of the requested Gregorian year
    /// falls. For example, 1 January 2024 falls in Hijri year 1445 AH, Hebrew year 5784, and Persian year 1402.
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// Iterate over the two candidate calendar years (lower-bound and lower-bound + 1). One pass covers the calendar
    /// year that started in the previous Gregorian year and is still active; the other covers the calendar year that
    /// starts during the requested Gregorian year.
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// For each candidate calendar year, resolve the target month number. When
    /// <see cref="NotableDateRule.CalendarMonthAlias" /> is set (Hebrew rules), the alias is mapped through
    /// <see cref="ResolveHebrewMonthAlias" />, which accounts for the leap-year-dependent renumbering of Adar / Nisan /
    /// Iyar / Sivan / Tammuz / Av / Elul. The alias returns <c>-1</c> when the named month does not exist in the
    /// candidate year (for example Adar II in a non-leap Hebrew year), in which case the candidate year is skipped.
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// Convert (calendar-year, month, day) to a Gregorian <see cref="DateTime" /> via
    /// <see cref="System.Globalization.Calendar.ToDateTime(int, int, int, int, int, int, int)" /> and accept it only
    /// when the resulting Gregorian year equals <paramref name="year" />. The first accepted candidate is returned,
    /// ensuring that when both calendar years contain a valid occurrence in the same Gregorian year, the
    /// chronologically earlier one wins.
    /// </description>
    /// </item>
    /// </list>
    /// <para>
    /// <b>Guards.</b> The sweep tolerates several conditions without throwing:
    /// </para>
    /// <list type="bullet">
    /// <item>
    /// <description>
    /// Gregorian year outside the calendar's supported range — <see cref="System.Globalization.Calendar.GetYear" />
    /// throws <see cref="ArgumentOutOfRangeException" /> and the sweep returns <see langword="null" />.
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// Day exceeds the month length in a candidate year (e.g. day 30 of a 29-day Hijri month) — the candidate year is
    /// skipped, the other candidate year is still evaluated.
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// Hebrew month alias resolves to <c>-1</c> in a candidate year — the candidate year is skipped.
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// Calendar-to-Gregorian conversion throws <see cref="ArgumentOutOfRangeException" /> — the candidate year is
    /// skipped.
    /// </description>
    /// </item>
    /// </list>
    /// <para>
    /// <b>Why both years are needed.</b> Non-Gregorian calendar years drift relative to the Gregorian year:
    /// </para>
    /// <list type="bullet">
    /// <item>
    /// <description>
    /// The Islamic (Hijri) calendar is purely lunar and roughly 11 days shorter than the Gregorian year, so any given
    /// Hijri month migrates earlier in the Gregorian year by ~11 days each year — meaning two Hijri new-year days can
    /// fall in the same Gregorian year approximately every 33 years.
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// The Hebrew calendar is lunisolar with intercalary months; its year starts in September or October (Rosh
    /// Hashanah). Hebrew dates between Rosh Hashanah and 31 December belong to the calendar year that started in the
    /// same Gregorian year; Hebrew dates between 1 January and the next Rosh Hashanah belong to the calendar year that
    /// started in the previous Gregorian year.
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// The Umm al-Qura calendar shares Hijri's lunar drift and ships the Saudi Arabian government's
    /// observation-corrected month starts; its sweep behavior is identical to Hijri.
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// The Persian (Solar Hijri) calendar's year starts at the vernal equinox (~20 March). Persian-year numbers are
    /// offset by ~621 from Gregorian-year numbers, so the sweep is required even though the year-to-year drift in the
    /// Gregorian calendar is small.
    /// </description>
    /// </item>
    /// </list>
    /// <para>
    /// See the <c>Bodu.Globalization.Calendar — Reference / Working with non-Gregorian calendars</c> DocFX article for
    /// worked examples and authoring guidance.
    /// </para>
    /// </remarks>
    private static DateTime? ResolveCalendarYearSweep(NotableDateRule rule, int year, System.Globalization.Calendar cal, int day)
    {
        int calYearForJan1;
        try
        {
            calYearForJan1 = cal.GetYear(new DateTime(year, 1, 1));
        }
        catch (ArgumentOutOfRangeException)
        {
            return null;
        }

        for (var h = calYearForJan1; h <= calYearForJan1 + 1; h++)
        {
            int monthNumber;
            if (rule.CalendarMonthAlias is { } alias)
            {
                var isLeapYear = cal.GetMonthsInYear(h) == 13;
                monthNumber = ResolveHebrewMonthAlias(alias, isLeapYear);
                if (monthNumber < 0)
                    continue;
            }
            else if (rule.Month is { } m)
            {
                monthNumber = m;
            }
            else
            {
                return null;
            }

            int daysInMonth;
            try
            {
                daysInMonth = cal.GetDaysInMonth(h, monthNumber);
            }
            catch (ArgumentOutOfRangeException)
            {
                continue;
            }

            if (day > daysInMonth)
                continue;

            DateTime candidate;
            try
            {
                candidate = cal.ToDateTime(h, monthNumber, day, 0, 0, 0, 0);
            }
            catch (ArgumentOutOfRangeException)
            {
                continue;
            }

            if (candidate.Year != year)
                continue;

            return DateTime.SpecifyKind(candidate.Date, DateTimeKind.Unspecified);
        }

        return null;
    }

    /// <summary>
    /// Resolves a <see cref="DateResolutionStrategy.Fixed" /> rule against a lunisolar calendar by advancing the
    /// conventional ordinal lunar month past any intercalary leap month inserted earlier in the same year.
    /// </summary>
    /// <param name="rule">The rule to resolve; must have <see cref="NotableDateRule.Month" /> set.</param>
    /// <param name="year">The Gregorian year.</param>
    /// <param name="cal">The <see cref="System.Globalization.ChineseLunisolarCalendar" /> instance.</param>
    /// <param name="day">The day of month in the lunisolar calendar.</param>
    /// <returns>
    /// The Gregorian date, or <see langword="null" /> if the year is out of range or the month/day do not exist.
    /// </returns>
    private static DateTime? ResolveChineseLeapMonthSkip(NotableDateRule rule, int year, System.Globalization.ChineseLunisolarCalendar cal, int day)
    {
        if (rule.Month is not { } lunarMonth)
            return null;

        if (year < cal.MinSupportedDateTime.Year || year >= cal.MaxSupportedDateTime.Year)
            return null;

        var monthsInYear = cal.GetMonthsInYear(year);
        var leapMonth = cal.GetLeapMonth(year);

        // GetLeapMonth returns the 1-based position of the intercalary month within the calendar's consecutive
        // 1..N month sequence, or 0 for a non-leap year. Conventional ordinal months at or after the leap slot
        // need their calendar index incremented by one to skip past the intercalary month.
        var calendarMonth = (leapMonth > 0 && lunarMonth >= leapMonth) ? lunarMonth + 1 : lunarMonth;
        if (calendarMonth > monthsInYear)
            return null;

        var daysInMonth = cal.GetDaysInMonth(year, calendarMonth);
        if (day > daysInMonth)
            return null;

        var result = cal.ToDateTime(year, calendarMonth, day, 0, 0, 0, 0);
        return DateTime.SpecifyKind(result.Date, DateTimeKind.Unspecified);
    }

    /// <summary>
    /// Maps a Hebrew month alias to the internal <see cref="System.Globalization.HebrewCalendar" /> month number for
    /// the given leap-year state, or returns <c>-1</c> if the month does not exist in that year type.
    /// </summary>
    /// <param name="alias">The Hebrew month name.</param>
    /// <param name="isLeapYear"><see langword="true" /> when the Hebrew year has 13 months.</param>
    /// <returns>The 1-based month number, or <c>-1</c> when the month is absent in the given year type.</returns>
    private static int ResolveHebrewMonthAlias(string alias, bool isLeapYear) =>
        alias switch
        {
            "Tishri" => 1,
            "Heshvan" => 2,
            "Kislev" => 3,
            "Tevet" => 4,
            "Shevat" => 5,
            "AdarI" => 6,
            "AdarII" => isLeapYear ? 7 : -1,
            "LastAdar" => isLeapYear ? 7 : 6,
            "Nisan" => isLeapYear ? 8 : 7,
            "Iyar" => isLeapYear ? 9 : 8,
            "Sivan" => isLeapYear ? 10 : 9,
            "Tammuz" => isLeapYear ? 11 : 10,
            "Av" => isLeapYear ? 12 : 11,
            "Elul" => isLeapYear ? 13 : 12,
            _ => -1,
        };

    /// <summary>
    /// Parses <paramref name="token" /> into a value compatible with <paramref name="targetType" /> for use as the
    /// first argument of an algorithm constructor. Supports enum, <see cref="int" />, and <see cref="string" />
    /// parameter types.
    /// </summary>
    /// <param name="token">The authored month token (typically the calendar's month name).</param>
    /// <param name="targetType">The target parameter type declared by the candidate constructor.</param>
    /// <param name="value">
    /// The coerced value when the method returns <see langword="true" />; otherwise <see langword="null" />.
    /// </param>
    /// <returns>
    /// <see langword="true" /> when <paramref name="token" /> was successfully coerced; otherwise
    /// <see langword="false" />.
    /// </returns>
    private static bool TryCoerceMonthArgument(string token, Type targetType, out object? value)
    {
        if (targetType.IsEnum)
        {
            if (Enum.TryParse(targetType, token, ignoreCase: true, out var parsed) && parsed is not null && Enum.IsDefined(targetType, parsed))
            {
                value = parsed;
                return true;
            }

            value = null;
            return false;
        }

        if (targetType == typeof(int))
        {
            if (int.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
            {
                value = parsed;
                return true;
            }

            value = null;
            return false;
        }

        if (targetType == typeof(string))
        {
            value = token;
            return true;
        }

        value = null;
        return false;
    }

    /// <summary>
    /// Instantiates the rule's <see cref="NotableDateRule.AlgorithmType" /> by selecting the constructor that matches
    /// the authored arguments: a two-parameter <c>(month, day)</c> constructor when
    /// <see cref="NotableDateRule.AlgorithmMonth" /> and <see cref="NotableDateRule.AlgorithmDay" /> are both supplied,
    /// otherwise the public parameterless constructor.
    /// </summary>
    /// <param name="rule">The rule whose algorithm is being constructed.</param>
    /// <returns>
    /// The constructed algorithm, or <see langword="null" /> when no compatible constructor exists or activation fails.
    /// </returns>
    private static INotableDateAlgorithm? TryCreateAlgorithm(NotableDateRule rule)
    {
        Type type = rule.AlgorithmType!;

        if (rule.AlgorithmMonth is { } monthToken && rule.AlgorithmDay is { } day)
        {
            foreach (ConstructorInfo ctor in type.GetConstructors())
            {
                ParameterInfo[] parameters = ctor.GetParameters();
                if (parameters.Length != 2) continue;
                if (parameters[1].ParameterType != typeof(int)) continue;

                if (!TryCoerceMonthArgument(monthToken, parameters[0].ParameterType, out var monthValue))
                    continue;

                try
                {
                    return ctor.Invoke([monthValue, (object)day]) as INotableDateAlgorithm;
                }
                catch (TargetInvocationException)
                {
                    return null;
                }
                catch (ArgumentException)
                {
                    return null;
                }
            }

            // No (month, int) constructor matched; fall through to the parameterless attempt below so a misauthored
            // rule still has a chance to surface via the legacy path rather than silently producing nothing.
        }

        try
        {
            return Activator.CreateInstance(type) as INotableDateAlgorithm;
        }
        catch (MissingMethodException)
        {
            return null;
        }
        catch (TargetInvocationException)
        {
            return null;
        }
    }

    /// <summary>
    /// Resolves a rule whose strategy is a registered <see cref="INotableDateAlgorithm" />, delegating to the
    /// configured algorithm registry.
    /// </summary>
    /// <param name="rule">The rule bound to an algorithm strategy.</param>
    /// <param name="year">The civil year.</param>
    /// <returns>
    /// The calculated date, or <see langword="null" /> if the configured algorithm returns no date for the year.
    /// </returns>
    private DateTime? ResolveAlgorithm(NotableDateRule rule, int year)
    {
        // Prefer registry lookup (DI-friendly, decoupled from CLR type names).
        if (!string.IsNullOrWhiteSpace(rule.AlgorithmKey)
            && _algorithms is not null
            && _algorithms.TryGet(rule.AlgorithmKey!, out INotableDateAlgorithm? algorithm)
            && algorithm is not null)
        {
            return algorithm.GetDate(year);
        }

        // Fallback: legacy CLR type instantiation, for compatibility with rules authored before the registry existed.
        if (rule.AlgorithmType is not null)
        {
            INotableDateAlgorithm? legacyAlgorithm = TryCreateAlgorithm(rule);
            if (legacyAlgorithm is not null)
                return legacyAlgorithm.GetDate(year);
        }

        return null;
    }

    /// <summary>
    /// Resolves <paramref name="rule" /> for <paramref name="year" />, tracking active names in
    /// <paramref name="resolving" /> to detect cycles among offset-anchored rules.
    /// </summary>
    /// <param name="rule">The rule to resolve.</param>
    /// <param name="year">The civil year.</param>
    /// <param name="resolving">The set of rule identities currently being resolved up the call stack.</param>
    /// <returns>The resolved date, or <see langword="null" /> if the rule does not apply.</returns>
    /// <exception cref="InvalidOperationException">A cycle was detected among offset-anchored rules.</exception>
    private DateTime? ResolveInternal(NotableDateRule rule, int year, HashSet<NotableDateRuleIdentity> resolving)
    {
        if (!IsApplicable(rule, year))
            return null;

        // Track the in-flight chain by full identity so two same-name variants are not mistaken for a cycle.
        NotableDateRuleIdentity identity = NotableDateRuleIdentity.From(rule);
        if (!resolving.Add(identity))
        {
            var chain = string.Join(" -> ", resolving.Select(i => i.Name).Concat([rule.Name]));
            throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, CalendarResourceStrings.Op_Invalid_CircularDependencyInRule, rule.Name, chain));
        }

        try
        {
            switch (rule.Strategy)
            {
                case DateResolutionStrategy.Fixed:
                    if (rule.Day is not { } d1)
                        return null;

                    if (rule.CalendarType is { } calType
                        && Activator.CreateInstance(calType) is System.Globalization.Calendar cal)
                    {
                        if (rule.SweepCalendarYears)
                            return ResolveCalendarYearSweep(rule, year, cal, d1);

                        if (rule.SkipLeapMonth && cal is System.Globalization.ChineseLunisolarCalendar chineseCal)
                            return ResolveChineseLeapMonthSkip(rule, year, chineseCal, d1);

                        if (rule.Month is { } m1)
                        {
                            try
                            {
                                var converted = cal.ToDateTime(year, m1, d1, 0, 0, 0, 0);
                                return DateTime.SpecifyKind(converted.Date, DateTimeKind.Unspecified);
                            }
                            catch (ArgumentOutOfRangeException)
                            {
                                // Year is outside the calendar's supported range; treat as no occurrence.
                                return null;
                            }
                        }

                        return null;
                    }

                    if (rule.Month is not { } gregorianMonth)
                        return null;

                    try
                    {
                        return new DateTime(year, gregorianMonth, d1, 0, 0, 0, DateTimeKind.Unspecified);
                    }
                    catch (ArgumentOutOfRangeException)
                    {
                        return null;
                    }

                case DateResolutionStrategy.DayOfWeekInMonth:
                    if (rule.Month is { } m2 && rule.WeekOrdinal is { } ord && rule.DayOfWeek is { } dow)
                        return DateTimeExtensions.GetNthDateOfWeekInMonth(year, m2, dow, ord);
                    return null;

                case DateResolutionStrategy.OffsetFromAnchor:
                    return ResolveOffsetAnchor(rule, year, resolving);

                case DateResolutionStrategy.WeekdayNearDate:
                    if (rule.Month is { } wnm && rule.Day is { } wnd && rule.DayOfWeek is { } wndow && rule.WeekdayProximity is { } proximity)
                        return ResolveWeekdayNearDate(year, wnm, wnd, wndow, proximity);
                    return null;

                case DateResolutionStrategy.RelativeWeekdayInMonth:
                    if (rule.Month is { } rwm && rule.DayOfWeek is { } anchorDow && rule.WeekOrdinal is { } anchorOrd
                        && rule.RelativeDayOfWeek is { } relativeDow && rule.WeekdayProximity is { } relativeProximity)
                        return ResolveRelativeWeekdayInMonth(year, rwm, anchorDow, anchorOrd, relativeDow, relativeProximity);
                    return null;

                case DateResolutionStrategy.Algorithm:
                    return ResolveAlgorithm(rule, year);

                default:
                    throw new NotSupportedException(string.Format(CultureInfo.InvariantCulture, CalendarResourceStrings.Op_NotSupported_DateResolutionStrategy, rule.Strategy, rule.Name));
            }
        }
        finally
        {
            resolving.Remove(identity);
        }
    }

    /// <summary>
    /// Resolves a <see cref="DateResolutionStrategy.WeekdayNearDate" /> rule by positioning the target weekday relative
    /// to the reference (month, day) in the supplied Gregorian year.
    /// </summary>
    /// <param name="year">The Gregorian year.</param>
    /// <param name="month">The reference month, in the range 1–12.</param>
    /// <param name="day">The reference day-of-month.</param>
    /// <param name="targetDayOfWeek">The weekday the rule resolves to.</param>
    /// <param name="proximity">
    /// The direction used to position <paramref name="targetDayOfWeek" /> relative to the reference date.
    /// </param>
    /// <returns>
    /// The resolved date with <see cref="DateTimeKind.Unspecified" />, or <see langword="null" /> when the reference
    /// (year, month, day) is not a valid date or the shifted result falls outside the representable range.
    /// </returns>
    /// <remarks>
    /// The forward and backward distances to the nearest occurrence of <paramref name="targetDayOfWeek" /> always sum
    /// to seven, so for <see cref="WeekdayProximity.Nearest" /> they are never equal and the closer one is unambiguous;
    /// when the reference date already falls on the target weekday every direction yields the reference date itself.
    /// </remarks>
    private static DateTime? ResolveWeekdayNearDate(int year, int month, int day, DayOfWeek targetDayOfWeek, WeekdayProximity proximity)
    {
        DateTime reference;
        try
        {
            reference = new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Unspecified);
        }
        catch (ArgumentOutOfRangeException)
        {
            // Reference (year, month, day) is not a valid Gregorian date; treat as no occurrence.
            return null;
        }

        return PositionWeekday(reference, targetDayOfWeek, proximity);
    }

    /// <summary>
    /// Resolves a <see cref="DateResolutionStrategy.RelativeWeekdayInMonth" /> rule by computing the anchor — the
    /// <paramref name="weekOrdinal" /> occurrence of <paramref name="anchorDayOfWeek" /> in <paramref name="month" /> —
    /// and then positioning <paramref name="relativeDayOfWeek" /> relative to that anchor.
    /// </summary>
    /// <param name="year">The Gregorian year.</param>
    /// <param name="month">The anchor month, in the range 1–12.</param>
    /// <param name="anchorDayOfWeek">The anchor weekday whose ordinal occurrence supplies the reference date.</param>
    /// <param name="weekOrdinal">Which occurrence of <paramref name="anchorDayOfWeek" /> serves as the anchor.</param>
    /// <param name="relativeDayOfWeek">The target weekday the rule resolves to.</param>
    /// <param name="proximity">
    /// The direction used to position <paramref name="relativeDayOfWeek" /> relative to the anchor.
    /// </param>
    /// <returns>
    /// The resolved date with <see cref="DateTimeKind.Unspecified" />, or <see langword="null" /> when the anchor
    /// occurrence does not exist (for example a fifth occurrence in a month that has only four) or the result falls
    /// outside the representable range.
    /// </returns>
    private static DateTime? ResolveRelativeWeekdayInMonth(int year, int month, DayOfWeek anchorDayOfWeek, WeekOfMonthOrdinal weekOrdinal, DayOfWeek relativeDayOfWeek, WeekdayProximity proximity)
    {
        DateTime anchor;
        try
        {
            anchor = DateTimeExtensions.GetNthDateOfWeekInMonth(year, month, anchorDayOfWeek, weekOrdinal);
        }
        catch (ArgumentOutOfRangeException)
        {
            // The requested ordinal occurrence does not exist in the month; treat as no occurrence.
            return null;
        }

        return PositionWeekday(anchor, relativeDayOfWeek, proximity);
    }

    /// <summary>
    /// Positions <paramref name="targetDayOfWeek" /> relative to <paramref name="reference" /> according to
    /// <paramref name="proximity" /> — the first such weekday on or after the reference, on or before it, or the
    /// nearest occurrence in either direction. Shared by the <see cref="DateResolutionStrategy.WeekdayNearDate" /> and
    /// <see cref="DateResolutionStrategy.RelativeWeekdayInMonth" /> strategies.
    /// </summary>
    /// <param name="reference">The reference date to position the target weekday around.</param>
    /// <param name="targetDayOfWeek">The weekday to resolve to.</param>
    /// <param name="proximity">The positioning direction.</param>
    /// <returns>
    /// The resolved date with <see cref="DateTimeKind.Unspecified" />, or <see langword="null" /> when the shifted
    /// result falls outside the representable range.
    /// </returns>
    /// <remarks>
    /// The forward and backward distances to the nearest occurrence of <paramref name="targetDayOfWeek" /> always sum
    /// to seven, so for <see cref="WeekdayProximity.Nearest" /> they are never equal and the closer one is unambiguous;
    /// when <paramref name="reference" /> already falls on the target weekday every direction yields the reference date
    /// itself.
    /// </remarks>
    private static DateTime? PositionWeekday(DateTime reference, DayOfWeek targetDayOfWeek, WeekdayProximity proximity)
    {
        try
        {
            return proximity switch
            {
                WeekdayProximity.OnOrAfter => reference.NextOrSameDateOfWeek(targetDayOfWeek),
                WeekdayProximity.OnOrBefore => reference.PreviousOrSameDateOfWeek(targetDayOfWeek),
                WeekdayProximity.Nearest => reference.NearestDateOfWeek(targetDayOfWeek),
                _ => reference,
            };
        }
        catch (ArgumentOutOfRangeException)
        {
            return null;
        }
    }

    /// <summary>
    /// Resolves the anchor date of a rule whose strategy is offset-based, looking up the anchor rule by name and
    /// applying the configured day offset.
    /// </summary>
    /// <param name="rule">The offset-based rule whose anchor must be resolved.</param>
    /// <param name="year">The civil year.</param>
    /// <param name="resolving">The set of rule identities currently being resolved up the call stack.</param>
    /// <returns>
    /// The resolved anchor date, or <see langword="null" /> if the anchor rule does not apply in the given year.
    /// </returns>
    private DateTime? ResolveOffsetAnchor(NotableDateRule rule, int year, HashSet<NotableDateRuleIdentity> resolving)
    {
        if (string.IsNullOrWhiteSpace(rule.AnchorRuleName))
            return null;

        // Resolve the anchor by name, applying any authored variant/territory/calendar filter and then disambiguating
        // with this rule's territory/calendar context when several variants share the name. An unresolvable name throws
        // not-found; an undisambiguable match throws ambiguous.
        NotableDateRuleReference reference = new(rule.AnchorRuleName!, rule.AnchorRuleVariant, rule.AnchorTerritoryCode, rule.AnchorCalendarType);
        RuleReferenceResult result = _index.Resolve(reference, rule.TerritoryCode, rule.CalendarType);
        if (result.Match == RuleReferenceMatch.Ambiguous)
            throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, CalendarResourceStrings.Op_Invalid_AmbiguousAnchorReference, rule.AnchorRuleName, rule.Name, result.CandidateCount));

        if (result.Match != RuleReferenceMatch.Unique || result.Rule is not { } anchorRule)
            throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, CalendarResourceStrings.Op_Invalid_AnchorRuleNotFound, rule.AnchorRuleName, rule.Name));

        DateTime? anchorDate = ResolveInternal(anchorRule, year, resolving);
        if (anchorDate is null || rule.OffsetDays is not { } offset)
            return null;

        try
        {
            return anchorDate.Value.AddDays(offset);
        }
        catch (ArgumentOutOfRangeException)
        {
            return null;
        }
    }
}
