// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Biff8FormatTable.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Formats.Excel.Binary.Biff8;

/// <summary>
/// Resolves a cell's extended-format index to its number-format index and code, built from the extended-format (XF) and
/// number-format (FORMAT) records of the workbook globals.
/// </summary>
/// <remarks>
/// A cell record references a format through its <c>ixfe</c> field, which indexes the ordered list of XF records. Each
/// XF in turn carries a number-format index, which a FORMAT record (or a built-in default) maps to a format code. This
/// table performs that two-step resolution and classifies the result as a date format where applicable.
/// </remarks>
internal sealed class Biff8FormatTable
{
    /// <summary>An empty table used when the workbook declares no globals.</summary>
    public static readonly Biff8FormatTable Empty = new([], new Dictionary<ushort, string>(), detectDateFormats: true);

    /// <summary>The number-format index of each XF, indexed by XF position.</summary>
    private readonly ushort[] _xfFormatIndex;

    /// <summary>The custom format codes declared by FORMAT records, keyed by number-format index.</summary>
    private readonly Dictionary<ushort, string> _formatCodes;

    /// <summary>Whether date-format classification is enabled.</summary>
    private readonly bool _detectDateFormats;

    /// <summary>
    /// Initializes a new instance of the <see cref="Biff8FormatTable" /> class.
    /// </summary>
    /// <param name="xfFormatIndex">The number-format index of each XF, in XF order.</param>
    /// <param name="formatCodes">The custom format codes, keyed by number-format index.</param>
    /// <param name="detectDateFormats">Whether date-format classification is enabled.</param>
    internal Biff8FormatTable(ushort[] xfFormatIndex, Dictionary<ushort, string> formatCodes, bool detectDateFormats)
    {
        _xfFormatIndex = xfFormatIndex;
        _formatCodes = formatCodes;
        _detectDateFormats = detectDateFormats;
    }

    /// <summary>
    /// Resolves the number-format index referenced by a cell's extended-format index.
    /// </summary>
    /// <param name="xfIndex">The cell's <c>ixfe</c> value.</param>
    /// <returns>The number-format index, or <c>0</c> (General) when the XF index is out of range.</returns>
    public ushort GetFormatIndex(ushort xfIndex) =>
        xfIndex < _xfFormatIndex.Length ? _xfFormatIndex[xfIndex] : (ushort)0;

    /// <summary>
    /// Gets the format code for a number-format index, falling back to the well-known built-in codes.
    /// </summary>
    /// <param name="formatIndex">The number-format index.</param>
    /// <returns>The format code, or <see langword="null" /> when none is known for the index.</returns>
    public string? GetFormatCode(ushort formatIndex) =>
        _formatCodes.TryGetValue(formatIndex, out string? code) ? code : ExcelNumberFormat.GetBuiltInFormatCode(formatIndex);

    /// <summary>
    /// Determines whether the cell referenced by an extended-format index is formatted as a date or time.
    /// </summary>
    /// <param name="xfIndex">The cell's <c>ixfe</c> value.</param>
    /// <returns>
    /// <see langword="true" /> when date-format detection is enabled and the resolved number format is a date or time
    /// format; otherwise <see langword="false" />.
    /// </returns>
    public bool IsDateFormatted(ushort xfIndex)
    {
        if (!_detectDateFormats)
            return false;

        ushort formatIndex = GetFormatIndex(xfIndex);
        _formatCodes.TryGetValue(formatIndex, out string? code);

        return ExcelNumberFormat.IsDateFormat(formatIndex, code);
    }
}
