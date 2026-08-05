// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RecurrenceRule.Parse.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.InteropServices;

namespace Bodu.Globalization.Recurrence;

public sealed partial class RecurrenceRule :
    IParsable<RecurrenceRule>,
    ISpanParsable<RecurrenceRule>
{
    /// <summary>
    /// Identifies which rule parts have been seen while parsing, so a duplicate part can be rejected.
    /// </summary>
    [Flags]
    private enum RulePart
    {
        /// <summary>
        /// No rule part.
        /// </summary>
        None = 0,

        /// <summary>
        /// The <c>FREQ</c> rule part.
        /// </summary>
        Freq = 1 << 0,

        /// <summary>
        /// The <c>INTERVAL</c> rule part.
        /// </summary>
        Interval = 1 << 1,

        /// <summary>
        /// The <c>COUNT</c> rule part.
        /// </summary>
        Count = 1 << 2,

        /// <summary>
        /// The <c>UNTIL</c> rule part.
        /// </summary>
        Until = 1 << 3,

        /// <summary>
        /// The <c>WKST</c> rule part.
        /// </summary>
        WeekStart = 1 << 4,

        /// <summary>
        /// The <c>BYSECOND</c> rule part.
        /// </summary>
        BySecond = 1 << 5,

        /// <summary>
        /// The <c>BYMINUTE</c> rule part.
        /// </summary>
        ByMinute = 1 << 6,

        /// <summary>
        /// The <c>BYHOUR</c> rule part.
        /// </summary>
        ByHour = 1 << 7,

        /// <summary>
        /// The <c>BYDAY</c> rule part.
        /// </summary>
        ByDay = 1 << 8,

        /// <summary>
        /// The <c>BYMONTHDAY</c> rule part.
        /// </summary>
        ByMonthDay = 1 << 9,

        /// <summary>
        /// The <c>BYYEARDAY</c> rule part.
        /// </summary>
        ByYearDay = 1 << 10,

        /// <summary>
        /// The <c>BYWEEKNO</c> rule part.
        /// </summary>
        ByWeekNo = 1 << 11,

        /// <summary>
        /// The <c>BYMONTH</c> rule part.
        /// </summary>
        ByMonth = 1 << 12,

        /// <summary>
        /// The <c>BYSETPOS</c> rule part.
        /// </summary>
        BySetPos = 1 << 13,
    }

    /// <summary>
    /// Parses the textual form of a recurrence rule, such as <c>FREQ=WEEKLY;INTERVAL=2;BYDAY=MO,WE,FR</c>.
    /// </summary>
    /// <param name="s">The rule text to parse. An optional leading <c>RRULE:</c> prefix is accepted.</param>
    /// <returns>The parsed rule.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="s" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="FormatException">Thrown when <paramref name="s" /> is not a valid recurrence rule.</exception>
    public static RecurrenceRule Parse(string s) =>
        Parse(s, null);

    /// <summary>
    /// Parses the textual form of a recurrence rule.
    /// </summary>
    /// <param name="s">The rule text to parse. An optional leading <c>RRULE:</c> prefix is accepted.</param>
    /// <param name="provider">Unused; recurrence-rule text is culture-invariant.</param>
    /// <returns>The parsed rule.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="s" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="FormatException">Thrown when <paramref name="s" /> is not a valid recurrence rule.</exception>
    public static RecurrenceRule Parse(string s, IFormatProvider? provider)
    {
        ThrowHelper.ThrowIfNull(s);

        return Parse(s.AsSpan(), provider);
    }

    /// <summary>
    /// Parses the textual form of a recurrence rule from a character span.
    /// </summary>
    /// <param name="s">The rule text to parse. An optional leading <c>RRULE:</c> prefix is accepted.</param>
    /// <param name="provider">Unused; recurrence-rule text is culture-invariant.</param>
    /// <returns>The parsed rule.</returns>
    /// <exception cref="FormatException">Thrown when <paramref name="s" /> is not a valid recurrence rule.</exception>
    public static RecurrenceRule Parse(ReadOnlySpan<char> s, IFormatProvider? provider) =>
        TryParseCore(s, out RecurrenceRule? result, out _)
            ? result
            : throw new FormatException(
                string.Format(CultureInfo.CurrentCulture, RecurrenceResourceStrings.Format_Invalid_RecurrenceRuleText, s.ToString()));

    /// <summary>
    /// Attempts to parse the textual form of a recurrence rule.
    /// </summary>
    /// <param name="s">The rule text to parse.</param>
    /// <param name="result">The parsed rule, or <see langword="null" /> on failure.</param>
    /// <returns><see langword="true" /> if parsing succeeded; otherwise <see langword="false" />.</returns>
    public static bool TryParse([NotNullWhen(true)] string? s, [MaybeNullWhen(false)] out RecurrenceRule result) =>
        TryParse(s, null, out result);

    /// <summary>
    /// Attempts to parse the textual form of a recurrence rule.
    /// </summary>
    /// <param name="s">The rule text to parse.</param>
    /// <param name="provider">Unused; recurrence-rule text is culture-invariant.</param>
    /// <param name="result">The parsed rule, or <see langword="null" /> on failure.</param>
    /// <returns><see langword="true" /> if parsing succeeded; otherwise <see langword="false" />.</returns>
    public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, [MaybeNullWhen(false)] out RecurrenceRule result)
    {
        if (s is null)
        {
            result = null;
            return false;
        }

        return TryParseCore(s.AsSpan(), out result, out _);
    }

    /// <summary>
    /// Attempts to parse the textual form of a recurrence rule from a character span.
    /// </summary>
    /// <param name="s">The rule text to parse.</param>
    /// <param name="provider">Unused; recurrence-rule text is culture-invariant.</param>
    /// <param name="result">The parsed rule, or <see langword="null" /> on failure.</param>
    /// <returns><see langword="true" /> if parsing succeeded; otherwise <see langword="false" />.</returns>
    public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, [MaybeNullWhen(false)] out RecurrenceRule result) =>
        TryParseCore(s, out result, out _);

    /// <summary>
    /// Attempts to parse the textual form of a recurrence rule, reporting the parse defect on failure.
    /// </summary>
    /// <param name="s">The rule text to parse.</param>
    /// <param name="result">The parsed rule, or <see langword="null" /> on failure.</param>
    /// <param name="failureMessage">
    /// <see langword="null" /> on success; otherwise a message naming the specific defect, suitable for surfacing to
    /// the user verbatim.
    /// </param>
    /// <returns><see langword="true" /> if parsing succeeded; otherwise <see langword="false" />.</returns>
    public static bool TryParse(
        string? s,
        [MaybeNullWhen(false)] out RecurrenceRule result,
        [NotNullWhen(false)] out string? failureMessage)
    {
        if (s is null)
        {
            result = null;
            failureMessage = RecurrenceResourceStrings.Format_Invalid_RecurrenceRuleEmpty;
            return false;
        }

        bool parsed = TryParseCore(s.AsSpan(), out result, out failureMessage);
        if (!parsed)
        {
            failureMessage ??= string.Format(CultureInfo.CurrentCulture, RecurrenceResourceStrings.Format_Invalid_RecurrenceRuleText, s);
        }

        return parsed;
    }

    /// <summary>
    /// Parses and validates every rule part, producing a rule when the text is a well-formed <c>RRULE</c>.
    /// </summary>
    /// <param name="s">The rule text to parse.</param>
    /// <param name="result">The parsed rule, or <see langword="null" /> on failure.</param>
    /// <param name="failureMessage">
    /// <see langword="null" /> on success; otherwise a message naming the specific defect.
    /// </param>
    /// <returns><see langword="true" /> if parsing succeeded; otherwise <see langword="false" />.</returns>
    private static bool TryParseCore(
        ReadOnlySpan<char> s,
        [MaybeNullWhen(false)] out RecurrenceRule result,
        out string? failureMessage)
    {
        result = null;
        failureMessage = null;

        s = s.Trim();
        if (s.StartsWith("RRULE:", StringComparison.OrdinalIgnoreCase))
        {
            s = s[6..].TrimStart();
        }

        if (s.IsEmpty)
        {
            failureMessage = RecurrenceResourceStrings.Format_Invalid_RecurrenceRuleEmpty;
            return false;
        }

        RulePart seen = RulePart.None;
        RecurrenceFrequency frequency = default;
        int interval = 1;
        int? count = null;
        DateTime? until = null;
        DayOfWeek weekStart = DayOfWeek.Monday;

        List<int>? bySecond = null;
        List<int>? byMinute = null;
        List<int>? byHour = null;
        List<int>? byMonthDay = null;
        List<int>? byYearDay = null;
        List<int>? byWeekNo = null;
        List<int>? byMonth = null;
        List<int>? bySetPos = null;
        List<WeekDayNum>? byDay = null;

        foreach (Range partRange in Split(s, ';'))
        {
            ReadOnlySpan<char> part = s[partRange].Trim();
            if (part.IsEmpty)
            {
                failureMessage = FormatDefect(RecurrenceResourceStrings.Format_Invalid_RecurrenceRulePart, part);
                return false;
            }

            int eq = part.IndexOf('=');
            if (eq <= 0)
            {
                failureMessage = FormatDefect(RecurrenceResourceStrings.Format_Invalid_RecurrenceRulePart, part);
                return false;
            }

            ReadOnlySpan<char> name = part[..eq].Trim();
            ReadOnlySpan<char> value = part[(eq + 1) ..].Trim();
            if (value.IsEmpty)
            {
                failureMessage = FormatDefect(RecurrenceResourceStrings.Format_Invalid_RecurrenceRulePart, part);
                return false;
            }

            bool valid;
            if (name.Equals("FREQ", StringComparison.OrdinalIgnoreCase))
            {
                valid = Seen(ref seen, RulePart.Freq, name, ref failureMessage)
                    && TryParseFrequency(value, out frequency);
            }
            else if (name.Equals("INTERVAL", StringComparison.OrdinalIgnoreCase))
            {
                valid = Seen(ref seen, RulePart.Interval, name, ref failureMessage)
                    && int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out interval)
                    && interval >= 1;
            }
            else if (name.Equals("COUNT", StringComparison.OrdinalIgnoreCase))
            {
                if (!Seen(ref seen, RulePart.Count, name, ref failureMessage))
                {
                    return false;
                }

                valid = int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out int parsedCount)
                    && parsedCount >= 1;
                if (valid)
                {
                    count = parsedCount;
                }
            }
            else if (name.Equals("UNTIL", StringComparison.OrdinalIgnoreCase))
            {
                if (!Seen(ref seen, RulePart.Until, name, ref failureMessage))
                {
                    return false;
                }

                valid = IcalValue.TryParseDateTime(value, out DateTime parsedUntil);
                if (valid)
                {
                    until = parsedUntil;
                }
            }
            else if (name.Equals("WKST", StringComparison.OrdinalIgnoreCase))
            {
                valid = Seen(ref seen, RulePart.WeekStart, name, ref failureMessage)
                    && IcalValue.TryParseWeekDay(value, out weekStart);
            }
            else if (name.Equals("BYSECOND", StringComparison.OrdinalIgnoreCase))
            {
                valid = Seen(ref seen, RulePart.BySecond, name, ref failureMessage)
                    && TryParseUIntList(value, 0, 60, out bySecond);
            }
            else if (name.Equals("BYMINUTE", StringComparison.OrdinalIgnoreCase))
            {
                valid = Seen(ref seen, RulePart.ByMinute, name, ref failureMessage)
                    && TryParseUIntList(value, 0, 59, out byMinute);
            }
            else if (name.Equals("BYHOUR", StringComparison.OrdinalIgnoreCase))
            {
                valid = Seen(ref seen, RulePart.ByHour, name, ref failureMessage)
                    && TryParseUIntList(value, 0, 23, out byHour);
            }
            else if (name.Equals("BYDAY", StringComparison.OrdinalIgnoreCase))
            {
                valid = Seen(ref seen, RulePart.ByDay, name, ref failureMessage)
                    && TryParseWeekDayList(value, out byDay);
            }
            else if (name.Equals("BYMONTHDAY", StringComparison.OrdinalIgnoreCase))
            {
                valid = Seen(ref seen, RulePart.ByMonthDay, name, ref failureMessage)
                    && TryParseSignedList(value, 1, 31, out byMonthDay);
            }
            else if (name.Equals("BYYEARDAY", StringComparison.OrdinalIgnoreCase))
            {
                valid = Seen(ref seen, RulePart.ByYearDay, name, ref failureMessage)
                    && TryParseSignedList(value, 1, 366, out byYearDay);
            }
            else if (name.Equals("BYWEEKNO", StringComparison.OrdinalIgnoreCase))
            {
                valid = Seen(ref seen, RulePart.ByWeekNo, name, ref failureMessage)
                    && TryParseSignedList(value, 1, 53, out byWeekNo);
            }
            else if (name.Equals("BYMONTH", StringComparison.OrdinalIgnoreCase))
            {
                valid = Seen(ref seen, RulePart.ByMonth, name, ref failureMessage)
                    && TryParseUIntList(value, 1, 12, out byMonth);
            }
            else if (name.Equals("BYSETPOS", StringComparison.OrdinalIgnoreCase))
            {
                valid = Seen(ref seen, RulePart.BySetPos, name, ref failureMessage)
                    && TryParseSignedList(value, 1, 366, out bySetPos);
            }
            else
            {
                // Unknown rule part (including RFC 7529 RSCALE/SKIP and X-* extensions) — outside the supported grammar.
                failureMessage = FormatDefect(RecurrenceResourceStrings.Format_Invalid_RecurrenceRuleUnknownPart, name);
                return false;
            }

            if (!valid)
            {
                // A duplicate part has already recorded its defect; otherwise the component's value is at fault.
                failureMessage ??= FormatDefect(RecurrenceResourceStrings.Format_Invalid_RecurrenceRulePart, part);
                return false;
            }
        }

        if (!seen.HasFlag(RulePart.Freq))
        {
            failureMessage = RecurrenceResourceStrings.Arg_Invalid_RecurrenceFrequencyRequired;
            return false;
        }

        // RFC 5545 §3.3.10: COUNT and UNTIL must not both appear in the same rule.
        if (count is not null && until is not null)
        {
            failureMessage = RecurrenceResourceStrings.Format_Invalid_RecurrenceRuleCountAndUntil;
            return false;
        }

        result = new RecurrenceRule(
            frequency,
            interval,
            count,
            until,
            weekStart,
            AsSpan(bySecond),
            AsSpan(byMinute),
            AsSpan(byHour),
            AsSpan(byDay),
            AsSpan(byMonthDay),
            AsSpan(byYearDay),
            AsSpan(byWeekNo),
            AsSpan(byMonth),
            AsSpan(bySetPos));
        return true;
    }

    /// <summary>
    /// Records that a rule part has been seen, reporting a duplicate-part defect when it was already present.
    /// </summary>
    /// <param name="seen">The accumulated set of seen rule parts.</param>
    /// <param name="part">The rule part being recorded.</param>
    /// <param name="name">The rule-part name, used in the duplicate defect message.</param>
    /// <param name="failureMessage">Set to the duplicate-part defect message when the part was already seen.</param>
    /// <returns>
    /// <see langword="true" /> if <paramref name="part" /> had not been seen; otherwise <see langword="false" />.
    /// </returns>
    private static bool Seen(ref RulePart seen, RulePart part, ReadOnlySpan<char> name, ref string? failureMessage)
    {
        if (seen.HasFlag(part))
        {
            failureMessage = FormatDefect(RecurrenceResourceStrings.Format_Invalid_RecurrenceRuleDuplicatePart, name);
            return false;
        }

        seen |= part;
        return true;
    }

    /// <summary>
    /// Formats a defect message that carries the offending token.
    /// </summary>
    /// <param name="format">The resource format string.</param>
    /// <param name="token">The offending token.</param>
    /// <returns>The formatted defect message.</returns>
    private static string FormatDefect(string format, ReadOnlySpan<char> token) =>
        string.Format(CultureInfo.CurrentCulture, format, token.ToString());

    /// <summary>
    /// Maps a <c>FREQ</c> token to a <see cref="RecurrenceFrequency" />.
    /// </summary>
    /// <param name="value">The candidate frequency token.</param>
    /// <param name="frequency">The mapped frequency on success.</param>
    /// <returns><see langword="true" /> if the token was recognized; otherwise <see langword="false" />.</returns>
    private static bool TryParseFrequency(ReadOnlySpan<char> value, out RecurrenceFrequency frequency)
    {
        frequency = value switch
        {
            _ when value.Equals("SECONDLY", StringComparison.OrdinalIgnoreCase) => RecurrenceFrequency.Secondly,
            _ when value.Equals("MINUTELY", StringComparison.OrdinalIgnoreCase) => RecurrenceFrequency.Minutely,
            _ when value.Equals("HOURLY", StringComparison.OrdinalIgnoreCase) => RecurrenceFrequency.Hourly,
            _ when value.Equals("DAILY", StringComparison.OrdinalIgnoreCase) => RecurrenceFrequency.Daily,
            _ when value.Equals("WEEKLY", StringComparison.OrdinalIgnoreCase) => RecurrenceFrequency.Weekly,
            _ when value.Equals("MONTHLY", StringComparison.OrdinalIgnoreCase) => RecurrenceFrequency.Monthly,
            _ when value.Equals("YEARLY", StringComparison.OrdinalIgnoreCase) => RecurrenceFrequency.Yearly,
            _ => (RecurrenceFrequency)(-1),
        };

        return frequency != (RecurrenceFrequency)(-1);
    }

    /// <summary>
    /// Parses a comma-separated list of non-negative integers, each within an inclusive range.
    /// </summary>
    /// <param name="value">The comma-separated list text.</param>
    /// <param name="lo">The inclusive lower bound.</param>
    /// <param name="hi">The inclusive upper bound.</param>
    /// <param name="result">The parsed values on success.</param>
    /// <returns>
    /// <see langword="true" /> if the list was non-empty and every value was in range; otherwise
    /// <see langword="false" />.
    /// </returns>
    private static bool TryParseUIntList(ReadOnlySpan<char> value, int lo, int hi, out List<int> result)
    {
        result = [];
        foreach (Range tokenRange in Split(value, ','))
        {
            ReadOnlySpan<char> token = value[tokenRange].Trim();
            if (!int.TryParse(token, NumberStyles.None, CultureInfo.InvariantCulture, out int n) || n < lo || n > hi)
            {
                return false;
            }

            result.Add(n);
        }

        return result.Count > 0;
    }

    /// <summary>
    /// Parses a comma-separated list of non-zero signed integers, each with a magnitude within an inclusive range.
    /// </summary>
    /// <param name="value">The comma-separated list text.</param>
    /// <param name="magLo">The inclusive lower bound on the magnitude.</param>
    /// <param name="magHi">The inclusive upper bound on the magnitude.</param>
    /// <param name="result">The parsed values on success.</param>
    /// <returns>
    /// <see langword="true" /> if the list was non-empty and every value was valid; otherwise <see langword="false" />.
    /// </returns>
    private static bool TryParseSignedList(ReadOnlySpan<char> value, int magLo, int magHi, out List<int> result)
    {
        result = [];
        foreach (Range tokenRange in Split(value, ','))
        {
            ReadOnlySpan<char> token = value[tokenRange].Trim();
            if (!int.TryParse(token, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out int n) || n == 0)
            {
                return false;
            }

            int magnitude = Math.Abs(n);
            if (magnitude < magLo || magnitude > magHi)
            {
                return false;
            }

            result.Add(n);
        }

        return result.Count > 0;
    }

    /// <summary>
    /// Parses a comma-separated <c>BYDAY</c> list of optionally ordinal-prefixed weekday tokens.
    /// </summary>
    /// <param name="value">The comma-separated list text.</param>
    /// <param name="result">The parsed weekday entries on success.</param>
    /// <returns>
    /// <see langword="true" /> if the list was non-empty and every entry was valid; otherwise <see langword="false" />.
    /// </returns>
    private static bool TryParseWeekDayList(ReadOnlySpan<char> value, out List<WeekDayNum> result)
    {
        result = [];
        foreach (Range tokenRange in Split(value, ','))
        {
            ReadOnlySpan<char> token = value[tokenRange].Trim();
            if (token.Length < 2)
            {
                return false;
            }

            ReadOnlySpan<char> code = token[^2..];
            ReadOnlySpan<char> ordinalText = token[..^2];
            if (!IcalValue.TryParseWeekDay(code, out DayOfWeek day))
            {
                return false;
            }

            int ordinal = 0;
            if (!ordinalText.IsEmpty
                && (!int.TryParse(ordinalText, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out ordinal)
                    || ordinal == 0
                    || Math.Abs(ordinal) > 53))
            {
                return false;
            }

            result.Add(new WeekDayNum(ordinal, day));
        }

        return result.Count > 0;
    }

    /// <summary>
    /// Returns a span over the supplied list, or an empty span when the list is <see langword="null" />.
    /// </summary>
    /// <param name="list">The list to view, or <see langword="null" />.</param>
    /// <returns>A span over the list's elements.</returns>
    private static ReadOnlySpan<int> AsSpan(List<int>? list) =>
        list is null ? default : CollectionsMarshal.AsSpan(list);

    /// <summary>
    /// Returns a span over the supplied list, or an empty span when the list is <see langword="null" />.
    /// </summary>
    /// <param name="list">The list to view, or <see langword="null" />.</param>
    /// <returns>A span over the list's elements.</returns>
    private static ReadOnlySpan<WeekDayNum> AsSpan(List<WeekDayNum>? list) =>
        list is null ? default : CollectionsMarshal.AsSpan(list);

    /// <summary>
    /// Enumerates the half-open ranges of <paramref name="source" /> delimited by <paramref name="separator" />.
    /// </summary>
    /// <param name="source">The span to split.</param>
    /// <param name="separator">The delimiter character.</param>
    /// <returns>The delimited sub-ranges, including empty ranges for adjacent separators.</returns>
    private static List<Range> Split(ReadOnlySpan<char> source, char separator)
    {
        // Materialize offsets eagerly so the ranges can be iterated without capturing the span.
        var ranges = new List<Range>();
        int start = 0;
        for (int i = 0; i < source.Length; i++)
        {
            if (source[i] == separator)
            {
                ranges.Add(start..i);
                start = i + 1;
            }
        }

        ranges.Add(start..source.Length);
        return ranges;
    }
}
