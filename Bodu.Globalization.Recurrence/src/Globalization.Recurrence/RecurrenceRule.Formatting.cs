// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RecurrenceRule.Formatting.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Text;

namespace Bodu.Globalization.Recurrence;

public sealed partial class RecurrenceRule : IFormattable
{
    /// <summary>
    /// Returns the canonical RFC 5545 text of the rule, such as <c>FREQ=WEEKLY;INTERVAL=2;BYDAY=MO,WE,FR</c>.
    /// </summary>
    /// <returns>The canonical rule text, which round-trips through <see cref="Parse(string)" />.</returns>
    public override string ToString() =>
        Format();

    /// <summary>
    /// Returns the canonical RFC 5545 text of the rule.
    /// </summary>
    /// <param name="format">
    /// The format specifier. Only the general specifier (<c>"G"</c> or <see langword="null" />) is supported.
    /// </param>
    /// <returns>The canonical rule text.</returns>
    /// <exception cref="FormatException">
    /// Thrown when <paramref name="format" /> is not a supported specifier.
    /// </exception>
    public string ToString(string? format) =>
        ToString(format, null);

    /// <summary>
    /// Returns the canonical RFC 5545 text of the rule.
    /// </summary>
    /// <param name="format">
    /// The format specifier. Only the general specifier (<c>"G"</c> or <see langword="null" />) is supported.
    /// </param>
    /// <param name="formatProvider">Unused; recurrence-rule text is culture-invariant.</param>
    /// <returns>The canonical rule text.</returns>
    /// <exception cref="FormatException">
    /// Thrown when <paramref name="format" /> is not a supported specifier.
    /// </exception>
    public string ToString(string? format, IFormatProvider? formatProvider)
    {
        if (!string.IsNullOrEmpty(format) && !format.Equals("G", StringComparison.OrdinalIgnoreCase))
            RecurrenceThrowHelper.ThrowRecurrenceRuleFormatUnsupported(format);

        return Format();
    }

    /// <summary>
    /// Builds the canonical rule text by appending each present rule part in RFC 5545 order.
    /// </summary>
    /// <returns>The canonical rule text.</returns>
    private string Format()
    {
        var builder = new StringBuilder(48);
        builder.Append("FREQ=").Append(FrequencyToken(Frequency));

        if (Interval != 1)
            builder.Append(";INTERVAL=").Append(Interval.ToString(CultureInfo.InvariantCulture));

        if (Count is int count)
            builder.Append(";COUNT=").Append(count.ToString(CultureInfo.InvariantCulture));

        if (Until is DateTime until)
            builder.Append(";UNTIL=").Append(IcalValue.FormatDateTime(until));

        AppendIntList(builder, "BYSECOND", _bySecond);
        AppendIntList(builder, "BYMINUTE", _byMinute);
        AppendIntList(builder, "BYHOUR", _byHour);
        AppendWeekDayList(builder);
        AppendIntList(builder, "BYMONTHDAY", _byMonthDay);
        AppendIntList(builder, "BYYEARDAY", _byYearDay);
        AppendIntList(builder, "BYWEEKNO", _byWeekNo);
        AppendIntList(builder, "BYMONTH", _byMonth);
        AppendIntList(builder, "BYSETPOS", _bySetPos);

        if (WeekStart != DayOfWeek.Monday)
            builder.Append(";WKST=").Append(IcalValue.FormatWeekDay(WeekStart));

        return builder.ToString();
    }

    /// <summary>
    /// Returns the RFC 5545 <c>FREQ</c> token for the specified frequency.
    /// </summary>
    /// <param name="frequency">The frequency to format.</param>
    /// <returns>The uppercase frequency token.</returns>
    private static string FrequencyToken(RecurrenceFrequency frequency) =>
        frequency switch
        {
            RecurrenceFrequency.Secondly => "SECONDLY",
            RecurrenceFrequency.Minutely => "MINUTELY",
            RecurrenceFrequency.Hourly => "HOURLY",
            RecurrenceFrequency.Daily => "DAILY",
            RecurrenceFrequency.Weekly => "WEEKLY",
            RecurrenceFrequency.Monthly => "MONTHLY",
            _ => "YEARLY",
        };

    /// <summary>
    /// Appends a named integer rule part as <c>;NAME=v1,v2,…</c>, or nothing when the part is empty.
    /// </summary>
    /// <param name="builder">The target builder.</param>
    /// <param name="name">The rule-part name.</param>
    /// <param name="values">The rule-part values.</param>
    private static void AppendIntList(StringBuilder builder, string name, int[] values)
    {
        if (values.Length == 0)
            return;

        builder.Append(';').Append(name).Append('=');
        for (int i = 0; i < values.Length; i++)
        {
            if (i > 0)
                builder.Append(',');
            builder.Append(values[i].ToString(CultureInfo.InvariantCulture));
        }
    }

    /// <summary>
    /// Appends the <c>BYDAY</c> rule part as <c>;BYDAY=[ord]DD,…</c>, or nothing when the part is empty.
    /// </summary>
    /// <param name="builder">The target builder.</param>
    private void AppendWeekDayList(StringBuilder builder)
    {
        if (_byDay.Length == 0)
            return;

        builder.Append(";BYDAY=");
        for (int i = 0; i < _byDay.Length; i++)
        {
            if (i > 0)
                builder.Append(',');

            WeekDayNum entry = _byDay[i];
            if (entry.Ordinal != 0)
                builder.Append(entry.Ordinal.ToString(CultureInfo.InvariantCulture));
            builder.Append(IcalValue.FormatWeekDay(entry.Day));
        }
    }
}
