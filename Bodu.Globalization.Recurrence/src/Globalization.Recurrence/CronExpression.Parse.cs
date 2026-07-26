// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CronExpression.Parse.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using System.Globalization;

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
    /// <exception cref="NotSupportedException">Thrown when the expression uses a Quartz extension token.</exception>
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
    /// <exception cref="NotSupportedException">Thrown when the expression uses a Quartz extension token.</exception>
    public static CronExpression Parse(string s, IFormatProvider? provider)
    {
        ThrowHelper.ThrowIfNull(s);

        if (TryParseCore(s, null, out CronExpression? result, out bool unsupported))
        {
            return result;
        }

        throw unsupported
            ? new NotSupportedException(
                string.Format(CultureInfo.CurrentCulture, RecurrenceResourceStrings.Op_NotSupported_CronExtensionToken, s))
            : new FormatException(
                string.Format(CultureInfo.CurrentCulture, RecurrenceResourceStrings.Format_Invalid_CronText, s));
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
    /// <exception cref="NotSupportedException">Thrown when the expression uses a Quartz extension token.</exception>
    public static CronExpression Parse(string s, CronFormat format)
    {
        ThrowHelper.ThrowIfNull(s);

        if (TryParseCore(s, format, out CronExpression? result, out bool unsupported))
        {
            return result;
        }

        throw unsupported
            ? new NotSupportedException(
                string.Format(CultureInfo.CurrentCulture, RecurrenceResourceStrings.Op_NotSupported_CronExtensionToken, s))
            : new FormatException(
                string.Format(CultureInfo.CurrentCulture, RecurrenceResourceStrings.Format_Invalid_CronText, s));
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
    /// <param name="unsupported">Set to <see langword="true" /> when a Quartz extension token is present.</param>
    /// <returns><see langword="true" /> if parsing succeeded; otherwise <see langword="false" />.</returns>
    private static bool TryParseCore(string text, CronFormat? requestedFormat, [MaybeNullWhen(false)] out CronExpression result, out bool unsupported)
    {
        result = null;
        unsupported = false;

        string trimmed = text.Trim();
        if (trimmed.Length == 0)
        {
            return false;
        }

        if (trimmed[0] == '@')
        {
            return TryParseMacro(trimmed, requestedFormat, out result);
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
                return false;
        }

        int index = 0;
        bool[] seconds = FullMask(60);
        if (format == CronFormat.WithSeconds && !TryParseField(fields[index++], 0, 59, null, false, seconds = new bool[60], out _, ref unsupported))
        {
            return false;
        }

        var minutes = new bool[60];
        var hours = new bool[24];
        var daysOfMonth = new bool[32];
        var months = new bool[13];
        var daysOfWeek = new bool[7];

        if (!TryParseField(fields[index++], 0, 59, null, false, minutes, out _, ref unsupported)
            || !TryParseField(fields[index++], 0, 23, null, false, hours, out _, ref unsupported)
            || !TryParseField(fields[index++], 1, 31, null, false, daysOfMonth, out bool domRestricted, ref unsupported)
            || !TryParseField(fields[index++], 1, 12, ResolveMonth, false, months, out _, ref unsupported)
            || !TryParseField(fields[index], 0, 7, ResolveWeekday, true, daysOfWeek, out bool dowRestricted, ref unsupported))
        {
            return false;
        }

        result = new CronExpression(format, seconds, minutes, hours, daysOfMonth, months, daysOfWeek, domRestricted, dowRestricted);
        return true;
    }

    /// <summary>
    /// Resolves a macro token (for example <c>@daily</c>) to its equivalent standard expression.
    /// </summary>
    /// <param name="token">The macro token, including the leading <c>@</c>.</param>
    /// <param name="requestedFormat">The required layout, or <see langword="null" /> to infer it.</param>
    /// <param name="result">The parsed expression, or <see langword="null" /> on failure.</param>
    /// <returns><see langword="true" /> if the macro was recognized; otherwise <see langword="false" />.</returns>
    private static bool TryParseMacro(string token, CronFormat? requestedFormat, [MaybeNullWhen(false)] out CronExpression result)
    {
        result = null;

        // Macros always expand to the five-field standard layout.
        if (requestedFormat == CronFormat.WithSeconds)
        {
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
            return false;
        }

        return TryParseCore(expanded, CronFormat.Standard, out result, out _);
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
    /// <param name="unsupported">Set to <see langword="true" /> when a Quartz extension token is present.</param>
    /// <returns><see langword="true" /> if the field parsed; otherwise <see langword="false" />.</returns>
    private static bool TryParseField(string field, int min, int max, Func<string, int?>? names, bool weekday, bool[] mask, out bool restricted, ref bool unsupported)
    {
        restricted = !field.Equals("*", StringComparison.Ordinal);

        if (field.AsSpan().IndexOfAny("LW#?") >= 0)
        {
            unsupported = true;
            return false;
        }

        foreach (string part in field.Split(','))
        {
            if (part.Length == 0 || !TryParsePart(part, min, max, names, weekday, mask))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Parses a single field element — <c>*</c>, a value, a range, or any of these with a <c>/step</c> — into the mask.
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
                    || !TryResolveValue(range[(dash + 1) ..], min, max, names, out hi))
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
}
