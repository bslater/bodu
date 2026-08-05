// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RecurrenceSet.Formatting.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

namespace Bodu.Globalization.Recurrence;

public sealed partial class RecurrenceSet : IFormattable
{
    /// <summary>
    /// Returns the canonical iCalendar property-block text of the set: a <c>DTSTART</c> line, one <c>RRULE</c> line per
    /// rule, and <c>RDATE</c> / <c>EXDATE</c> lines when explicit or exception dates are present.
    /// </summary>
    /// <returns>The canonical property-block text, which round-trips through <see cref="Parse(string)" />.</returns>
    /// <remarks>
    /// Lines are separated by CRLF per RFC 5545, with no trailing line break. Explicit and exception dates each render
    /// as a single comma-separated line in ascending order.
    /// </remarks>
    public override string ToString() =>
        Format();

    /// <summary>
    /// Returns the canonical iCalendar property-block text of the set.
    /// </summary>
    /// <param name="format">
    /// The format specifier. Only the general specifier (<c>"G"</c> or <see langword="null" />) is supported.
    /// </param>
    /// <returns>The canonical property-block text.</returns>
    /// <exception cref="FormatException">
    /// Thrown when <paramref name="format" /> is not a supported specifier.
    /// </exception>
    public string ToString(string? format) =>
        ToString(format, null);

    /// <summary>
    /// Returns the canonical iCalendar property-block text of the set.
    /// </summary>
    /// <param name="format">
    /// The format specifier. Only the general specifier (<c>"G"</c> or <see langword="null" />) is supported.
    /// </param>
    /// <param name="formatProvider">Unused; property-block text is culture-invariant.</param>
    /// <returns>The canonical property-block text.</returns>
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
    /// Builds the canonical property-block text from the set's components.
    /// </summary>
    /// <returns>The canonical property-block text.</returns>
    private string Format()
    {
        var builder = new StringBuilder(64);
        builder.Append("DTSTART:").Append(IcalValue.FormatDateTime(Start));

        foreach (RecurrenceRule rule in _rules)
        {
            builder.Append("\r\nRRULE:").Append(rule.ToString());
        }

        AppendDateList(builder, "RDATE", _dates);
        AppendDateList(builder, "EXDATE", _exceptionDates);

        return builder.ToString();
    }

    /// <summary>
    /// Appends a named date-list property line as <c>NAME:v1,v2,…</c>, or nothing when the list is empty.
    /// </summary>
    /// <param name="builder">The target builder.</param>
    /// <param name="name">The property name.</param>
    /// <param name="values">The property's instants.</param>
    private static void AppendDateList(StringBuilder builder, string name, DateTime[] values)
    {
        if (values.Length == 0)
            return;

        builder.Append("\r\n").Append(name).Append(':');
        for (int i = 0; i < values.Length; i++)
        {
            if (i > 0)
                builder.Append(',');
            builder.Append(IcalValue.FormatDateTime(values[i]));
        }
    }
}
