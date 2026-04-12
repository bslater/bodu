// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DaysOfWeekSet.Functions.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

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
            // Auto-detect binary
            char firstChar = char.ToUpperInvariant(input[0]);
            isBinary = firstChar == '0' || firstChar == '1';
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
                if (unselectedChar is null && (c == ' ' || c == '-' || c == '*' || c == '_'))
                    unselectedChar = c;

                if (isMondayStart is null)
                {
                    char normalized = char.ToUpperInvariant(c);
                    if (normalized == WeekdaySymbols[i] || i == 6)
                        isMondayStart = false;
                    else if (normalized == WeekdaySymbols[(i + 1) % 7])
                        isMondayStart = true;
                }

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
    /// Parses the format string for use in <see cref="ParseExact" /> or <see cref="TryParseExact" />, throwing
    /// <see cref="FormatException" /> if invalid.
    /// </summary>
    private static (char? startDay, char? unselectedChar, bool isBinary) ParseFormatForParse(string format)
    {
        ThrowHelper.ThrowIfNull(format);
        if (string.IsNullOrEmpty(format) || !TryParseFormatInfo(format, out (char? startDay, char? unselectedChar, bool isBinary) info))
            throw new FormatException(ResourceStrings.Arg_Invalid_FormatString);

        return info;
    }

    /// <summary>
    /// Attempts to parse the format string into its parts for formatting or parsing a <see cref="DaysOfWeekSet" />.
    /// </summary>
    /// <param name="format">The format string (1–2 characters).</param>
    /// <param name="info">
    /// When successful, contains the tuple: (startDay: 'S' or 'M', unselectedChar: a symbol like '_', '-', '*', or ' ', and isBinary).
    /// </param>
    /// <returns><c>true</c> if the format is valid; otherwise, <c>false</c>.</returns>
    private static bool TryParseFormatInfo(string format, out (char? startDay, char? unselectedChar, bool isBinary) info)
    {
        info = default;

        format = format.ToUpperInvariant();

        // Binary format
        if (format == "B")
        {
            info = ('S', '0', true);
            return true;
        }

        // Format is either specifying Sunday/Monday order, or using a special character for unselected days
        if (format.Length == 1)
        {
            char c = format[0];
            info = c switch
            {
                'S' => ('S', null, false),
                'M' => ('M', null, false),
                'E' => (null, ' ', false),
                'U' => (null, '_', false),
                'D' => (null, '-', false),
                'A' => (null, '*', false),
                _ => default
            };

            return info != default;
        }

        // Format is specifying Sunday/Monday order and a special character for unselected days
        if (format.Length == 2)
        {
            char startDay = format[0];
            char spec = format[1];

            if (startDay is not ('S' or 'M'))
                return false; // incorrect order specifier

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

            info = (startDay, unselectedChar, false);
            return true;
        }

        return false;
    }
}