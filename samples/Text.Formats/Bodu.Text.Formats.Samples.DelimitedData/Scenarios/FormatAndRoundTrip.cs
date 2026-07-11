// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FormatAndRoundTrip.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Delimited;

namespace Bodu.Samples.Text.Formats.DelimitedData.Scenarios;

/// <summary>
/// Demonstrates the write direction: <c>Delimited.Format</c> turns a document back into RFC 4180
/// text — re-quoting exactly the fields that need it — and the same options type retargets the
/// output to another dialect, so CSV-in / TSV-out conversion is a parse and a format.
/// </summary>
public static class FormatAndRoundTrip
{
    /// <summary>
    /// Round-trips <c>Data/trades.csv</c> and converts it to TSV.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- Format: document -> text (and CSV -> TSV) ---");

        var csv = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Data", "trades.csv"));
        var document = Delimited.Parse(csv);

        // Format re-emits RFC 4180 text; a re-parse yields the same shape and values.
        var formatted = Delimited.Format(document);
        var reparsed = Delimited.Parse(formatted);

        var sameShape = reparsed.Rows.Count == document.Rows.Count && reparsed.Headers.SequenceEqual(document.Headers);
        var sameQuoted = reparsed.Rows[^1]["symbol"] == document.Rows[^1]["symbol"];
        Console.WriteLine($"round trip: shape preserved -> {sameShape}, quoted comma field preserved -> {sameQuoted}");

        // Only fields that need quoting get quotes - here the one containing a comma.
        var lastLine = formatted.TrimEnd().Split('\n')[^1].TrimEnd();
        Console.WriteLine($"last row re-emitted: {lastLine}");

        // Dialect conversion: format the same document with a tab delimiter.
        var tsv = Delimited.Format(document, new DelimitedParseOptions { Delimiter = '\t' });
        var tsvFirstRow = tsv.TrimEnd().Split('\n')[1].TrimEnd();
        Console.WriteLine($"as TSV: {tsvFirstRow.Replace("\t", " <TAB> ")}");

        Console.WriteLine();
    }
}
