// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CronExpression.Formatting.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Text;

namespace Bodu.Globalization.Recurrence;

public sealed partial class CronExpression : IFormattable
{
    /// <summary>
    /// Returns the canonical cron text of the expression, with each field rendered as <c>*</c> or a comma-separated
    /// list of numeric values.
    /// </summary>
    /// <returns>The canonical cron text, which round-trips through <see cref="Parse(string)" />.</returns>
    public override string ToString() =>
        FormatCore();

    /// <summary>
    /// Returns the canonical cron text of the expression.
    /// </summary>
    /// <param name="format">
    /// The format specifier. Only the general specifier (<c>"G"</c> or <see langword="null" />) is supported.
    /// </param>
    /// <param name="formatProvider">Unused; cron text is culture-invariant.</param>
    /// <returns>The canonical cron text.</returns>
    /// <exception cref="FormatException">
    /// Thrown when <paramref name="format" /> is not a supported specifier.
    /// </exception>
    public string ToString(string? format, IFormatProvider? formatProvider)
    {
        if (!string.IsNullOrEmpty(format) && !format.Equals("G", StringComparison.OrdinalIgnoreCase))
        {
            throw new FormatException(
                string.Format(CultureInfo.CurrentCulture, RecurrenceResourceStrings.Format_Invalid_RecurrenceRuleFormat, format));
        }

        return FormatCore();
    }

    /// <summary>
    /// Builds the canonical cron text from the field masks.
    /// </summary>
    /// <returns>The canonical cron text.</returns>
    private string FormatCore()
    {
        var builder = new StringBuilder(32);

        if (Format == CronFormat.WithSeconds)
        {
            builder.Append(FormatField(_seconds, 0, 59)).Append(' ');
        }

        builder.Append(FormatField(_minutes, 0, 59)).Append(' ');
        builder.Append(FormatField(_hours, 0, 23)).Append(' ');
        builder.Append(FormatField(_daysOfMonth, 1, 31)).Append(' ');
        builder.Append(FormatField(_months, 1, 12)).Append(' ');
        builder.Append(FormatField(_daysOfWeek, 0, 6));

        return builder.ToString();
    }

    /// <summary>
    /// Renders a single field as <c>*</c> when it spans its whole range, or a comma-separated value list otherwise.
    /// </summary>
    /// <param name="mask">The field mask.</param>
    /// <param name="min">The inclusive minimum value.</param>
    /// <param name="max">The inclusive maximum value.</param>
    /// <returns>The rendered field text.</returns>
    private static string FormatField(bool[] mask, int min, int max)
    {
        var values = new List<int>();
        for (int value = min; value <= max; value++)
        {
            if (mask[value])
            {
                values.Add(value);
            }
        }

        if (values.Count == max - min + 1)
        {
            return "*";
        }

        return string.Join(',', values.Select(v => v.ToString(CultureInfo.InvariantCulture)));
    }
}
