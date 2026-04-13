// --------------------------------------------------------------------------------------------------------------- //
// <copyright file="DaysOfWeekSet.Functions.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------- //

using System;

namespace Bodu;

public partial struct DaysOfWeekSet
{
    private static DaysOfWeekSet ParseCore(string input, (char? startDay, char? unselectedChar, bool isBinary)? formatInfo)
    {
        ThrowHelper.ThrowIfNull(input);
        if (input.Length != 7)
            throw new FormatException(string.Format(ResourceStrings.Format_Invalid_StringLength, 7));

        DaysOfWeekSet temp = Empty;

        bool isBinary;
        char? startDay = null;
        char? unselectedChar = null;
        bool? isMondayStart = null;

        if (formatInfo is null)
        {
            // Auto-detect binary by examining the first character.
            char firstChar = char.ToUpperInvariant(input[0]);
            isBinary = firstChar == '0' || firstChar == '1';

            if (!isBinary)
            {
                // Pre-scan the entire input to determine day ordering before the main loop begins.
                // This ensures that leading unselected-day placeholders are mapped to the correct day
                // indices, rather than being assigned using the Sunday-first fallback before ordering
                // has been inferred from a later weekday character.
                for (int j = 0; j < 7 && isMondayStart is null; j++)
                {
                    char normalized = char.ToUpperInvariant(input[j]);
                    bool matchesSundayFirst = normalized == WeekdaySymbols[j];
                    bool matchesMondayFirst = normalized == WeekdaySymbols[(j + 1) % 7];

                    if (matchesSundayFirst && !matchesMondayFirst)
                        isMondayStart = false; // Unambiguously Sunday-first at this position
                    else if (matchesMondayFirst && !matchesSundayFirst)
                        isMondayStart = true;  // Unambiguously Monday-first at this position
                    // If both match (ambiguous symbol) or neither (placeholder), continue scanning.
                }

                // Default to Sunday-first when ordering cannot be inferred from any character
                // (e.g. all selected positions carry ambiguous symbols such as 'T' at position 2).
                isMondayStart ??= false;
            }
        }
        else
        {
            (startDay, unselectedChar, isBinary) = formatInfo.Value;
            isMondayStart = startDay switch
            {
                'M' => true,
                'S' => false,
                _ => null
            };

            // For non-binary formats without an explicit start-day specifier, default to Sunday-first.
            if (!isBinary)
                isMondayStart ??= false;
        }

        for (int i = 0; i < 7; i++)
        {
            char c = input[i];

            if (isBinary)
            {
                temp[i] = c switch
                {
                    '0' => false,
                    '1' => true,
                    _ => throw new FormatException(
                            string.Format(ResourceStrings.Format_Invalid_Character, c, i + 1)),
                };
            }
            else
            {
                // Auto-detect the unselected-day placeholder from the first matching character encountered.
                if (unselectedChar is null && (c == ' ' || c == '-' || c == '*' || c == '_'))
                    unselectedChar = c;

                int dayIndex = isMondayStart == true ? (i + 1) % 7 : i;
                char normalizedDay = char.ToUpperInvariant(c);

                if (normalizedDay == WeekdaySymbols[dayIndex])
                {
                    temp[dayIndex] = true;
                }
                else if (c == unselectedChar)
                {
                    temp[dayIndex] = false;
                }
                else
                {
                    throw new FormatException(
                        string.Format(ResourceStrings.Format_Invalid_Character, c, i + 1));
                }
            }
        }

        return temp;
    }

    /// <summary>
    /// Resolves the format string for use in <see cref="ParseExact" /> or <see cref="TryParseExact" />, throwing
    /// <see cref="FormatException" /> if the format is invalid.
    /// </summary>
    private static (char? startDay, char? unselectedChar, bool isBinary) ParseFormatForParse(string format)
    {
        ThrowHelper.ThrowIfNull(format);
        if (string.IsNullOrEmpty(format) || !TryParseFormatInfo(format, out (char? startDay, char? unselectedChar, bool isBinary) info))
            throw new FormatException(ResourceStrings.Arg_Invalid_FormatString);

        return info;
    }

    /// <summary>
    /// Attempts to parse a format string into its constituent parts for use in formatting or parsing a <see cref="DaysOfWeekSet" />.
    /// </summary>
    /// <param name="format">The format string to parse; must be 1 or 2 characters long.</param>
    /// <param name="info">
    /// When this method returns <see langword="true" />, contains a tuple comprising: the start day (<c>'S'</c> or <c>'M'</c>, or
    /// <see langword="null" /> if not specified); the placeholder character for unselected days (e.g. <c>'_'</c>, <c>'-'</c>,
    /// <c>'*'</c>, or <c>' '</c>), or <see langword="null" /> for binary formats; and a flag indicating whether binary output is used.
    /// </param>
    /// <returns><see langword="true" /> if the format is valid and recognised; otherwise, <see langword="false" />.</returns>
    private static bool TryParseFormatInfo(string format, out (char? startDay, char? unselectedChar, bool isBinary) info)
    {
        info = default;
        format = format.ToUpperInvariant();

        // Binary format specifiers — '0', '1', 'B', and '01' are all treated as equivalent.
        // unselectedChar is null because binary mode uses '0'/'1' directly, not a placeholder symbol.
        if (format is "0" or "1" or "B" or "01")
        {
            info = ('S', null, true);
            return true;
        }

        if (format.Length == 1)
        {
            char c = format[0];

            // Use an explicit membership check to guard the assignment, avoiding reliance on the
            // tuple's default value as a sentinel — which would be fragile if a valid mapping
            // ever resolved to (null, null, false).
            bool recognised = c is 'S' or 'M' or 'E' or 'U' or 'D' or 'A';
            if (!recognised)
                return false;

            info = c switch
            {
                'S' => ('S', null, false),
                'M' => ('M', null, false),
                'E' => (null, ' ', false),
                'U' => (null, '_', false),
                'D' => (null, '-', false),
                'A' => (null, '*', false),
                _ => default // unreachable due to 'recognised' guard above
            };

            return true;
        }

        if (format.Length == 2)
        {
            char startDayChar = format[0];
            char spec = format[1];

            if (startDayChar is not ('S' or 'M'))
                return false;

            char unselectedChar = spec switch
            {
                'U' => '_',
                'D' => '-',
                'A' => '*',
                'E' => ' ',
                _ => '\0',
            };

            if (unselectedChar == '\0')
                return false;

            info = (startDayChar, unselectedChar, false);
            return true;
        }

        return false;
    }
}
