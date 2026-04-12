// --------------------------------------------------------------------------------------------------------------- //
// <copyright file="DaysOfWeekSet.Formatting.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu;

public partial struct DaysOfWeekSet
{
    /// <summary>
    /// Returns a string representation of the current <see cref="DaysOfWeekSet" /> using the default format.
    /// </summary>
    /// <returns>A string representing the selected days in Sunday-to-Saturday order, using underscore ( <c>'_'</c>) for unselected days.</returns>
    public override string ToString() => ToString("S", null!);

    /// <summary>
    /// Returns a string representation of the current <see cref="DaysOfWeekSet" /> using the specified format.
    /// </summary>
    /// <param name="format">A format string that defines the day ordering and symbol used for unselected days.</param>
    /// <returns>A formatted string representing the selected days.</returns>
    /// <exception cref="ArgumentException">Thrown if the <paramref name="format" /> is invalid.</exception>
    /// <remarks>
    /// <para>Use this overload for convenience when culture-specific formatting is not required.</para>
    /// <para>For formatting details, see <see cref="ToString(string, IFormatProvider)" />.</para>
    /// <code language="csharp">
    ///<![CDATA[
    /// Create a set with Monday, Wednesday, and Friday selected
    /// var partialWeek = new DaysOfWeekSet(DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday);
    ///
    /// Format as Sunday-first with underscores: " M_W_F_"
    /// string result1 = partialWeek.ToString("S");  // e.g., "S_M_W_F"
    ///
    /// Format as Monday-first with dashes for unselected: "M-W-F--"
    /// string result2 = partialWeek.ToString("MD");
    ///
    /// Format using binary representation: "0110100"
    /// string result3 = partialWeek.ToString("01");
    ///]]>
    /// </code>
    /// </remarks>
    public string ToString(string format) => ToString(format, null!);

    /// <summary>
    /// Returns a string representation of the current <see cref="DaysOfWeekSet" /> using the default format and the specified
    /// culture-specific formatting information.
    /// </summary>
    /// <param name="provider">An <see cref="IFormatProvider" /> supplying culture-specific formatting information (currently ignored).</param>
    /// <returns>A string representing the selected days in Sunday-to-Saturday order using underscore ( <c>'_'</c>) for unselected days.</returns>
    public string ToString(IFormatProvider provider) => ToString("S", provider);

    /// <summary>
    /// Returns a string representation of the current <see cref="DaysOfWeekSet" /> using the specified format and culture-specific
    /// formatting information.
    /// </summary>
    /// <param name="format">A format string that defines the day ordering and symbol used for unselected days.</param>
    /// <param name="provider">An <see cref="IFormatProvider" /> supplying culture-specific formatting information (currently ignored).</param>
    /// <returns>A formatted string representing the selected days.</returns>
    /// <exception cref="ArgumentException">Thrown if the <paramref name="format" /> is invalid.</exception>
    /// <remarks>
    /// The <paramref name="format" /> string determines the order of days and the character used for unselected days.
    /// <para>Supported formats include:</para>
    /// <list type="bullet">
    /// <item>
    /// <description><c>'S'</c> or <c>'s'</c> - Sunday-to-Saturday using weekday symbols; unselected days = <c>'_'</c></description>
    /// </item>
    /// <item>
    /// <description><c>'M'</c> or <c>'m'</c> - Monday-to-Sunday using weekday symbols; unselected days = <c>'_'</c></description>
    /// </item>
    /// <item>
    /// <description>
    /// <c>'E'</c>, <c>'U'</c>, <c>'D'</c>, or <c>'A'</c> - Sunday-to-Saturday using weekday symbols and a specific symbol for
    /// unselected days:
    /// <list type="bullet">
    /// <item>
    /// <description><c>'E'</c> - space ( <c>' '</c>)</description>
    /// </item>
    /// <item>
    /// <description><c>'U'</c> - underscore ( <c>'_'</c>)</description>
    /// </item>
    /// <item>
    /// <description><c>'D'</c> - dash ( <c>'-'</c>)</description>
    /// </item>
    /// <item>
    /// <description><c>'A'</c> - asterisk ( <c>'*'</c>)</description>
    /// </item>
    /// </list>
    /// </description>
    /// </item>
    /// <item>
    /// <description><c>'0'</c> or <c>'1'</c> - Binary format with <c>'1'</c> = selected, <c>'0'</c> = unselected</description>
    /// </item>
    /// <item>
    /// <description>Two-character format:
    /// <list type="bullet">
    /// <item>
    /// <description>First character: <c>'S'</c> or <c>'M'</c> for Sunday- or Monday-first</description>
    /// </item>
    /// <item>
    /// <description>Second character: <c>'E'</c>, <c>'U'</c>, <c>'D'</c>, or <c>'A'</c> for unselected-day symbol</description>
    /// </item>
    /// </list>
    /// </description>
    /// </item>
    /// </list>
    /// <code language="csharp">
    ///<![CDATA[
    /// Create a set with Monday through Friday selected
    /// var weekdays = new DaysOfWeekSet(DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday,
    ///                                   DayOfWeek.Thursday, DayOfWeek.Friday);
    ///
    /// Format as "MTWTF__" (Monday-first, unselected = underscore)
    /// string monFirst = weekdays.ToString("M", null);   // "MTWTF__"
    ///
    /// Format as "MTWTF  " (Monday-first, unselected = space)
    /// string monFirstSpaces = weekdays.ToString("ME", null); // "MTWTF  "
    ///
    /// Format as "MTWTF--" (Monday-first, unselected = dash)
    /// string monFirstDash = weekdays.ToString("MD", null); // "MTWTF--"
    ///
    /// Format as "S_____S" (Sunday and Saturday selected)
    /// var weekend = new DaysOfWeekSet(DayOfWeek.Saturday, DayOfWeek.Sunday);
    /// string sunFirst = weekend.ToString("S", null);    // "S_____S"
    ///
    /// Format as binary: selected = '1', unselected = '0'
    /// string binary = weekdays.ToString("01", null);    // "0111110"
    ///]]>
    /// </code>
    /// </remarks>
    public string ToString(string format, IFormatProvider provider)
    {
        (char? startDay, char? unselectedChar, bool isBinary) = ParseFormatForToString(format);
        unselectedChar ??= '_'; // Default to underscore if not specified
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
    /// Parses the format string for use in <see cref="ToString(string, IFormatProvider)" />, throwing <see cref="ArgumentException" /> if invalid.
    /// </summary>
    private static (char? startDay, char? unselectedChar, bool isBinary) ParseFormatForToString(string format)
    {
        format ??= "S";
        if (!TryParseFormatInfo(format, out (char? startDay, char? unselectedChar, bool isBinary) info))
            throw new ArgumentException(ResourceStrings.Arg_Invalid_FormatString, nameof(format));

        return info;
    }
}
