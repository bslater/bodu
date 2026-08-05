// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AnchoredInterval.Parse.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Bodu.Globalization.Recurrence;

public sealed partial class AnchoredInterval :
    IParsable<AnchoredInterval>,
    ISpanParsable<AnchoredInterval>
{
    /// <summary>
    /// Orders the duration units so a repeated or misordered component can be rejected.
    /// </summary>
    private enum DurationUnit
    {
        /// <summary>
        /// No component parsed yet.
        /// </summary>
        None = 0,

        /// <summary>
        /// The <c>W</c> (weeks) component.
        /// </summary>
        Weeks,

        /// <summary>
        /// The <c>D</c> (days) component.
        /// </summary>
        Days,

        /// <summary>
        /// The <c>H</c> (hours) component.
        /// </summary>
        Hours,

        /// <summary>
        /// The <c>M</c> (minutes) component.
        /// </summary>
        Minutes,

        /// <summary>
        /// The <c>S</c> (seconds) component.
        /// </summary>
        Seconds,
    }

    /// <summary>
    /// Parses the RFC 5545 §3.3.6 duration text of an interval, such as <c>PT4H</c> or <c>P1DT2H30M</c>.
    /// </summary>
    /// <param name="s">The duration text to parse.</param>
    /// <returns>The parsed interval.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="s" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="FormatException">
    /// Thrown when <paramref name="s" /> is not a valid positive iCalendar duration; the message names the defect.
    /// </exception>
    public static AnchoredInterval Parse(string s) =>
        Parse(s, null);

    /// <summary>
    /// Parses the RFC 5545 §3.3.6 duration text of an interval.
    /// </summary>
    /// <param name="s">The duration text to parse.</param>
    /// <param name="provider">Unused; duration text is culture-invariant.</param>
    /// <returns>The parsed interval.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="s" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="FormatException">
    /// Thrown when <paramref name="s" /> is not a valid positive iCalendar duration; the message names the defect.
    /// </exception>
    public static AnchoredInterval Parse(string s, IFormatProvider? provider)
    {
        ThrowHelper.ThrowIfNull(s);

        return Parse(s.AsSpan(), provider);
    }

    /// <summary>
    /// Parses the RFC 5545 §3.3.6 duration text of an interval from a character span.
    /// </summary>
    /// <param name="s">The duration text to parse.</param>
    /// <param name="provider">Unused; duration text is culture-invariant.</param>
    /// <returns>The parsed interval.</returns>
    /// <exception cref="FormatException">
    /// Thrown when <paramref name="s" /> is not a valid positive iCalendar duration; the message names the defect.
    /// </exception>
    public static AnchoredInterval Parse(ReadOnlySpan<char> s, IFormatProvider? provider) =>
        TryParseCore(s, out AnchoredInterval? result, out string? failureMessage)
            ? result
            : throw new FormatException(failureMessage);

    /// <summary>
    /// Attempts to parse the RFC 5545 §3.3.6 duration text of an interval.
    /// </summary>
    /// <param name="s">The duration text to parse.</param>
    /// <param name="result">The parsed interval, or <see langword="null" /> on failure.</param>
    /// <returns><see langword="true" /> if parsing succeeded; otherwise <see langword="false" />.</returns>
    public static bool TryParse([NotNullWhen(true)] string? s, [MaybeNullWhen(false)] out AnchoredInterval result) =>
        TryParse(s, null, out result);

    /// <summary>
    /// Attempts to parse the RFC 5545 §3.3.6 duration text of an interval.
    /// </summary>
    /// <param name="s">The duration text to parse.</param>
    /// <param name="provider">Unused; duration text is culture-invariant.</param>
    /// <param name="result">The parsed interval, or <see langword="null" /> on failure.</param>
    /// <returns><see langword="true" /> if parsing succeeded; otherwise <see langword="false" />.</returns>
    public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, [MaybeNullWhen(false)] out AnchoredInterval result)
    {
        if (s is null)
        {
            result = null;
            return false;
        }

        return TryParseCore(s.AsSpan(), out result, out _);
    }

    /// <summary>
    /// Attempts to parse the RFC 5545 §3.3.6 duration text of an interval from a character span.
    /// </summary>
    /// <param name="s">The duration text to parse.</param>
    /// <param name="provider">Unused; duration text is culture-invariant.</param>
    /// <param name="result">The parsed interval, or <see langword="null" /> on failure.</param>
    /// <returns><see langword="true" /> if parsing succeeded; otherwise <see langword="false" />.</returns>
    public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, [MaybeNullWhen(false)] out AnchoredInterval result) =>
        TryParseCore(s, out result, out _);

    /// <summary>
    /// Attempts to parse the RFC 5545 §3.3.6 duration text of an interval, reporting the parse defect on failure.
    /// </summary>
    /// <param name="s">The duration text to parse.</param>
    /// <param name="result">The parsed interval, or <see langword="null" /> on failure.</param>
    /// <param name="failureMessage">
    /// <see langword="null" /> on success; otherwise a message naming the specific defect, suitable for surfacing to
    /// the user verbatim.
    /// </param>
    /// <returns><see langword="true" /> if parsing succeeded; otherwise <see langword="false" />.</returns>
    public static bool TryParse(
        string? s,
        [MaybeNullWhen(false)] out AnchoredInterval result,
        [NotNullWhen(false)] out string? failureMessage)
    {
        if (s is null)
        {
            result = null;
            failureMessage = RecurrenceResourceStrings.Format_Invalid_DurationEmpty;
            return false;
        }

        return TryParseCore(s.AsSpan(), out result, out failureMessage);
    }

    /// <summary>
    /// Parses and validates every duration component, producing an interval when the text is a well-formed positive
    /// duration.
    /// </summary>
    /// <param name="s">The duration text to parse.</param>
    /// <param name="result">The parsed interval, or <see langword="null" /> on failure.</param>
    /// <param name="failureMessage">
    /// <see langword="null" /> on success; otherwise a message naming the specific defect.
    /// </param>
    /// <returns><see langword="true" /> if parsing succeeded; otherwise <see langword="false" />.</returns>
    private static bool TryParseCore(
        ReadOnlySpan<char> s,
        [MaybeNullWhen(false)] out AnchoredInterval result,
        [NotNullWhen(false)] out string? failureMessage)
    {
        result = null;

        s = s.Trim();
        if (s.IsEmpty)
        {
            failureMessage = RecurrenceResourceStrings.Format_Invalid_DurationEmpty;
            return false;
        }

        if (s[0] is '+' or '-')
        {
            failureMessage = RecurrenceResourceStrings.Format_Invalid_DurationNotPositive;
            return false;
        }

        if (char.ToUpperInvariant(s[0]) != 'P')
        {
            failureMessage = RecurrenceResourceStrings.Format_Invalid_DurationMissingPrefix;
            return false;
        }

        bool inTime = false;
        DurationUnit lastUnit = DurationUnit.None;
        long weeks = 0, days = 0, hours = 0, minutes = 0, seconds = 0;
        int componentCount = 0;
        int index = 1;

        while (index < s.Length)
        {
            if (char.ToUpperInvariant(s[index]) == 'T')
            {
                // The time designator may appear once, before any H/M/S component and after any W component is ruled out.
                if (inTime || lastUnit > DurationUnit.Days)
                {
                    failureMessage = FormatDefect(RecurrenceResourceStrings.Format_Invalid_DurationUnitOrder, "T");
                    return false;
                }

                inTime = true;
                index++;
                continue;
            }

            int digitStart = index;
            long value = 0;
            while (index < s.Length && char.IsAsciiDigit(s[index]))
            {
                int digit = s[index] - '0';
                if (value > (long.MaxValue - digit) / 10)
                {
                    failureMessage = RecurrenceResourceStrings.Format_Invalid_DurationTooLarge;
                    return false;
                }

                value = (value * 10) + digit;
                index++;
            }

            if (index == digitStart || index == s.Length)
            {
                failureMessage = FormatDefect(
                    RecurrenceResourceStrings.Format_Invalid_DurationComponent,
                    s[digitStart..Math.Min(index + 1, s.Length)].ToString());
                return false;
            }

            char unitChar = char.ToUpperInvariant(s[index]);
            DurationUnit unit = unitChar switch
            {
                'W' => DurationUnit.Weeks,
                'D' => DurationUnit.Days,
                'H' => DurationUnit.Hours,
                'M' => DurationUnit.Minutes,
                'S' => DurationUnit.Seconds,
                _ => DurationUnit.None,
            };

            if (unit == DurationUnit.None)
            {
                int componentEnd = index + 1;
                failureMessage = FormatDefect(
                    RecurrenceResourceStrings.Format_Invalid_DurationComponent,
                    s[digitStart..componentEnd].ToString());
                return false;
            }

            if (unit >= DurationUnit.Hours && !inTime)
            {
                failureMessage = RecurrenceResourceStrings.Format_Invalid_DurationMissingTimeDesignator;
                return false;
            }

            if (unit <= lastUnit || (unit <= DurationUnit.Days && inTime))
            {
                failureMessage = FormatDefect(RecurrenceResourceStrings.Format_Invalid_DurationUnitOrder, unitChar.ToString());
                return false;
            }

            switch (unit)
            {
                case DurationUnit.Weeks:
                    weeks = value;
                    break;
                case DurationUnit.Days:
                    days = value;
                    break;
                case DurationUnit.Hours:
                    hours = value;
                    break;
                case DurationUnit.Minutes:
                    minutes = value;
                    break;
                default:
                    seconds = value;
                    break;
            }

            lastUnit = unit;
            componentCount++;
            index++;
        }

        if (componentCount == 0)
        {
            failureMessage = RecurrenceResourceStrings.Format_Invalid_DurationNoComponents;
            return false;
        }

        if (weeks != 0 && componentCount > 1)
        {
            failureMessage = RecurrenceResourceStrings.Format_Invalid_DurationWeeksCombined;
            return false;
        }

        if (!TryComputeTicks(weeks, days, hours, minutes, seconds, out long ticks))
        {
            failureMessage = RecurrenceResourceStrings.Format_Invalid_DurationTooLarge;
            return false;
        }

        if (ticks <= 0)
        {
            failureMessage = RecurrenceResourceStrings.Format_Invalid_DurationNotPositive;
            return false;
        }

        result = new AnchoredInterval(TimeSpan.FromTicks(ticks));
        failureMessage = null;
        return true;
    }

    /// <summary>
    /// Sums the parsed duration components into a tick count, detecting overflow.
    /// </summary>
    /// <param name="weeks">The weeks component.</param>
    /// <param name="days">The days component.</param>
    /// <param name="hours">The hours component.</param>
    /// <param name="minutes">The minutes component.</param>
    /// <param name="seconds">The seconds component.</param>
    /// <param name="ticks">The total tick count on success.</param>
    /// <returns><see langword="true" /> when the total is representable; otherwise <see langword="false" />.</returns>
    private static bool TryComputeTicks(long weeks, long days, long hours, long minutes, long seconds, out long ticks)
    {
        ticks = 0;
        try
        {
            checked
            {
                ticks = (weeks * 7 * TimeSpan.TicksPerDay)
                    + (days * TimeSpan.TicksPerDay)
                    + (hours * TimeSpan.TicksPerHour)
                    + (minutes * TimeSpan.TicksPerMinute)
                    + (seconds * TimeSpan.TicksPerSecond);
            }
        }
        catch (OverflowException)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Formats a defect message that carries the offending token.
    /// </summary>
    /// <param name="format">The resource format string.</param>
    /// <param name="token">The offending token.</param>
    /// <returns>The formatted defect message.</returns>
    private static string FormatDefect(string format, string token) =>
        string.Format(CultureInfo.CurrentCulture, format, token);
}
