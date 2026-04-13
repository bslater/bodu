// --------------------------------------------------------------------------------------------------------------- //
// <copyright file="DaysOfWeekSet.Parse.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------- //

using System;

namespace Bodu;

public partial struct DaysOfWeekSet
{
    /// <summary>
    /// Converts the string representation of selected days into a corresponding <see cref="DaysOfWeekSet" /> instance, automatically
    /// inferring the format.
    /// </summary>
    /// <param name="input">The input string representing selected days. Must not be <see langword="null" />.</param>
    /// <returns>A new <see cref="DaysOfWeekSet" /> instance corresponding to the selected days.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="input" /> is <see langword="null" />.</exception>
    /// <exception cref="FormatException">
    /// Thrown if <paramref name="input" /> is not exactly 7 characters long, or contains invalid or unrecognised characters.
    /// </exception>
    /// <remarks>
    /// <para>
    /// The format is inferred from the length and character patterns of the input. For ambiguous inputs, the parser defaults to
    /// Sunday-first ordering with underscore ( <c>'_'</c>) for unselected days.
    /// </para>
    /// <para>To avoid ambiguity or enforce a specific format, use <see cref="ParseExact(string, string)" />.</para>
    /// </remarks>
    public static DaysOfWeekSet Parse(string input)
    {
        ThrowHelper.ThrowIfNull(input);

        return ParseCore(input, formatInfo: null);
    }

    /// <summary>
    /// Converts the string representation of selected days into a <see cref="DaysOfWeekSet" /> instance using the specified format.
    /// </summary>
    /// <param name="input">The input string representing selected days. Must not be <see langword="null" />.</param>
    /// <param name="format">
    /// A format string defining the day ordering and the symbol used for unselected days. Must not be <see langword="null" />.
    /// Supported values:
    /// <list type="bullet">
    /// <item>
    /// <description><c>'S'</c> or <c>'M'</c> — Sunday- or Monday-first, using underscore ( <c>'_'</c>) for unselected days.</description>
    /// </item>
    /// <item>
    /// <description>
    /// <c>'E'</c>, <c>'U'</c>, <c>'D'</c>, <c>'A'</c> — Sunday-first with space, underscore, dash, or asterisk for unselected days.
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// <c>'0'</c>, <c>'1'</c>, <c>'B'</c>, or <c>"01"</c> — Binary format with <c>'1'</c> for selected and <c>'0'</c> for unselected days.
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// Two-character formats: first character <c>'S'</c> or <c>'M'</c> (order), followed by <c>'E'</c>, <c>'U'</c>, <c>'D'</c>,
    /// or <c>'A'</c> (unselected-day symbol).
    /// </description>
    /// </item>
    /// </list>
    /// </param>
    /// <returns>A <see cref="DaysOfWeekSet" /> parsed according to the specified format.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="input" /> or <paramref name="format" /> is <see langword="null" />.</exception>
    /// <exception cref="FormatException">Thrown if <paramref name="input" /> or <paramref name="format" /> is invalid or unrecognised.</exception>
    public static DaysOfWeekSet ParseExact(string input, string format)
    {
        ThrowHelper.ThrowIfNull(input);

        return ParseCore(input, ParseFormatForParse(format));
    }

    /// <summary>
    /// Attempts to parse the string representation of selected days into a <see cref="DaysOfWeekSet" />, automatically inferring the
    /// format.
    /// </summary>
    /// <param name="input">The input string representing selected days.</param>
    /// <param name="result">
    /// When this method returns, contains the parsed <see cref="DaysOfWeekSet" /> if parsing succeeded; otherwise,
    /// <see cref="DaysOfWeekSet.Empty" />.
    /// </param>
    /// <returns><see langword="true" /> if parsing succeeded; otherwise, <see langword="false" />.</returns>
    /// <remarks>
    /// For ambiguous or invalid inputs, this method returns <see langword="false" /> rather than throwing. Use
    /// <see cref="TryParseExact(string, string, out DaysOfWeekSet)" /> for stricter format control.
    /// </remarks>
    public static bool TryParse(string input, out DaysOfWeekSet result)
    {
        try
        {
            result = ParseCore(input, formatInfo: null);
            return true;
        }
        catch (Exception ex) when (ex is FormatException or ArgumentNullException or ArgumentException)
        {
            result = DaysOfWeekSet.Empty;
            return false;
        }
    }

    /// <summary>
    /// Attempts to parse the string representation of selected days into a <see cref="DaysOfWeekSet" /> using the specified format.
    /// </summary>
    /// <param name="input">The input string representing selected days.</param>
    /// <param name="format">
    /// A format string defining the day ordering and the symbol used for unselected days. Supported values are the same as those
    /// accepted by <see cref="ParseExact(string, string)" />.
    /// </param>
    /// <param name="result">
    /// When this method returns, contains the parsed <see cref="DaysOfWeekSet" /> if parsing succeeded; otherwise,
    /// <see cref="DaysOfWeekSet.Empty" />.
    /// </param>
    /// <returns><see langword="true" /> if parsing succeeded; otherwise, <see langword="false" />.</returns>
    public static bool TryParseExact(string input, string format, out DaysOfWeekSet result)
    {
        try
        {
            result = ParseCore(input, ParseFormatForParse(format));
            return true;
        }
        catch (Exception ex) when (ex is FormatException or ArgumentNullException or ArgumentException)
        {
            result = DaysOfWeekSet.Empty;
            return false;
        }
    }
}
