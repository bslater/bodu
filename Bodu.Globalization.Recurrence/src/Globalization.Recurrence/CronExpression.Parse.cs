// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CronExpression.Parse.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Bodu.Extensions;

namespace Bodu.Globalization.Recurrence;

public sealed partial class CronExpression : IParsable<CronExpression>
{
    /// <summary>
    /// Parses a cron expression, inferring the field layout from the field count (five or six).
    /// </summary>
    /// <param name="s">The cron expression text.</param>
    /// <returns>The parsed expression.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="s" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="FormatException">Thrown when <paramref name="s" /> is not a valid cron expression.</exception>
    public static CronExpression Parse(string s) =>
        Parse(s, null);

    /// <summary>
    /// Parses a cron expression, inferring the field layout from the field count (five or six).
    /// </summary>
    /// <param name="s">The cron expression text.</param>
    /// <param name="provider">Unused; cron text is culture-invariant.</param>
    /// <returns>The parsed expression.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="s" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="FormatException">Thrown when <paramref name="s" /> is not a valid cron expression.</exception>
    public static CronExpression Parse(string s, IFormatProvider? provider)
    {
        ThrowHelper.ThrowIfNull(s);

        if (TryParseCore(s, null, out CronExpression? result, out _))
        {
            return result;
        }

        throw new FormatException(string.Format(CultureInfo.CurrentCulture, RecurrenceResourceStrings.Format_Invalid_CronText, s));
    }

    /// <summary>
    /// Parses a cron expression using the specified field layout.
    /// </summary>
    /// <param name="s">The cron expression text.</param>
    /// <param name="format">The expected field layout.</param>
    /// <returns>The parsed expression.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="s" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="FormatException">Thrown when <paramref name="s" /> does not match the layout.</exception>
    public static CronExpression Parse(string s, CronFormat format)
    {
        ThrowHelper.ThrowIfNull(s);

        if (TryParseCore(s, format, out CronExpression? result, out _))
        {
            return result;
        }

        throw new FormatException(string.Format(CultureInfo.CurrentCulture, RecurrenceResourceStrings.Format_Invalid_CronText, s));
    }

    /// <summary>
    /// Attempts to parse a cron expression, inferring the field layout from the field count.
    /// </summary>
    /// <param name="s">The cron expression text.</param>
    /// <param name="result">The parsed expression, or <see langword="null" /> on failure.</param>
    /// <returns><see langword="true" /> if parsing succeeded; otherwise <see langword="false" />.</returns>
    public static bool TryParse([NotNullWhen(true)] string? s, [MaybeNullWhen(false)] out CronExpression result) =>
        TryParse(s, null, out result);

    /// <summary>
    /// Attempts to parse a cron expression, inferring the field layout from the field count.
    /// </summary>
    /// <param name="s">The cron expression text.</param>
    /// <param name="provider">Unused; cron text is culture-invariant.</param>
    /// <param name="result">The parsed expression, or <see langword="null" /> on failure.</param>
    /// <returns><see langword="true" /> if parsing succeeded; otherwise <see langword="false" />.</returns>
    public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, [MaybeNullWhen(false)] out CronExpression result)
    {
        if (s is null)
        {
            result = null;
            return false;
        }

        return TryParseCore(s, null, out result, out _);
    }

    /// <summary>
    /// Attempts to parse a cron expression, reporting the parse defect on failure.
    /// </summary>
    /// <param name="s">The cron expression text.</param>
    /// <param name="result">The parsed expression, or <see langword="null" /> on failure.</param>
    /// <param name="failureMessage">
    /// <see langword="null" /> on success; otherwise a message naming the specific defect, suitable for surfacing to
    /// the user verbatim.
    /// </param>
    /// <returns><see langword="true" /> if parsing succeeded; otherwise <see langword="false" />.</returns>
    public static bool TryParse(
        string? s,
        [MaybeNullWhen(false)] out CronExpression result,
        [NotNullWhen(false)] out string? failureMessage)
    {
        if (s is null)
        {
            result = null;
            failureMessage = RecurrenceResourceStrings.Format_Invalid_CronEmpty;
            return false;
        }

        bool parsed = TryParseCore(s, null, out result, out failureMessage);
        if (!parsed)
        {
            failureMessage ??= string.Format(CultureInfo.CurrentCulture, RecurrenceResourceStrings.Format_Invalid_CronText, s);
        }

        return parsed;
    }

    /// <summary>
    /// Attempts to parse a cron expression using the specified field layout, reporting the parse defect on failure.
    /// </summary>
    /// <param name="s">The cron expression text.</param>
    /// <param name="format">The expected field layout.</param>
    /// <param name="result">The parsed expression, or <see langword="null" /> on failure.</param>
    /// <param name="failureMessage">
    /// <see langword="null" /> on success; otherwise a message naming the specific defect, suitable for surfacing to
    /// the user verbatim.
    /// </param>
    /// <returns><see langword="true" /> if parsing succeeded; otherwise <see langword="false" />.</returns>
    public static bool TryParse(
        string? s,
        CronFormat format,
        [MaybeNullWhen(false)] out CronExpression result,
        [NotNullWhen(false)] out string? failureMessage)
    {
        if (s is null)
        {
            result = null;
            failureMessage = RecurrenceResourceStrings.Format_Invalid_CronEmpty;
            return false;
        }

        bool parsed = TryParseCore(s, format, out result, out failureMessage);
        if (!parsed)
        {
            failureMessage ??= string.Format(CultureInfo.CurrentCulture, RecurrenceResourceStrings.Format_Invalid_CronText, s);
        }

        return parsed;
    }

    /// <summary>
    /// Attempts to parse a cron expression using the specified field layout.
    /// </summary>
    /// <param name="s">The cron expression text.</param>
    /// <param name="format">The expected field layout.</param>
    /// <param name="result">The parsed expression, or <see langword="null" /> on failure.</param>
    /// <returns><see langword="true" /> if parsing succeeded; otherwise <see langword="false" />.</returns>
    public static bool TryParse([NotNullWhen(true)] string? s, CronFormat format, [MaybeNullWhen(false)] out CronExpression result)
    {
        if (s is null)
        {
            result = null;
            return false;
        }

        return TryParseCore(s, format, out result, out _);
    }

    /// <summary>
    /// Parses the expression, resolving macros and each field into a match set.
    /// </summary>
    /// <param name="text">The cron expression text.</param>
    /// <param name="requestedFormat">The required layout, or <see langword="null" /> to infer it.</param>
    /// <param name="result">The parsed expression, or <see langword="null" /> on failure.</param>
    /// <param name="failureMessage">
    /// <see langword="null" /> on success; otherwise a message naming the specific defect.
    /// </param>
    /// <returns><see langword="true" /> if parsing succeeded; otherwise <see langword="false" />.</returns>
    private static bool TryParseCore(
        string text,
        CronFormat? requestedFormat,
        [MaybeNullWhen(false)] out CronExpression result,
        out string? failureMessage)
    {
        result = null;
        failureMessage = null;

        string trimmed = text.Trim();
        if (trimmed.Length == 0)
        {
            failureMessage = RecurrenceResourceStrings.Format_Invalid_CronEmpty;
            return false;
        }

        if (trimmed[0] == '@')
        {
            return TryParseMacro(trimmed, requestedFormat, out result, out failureMessage);
        }

        string[] fields = trimmed.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        CronFormat format;
        switch (requestedFormat)
        {
            case null:
                format = fields.Length switch
                {
                    5 => CronFormat.Standard,
                    6 => CronFormat.WithSeconds,
                    _ => (CronFormat)(-1),
                };
                if (format == (CronFormat)(-1))
                {
                    failureMessage = RecurrenceResourceStrings.Format_Invalid_CronFieldCount;
                    return false;
                }

                break;

            case CronFormat.WithSeconds when fields.Length == 6:
                format = CronFormat.WithSeconds;
                break;

            case CronFormat.Standard when fields.Length == 5:
                format = CronFormat.Standard;
                break;

            default:
                failureMessage = RecurrenceResourceStrings.Format_Invalid_CronFieldCount;
                return false;
        }

        int index = 0;
        bool[] seconds = FullMask(60);
        if (format == CronFormat.WithSeconds && !TryParseField(fields[index++], 0, 59, null, false, seconds = new bool[60], out _, ref failureMessage))
        {
            return false;
        }

        var minutes = new bool[60];
        var hours = new bool[24];
        var daysOfMonth = new bool[32];
        var months = new bool[13];
        var daysOfWeek = new bool[7];

        if (!TryParseField(fields[index++], 0, 59, null, false, minutes, out _, ref failureMessage)
            || !TryParseField(fields[index++], 0, 23, null, false, hours, out _, ref failureMessage)
            || !TryParseDayOfMonthField(fields[index++], daysOfMonth, out bool domRestricted, out DayOfMonthToken? dayOfMonthToken, ref failureMessage)
            || !TryParseField(fields[index++], 1, 12, ResolveMonth, false, months, out _, ref failureMessage)
            || !TryParseDayOfWeekField(fields[index], daysOfWeek, out bool dowRestricted, out DayOfWeekToken? dayOfWeekToken, ref failureMessage))
        {
            return false;
        }

        result = new CronExpression(
            format,
            SecondSet.FromUInt64(ToBits(seconds, 0, 59)),
            MinuteSet.FromUInt64(ToBits(minutes, 0, 59)),
            HourSet.FromUInt64(ToBits(hours, 0, 23)),
            DayOfMonthSet.FromUInt64(ToBits(daysOfMonth, 1, 31)),
            MonthSet.FromUInt64(ToBits(months, 1, 12)),
            DayOfWeekSet.FromUInt64(ToBits(daysOfWeek, 0, 6)),
            domRestricted,
            dowRestricted,
            dayOfMonthToken,
            dayOfWeekToken);
        return true;
    }

    /// <summary>
    /// Resolves a macro token (for example <c>@daily</c>) to its equivalent standard expression.
    /// </summary>
    /// <param name="token">The macro token, including the leading <c>@</c>.</param>
    /// <param name="requestedFormat">The required layout, or <see langword="null" /> to infer it.</param>
    /// <param name="result">The parsed expression, or <see langword="null" /> on failure.</param>
    /// <param name="failureMessage">
    /// <see langword="null" /> on success; otherwise a message naming the specific defect.
    /// </param>
    /// <returns><see langword="true" /> if the macro was recognized; otherwise <see langword="false" />.</returns>
    private static bool TryParseMacro(
        string token,
        CronFormat? requestedFormat,
        [MaybeNullWhen(false)] out CronExpression result,
        out string? failureMessage)
    {
        result = null;

        // Macros always expand to the five-field standard layout.
        if (requestedFormat == CronFormat.WithSeconds)
        {
            failureMessage = string.Format(CultureInfo.CurrentCulture, RecurrenceResourceStrings.Format_Invalid_CronMacro, token);
            return false;
        }

        string? expanded = token.ToUpperInvariant() switch
        {
            "@YEARLY" or "@ANNUALLY" => "0 0 1 1 *",
            "@MONTHLY" => "0 0 1 * *",
            "@WEEKLY" => "0 0 * * 0",
            "@DAILY" or "@MIDNIGHT" => "0 0 * * *",
            "@HOURLY" => "0 * * * *",
            _ => null,
        };

        if (expanded is null)
        {
            failureMessage = string.Format(CultureInfo.CurrentCulture, RecurrenceResourceStrings.Format_Invalid_CronMacro, token);
            return false;
        }

        return TryParseCore(expanded, CronFormat.Standard, out result, out failureMessage);
    }

    /// <summary>
    /// Parses one cron field (comma-separated ranges and steps) into the supplied match mask.
    /// </summary>
    /// <param name="field">The field text.</param>
    /// <param name="min">The inclusive minimum value.</param>
    /// <param name="max">The inclusive maximum value (7 for the wrap-to-Sunday weekday field).</param>
    /// <param name="names">An optional name resolver for month or weekday tokens.</param>
    /// <param name="weekday">Whether the field is the weekday field, where <c>7</c> maps to Sunday.</param>
    /// <param name="mask">The mask to populate.</param>
    /// <param name="restricted">Set to <see langword="true" /> when the field is not <c>*</c>.</param>
    /// <param name="failureMessage">Set to a message naming the failing field when the field does not parse.</param>
    /// <returns><see langword="true" /> if the field parsed; otherwise <see langword="false" />.</returns>
    /// <remarks>
    /// The Quartz day tokens are read before this method sees a day field, so a token here, in any field, fails as the
    /// malformed value it is.
    /// </remarks>
    private static bool TryParseField(string field, int min, int max, Func<string, int?>? names, bool weekday, bool[] mask, out bool restricted, ref string? failureMessage)
    {
        // Vixie decides whether a day field is restricted from its leading character alone - cronie sets DOM_STAR /
        // DOW_STAR when the field begins with '*', before the field's values are parsed. A stepped star such as
        // "*/2" is therefore unrestricted while the equivalent explicit range "1-31/2" is restricted, even though
        // both denote the same days; the difference selects the union or intersection branch in DayMatches.
        restricted = field.Length > 0 && field[0] != '*';

        foreach (string part in field.Split(','))
        {
            if (part.Length == 0 || !TryParsePart(part, min, max, names, weekday, mask))
            {
                failureMessage = string.Format(CultureInfo.CurrentCulture, RecurrenceResourceStrings.Format_Invalid_CronField, field);
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Parses the day-of-month field, which holds values, <c>?</c>, or one of the Quartz tokens <c>L</c>, <c>L-n</c>,
    /// <c>LW</c>, <c>L-nW</c> and <c>nW</c>.
    /// </summary>
    /// <param name="field">The field text.</param>
    /// <param name="mask">The mask to populate when the field holds values.</param>
    /// <param name="restricted">Set to <see langword="true" /> when the field restricts the days.</param>
    /// <param name="token">Set to the token the field holds, or <see langword="null" /> when it holds values.</param>
    /// <param name="failureMessage">Set to a message naming the field when it does not parse.</param>
    /// <returns><see langword="true" /> if the field parsed; otherwise <see langword="false" />.</returns>
    private static bool TryParseDayOfMonthField(string field, bool[] mask, out bool restricted, out DayOfMonthToken? token, ref string? failureMessage)
    {
        if (TryParseDayOfMonthToken(field, out DayOfMonthToken parsed))
        {
            token = parsed;
            restricted = true;
            return true;
        }

        token = null;
        return TryParseField(field == "?" ? "*" : field, 1, 31, null, false, mask, out restricted, ref failureMessage);
    }

    /// <summary>
    /// Parses the day-of-week field, which holds values, <c>?</c>, or one of the Quartz tokens <c>dL</c> and <c>d#k</c>.
    /// </summary>
    /// <param name="field">The field text.</param>
    /// <param name="mask">The mask to populate when the field holds values.</param>
    /// <param name="restricted">Set to <see langword="true" /> when the field restricts the days.</param>
    /// <param name="token">Set to the token the field holds, or <see langword="null" /> when it holds values.</param>
    /// <param name="failureMessage">Set to a message naming the field when it does not parse.</param>
    /// <returns><see langword="true" /> if the field parsed; otherwise <see langword="false" />.</returns>
    private static bool TryParseDayOfWeekField(string field, bool[] mask, out bool restricted, out DayOfWeekToken? token, ref string? failureMessage)
    {
        if (TryParseDayOfWeekToken(field, out DayOfWeekToken parsed))
        {
            token = parsed;
            restricted = true;
            return true;
        }

        token = null;
        return TryParseField(field == "?" ? "*" : field, 0, 7, ResolveWeekday, true, mask, out restricted, ref failureMessage);
    }

    /// <summary>
    /// Attempts to read a day-of-month field that is wholly one Quartz token.
    /// </summary>
    /// <param name="field">The field text, in either case.</param>
    /// <param name="token">The token on success; otherwise the default.</param>
    /// <returns><see langword="true" /> when the field is a token; otherwise <see langword="false" />.</returns>
    /// <remarks>
    /// <c>L</c> stands for the last day, <c>L-n</c> for n days before it with n up to 30, a trailing <c>W</c> for the
    /// weekday nearest either, and <c>nW</c> for the weekday nearest day n, from 1 to 31. A token stands for the whole
    /// field, so <c>1,2W</c>, <c>1-2W</c> and <c>1/2W</c> are not tokens, and fail as values do.
    /// </remarks>
    private static bool TryParseDayOfMonthToken(string field, out DayOfMonthToken token)
    {
        token = default;
        if (field.Length == 0)
        {
            return false;
        }

        bool nearestWeekday = field[^1] is 'W' or 'w';
        ReadOnlySpan<char> body = nearestWeekday ? field.AsSpan(0, field.Length - 1) : field.AsSpan();
        if (body.Length > 0 && body[0] is 'L' or 'l')
        {
            int offset = 0;
            if (body.Length > 1
                && (body[1] != '-' || !int.TryParse(body[2..], NumberStyles.None, CultureInfo.InvariantCulture, out offset) || offset > 30))
            {
                return false;
            }

            token = new DayOfMonthToken(offset, FromEnd: true, NearestWeekday: nearestWeekday);
            return true;
        }

        if (nearestWeekday
            && int.TryParse(body, NumberStyles.None, CultureInfo.InvariantCulture, out int day)
            && day is >= 1 and <= 31)
        {
            token = new DayOfMonthToken(day, FromEnd: false, NearestWeekday: true);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Attempts to read a day-of-week field that is wholly one Quartz token.
    /// </summary>
    /// <param name="field">The field text, in either case.</param>
    /// <param name="token">The token on success; otherwise the default.</param>
    /// <returns><see langword="true" /> when the field is a token; otherwise <see langword="false" />.</returns>
    /// <remarks>
    /// <c>dL</c> stands for the month's last weekday d and <c>d#k</c> for its k-th, a single digit from 1 to 5, with d
    /// a number from 0 to 7 or a three-letter name. <c>L</c> alone, which Quartz reads as Saturday, is not a token
    /// here.
    /// </remarks>
    private static bool TryParseDayOfWeekToken(string field, out DayOfWeekToken token)
    {
        token = default;
        int hash = field.IndexOf('#', StringComparison.Ordinal);
        if (hash > 0)
        {
            if (field.Length != hash + 2
                || field[hash + 1] is < '1' or > '5'
                || !TryResolveValue(field[..hash], 0, 7, ResolveWeekday, out int weekday))
            {
                return false;
            }

            token = new DayOfWeekToken((DayOfWeek)(weekday % 7), (WeekOrdinal)(field[hash + 1] - '0'));
            return true;
        }

        if (field.Length > 1
            && field[^1] is 'L' or 'l'
            && TryResolveValue(field[..^1], 0, 7, ResolveWeekday, out int last))
        {
            token = new DayOfWeekToken((DayOfWeek)(last % 7), WeekOrdinal.Last);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Parses a single field element - <c>*</c>, a value, a range, or any of these with a <c>/step</c> - into the mask.
    /// </summary>
    /// <param name="part">The element text.</param>
    /// <param name="min">The inclusive minimum value.</param>
    /// <param name="max">The inclusive maximum value.</param>
    /// <param name="names">An optional name resolver.</param>
    /// <param name="weekday">Whether the field is the weekday field.</param>
    /// <param name="mask">The mask to populate.</param>
    /// <returns><see langword="true" /> if the element parsed; otherwise <see langword="false" />.</returns>
    private static bool TryParsePart(string part, int min, int max, Func<string, int?>? names, bool weekday, bool[] mask)
    {
        int step = 1;
        string range = part;
        int slash = part.IndexOf('/');
        if (slash >= 0)
        {
            if (!int.TryParse(part.AsSpan(slash + 1), NumberStyles.None, CultureInfo.InvariantCulture, out step) || step < 1)
            {
                return false;
            }

            range = part[..slash];
        }

        int lo;
        int hi;
        if (range == "*")
        {
            lo = min;
            hi = max;
        }
        else
        {
            int dash = range.IndexOf('-');
            if (dash > 0)
            {
                if (!TryResolveValue(range[..dash], min, max, names, out lo)
                    || !TryResolveValue(range[(dash + 1)..], min, max, names, out hi))
                {
                    return false;
                }
            }
            else
            {
                if (!TryResolveValue(range, min, max, names, out lo))
                {
                    return false;
                }

                hi = slash >= 0 ? max : lo;
            }
        }

        if (lo > hi)
        {
            return false;
        }

        for (int value = lo; value <= hi; value += step)
        {
            mask[weekday && value == 7 ? 0 : value] = true;
        }

        return true;
    }

    /// <summary>
    /// Resolves a single cron value token, which may be a number or a month/weekday name.
    /// </summary>
    /// <param name="token">The value token.</param>
    /// <param name="min">The inclusive minimum value.</param>
    /// <param name="max">The inclusive maximum value.</param>
    /// <param name="names">An optional name resolver.</param>
    /// <param name="value">The resolved value on success.</param>
    /// <returns>
    /// <see langword="true" /> if the token resolved within range; otherwise <see langword="false" />.
    /// </returns>
    private static bool TryResolveValue(string token, int min, int max, Func<string, int?>? names, out int value)
    {
        if (names is not null && names(token.ToUpperInvariant()) is int named)
        {
            value = named;
            return true;
        }

        return int.TryParse(token, NumberStyles.None, CultureInfo.InvariantCulture, out value) && value >= min && value <= max;
    }

    /// <summary>
    /// Resolves a three-letter month name to its one-based number.
    /// </summary>
    /// <param name="token">The uppercase month token.</param>
    /// <returns>The month number, or <see langword="null" /> when the token is not a month name.</returns>
    private static int? ResolveMonth(string token) =>
        token switch
        {
            "JAN" => 1,
            "FEB" => 2,
            "MAR" => 3,
            "APR" => 4,
            "MAY" => 5,
            "JUN" => 6,
            "JUL" => 7,
            "AUG" => 8,
            "SEP" => 9,
            "OCT" => 10,
            "NOV" => 11,
            "DEC" => 12,
            _ => null,
        };

    /// <summary>
    /// Resolves a three-letter weekday name to its zero-based number (Sunday is zero).
    /// </summary>
    /// <param name="token">The uppercase weekday token.</param>
    /// <returns>The weekday number, or <see langword="null" /> when the token is not a weekday name.</returns>
    private static int? ResolveWeekday(string token) =>
        token switch
        {
            "SUN" => 0,
            "MON" => 1,
            "TUE" => 2,
            "WED" => 3,
            "THU" => 4,
            "FRI" => 5,
            "SAT" => 6,
            _ => null,
        };

    /// <summary>
    /// Creates a mask of the given length with every entry set.
    /// </summary>
    /// <param name="length">The mask length.</param>
    /// <returns>The fully set mask.</returns>
    private static bool[] FullMask(int length)
    {
        var mask = new bool[length];
        Array.Fill(mask, true);
        return mask;
    }

    /// <summary>
    /// Packs a field mask into the bits of the field's set, bit n for the value <paramref name="min" /> + n.
    /// </summary>
    /// <param name="mask">The field mask, indexed by value.</param>
    /// <param name="min">The inclusive minimum value.</param>
    /// <param name="max">The inclusive maximum value.</param>
    /// <returns>The bits the field's set type reads through <c>FromUInt64</c>.</returns>
    private static ulong ToBits(bool[] mask, int min, int max)
    {
        ulong bits = 0;
        for (int value = min; value <= max; value++)
        {
            if (mask[value])
            {
                bits |= 1UL << (value - min);
            }
        }

        return bits;
    }
}
