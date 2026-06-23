// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExcelNumberFormat.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Formats.Excel;

/// <summary>
/// Classifies BIFF8 number formats, resolving built-in format codes and determining whether a format renders a value as
/// a date or time.
/// </summary>
/// <remarks>
/// Detection mirrors the approach used by mainstream spreadsheet readers: a format is treated as a date or time when
/// its index is one of the built-in date or time formats, or when its format code contains date or time field
/// characters outside of quoted literals, escapes, and bracketed conditions.
/// </remarks>
internal static class ExcelNumberFormat
{
    /// <summary>The canonical format codes of the well-known built-in format indexes (0-49).</summary>
    private static readonly Dictionary<ushort, string> s_builtInFormats = new()
    {
        [0] = "General",
        [1] = "0",
        [2] = "0.00",
        [3] = "#,##0",
        [4] = "#,##0.00",
        [5] = "$#,##0_);($#,##0)",
        [6] = "$#,##0_);[Red]($#,##0)",
        [7] = "$#,##0.00_);($#,##0.00)",
        [8] = "$#,##0.00_);[Red]($#,##0.00)",
        [9] = "0%",
        [10] = "0.00%",
        [11] = "0.00E+00",
        [12] = "# ?/?",
        [13] = "# ??/??",
        [14] = "m/d/yyyy",
        [15] = "d-mmm-yy",
        [16] = "d-mmm",
        [17] = "mmm-yy",
        [18] = "h:mm AM/PM",
        [19] = "h:mm:ss AM/PM",
        [20] = "h:mm",
        [21] = "h:mm:ss",
        [22] = "m/d/yyyy h:mm",
        [37] = "#,##0_);(#,##0)",
        [38] = "#,##0_);[Red](#,##0)",
        [39] = "#,##0.00_);(#,##0.00)",
        [40] = "#,##0.00_);[Red](#,##0.00)",
        [41] = "_(* #,##0_);_(* (#,##0);_(* \"-\"_);_(@_)",
        [42] = "_($* #,##0_);_($* (#,##0);_($* \"-\"_);_(@_)",
        [43] = "_(* #,##0.00_);_(* (#,##0.00);_(* \"-\"??_);_(@_)",
        [44] = "_($* #,##0.00_);_($* (#,##0.00);_($* \"-\"??_);_(@_)",
        [45] = "mm:ss",
        [46] = "[h]:mm:ss",
        [47] = "mm:ss.0",
        [48] = "##0.0E+0",
        [49] = "@",
    };

    /// <summary>
    /// Gets the canonical format code of a well-known built-in format index.
    /// </summary>
    /// <param name="formatIndex">The number-format index.</param>
    /// <returns>The built-in format code, or <see langword="null" /> when the index is not a known built-in.</returns>
    public static string? GetBuiltInFormatCode(ushort formatIndex) =>
        s_builtInFormats.TryGetValue(formatIndex, out string? code) ? code : null;

    /// <summary>
    /// Determines whether a format index and its optional code denote a date or time format.
    /// </summary>
    /// <param name="formatIndex">The number-format index.</param>
    /// <param name="formatCode">The custom format code for the index, when one is defined.</param>
    /// <returns>
    /// <see langword="true" /> when the format renders a value as a date or time; otherwise <see langword="false" />.
    /// </returns>
    public static bool IsDateFormat(ushort formatIndex, string? formatCode)
    {
        if (IsBuiltInDateFormatIndex(formatIndex))
            return true;

        string? code = formatCode ?? GetBuiltInFormatCode(formatIndex);
        return code is not null && CodeContainsDateField(code);
    }

    /// <summary>
    /// Determines whether a format index falls within the built-in date or time ranges.
    /// </summary>
    /// <param name="formatIndex">The number-format index.</param>
    /// <returns><see langword="true" /> when the index is a built-in date or time format.</returns>
    private static bool IsBuiltInDateFormatIndex(ushort formatIndex) =>
        formatIndex is >= 14 and <= 22 or >= 27 and <= 36 or >= 45 and <= 47 or >= 50 and <= 58;

    /// <summary>
    /// Determines whether the first section of a format code contains a date or time field character, ignoring quoted
    /// literals, escaped characters, fill and spacing tokens, and non-elapsed bracketed conditions.
    /// </summary>
    /// <param name="formatCode">The format code.</param>
    /// <returns><see langword="true" /> when a date or time field character is present.</returns>
    private static bool CodeContainsDateField(string formatCode)
    {
        for (int i = 0; i < formatCode.Length; i++)
        {
            char c = formatCode[i];
            switch (c)
            {
                case ';':
                    // Only the first (positive) section determines the rendered kind.
                    return false;

                case '"':
                    i = SkipQuoted(formatCode, i);
                    break;

                case '\\':
                case '_':
                case '*':
                    // A backslash escapes, and '_'/'*' consume, the single following character.
                    i++;
                    break;

                case '[':
                    if (IsElapsedTimeBracket(formatCode, i, out int end))
                        return true;
                    i = end;
                    break;

                case 'y':
                case 'Y':
                case 'd':
                case 'D':
                case 'h':
                case 'H':
                case 's':
                case 'S':
                    return true;

                case 'm':
                case 'M':
                    return true;

                default:
                    break;
            }
        }

        return false;
    }

    /// <summary>
    /// Returns the index of the closing quote of a quoted literal that starts at <paramref name="start" />.
    /// </summary>
    /// <param name="text">The format code.</param>
    /// <param name="start">The index of the opening quote.</param>
    /// <returns>The index of the closing quote, or the last index when the literal is unterminated.</returns>
    private static int SkipQuoted(string text, int start)
    {
        for (int i = start + 1; i < text.Length; i++)
        {
            if (text[i] == '"')
                return i;
        }

        return text.Length - 1;
    }

    /// <summary>
    /// Determines whether a bracketed token starting at <paramref name="start" /> is an elapsed-time token (for example
    /// <c>[h]</c> or <c>[mm]</c>), and reports the index of its closing bracket.
    /// </summary>
    /// <param name="text">The format code.</param>
    /// <param name="start">The index of the opening bracket.</param>
    /// <param name="end">When this method returns, the index of the closing bracket (or the last index).</param>
    /// <returns>
    /// <see langword="true" /> when the bracketed token consists solely of elapsed time field characters.
    /// </returns>
    private static bool IsElapsedTimeBracket(string text, int start, out int end)
    {
        int close = text.IndexOf(']', start + 1);
        end = close < 0 ? text.Length - 1 : close;
        if (close <= start + 1)
            return false;

        for (int i = start + 1; i < close; i++)
        {
            char c = char.ToLowerInvariant(text[i]);
            if (c is not 'h' and not 'm' and not 's')
                return false;
        }

        return true;
    }
}
