// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiffFormatTable.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Formats.Excel.Biff;

/// <summary>
/// Resolves a cell's extended-format (XF) index to its number-format index and format code, and classifies whether that
/// format denotes a date.
/// </summary>
/// <remarks>
/// The workbook globals declare XF records in sequence (their position is the XF index) and FORMAT records keyed by
/// format index. Built-in formats have no FORMAT record; their codes and date classification come from
/// <see cref="ExcelNumberFormat" />.
/// </remarks>
internal sealed class BiffFormatTable
{
    /// <summary>A table with no XF or FORMAT records, used by tests and for sheets read without globals.</summary>
    public static readonly BiffFormatTable Empty = new([], new Dictionary<ushort, string>(), detectDateFormats: true);

    /// <summary>The number-format index of each XF record, indexed by XF index.</summary>
    private readonly ushort[] _xfFormatIndex;

    /// <summary>The custom format codes declared by FORMAT records, keyed by format index.</summary>
    private readonly Dictionary<ushort, string> _formatCodes;

    /// <summary>Whether date classification is enabled.</summary>
    private readonly bool _detectDateFormats;

    /// <summary>
    /// Initializes a new instance of the <see cref="BiffFormatTable" /> class.
    /// </summary>
    /// <param name="xfFormatIndex">The number-format index of each XF record, in XF order.</param>
    /// <param name="formatCodes">The custom format codes keyed by format index.</param>
    /// <param name="detectDateFormats">Whether numeric cells are classified as dates from their format.</param>
    internal BiffFormatTable(ushort[] xfFormatIndex, Dictionary<ushort, string> formatCodes, bool detectDateFormats)
    {
        _xfFormatIndex = xfFormatIndex;
        _formatCodes = formatCodes;
        _detectDateFormats = detectDateFormats;
    }

    /// <summary>
    /// Resolves an XF index to its number-format index.
    /// </summary>
    /// <param name="xfIndex">The cell's XF index.</param>
    /// <returns>The number-format index, or zero (General) when the XF index is out of range.</returns>
    public ushort GetFormatIndex(ushort xfIndex) =>
        xfIndex < _xfFormatIndex.Length ? _xfFormatIndex[xfIndex] : (ushort)0;

    /// <summary>
    /// Resolves a number-format index to its format code.
    /// </summary>
    /// <param name="formatIndex">The number-format index.</param>
    /// <returns>
    /// The custom code from the workbook's FORMAT record, the built-in code for a well-known index, or
    /// <see langword="null" /> when the index is unknown.
    /// </returns>
    public string? GetFormatCode(ushort formatIndex) =>
        _formatCodes.TryGetValue(formatIndex, out string? code) ? code : ExcelNumberFormat.GetBuiltInFormatCode(formatIndex);

    /// <summary>
    /// Determines whether the number format behind an XF index denotes a date or time.
    /// </summary>
    /// <param name="xfIndex">The cell's XF index.</param>
    /// <returns><see langword="true" /> when date detection is enabled and the format is a date format.</returns>
    public bool IsDateFormatted(ushort xfIndex)
    {
        if (!_detectDateFormats)
            return false;

        ushort formatIndex = GetFormatIndex(xfIndex);
        _formatCodes.TryGetValue(formatIndex, out string? code);
        return ExcelNumberFormat.IsDateFormat(formatIndex, code);
    }
}
