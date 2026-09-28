// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExcelBinaryReaderOptions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Formats.Excel;

/// <summary>
/// Controls how an <see cref="ExcelBinaryWorkbook" /> opens and reads a workbook, allowing callers to trade optional
/// metadata work for throughput and to govern stream ownership.
/// </summary>
/// <remarks>
/// The defaults read the full metadata surface (document properties and date-format detection) and dispose a
/// caller-supplied stream with the workbook. For a numeric, time-series workload — where only row, column, and numeric
/// value matter — clearing <see cref="ReadDocumentProperties" /> and <see cref="DetectDateFormats" /> skips the
/// property sets and number-format interpretation.
/// <example>
/// <code language="csharp">
///<![CDATA[
/// using Bodu.Formats.Excel;
///
/// var options = new ExcelBinaryReaderOptions
/// {
///     LeaveOpen = true,               // do not dispose the caller's stream
///     ReadDocumentProperties = false, // skip the summary-information property sets
///     DetectDateFormats = false,      // skip number-format classification
/// };
///
/// using var source = File.OpenRead("data.xls");
/// using var workbook = ExcelBinaryWorkbook.Open(source, options);
///]]>
/// </code>
/// </example>
/// </remarks>
public sealed class ExcelBinaryReaderOptions
{
    /// <summary>The shared default options instance.</summary>
    internal static readonly ExcelBinaryReaderOptions Default = new();

    /// <summary>
    /// Gets a value indicating whether a caller-supplied stream is left open when the workbook is disposed.
    /// </summary>
    /// <value>
    /// <see langword="true" /> to leave the source stream open; <see langword="false" /> to dispose it with the
    /// workbook. The default is <see langword="false" />.
    /// </value>
    public bool LeaveOpen { get; init; }

    /// <summary>
    /// Gets a value indicating whether the workbook's document-property sets are read when opening.
    /// </summary>
    /// <value>
    /// <see langword="true" /> to read the summary-information property sets; <see langword="false" /> to skip them and
    /// surface an empty <see cref="ExcelWorkbookProperties" />. The default is <see langword="true" />.
    /// </value>
    public bool ReadDocumentProperties { get; init; } = true;

    /// <summary>
    /// Gets a value indicating whether the number format of each numeric cell is inspected to detect date formatting.
    /// </summary>
    /// <value>
    /// <see langword="true" /> to classify date-formatted numbers; <see langword="false" /> to skip the classification,
    /// leaving <see cref="ExcelCell.IsDateFormatted" /> always <see langword="false" />. The default is
    /// <see langword="true" />.
    /// </value>
    public bool DetectDateFormats { get; init; } = true;
}
