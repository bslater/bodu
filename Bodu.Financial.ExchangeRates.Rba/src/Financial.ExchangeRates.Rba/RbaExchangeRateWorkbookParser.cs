// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RbaExchangeRateWorkbookParser.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Formats.Excel.Binary;

namespace Bodu.Financial.ExchangeRates.Rba;

/// <summary>
/// Interprets the RBA-specific layout of an Excel workbook, turning the raw worksheet cells of the <c>Data</c> sheet
/// into a normalized <see cref="RbaExchangeRateTable" />.
/// </summary>
/// <remarks>
/// <para>
/// This is the only layer that understands the RBA convention: a <c>Data</c> sheet whose header rows are keyed by a
/// label in the first column (<c>Title</c>, <c>Units</c>, <c>Series ID</c>, …), a date in the first column of every
/// data row, and one rate column per currency, each quoting the Australian dollar against the column's currency (
/// <c>A$1 = currency</c>). The currency for a column is taken from the units row and validated to a three-letter ISO
/// code, which drops non-currency columns such as the trade-weighted index.
/// </para>
/// <para>
/// Header rows are located by their label rather than a fixed position so the parser tolerates the minor layout
/// differences between RBA era files. A workbook that does not match the expected shape raises
/// <see cref="RbaExchangeRateFormatException" />.
/// </para>
/// </remarks>
internal static class RbaExchangeRateWorkbookParser
{
    /// <summary>The worksheet that holds the rate data.</summary>
    private const string DataSheetName = "Data";

    /// <summary>The first-column label of the units header row.</summary>
    private const string UnitsLabel = "Units";

    /// <summary>The first-column label of the series-identifier header row.</summary>
    private const string SeriesIdLabel = "Series ID";

    /// <summary>The first-column label of the title header row.</summary>
    private const string TitleLabel = "Title";

    /// <summary>The first-column label of the description header row.</summary>
    private const string DescriptionLabel = "Description";

    /// <summary>The prefix of the title cell from which a currency code can be recovered when the units row is absent.</summary>
    private const string TitlePrefix = "A$1=";

    /// <summary>
    /// Parses the RBA <c>Data</c> sheet of a workbook into a normalized table.
    /// </summary>
    /// <param name="workbook">The opened BIFF8 workbook.</param>
    /// <param name="options">The provider options supplying the currency-alias map.</param>
    /// <returns>The normalized <see cref="RbaExchangeRateTable" />.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="workbook" /> or <paramref name="options" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="RbaExchangeRateFormatException">
    /// Thrown when the workbook does not match the expected RBA layout.
    /// </exception>
    internal static RbaExchangeRateTable Parse(Biff8WorkbookReader workbook, RbaExchangeRateOptions options)
    {
        ThrowHelper.ThrowIfNull(workbook);
        ThrowHelper.ThrowIfNull(options);

        if (!HasDataSheet(workbook))
            throw new RbaExchangeRateFormatException(RbaResourceStrings.Format_Invalid_RbaDataSheetMissing);

        Dictionary<(int Row, int Column), ExcelCell> grid = BuildGrid(workbook);

        int unitsRow = FindLabelRow(grid, UnitsLabel);
        int seriesIdRow = FindLabelRow(grid, SeriesIdLabel);
        int titleRow = FindLabelRow(grid, TitleLabel);
        int descriptionRow = FindLabelRow(grid, DescriptionLabel);

        if (unitsRow < 0 && titleRow < 0)
            throw new RbaExchangeRateFormatException(RbaResourceStrings.Format_Invalid_RbaHeaderRows);

        List<int> dataRows = FindDataRows(grid);
        if (dataRows.Count == 0)
            throw new RbaExchangeRateFormatException(RbaResourceStrings.Format_Invalid_RbaNoDataRows);

        List<RbaExchangeRateSeries> series = BuildSeries(grid, unitsRow, titleRow, seriesIdRow, descriptionRow, options);
        List<RbaExchangeRateRow> rows = BuildRows(grid, dataRows, series);

        return new RbaExchangeRateTable(series, rows);
    }

    /// <summary>
    /// Reports whether the workbook contains a sheet named <c>Data</c>.
    /// </summary>
    /// <param name="workbook">The workbook to inspect.</param>
    /// <returns><see langword="true" /> when a <c>Data</c> sheet exists; otherwise <see langword="false" />.</returns>
    private static bool HasDataSheet(Biff8WorkbookReader workbook)
    {
        foreach (Biff8SheetInfo sheet in workbook.Sheets)
        {
            if (string.Equals(sheet.Name, DataSheetName, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Reads the <c>Data</c> sheet into a position-keyed cell grid.
    /// </summary>
    /// <param name="workbook">The workbook to read.</param>
    /// <returns>A dictionary mapping each populated cell's (row, column) to its value.</returns>
    private static Dictionary<(int Row, int Column), ExcelCell> BuildGrid(Biff8WorkbookReader workbook)
    {
        Dictionary<(int Row, int Column), ExcelCell> grid = new();
        foreach (ExcelCell cell in workbook.ReadSheetCells(DataSheetName))
            grid[(cell.RowIndex, cell.ColumnIndex)] = cell;

        return grid;
    }

    /// <summary>
    /// Finds the row whose first-column text equals <paramref name="label" />.
    /// </summary>
    /// <param name="grid">The cell grid.</param>
    /// <param name="label">The label to match, ignoring case and surrounding white space.</param>
    /// <returns>The matching row index, or <c>-1</c> when no row matches.</returns>
    private static int FindLabelRow(Dictionary<(int Row, int Column), ExcelCell> grid, string label)
    {
        foreach (KeyValuePair<(int Row, int Column), ExcelCell> entry in grid)
        {
            if (entry.Key.Column == 0 &&
                entry.Value.Kind == ExcelCellKind.String &&
                string.Equals(entry.Value.StringValue?.Trim(), label, StringComparison.OrdinalIgnoreCase))
            {
                return entry.Key.Row;
            }
        }

        return -1;
    }

    /// <summary>
    /// Finds the rows whose first column holds a number, which mark the data (date) rows.
    /// </summary>
    /// <param name="grid">The cell grid.</param>
    /// <returns>The data row indices in ascending order.</returns>
    private static List<int> FindDataRows(Dictionary<(int Row, int Column), ExcelCell> grid)
    {
        List<int> rows = new();
        foreach (KeyValuePair<(int Row, int Column), ExcelCell> entry in grid)
        {
            if (entry.Key.Column == 0 && entry.Value.Kind == ExcelCellKind.Number)
                rows.Add(entry.Key.Row);
        }

        rows.Sort();
        return rows;
    }

    /// <summary>
    /// Builds the currency series from the header rows, keeping only columns that resolve to a valid currency code.
    /// </summary>
    /// <param name="grid">The cell grid.</param>
    /// <param name="unitsRow">The units row index, or <c>-1</c> when absent.</param>
    /// <param name="titleRow">The title row index, or <c>-1</c> when absent.</param>
    /// <param name="seriesIdRow">The series-identifier row index, or <c>-1</c> when absent.</param>
    /// <param name="descriptionRow">The description row index, or <c>-1</c> when absent.</param>
    /// <param name="options">The provider options supplying the currency-alias map.</param>
    /// <returns>The currency series in ascending column order.</returns>
    private static List<RbaExchangeRateSeries> BuildSeries(
        Dictionary<(int Row, int Column), ExcelCell> grid,
        int unitsRow,
        int titleRow,
        int seriesIdRow,
        int descriptionRow,
        RbaExchangeRateOptions options)
    {
        int headerRow = unitsRow >= 0 ? unitsRow : titleRow;
        SortedSet<int> columns = new();
        foreach (KeyValuePair<(int Row, int Column), ExcelCell> entry in grid)
        {
            if (entry.Key.Row == headerRow && entry.Key.Column >= 1 && entry.Value.Kind == ExcelCellKind.String)
                columns.Add(entry.Key.Column);
        }

        List<RbaExchangeRateSeries> series = new();
        foreach (int column in columns)
        {
            string units = GetText(grid, unitsRow, column);
            string title = GetText(grid, titleRow, column);

            string? currencyCode = ResolveCurrencyCode(units, title, options);
            if (currencyCode is null)
                continue;

            series.Add(new RbaExchangeRateSeries(
                column,
                GetText(grid, seriesIdRow, column),
                currencyCode,
                units,
                title,
                GetText(grid, descriptionRow, column)));
        }

        return series;
    }

    /// <summary>
    /// Builds the dated rows, reading each series' cell as a recovered decimal rate.
    /// </summary>
    /// <param name="grid">The cell grid.</param>
    /// <param name="dataRows">The data row indices in ascending order.</param>
    /// <param name="series">The currency series.</param>
    /// <returns>The dated rows, aligned to <paramref name="series" />.</returns>
    private static List<RbaExchangeRateRow> BuildRows(
        Dictionary<(int Row, int Column), ExcelCell> grid,
        List<int> dataRows,
        List<RbaExchangeRateSeries> series)
    {
        List<RbaExchangeRateRow> rows = new(dataRows.Count);
        foreach (int row in dataRows)
        {
            double serial = grid[(row, 0)].NumberValue!.Value;
            if (!double.IsFinite(serial))
                continue;

            DateOnly date = ExcelSerialDate.FromSerialDate(serial);
            decimal?[] values = new decimal?[series.Count];
            for (int i = 0; i < series.Count; i++)
            {
                if (grid.TryGetValue((row, series[i].ColumnIndex), out ExcelCell cell) &&
                    cell.Kind == ExcelCellKind.Number &&
                    cell.NumberValue is double value &&
                    double.IsFinite(value))
                {
                    values[i] = RecoverDecimal(value);
                }
            }

            rows.Add(new RbaExchangeRateRow(date, values));
        }

        return rows;
    }

    /// <summary>
    /// Resolves a column's currency code from its units (preferred) or title cell, applying alias mapping and rejecting
    /// non-currency columns.
    /// </summary>
    /// <param name="units">The units cell text.</param>
    /// <param name="title">The title cell text.</param>
    /// <param name="options">The provider options supplying the currency-alias map.</param>
    /// <returns>
    /// The resolved three-letter ISO code, or <see langword="null" /> when the column is not a currency.
    /// </returns>
    private static string? ResolveCurrencyCode(string units, string title, RbaExchangeRateOptions options)
    {
        string raw = !string.IsNullOrWhiteSpace(units)
            ? units
            : title.StartsWith(TitlePrefix, StringComparison.OrdinalIgnoreCase) ? title[TitlePrefix.Length..] : string.Empty;

        raw = raw.Trim().ToUpperInvariant();
        if (options.CurrencyAliases.TryGetValue(raw, out string? aliased))
            raw = aliased.Trim().ToUpperInvariant();

        bool isCurrency = raw.Length == 3
            && char.IsAsciiLetterUpper(raw[0])
            && char.IsAsciiLetterUpper(raw[1])
            && char.IsAsciiLetterUpper(raw[2])
            && !string.Equals(raw, RbaExchangeRateProvider.BaseCurrencyIsoCode, StringComparison.Ordinal);

        return isCurrency ? raw : null;
    }

    /// <summary>
    /// Recovers the published decimal rate from a workbook double.
    /// </summary>
    /// <param name="value">The cell value as a double.</param>
    /// <returns>The recovered decimal.</returns>
    /// <remarks>
    /// RBA rates are published with limited precision (for example, <c>0.6828</c>), yet the workbook stores each as an
    /// 8-byte IEEE 754 double that can sit up to one unit in the last place away from that figure. The explicit
    /// <see langword="double" />-to-<see langword="decimal" /> conversion rounds to 15 significant digits, which
    /// absorbs that sub-15-digit binary noise and restores the published value; every RBA rate carries far fewer than
    /// 15 significant digits, so none is truncated. A shortest-round-trip string conversion instead preserves the noise
    /// — for example, yielding <c>6.6754999999999995</c> for a cell published as <c>6.6755</c>.
    /// </remarks>
    internal static decimal RecoverDecimal(double value) =>
        (decimal)value;

    /// <summary>
    /// Reads the trimmed text of a cell, returning an empty string when the cell is absent, non-text, or the row is
    /// unknown.
    /// </summary>
    /// <param name="grid">The cell grid.</param>
    /// <param name="row">The row index, or <c>-1</c> when the row is unknown.</param>
    /// <param name="column">The column index.</param>
    /// <returns>The cell text, or an empty string.</returns>
    private static string GetText(Dictionary<(int Row, int Column), ExcelCell> grid, int row, int column)
    {
        if (row >= 0 && grid.TryGetValue((row, column), out ExcelCell cell) && cell.Kind == ExcelCellKind.String)
            return cell.StringValue ?? string.Empty;

        return string.Empty;
    }
}
