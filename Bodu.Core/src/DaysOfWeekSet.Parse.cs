// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DaysOfWeekSet.Parse.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu;

public partial struct DaysOfWeekSet
{
    /// <summary>
    /// Converts the string representation of selected days into a corresponding <see cref="DaysOfWeekSet" /> instance, automatically
    /// inferring the format if none is specified.
    /// </summary>
    /// <param name="input">The input string that represents selected days.</param>
    /// <returns>A new <see cref="DaysOfWeekSet" /> instance corresponding to the selected days.</returns>
    /// <exception cref="FormatException">
    /// Thrown if the <paramref name="input" /> is <see langword="null" />, incorrectly formatted, or cannot be parsed.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The format will be inferred from the length and character patterns of the input. For ambiguous inputs, the parser defaults to
    /// Sunday-first ordering with underscore ( <c>'_'</c>) for unselected days.
    /// </para>
    /// <para>To avoid ambiguity or ensure specific formatting, use <see cref="ParseExact(string, string)" /> with an explicit format string.</para>
    /// </remarks>
    public static DaysOfWeekSet Parse(string input)
    {
        ThrowHelper.ThrowIfNull(input);

        return ParseCore(input, formatInfo: null);
    }

    /// <summary>
    /// Converts the string representation of selected days into a <see cref="DaysOfWeekSet" /> instance using a specified format.
    /// </summary>
    /// <param name="input">The input string that represents selected days.</param>
    /// <param name="format">
    /// A format string that defines the day ordering and symbol used for unselected days. Supported values:
    /// <list type="bullet">
    /// <item>
    /// <description><c>'S'</c> or <c>'M'</c> - Sunday- or Monday-first, using underscore ( <c>'_'</c>) for unselected days.</description>
    /// </item>
    /// <item>
    /// <description>
    /// <c>'E'</c>, <c>'U'</c>, <c>'D'</c>, <c>'A'</c> - Sunday-first with space, underscore, dash, or asterisk for unselected days, respectively.
    /// </description>
    /// </item>
    /// <item>
    /// <description><c>'0'</c> or <c>'1'</c> - Binary format with <c>'1'</c> for selected and <c>'0'</c> for unselected days.</description>
    /// </item>
    /// <item>
    /// <description>Two-character formats:
    /// <list type="bullet">
    /// <item>
    /// <description>First character: <c>'S'</c> or <c>'M'</c></description>
    /// </item>
    /// <item>
    /// <description>Second character: <c>'E'</c>, <c>'U'</c>, <c>'D'</c>, or <c>'A'</c> (space, underscore, dash, or asterisk).</description>
    /// </item>
    /// </list>
    /// </description>
    /// </item>
    /// </list>
    /// </param>
    /// <returns>A <see cref="DaysOfWeekSet" /> parsed according to the specified format.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="input" /> or <paramref name="format" /> is <see langword="null" />.</exception>
    /// <exception cref="FormatException">Thrown if <paramref name="input" /> or <paramref name="format" /> are invalid or unrecognized.</exception>
    public static DaysOfWeekSet ParseExact(string input, string format)
    {
        ThrowHelper.ThrowIfNull(input);

        return ParseCore(input, ParseFormatForParse(format));
    }

    /// <summary>
    /// Attempts to parse the string representation of selected days into a <see cref="DaysOfWeekSet" />, automatically inferring the format
    /// if none is specified.
    /// </summary>
    /// <param name="input">The input string that represents selected days.</param>
    /// <param name="result">
    /// When this method returns, contains the parsed <see cref="DaysOfWeekSet" /> if parsing succeeded; otherwise, contains <see cref="DaysOfWeekSet.Empty" />.
    /// </param>
    /// <returns><c>true</c> if parsing succeeded; otherwise, <c>false</c>.</returns>
    /// <remarks>
    /// For ambiguous or invalid formats, the parser falls back to Sunday-first with underscore ( <c>'_'</c>) for unselected days. Use
    /// <see cref="TryParseExact(string, string, out DaysOfWeekSet)" /> for stricter format control.
    /// </remarks>
    public static bool TryParse(string input, out DaysOfWeekSet result)
    {
        try
        {
            result = ParseCore(input, formatInfo: null);
            return true;
        }
        catch
        {
            result = DaysOfWeekSet.Empty;
            return false;
        }
    }

    /// <summary>
    /// Attempts to parse the string representation of selected days into a <see cref="DaysOfWeekSet" /> using a specified format.
    /// </summary>
    /// <param name="input">The input string that represents selected days.</param>
    /// <param name="format">
    /// A format string that defines the day ordering and symbol used for unselected days. Supported values:
    /// <list type="bullet">
    /// <item>
    /// <description><c>'S'</c> or <c>'M'</c> - Sunday- or Monday-first, using underscore ( <c>'_'</c>) for unselected days.</description>
    /// </item>
    /// <item>
    /// <description>
    /// <c>'E'</c>, <c>'U'</c>, <c>'D'</c>, <c>'A'</c> - Sunday-first with space, underscore, dash, or asterisk for unselected days, respectively.
    /// </description>
    /// </item>
    /// <item>
    /// <description><c>'0'</c> or <c>'1'</c> - Binary format with <c>'1'</c> for selected and <c>'0'</c> for unselected days.</description>
    /// </item>
    /// <item>
    /// <description>Two-character formats:
    /// <list type="bullet">
    /// <item>
    /// <description>First character: <c>'S'</c> or <c>'M'</c></description>
    /// </item>
    /// <item>
    /// <description>Second character: <c>'E'</c>, <c>'U'</c>, <c>'D'</c>, or <c>'A'</c></description>
    /// </item>
    /// </list>
    /// </description>
    /// </item>
    /// </list>
    /// </param>
    /// <param name="result">
    /// When this method returns, contains the parsed <see cref="DaysOfWeekSet" /> if parsing succeeded; otherwise, contains <see cref="DaysOfWeekSet.Empty" />.
    /// </param>
    /// <returns><c>true</c> if parsing succeeded; otherwise, <c>false</c>.</returns>
    public static bool TryParseExact(string input, string format, out DaysOfWeekSet result)
    {
        try
        {
            result = ParseCore(input, ParseFormatForParse(format));
            return true;
        }
        catch
        {
            result = DaysOfWeekSet.Empty;
            return false;
        }
    }
}
