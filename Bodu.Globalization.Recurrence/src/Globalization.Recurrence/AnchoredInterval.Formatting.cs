// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AnchoredInterval.Formatting.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Text;

namespace Bodu.Globalization.Recurrence;

public sealed partial class AnchoredInterval : IFormattable
{
    /// <summary>
    /// Returns the canonical RFC 5545 §3.3.6 duration text of the interval, such as <c>PT4H</c> or <c>P1DT2H30M</c>.
    /// </summary>
    /// <returns>The canonical duration text, which round-trips through <see cref="Parse(string)" />.</returns>
    /// <remarks>
    /// An interval that is an exact multiple of seven days renders in the weeks form (<c>P2W</c>); otherwise the
    /// normalized day and time components render with zero components omitted.
    /// </remarks>
    public override string ToString() =>
        Format();

    /// <summary>
    /// Returns the canonical RFC 5545 §3.3.6 duration text of the interval.
    /// </summary>
    /// <param name="format">
    /// The format specifier. Only the general specifier (<c>"G"</c> or <see langword="null" />) is supported.
    /// </param>
    /// <returns>The canonical duration text.</returns>
    /// <exception cref="FormatException">
    /// Thrown when <paramref name="format" /> is not a supported specifier.
    /// </exception>
    public string ToString(string? format) =>
        ToString(format, null);

    /// <summary>
    /// Returns the canonical RFC 5545 §3.3.6 duration text of the interval.
    /// </summary>
    /// <param name="format">
    /// The format specifier. Only the general specifier (<c>"G"</c> or <see langword="null" />) is supported.
    /// </param>
    /// <param name="formatProvider">Unused; duration text is culture-invariant.</param>
    /// <returns>The canonical duration text.</returns>
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
    /// Builds the canonical duration text from the interval's normalized components.
    /// </summary>
    /// <returns>The canonical duration text.</returns>
    private string Format()
    {
        long ticks = Interval.Ticks;
        if (ticks % (7 * TimeSpan.TicksPerDay) == 0)
        {
            return string.Create(
                CultureInfo.InvariantCulture,
                $"P{ticks / (7 * TimeSpan.TicksPerDay)}W");
        }

        var builder = new StringBuilder(16);
        builder.Append('P');

        if (Interval.Days > 0)
            builder.Append(Interval.Days.ToString(CultureInfo.InvariantCulture)).Append('D');

        if (Interval.Hours > 0 || Interval.Minutes > 0 || Interval.Seconds > 0)
        {
            builder.Append('T');
            if (Interval.Hours > 0)
                builder.Append(Interval.Hours.ToString(CultureInfo.InvariantCulture)).Append('H');
            if (Interval.Minutes > 0)
                builder.Append(Interval.Minutes.ToString(CultureInfo.InvariantCulture)).Append('M');
            if (Interval.Seconds > 0)
                builder.Append(Interval.Seconds.ToString(CultureInfo.InvariantCulture)).Append('S');
        }

        return builder.ToString();
    }
}
