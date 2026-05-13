// ---------------------------------------------------------------------------------------------------------------
// <copyright file="WeekPattern.Parse.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu;

public partial struct WeekPattern
{
    /// <summary>
    /// Converts the string representation of selected days into a <see cref="WeekPattern"/>, automatically
    /// inferring the format.
    /// </summary>
    /// <param name="input">The input string that represents selected days. Must not be <see langword="null"/>.</param>
    /// <returns>A <see cref="WeekPattern"/> corresponding to the selected days in <paramref name="input"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="input"/> is <see langword="null"/>.</exception>
    /// <exception cref="FormatException">
    /// Thrown if <paramref name="input"/> is incorrectly formatted or cannot be parsed.
    /// </exception>
    /// <remarks>
    /// The format is inferred from the length and character patterns of the input. For unambiguous
    /// parsing, use <see cref="ParseExact(string, string)"/> with an explicit format string.
    /// </remarks>
    public static WeekPattern Parse(string input)
    {
        ThrowHelper.ThrowIfNull(input);
        return ParseCore(input, formatInfo: null);
    }

    /// <summary>
    /// Converts the string representation of selected days into a <see cref="WeekPattern"/> using a
    /// specified format.
    /// </summary>
    /// <param name="input">The input string that represents selected days. Must not be <see langword="null"/>.</param>
    /// <param name="format">
    /// A format string that defines the day ordering and the character used for unselected days. See
    /// <see cref="WeekPattern.ToString(string, IFormatProvider)"/> for supported values. Must not be
    /// <see langword="null"/>.
    /// </param>
    /// <returns>A <see cref="WeekPattern"/> parsed according to the specified format.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="input"/> or <paramref name="format"/> is <see langword="null"/>.</exception>
    /// <exception cref="FormatException">Thrown if <paramref name="input"/> or <paramref name="format"/> is invalid or unrecognized.</exception>
    public static WeekPattern ParseExact(string input, string format)
    {
        ThrowHelper.ThrowIfNull(input);
        return ParseCore(input, ParseFormatForParse(format));
    }

    /// <summary>
    /// Attempts to parse the string representation of selected days into a <see cref="WeekPattern"/>,
    /// automatically inferring the format.
    /// </summary>
    /// <param name="input">The input string that represents selected days.</param>
    /// <param name="result">
    /// When this method returns, contains the parsed <see cref="WeekPattern"/> if parsing succeeded;
    /// otherwise, contains <see cref="WeekPattern.Empty"/>.
    /// </param>
    /// <returns><see langword="true"/> if parsing succeeded; otherwise, <see langword="false"/>.</returns>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "Argument validation is performed centrally in ParseCore.")]
    public static bool TryParse(string input, out WeekPattern result)
    {
        if (input is null)
        {
            result = WeekPattern.Empty;
            return false;
        }

        try
        {
            result = ParseCore(input, formatInfo: null);
            return true;
        }
        catch
        {
            result = WeekPattern.Empty;
            return false;
        }
    }

    /// <summary>
    /// Attempts to parse the string representation of selected days into a <see cref="WeekPattern"/>
    /// using a specified format.
    /// </summary>
    /// <param name="input">The input string that represents selected days.</param>
    /// <param name="format">
    /// A format string that defines the day ordering and the character used for unselected days. See
    /// <see cref="WeekPattern.ToString(string, IFormatProvider)"/> for supported values.
    /// </param>
    /// <param name="result">
    /// When this method returns, contains the parsed <see cref="WeekPattern"/> if parsing succeeded;
    /// otherwise, contains <see cref="WeekPattern.Empty"/>.
    /// </param>
    /// <returns><see langword="true"/> if parsing succeeded; otherwise, <see langword="false"/>.</returns>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "Argument validation is performed centrally in ParseCore.")]
    public static bool TryParseExact(string input, string format, out WeekPattern result)
    {
        if (input is null)
        {
            result = WeekPattern.Empty;
            return false;
        }

        try
        {
            result = ParseCore(input, ParseFormatForParse(format));
            return true;
        }
        catch
        {
            result = WeekPattern.Empty;
            return false;
        }
    }
}
