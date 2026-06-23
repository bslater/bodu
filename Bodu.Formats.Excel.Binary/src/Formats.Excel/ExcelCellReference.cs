// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExcelCellReference.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Formats.Excel;

/// <summary>
/// Converts between zero-based cell coordinates and the spreadsheet A1 reference notation (for example, <c>A1</c> or
/// <c>AB10</c>).
/// </summary>
/// <remarks>
/// The notation names a column by a bijective base-26 sequence of letters (<c>A</c>..<c>Z</c>, <c>AA</c>..<c>AZ</c>,
/// and so on) followed by a one-based row number. The conversions are culture-independent and accept upper- or
/// lower-case column letters when parsing.
/// </remarks>
public static class ExcelCellReference
{
    /// <summary>
    /// Converts a zero-based column index to its A1 column name.
    /// </summary>
    /// <param name="columnIndex">The zero-based column index.</param>
    /// <returns>The column name, for example <c>A</c> for index <c>0</c> or <c>AA</c> for index <c>26</c>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="columnIndex" /> is negative.
    /// </exception>
    public static string ColumnName(int columnIndex)
    {
        ThrowHelper.ThrowIfLessThan(columnIndex, 0);

        Span<char> buffer = stackalloc char[8];
        int position = buffer.Length;
        int value = columnIndex + 1;
        while (value > 0)
        {
            int remainder = (value - 1) % 26;
            buffer[--position] = (char)('A' + remainder);
            value = (value - 1) / 26;
        }

        return new string(buffer.Slice(position));
    }

    /// <summary>
    /// Converts zero-based cell coordinates to their A1 reference.
    /// </summary>
    /// <param name="rowIndex">The zero-based row index.</param>
    /// <param name="columnIndex">The zero-based column index.</param>
    /// <returns>The A1 reference, for example <c>A1</c> for row <c>0</c>, column <c>0</c>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="rowIndex" /> or <paramref name="columnIndex" /> is negative.
    /// </exception>
    public static string ToA1(int rowIndex, int columnIndex)
    {
        ThrowHelper.ThrowIfLessThan(rowIndex, 0);
        ThrowHelper.ThrowIfLessThan(columnIndex, 0);

        return string.Concat(ColumnName(columnIndex), (rowIndex + 1).ToString(CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// Attempts to parse an A1 reference into zero-based cell coordinates.
    /// </summary>
    /// <param name="reference">The A1 reference, for example <c>A1</c> or <c>ab10</c>.</param>
    /// <param name="rowIndex">When this method returns, the zero-based row index when parsing succeeds.</param>
    /// <param name="columnIndex">When this method returns, the zero-based column index when parsing succeeds.</param>
    /// <returns><see langword="true" /> when <paramref name="reference" /> is a valid A1 reference.</returns>
    public static bool TryParseA1(string? reference, out int rowIndex, out int columnIndex)
    {
        rowIndex = 0;
        columnIndex = 0;
        if (string.IsNullOrEmpty(reference))
            return false;

        int i = 0;
        long column = 0;
        while (i < reference.Length && IsColumnLetter(reference[i]))
        {
            column = (column * 26) + (char.ToUpperInvariant(reference[i]) - 'A' + 1);
            if (column > int.MaxValue)
                return false;

            i++;
        }

        if (i == 0 || i == reference.Length)
            return false;

        long row = 0;
        for (int j = i; j < reference.Length; j++)
        {
            if (reference[j] is < '0' or > '9')
                return false;

            row = (row * 10) + (reference[j] - '0');
            if (row > int.MaxValue)
                return false;
        }

        if (row == 0)
            return false;

        rowIndex = (int)(row - 1);
        columnIndex = (int)(column - 1);
        return true;
    }

    /// <summary>
    /// Determines whether a character is an A1 column letter.
    /// </summary>
    /// <param name="c">The character to test.</param>
    /// <returns><see langword="true" /> when the character is an ASCII letter.</returns>
    private static bool IsColumnLetter(char c) =>
        c is >= 'A' and <= 'Z' or >= 'a' and <= 'z';
}
