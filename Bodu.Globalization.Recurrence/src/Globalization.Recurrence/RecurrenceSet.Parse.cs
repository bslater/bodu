// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RecurrenceSet.Parse.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Bodu.Globalization.Recurrence;

public sealed partial class RecurrenceSet : IParsable<RecurrenceSet>
{
    /// <summary>
    /// Parses an iCalendar property block containing a <c>DTSTART</c> line, one or more <c>RRULE</c> lines, and
    /// optional <c>RDATE</c> / <c>EXDATE</c> lines.
    /// </summary>
    /// <param name="s">
    /// The property-block text. Lines other than the recognized recurrence properties are ignored.
    /// </param>
    /// <returns>The parsed set.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="s" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="FormatException">Thrown when the text is not a valid recurrence-set property block.</exception>
    public static RecurrenceSet Parse(string s) =>
        Parse(s, null);

    /// <summary>
    /// Parses an iCalendar property block.
    /// </summary>
    /// <param name="s">
    /// The property-block text. Lines other than the recognized recurrence properties are ignored.
    /// </param>
    /// <param name="provider">Unused; property-block text is culture-invariant.</param>
    /// <returns>The parsed set.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="s" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="FormatException">Thrown when the text is not a valid recurrence-set property block.</exception>
    public static RecurrenceSet Parse(string s, IFormatProvider? provider)
    {
        ThrowHelper.ThrowIfNull(s);

        return TryParseCore(s, out RecurrenceSet? result)
            ? result
            : throw new FormatException(RecurrenceResourceStrings.Format_Invalid_RecurrenceSetText);
    }

    /// <summary>
    /// Attempts to parse an iCalendar property block.
    /// </summary>
    /// <param name="s">The property-block text.</param>
    /// <param name="result">The parsed set, or <see langword="null" /> on failure.</param>
    /// <returns><see langword="true" /> if parsing succeeded; otherwise <see langword="false" />.</returns>
    public static bool TryParse([NotNullWhen(true)] string? s, [MaybeNullWhen(false)] out RecurrenceSet result) =>
        TryParse(s, null, out result);

    /// <summary>
    /// Attempts to parse an iCalendar property block.
    /// </summary>
    /// <param name="s">The property-block text.</param>
    /// <param name="provider">Unused; property-block text is culture-invariant.</param>
    /// <param name="result">The parsed set, or <see langword="null" /> on failure.</param>
    /// <returns><see langword="true" /> if parsing succeeded; otherwise <see langword="false" />.</returns>
    public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, [MaybeNullWhen(false)] out RecurrenceSet result)
    {
        if (s is null)
        {
            result = null;
            return false;
        }

        return TryParseCore(s, out result);
    }

    /// <summary>
    /// Parses the property block and builds a set when a start plus at least one rule or explicit date is present.
    /// </summary>
    /// <param name="s">The property-block text.</param>
    /// <param name="result">The parsed set, or <see langword="null" /> on failure.</param>
    /// <returns><see langword="true" /> if parsing succeeded; otherwise <see langword="false" />.</returns>
    private static bool TryParseCore(string s, [MaybeNullWhen(false)] out RecurrenceSet result)
    {
        result = null;

        DateTime? start = null;
        var rules = new List<RecurrenceRule>();
        var dates = new List<DateTime>();
        var exceptionDates = new List<DateTime>();

        foreach (string rawLine in Unfold(s).Split('\n'))
        {
            string line = rawLine.Trim();
            if (line.Length == 0)
            {
                continue;
            }

            int colon = line.IndexOf(':');
            if (colon <= 0)
            {
                continue;
            }

            ReadOnlySpan<char> nameSpan = line.AsSpan(0, colon);
            int semicolon = nameSpan.IndexOf(';');
            if (semicolon >= 0)
            {
                nameSpan = nameSpan[..semicolon];
            }

            string name = nameSpan.Trim().ToString();
            string value = line[(colon + 1) ..].Trim();

            if (name.Equals("DTSTART", StringComparison.OrdinalIgnoreCase))
            {
                if (start is not null || !IcalValue.TryParseDateTime(value, out DateTime parsedStart))
                {
                    return false;
                }

                start = parsedStart;
            }
            else if (name.Equals("RRULE", StringComparison.OrdinalIgnoreCase))
            {
                if (!RecurrenceRule.TryParse(value, out RecurrenceRule? rule))
                {
                    return false;
                }

                rules.Add(rule);
            }
            else if (name.Equals("RDATE", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryParseDateList(value, dates))
                {
                    return false;
                }
            }
            else if (name.Equals("EXDATE", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryParseDateList(value, exceptionDates))
                {
                    return false;
                }
            }
        }

        if (start is null || (rules.Count == 0 && dates.Count == 0))
        {
            return false;
        }

        result = new RecurrenceSet(start.Value, rules, dates, exceptionDates);
        return true;
    }

    /// <summary>
    /// Parses a comma-separated list of iCalendar date-time values, appending each to <paramref name="target" />.
    /// </summary>
    /// <param name="value">The comma-separated value text.</param>
    /// <param name="target">The list the parsed instants are appended to.</param>
    /// <returns>
    /// <see langword="true" /> if the list was non-empty and every value parsed; otherwise <see langword="false" />.
    /// </returns>
    private static bool TryParseDateList(string value, List<DateTime> target)
    {
        string[] tokens = value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (tokens.Length == 0)
        {
            return false;
        }

        foreach (string token in tokens)
        {
            if (!IcalValue.TryParseDateTime(token, out DateTime parsed))
            {
                return false;
            }

            target.Add(parsed);
        }

        return true;
    }

    /// <summary>
    /// Reverses RFC 5545 line folding, joining each continuation line (one led by a space or tab) to its predecessor.
    /// </summary>
    /// <param name="text">The raw property-block text.</param>
    /// <returns>The unfolded text with normalized line endings.</returns>
    private static string Unfold(string text) =>
        text.Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace("\n ", string.Empty, StringComparison.Ordinal)
            .Replace("\n\t", string.Empty, StringComparison.Ordinal);
}
