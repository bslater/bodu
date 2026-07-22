// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FormatAndRoundTrip.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Text;
using Bodu.Text.Delimited.Document;
using Bodu.Text.Delimited.Nodes;
using Bodu.Text.Delimited.Writer;

namespace Bodu.Samples.Text.Formats.DelimitedData.Scenarios;

/// <summary>
/// Demonstrates the write direction: the mutable <see cref="DelimitedNode" /> DOM parses a CSV, writes it back out —
/// re-quoting exactly the fields that need it — and the writer options retarget the same tree to another dialect, so
/// CSV-in / TSV-out conversion is a parse and a write.
/// </summary>
public static class FormatAndRoundTrip
{
    /// <summary>
    /// Round-trips <c>Data/trades.csv</c> and converts it to TSV.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- Nodes DOM: document -> text (and CSV -> TSV) ---");

        var csvBytes = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Data", "trades.csv"));
        var records = DelimitedNode.Parse(csvBytes);

        // WriteTo re-emits RFC 4180 text; a re-parse yields the same shape and values.
        var formatted = records.ToString();
        var reparsed = DelimitedNode.Parse(Encoding.UTF8.GetBytes(formatted));

        using var original = DelimitedDocument.Parse(csvBytes);
        using var roundTripped = DelimitedDocument.Parse(Encoding.UTF8.GetBytes(formatted));

        var sameShape = reparsed.Count == records.Count && roundTripped.Headers.SequenceEqual(original.Headers);
        var lastIndex = original.RootElement.GetArrayLength() - 1;
        var sameQuoted = roundTripped.RootElement[lastIndex].GetProperty("symbol").GetString()
            == original.RootElement[lastIndex].GetProperty("symbol").GetString();
        Console.WriteLine($"round trip: shape preserved -> {sameShape}, quoted comma field preserved -> {sameQuoted}");

        // Only fields that need quoting get quotes - here the one containing a comma.
        var lastLine = formatted.TrimEnd().Split('\n')[^1].TrimEnd();
        Console.WriteLine($"last row re-emitted: {lastLine}");

        // Dialect conversion: write the same tree with a tab delimiter.
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new Utf8DelimitedWriter(buffer, new DelimitedWriterOptions { Delimiter = '\t' });
        records.WriteTo(ref writer);
        writer.Flush();

        var tsv = Encoding.UTF8.GetString(buffer.WrittenSpan);
        var tsvFirstRow = tsv.TrimEnd().Split('\n')[1].TrimEnd();
        Console.WriteLine($"as TSV: {tsvFirstRow.Replace("\t", " <TAB> ")}");

        Console.WriteLine();
    }
}
