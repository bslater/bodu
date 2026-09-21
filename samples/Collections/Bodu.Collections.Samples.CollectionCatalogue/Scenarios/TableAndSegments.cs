// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TableAndSegments.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Collections.Generic;

namespace Bodu.Collections.Samples.CollectionCatalogue.Scenarios;

/// <summary>
/// Demonstrates <see cref="Table{TRow, TColumn, TValue}" />, a sparse two-key map with live row and column views, and
/// <see cref="SegmentedBuffer{T}" />, an append-only list that grows by adding fixed-size segments instead of
/// reallocating and copying.
/// </summary>
public static class TableAndSegments
{
    /// <summary>
    /// Runs the two-key table and segmented-buffer walkthroughs.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- Table / SegmentedBuffer ---");

        RunTable();
        RunSegmentedBuffer();

        Console.WriteLine();
    }

    /// <summary>
    /// Builds a sparse (city, month) rainfall table and reads it back by row, by column, and by cell.
    /// </summary>
    private static void RunTable()
    {
        Console.WriteLine("  Table<TRow, TColumn, TValue> (sparse two-key map):");

        // Addressed by a (row, column) pair rather than a composite key or a Dictionary-of-Dictionaries that every
        // caller has to remember to create the inner level of. It is sparse: absent cells cost nothing.
        var rainfall = new Table<string, string, int>(StringComparer.Ordinal, StringComparer.Ordinal);

        rainfall["Sydney", "Jan"] = 102;
        rainfall["Sydney", "Feb"] = 117;
        rainfall["Sydney", "Mar"] = 129;
        rainfall["Perth", "Jan"] = 9;
        rainfall["Perth", "Mar"] = 19;       // no February reading for Perth - the cell simply does not exist
        rainfall.Add("Darwin", "Jan", 468);

        Console.WriteLine($"    cells        : {rainfall.Count} across {rainfall.RowKeys.Count} rows and {rainfall.ColumnKeys.Count} columns");

        // TryAdd is the non-throwing counterpart of Add; Add rejects an occupied cell.
        Console.WriteLine($"    TryAdd occupied cell : {rainfall.TryAdd("Sydney", "Jan", 999)} (Sydney/Jan is taken)");
        Console.WriteLine($"    TryGetValue Perth/Feb: {rainfall.TryGetValue("Perth", "Feb", out var perthFeb)} (value {perthFeb})");
        Console.WriteLine($"    Contains Perth/Feb   : {rainfall.Contains("Perth", "Feb")}");
        Console.WriteLine($"    ContainsRow/Column   : Perth={rainfall.ContainsRow("Perth")}, Feb={rainfall.ContainsColumn("Feb")}");

        // Row(row) and Column(column) are read-only *views* over one slice, so a caller can hand out a single city's
        // series or a single month's cross-section without materializing a copy.
        foreach (var city in rainfall.RowKeys.OrderBy(key => key, StringComparer.Ordinal))
        {
            var series = rainfall.Row(city);
            var cells = series.OrderBy(pair => MonthOrder(pair.Key)).Select(pair => $"{pair.Key}={pair.Value}");
            Console.WriteLine($"      row {city,-7}: {string.Join(", ", cells)}");
        }

        // A column view is the cross-section: every city that reported that month, and only those.
        Console.WriteLine($"      col Jan    : {RenderColumn(rainfall, "Jan")} (all three cities)");
        Console.WriteLine($"      col Feb    : {RenderColumn(rainfall, "Feb")} (Perth and Darwin never reported)");

        // RemoveColumn drops one column across every row in a single call - the operation a nested-dictionary layout
        // makes the caller write a loop for.
        Console.WriteLine($"    RemoveColumn(\"Mar\"): {rainfall.RemoveColumn("Mar")} -> {rainfall.Count} cells remain");
        Console.WriteLine($"    RemoveRow(\"Darwin\"): {rainfall.RemoveRow("Darwin")} -> {rainfall.Count} cells remain");

        // Enumeration yields a ((row, column), value) pair per populated cell; order is unspecified, so sort to print.
        var remaining = rainfall
            .OrderBy(cell => cell.Key.Row, StringComparer.Ordinal)
            .ThenBy(cell => MonthOrder(cell.Key.Column));
        Console.WriteLine($"    cells        : {string.Join(", ", remaining.Select(cell => $"({cell.Key.Row},{cell.Key.Column})={cell.Value}"))}");
    }

    /// <summary>
    /// Appends to a segmented buffer and shows indexed access across a segment boundary.
    /// </summary>
    private static void RunSegmentedBuffer()
    {
        Console.WriteLine("  SegmentedBuffer<T> (append-only, no reallocation):");

        // A List<T> doubles its array and copies everything across on each growth, which costs large contiguous
        // allocations (and LOH pressure) as it gets big. A SegmentedBuffer adds another fixed-size segment instead:
        // appends never copy existing items, and no allocation is larger than one segment.
        var buffer = new SegmentedBuffer<int>(segmentSize: 4);

        for (var value = 1; value <= 10; value++) buffer.Add(value);

        Console.WriteLine($"    count        : {buffer.Count} (segment size 4, so three segments are in use)");

        // Indexed access stays O(1) - the index divides into a segment number and an offset.
        Console.WriteLine($"    [0] / [3] / [4] / [9]: {buffer[0]} / {buffer[3]} / {buffer[4]} / {buffer[9]} (4 -> 5 crosses a boundary)");

        // Enumeration walks the segments in order, so the logical sequence is the insertion sequence.
        Console.WriteLine($"    enumerated   : {string.Join(", ", buffer)}");

        // The indexer is settable, so the buffer works as a write-through backing store for computed values.
        buffer[9] = 100;
        Console.WriteLine($"    after [9] = 100: last item {buffer[buffer.Count - 1]}");

        // CopyTo and ToArray flatten the segments into one contiguous array when an API demands one.
        Console.WriteLine($"    ToArray()    : [{string.Join(", ", buffer.ToArray())}]");

        // Clear() empties the buffer; TrimExcess() then releases the segments the buffer no longer needs.
        buffer.Clear();
        buffer.TrimExcess();
        Console.WriteLine($"    after Clear + TrimExcess: count {buffer.Count}");
    }

    /// <summary>
    /// Renders one column view of the table in row order.
    /// </summary>
    /// <param name="table">The table to read.</param>
    /// <param name="column">The column to render.</param>
    /// <returns>A comma-separated list of row/value pairs.</returns>
    private static string RenderColumn(Table<string, string, int> table, string column) =>
        string.Join(
            ", ",
            table.Column(column)
                .OrderBy(pair => pair.Key, StringComparer.Ordinal)
                .Select(pair => $"{pair.Key}={pair.Value}"));

    /// <summary>
    /// Returns a sort key that orders the month abbreviations used by this scenario chronologically.
    /// </summary>
    /// <param name="month">The three-letter month abbreviation.</param>
    /// <returns>The month's ordinal position.</returns>
    private static int MonthOrder(string month) =>
        month switch { "Jan" => 1, "Feb" => 2, "Mar" => 3, _ => 99 };
}
