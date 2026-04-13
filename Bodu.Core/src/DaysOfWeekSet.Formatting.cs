// --------------------------------------------------------------------------------------------------------------- //
// <copyright file="DaysOfWeekSet.Formatting.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------- //

using System;

namespace Bodu;

public partial struct DaysOfWeekSet :
    System.IFormattable
{
    /// <summary>
    /// Returns a string representation of the current <see cref="DaysOfWeekSet" /> using the default format.
    /// </summary>
    /// <returns>
    /// A string representing the selected days in Sunday-to-Saturday order, using the weekday initial for selected days and
    /// underscore ( <c>'_'</c>) for unselected days.
    /// </returns>
    public override string ToString() => this.ToString("S", null!);

    /// <summary>
    /// Returns a string representation of the current <see cref="DaysOfWeekSet" /> using the specified format.
    /// </summary>
    /// <param name="format">
    /// A format string that defines the day ordering and the symbol used for unselected days. See
    /// <see cref="ToString(string, IFormatProvider)" /> for supported values.
    /// </param>
    /// <returns>A formatted string representing the selected days.</returns>
    /// <exception cref="ArgumentException">Thrown if <paramref name="format" /> is not a recognised format string.</exception>
    /// <remarks>
    /// <para>Use this overload for convenience when culture-specific formatting is not required.</para>
    /// <para>For full format documentation, see <see cref="ToString(string, IFormatProvider)" />.</para>
    /// <code language="csharp">
    ///<![CDATA[
    /// // Create a set with Monday, Wednesday, and Friday selected
    /// var partialWeek = new DaysOfWeekSet(DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday);
    ///
    /// // Sunday-first with underscores for unselected days
    /// string result1 = partialWeek.ToString("S");   // "_M_W_F_"
    ///
    /// // Monday-first with dashes for unselected days
    /// string result2 = partialWeek.ToString("MD");  // "M-W-F--"
    ///
    /// // Binary: '1' for selected, '0' for unselected, Sunday-first
    /// string result3 = partialWeek.ToString("0");   // "0101010"
    ///]]>
    /// </code>
    /// </remarks>
    public string ToString(string format) => this.ToString(format, null!);

    /// <summary>
    /// Returns a string representation of the current <see cref="DaysOfWeekSet" /> using the default format and the specified
    /// culture-specific formatting information.
    /// </summary>
    /// <param name="provider">An <see cref="IFormatProvider" /> supplying culture-specific formatting information (currently unused).</param>
    /// <returns>
    /// A string representing the selected days in Sunday-to-Saturday order, using the weekday initial for selected days and
    /// underscore ( <c>'_'</c>) for unselected days.
    /// </returns>
    public string ToString(IFormatProvider provider) => this.ToString("S", provider);

    /// <summary>
    /// Returns a string representation of the current <see cref="DaysOfWeekSet" /> using the specified format and culture-specific
    /// formatting information.
    /// </summary>
    /// <param name="format">A format string that defines the day ordering and the symbol used for unselected days.</param>
    /// <param name="formatProvider">An <see cref="IFormatProvider" /> supplying culture-specific formatting information (currently unused).</param>
    /// <returns>A formatted string representing the selected days.</returns>
    /// <exception cref="ArgumentException">Thrown if <paramref name="format" /> is not a recognised format string.</exception>
    /// <remarks>
    /// <para>The <paramref name="format" /> string controls both the ordering of days and the character used for unselected positions.</para>
    /// <para>Supported single-character formats:</para>
    /// <list type="bullet">
    /// <item>
    /// <description><c>'S'</c> or <c>'s'</c> — Sunday-to-Saturday order; weekday initial for selected, <c>'_'</c> for unselected.</description>
    /// </item>
    /// <item>
    /// <description><c>'M'</c> or <c>'m'</c> — Monday-to-Sunday order; weekday initial for selected, <c>'_'</c> for unselected.</description>
    /// </item>
    /// <item>
    /// <description>
    /// <c>'E'</c>, <c>'U'</c>, <c>'D'</c>, or <c>'A'</c> — Sunday-to-Saturday order with a specific unselected-day symbol:
    /// space ( <c>'E'</c>), underscore ( <c>'U'</c>), dash ( <c>'D'</c>), or asterisk ( <c>'A'</c>).
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// <c>'0'</c>, <c>'1'</c>, or <c>'B'</c> — Binary format: <c>'1'</c> for selected, <c>'0'</c> for unselected, in Sunday-to-Saturday order.
    /// </description>
    /// </item>
    /// </list>
    /// <para>Supported two-character formats:</para>
    /// <list type="bullet">
    /// <item>
    /// <description>
    /// First character <c>'S'</c> or <c>'M'</c> (order), followed by <c>'E'</c>, <c>'U'</c>, <c>'D'</c>, or <c>'A'</c>
    /// (unselected-day symbol). For example, <c>"MD"</c> gives Monday-first with dashes for unselected days.
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// <c>"01"</c> — Binary format equivalent to <c>'0'</c>, <c>'1'</c>, or <c>'B'</c>.
    /// </description>
    /// </item>
    /// </list>
    /// <para>A <see langword="null" /> or empty <paramref name="format" /> defaults to <c>'S'</c>.</para>
    /// <code language="csharp">
    ///<![CDATA[
    /// // Create a set with Monday through Friday selected
    /// var weekdays = new DaysOfWeekSet(DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday,
    ///                                   DayOfWeek.Thursday, DayOfWeek.Friday);
    ///
    /// // Monday-first, underscore for unselected
    /// string monFirst = weekdays.ToString("M", null);        // "MTWTF__"
    ///
    /// // Monday-first, space for unselected
    /// string monFirstSpaces = weekdays.ToString("ME", null); // "MTWTF  "
    ///
    /// // Monday-first, dash for unselected
    /// string monFirstDash = weekdays.ToString("MD", null);   // "MTWTF--"
    ///
    /// // Sunday-first with underscore for unselected (Saturday and Sunday selected)
    /// var weekend = new DaysOfWeekSet(DayOfWeek.Saturday, DayOfWeek.Sunday);
    /// string sunFirst = weekend.ToString("S", null);         // "S_____S"
    ///
    /// // Binary: '1' for selected, '0' for unselected, Sunday-first
    /// string binary = weekdays.ToString("01", null);         // "0111110"
    ///]]>
    /// </code>
    /// </remarks>
    public string ToString(string? format, IFormatProvider? formatProvider)
    {
        (char? startDay, char? unselectedChar, bool isBinary) = ParseFormatForToString(format);
        unselectedChar ??= '_'; // Default to underscore when no unselected-day symbol is specified
        bool isMondayStart = startDay == 'M';
        char[] buffer = new char[7];

        for (int i = 0; i < 7; i++)
        {
            int dayIndex = isMondayStart ? (i + 1) % 7 : i;
            bool selected = this[dayIndex];

            buffer[i] = selected
                ? (isBinary ? '1' : WeekdaySymbols[dayIndex])
                : (isBinary ? '0' : unselectedChar.Value);
        }

        return new string(buffer);
    }

    /// <summary>
    /// Resolves the format string for use in <see cref="ToString(string, IFormatProvider)" />, throwing
    /// <see cref="ArgumentException" /> if the format is invalid.
    /// </summary>
    private static (char? startDay, char? unselectedChar, bool isBinary) ParseFormatForToString(string? format)
    {
        format ??= "S";
        if (!TryParseFormatInfo(format, out (char? startDay, char? unselectedChar, bool isBinary) info))
            throw new ArgumentException(ResourceStrings.Arg_Invalid_FormatString, nameof(format));

        return info;
    }
}
