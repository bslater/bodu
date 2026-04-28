// ---------------------------------------------------------------------------------------------------------------
// <copyright file="WeekPattern.Functions.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System;

namespace Bodu;

public partial struct WeekPattern
{
    /// <summary>
    /// Parses a seven-character week-pattern string into a <see cref="WeekPattern" />,
    /// auto-detecting the format when <paramref name="formatInfo" /> is <see langword="null" />.
    /// </summary>
    /// <param name="input">A seven-character string representing the days of the week. Each position
    /// indicates whether the corresponding day is selected.</param>
    /// <param name="formatInfo">An optional format descriptor containing the week-start character
    /// (<c>'S'</c> or <c>'M'</c>), the character used for unselected days, and whether the encoding
    /// is binary (<c>'1'</c>/<c>'0'</c>). When <see langword="null" />, these are inferred from
    /// <paramref name="input" />.</param>
    /// <returns>The parsed <see cref="WeekPattern" /> value.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="input" /> is <see langword="null" />.</exception>
    /// <exception cref="FormatException"><paramref name="input" /> is not exactly seven characters, or
    /// contains characters that do not match the detected or supplied format.</exception>
    private static WeekPattern ParseCore(string input, (char? startDay, char? unselectedChar, bool isBinary)? formatInfo)
    {
        ThrowHelper.ThrowIfNull(input);
        if (input.Length != 7)
            throw new FormatException(string.Format(ResourceStrings.Format_Invalid_StringLength, 7));

        byte mask = 0;

        bool isBinary;
        char? startDay = null;
        char? unselectedChar = null;
        bool? isMondayStart = null;

        if (formatInfo is null)
        {
            // Auto-detect binary: if the first character is '0' or '1', treat the whole string as binary.
            char firstChar = char.ToUpperInvariant(input[0]);
            isBinary = firstChar == '0' || firstChar == '1';
        }
        else
        {
            (startDay, unselectedChar, isBinary) = formatInfo.Value;

            // startDay is always 'S', 'M', or null (null only for auto-detect callers).
            isMondayStart = startDay switch
            {
                'M' => true,
                'S' => false,
                _ => null
            };
        }

        for (int i = 0; i < 7; i++)
        {
            char c = input[i];

            if (isBinary)
            {
                bool bitSet = c switch
                {
                    '0' => false,
                    '1' => true,
                    _ => throw new FormatException(
                               string.Format(ResourceStrings.Format_Invalid_Character, c, i + 1))
                };

                if (bitSet)
                    mask |= (byte)(ShiftValue << (6 - i));
            }
            else
            {
                // Learn the unselected placeholder from the first non-day character encountered,
                // when the format did not specify one explicitly.
                if (unselectedChar is null && (c == ' ' || c == '-' || c == '*' || c == '_'))
                    unselectedChar = c;

                // Infer day ordering from the first character whose position disambiguates it,
                // but only when no explicit ordering was given.
                if (isMondayStart is null)
                {
                    char normalized = char.ToUpperInvariant(c);
                    if (normalized == WeekdaySymbols[i] || i == 6)
                        isMondayStart = false; // Sunday-first: symbol matches Sunday-first position
                    else if (normalized == WeekdaySymbols[(i + 1) % 7])
                        isMondayStart = true;  // Monday-first: symbol matches Monday-first position
                }

                int dayIndex = isMondayStart == true ? (i + 1) % 7 : i;
                char normalizedDay = char.ToUpperInvariant(c);

                if (normalizedDay == WeekdaySymbols[dayIndex])
                {
                    mask |= (byte)(ShiftValue << (6 - dayIndex));
                }
                else if (c == unselectedChar)
                {
                    // Bit is already 0 in mask — no action needed.
                }
                else
                {
                    throw new FormatException(
                        string.Format(ResourceStrings.Format_Invalid_Character, c, i + 1));
                }
            }
        }

        return new WeekPattern(mask);
    }

    /// <summary>
    /// Parses the format string for use in <see cref="ParseExact"/> or <see cref="TryParseExact"/>,
    /// throwing <see cref="FormatException"/> if the format is unrecognised or <see langword="null"/>.
    /// </summary>
    private static (char? startDay, char? unselectedChar, bool isBinary) ParseFormatForParse(string format)
    {
        ThrowHelper.ThrowIfNull(format);
        if (string.IsNullOrEmpty(format) || !TryParseFormatInfo(format, out var info))
            throw new FormatException(ResourceStrings.Arg_Invalid_FormatString);

        return info;
    }

    /// <summary>
    /// Parses the format string for use in <see cref="ToString(string, IFormatProvider)"/>,
    /// throwing <see cref="ArgumentException"/> if the format is unrecognised.
    /// </summary>
    private static (char? startDay, char? unselectedChar, bool isBinary) ParseFormatForToString(string? format)
    {
        format ??= "S";
        if (!TryParseFormatInfo(format, out var info))
            throw new ArgumentException(ResourceStrings.Arg_Invalid_FormatString, nameof(format));

        return info;
    }

    /// <summary>
    /// Attempts to parse the format string into its constituent parts for use in formatting or parsing a
    /// <see cref="WeekPattern"/>.
    /// </summary>
    /// <param name="format">The format string (one or two characters).</param>
    /// <param name="info">
    /// When this method returns <see langword="true"/>, contains the parsed format tuple:
    /// <c>startDay</c> is <c>'S'</c> or <c>'M'</c>; <c>unselectedChar</c> is the placeholder symbol;
    /// <c>isBinary</c> indicates binary output mode.
    /// </param>
    /// <returns><see langword="true"/> if the format is recognised; otherwise, <see langword="false"/>.</returns>
    private static bool TryParseFormatInfo(string format, out (char? startDay, char? unselectedChar, bool isBinary) info)
    {
        info = default;

        format = format.ToUpperInvariant();

        // Explicit binary specifiers
        if (format is "B" or "0" or "1" or "01")
        {
            info = ('S', '0', true);
            return true;
        }

        if (format.Length == 1)
        {
            char c = format[0];
            info = c switch
            {
                'S' => ('S', null, false),  // Sunday-first, infer unselected char from input
                'M' => ('M', null, false),  // Monday-first, infer unselected char from input

                // Single-char unselected specifiers: always Sunday-first.
                // Monday-first inputs must use two-char specifiers such as 'MU', 'MD', etc.
                'E' => ('S', ' ', false),
                'U' => ('S', '_', false),
                'D' => ('S', '-', false),
                'A' => ('S', '*', false),
                _ => default
            };

            return info != default;
        }

        if (format.Length == 2)
        {
            char startDayChar = format[0];
            char specChar = format[1];

            if (startDayChar is not('S' or 'M'))
                return false;

            char unselected = specChar switch
            {
                'U' => '_',
                'D' => '-',
                'A' => '*',
                'E' => ' ',
                _ => '\0',
            };

            if (unselected == '\0')
                return false;

            info = (startDayChar, unselected, false);
            return true;
        }

        return false;
    }
}
