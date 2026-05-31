// ---------------------------------------------------------------------------------------------------------------
// <copyright file="WeekPattern.Formatting.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu;

public partial struct WeekPattern : System.IFormattable
{
    /// <summary>
    /// Returns a string representation of the current <see cref="WeekPattern" /> using the default Sunday-first format
    /// with underscore for unselected days.
    /// </summary>
    /// <returns>
    /// A seven-character string in Sunday-to-Saturday order where each selected day is represented by its initial
    /// letter and each unselected day by <c>'_'</c>.
    /// </returns>
    public override string ToString() => ToString("S", null!);

    /// <summary>
    /// Returns a string representation of the current <see cref="WeekPattern" /> using the specified format.
    /// </summary>
    /// <param name="format">
    /// A format string that defines the day ordering and the character used for unselected days. See
    /// <see cref="ToString(string, IFormatProvider)" /> for supported values.
    /// </param>
    /// <returns>A formatted string representing the selected days.</returns>
    /// <exception cref="ArgumentException">Thrown if <paramref name="format" /> is not recognized.</exception>
    public string ToString(string format) => ToString(format, null!);

    /// <summary>
    /// Returns a string representation of the current <see cref="WeekPattern" /> using the default format and the
    /// specified culture-specific formatting information.
    /// </summary>
    /// <param name="provider">
    /// An <see cref="IFormatProvider" /> supplying culture-specific formatting information (currently ignored).
    /// </param>
    /// <returns>
    /// A string representing the selected days in Sunday-to-Saturday order with <c>'_'</c> for unselected days.
    /// </returns>
    public string ToString(IFormatProvider provider) => ToString("S", provider);

    /// <summary>
    /// Returns a string representation of the current <see cref="WeekPattern" /> using the specified format and
    /// culture-specific formatting information.
    /// </summary>
    /// <param name="format">
    /// A format string determining day ordering and the unselected-day placeholder. Supported values:
    /// <list type="bullet">
    /// <item>
    /// <description>
    /// <c>'S'</c> or <c>'s'</c> — Sunday-to-Saturday; unselected = <c>'_'</c>.
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// <c>'M'</c> or <c>'m'</c> — Monday-to-Sunday; unselected = <c>'_'</c>.
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// <c>'E'</c>, <c>'U'</c>, <c>'D'</c>, <c>'A'</c> — Sunday-to-Saturday with space, underscore, dash, or asterisk
    /// for unselected days respectively.
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// <c>'0'</c>, <c>'1'</c>, <c>'B'</c>, or <c>"01"</c> — Binary: <c>'1'</c> selected, <c>'0'</c> unselected.
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// Two-character format: first character <c>'S'</c> or <c>'M'</c> for day ordering; second character <c>'E'</c>,
    /// <c>'U'</c>, <c>'D'</c>, or <c>'A'</c> for the unselected placeholder.
    /// </description>
    /// </item>
    /// </list>
    /// </param>
    /// <param name="provider">An <see cref="IFormatProvider" /> (currently ignored).</param>
    /// <returns>A formatted seven-character string representing the selected days.</returns>
    /// <exception cref="ArgumentException">Thrown if <paramref name="format" /> is not recognized.</exception>
    public string ToString(string? format, IFormatProvider? provider)
    {
        (var startDay, var unselectedChar, var isBinary) = ParseFormatForToString(format);
        unselectedChar ??= '_';
        var isMondayStart = startDay == 'M';
        var buffer = new char[7];

        for (var i = 0; i < 7; i++)
        {
            var dayIndex = isMondayStart ? (i + 1) % 7 : i;
            var selected = this[(DayOfWeek)dayIndex];

            buffer[i] = selected
                ? (isBinary ? '1' : WeekdaySymbols[dayIndex])
                : (isBinary ? '0' : unselectedChar.Value);
        }

        return new string(buffer);
    }
}
