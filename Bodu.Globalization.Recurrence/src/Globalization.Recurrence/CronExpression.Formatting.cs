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
    /// <remarks>
    /// Where the two day fields are concerned, Vixie reads restricted-ness from the field's leading character alone,
    /// and when both fields are restricted they combine by union rather than intersection. The canonical text
    /// therefore keeps a leading <c>*</c> on an unrestricted field that does not select every value (<c>*/2</c>), and
    /// keeps a restricted field explicit when it does (<c>1-31</c>), but only in the cases where the plain rendering
    /// would flip that combination — so the text always re-parses to an equal expression.
    /// </remarks>
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
        (string dayOfMonth, string dayOfWeek) = FormatDayFields();

        builder.Append(dayOfMonth).Append(' ');
        builder.Append(FormatField(_months, 1, 12)).Append(' ');
        builder.Append(dayOfWeek);

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

    /// <summary>
    /// Renders the two day fields together, so that the pair re-parses to the same combination mode.
    /// </summary>
    /// <returns>The rendered day-of-month and day-of-week field text.</returns>
    /// <remarks>
    /// The plain rendering implies a restricted-ness of its own — <c>*</c> for a mask that selects every value, a
    /// value list otherwise — and that is usually the right answer, so it is preferred whenever it reproduces
    /// <see cref="DaysCombineByUnion" />. Only when it would flip the combination does either field switch to the
    /// restriction-preserving spelling, which keeps the canonical text as close to the plain form as correctness allows.
    /// </remarks>
    private (string DayOfMonth, string DayOfWeek) FormatDayFields()
    {
        bool domSelectsEveryValue = SelectsEveryValue(_daysOfMonth, 1, 31);
        bool dowSelectsEveryValue = SelectsEveryValue(_daysOfWeek, 0, 6);

        // Re-parsing the plain rendering marks a field restricted exactly when it does not select every value.
        bool plainCombinesByUnion = !domSelectsEveryValue && !dowSelectsEveryValue;

        if (plainCombinesByUnion == DaysCombineByUnion)
        {
            return (FormatField(_daysOfMonth, 1, 31), FormatField(_daysOfWeek, 0, 6));
        }

        return (
            FormatDayField(_daysOfMonth, 1, 31, _domRestricted),
            FormatDayField(_daysOfWeek, 0, 6, _dowRestricted));
    }

    /// <summary>
    /// Determines whether a mask selects every value in its range.
    /// </summary>
    /// <param name="mask">The field mask.</param>
    /// <param name="min">The inclusive minimum value.</param>
    /// <param name="max">The inclusive maximum value.</param>
    /// <returns><see langword="true" /> when every value is selected; otherwise <see langword="false" />.</returns>
    private static bool SelectsEveryValue(bool[] mask, int min, int max)
    {
        for (int value = min; value <= max; value++)
        {
            if (!mask[value])
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Renders one of the two day fields, choosing a spelling whose leading character re-parses to the same
    /// restricted-ness as the field carries.
    /// </summary>
    /// <param name="mask">The field mask.</param>
    /// <param name="min">The inclusive minimum value.</param>
    /// <param name="max">The inclusive maximum value.</param>
    /// <param name="restricted">Whether the field is restricted.</param>
    /// <returns>The rendered field text.</returns>
    /// <remarks>
    /// <para>
    /// A restricted field must not begin with <c>*</c>, so a restricted field that happens to select every value is
    /// written as its explicit range <c>min-max</c> rather than as <c>*</c>.
    /// </para>
    /// <para>
    /// An unrestricted field must begin with <c>*</c>. Every leading <c>*</c> element selects <paramref name="min" />,
    /// so an unrestricted mask always contains it; when the mask is exactly the progression from
    /// <paramref name="min" /> at some step it is written as <c>*&#47;step</c>, and otherwise as the degenerate
    /// <c>*&#47;(max - min + 1)</c> — which selects <paramref name="min" /> alone — followed by the remaining values.
    /// </para>
    /// </remarks>
    private static string FormatDayField(bool[] mask, int min, int max, bool restricted)
    {
        var values = new List<int>();
        for (int value = min; value <= max; value++)
        {
            if (mask[value])
            {
                values.Add(value);
            }
        }

        bool selectsEveryValue = values.Count == max - min + 1;
        if (restricted)
        {
            return selectsEveryValue
                ? string.Create(CultureInfo.InvariantCulture, $"{min}-{max}")
                : string.Join(',', values.Select(v => v.ToString(CultureInfo.InvariantCulture)));
        }

        if (selectsEveryValue)
        {
            return "*";
        }

        // The mask came from a field led by "*" or "*/step", so min is selected and a step spelling may exist.
        int step = values.Count > 1 ? values[1] - values[0] : max - min + 1;
        bool isProgression = values.Count > 1
            && values.Select((v, i) => v == min + (i * step)).All(static matched => matched)
            && min + (values.Count * step) > max;

        if (isProgression)
        {
            return string.Create(CultureInfo.InvariantCulture, $"*/{step}");
        }

        // No single step describes the mask, so select min with a degenerate step and list the rest explicitly.
        IEnumerable<string> rest = values.Skip(1).Select(v => v.ToString(CultureInfo.InvariantCulture));
        return string.Join(',', new[] { string.Create(CultureInfo.InvariantCulture, $"*/{max - min + 1}") }.Concat(rest));
    }
}
