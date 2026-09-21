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
        SampleConsole.Scenario(
            "Writing - round-tripping a CSV and converting its dialect",
            what: "Parses the trades file into the mutable DOM, writes it back out, re-parses the result to "
                + "compare shape and the quoted field, prints the re-emitted last row, then writes the same tree "
                + "again with a tab delimiter.",
            why: "The interesting half of writing CSV is quoting, and the rule is that a field is quoted when it "
                + "has to be - because it contains the delimiter, a quote or a line break - and left bare "
                + "otherwise. Quoting everything is valid but produces a file that diffs badly against its "
                + "source and that some consumers mishandle; quoting nothing produces a file that is simply "
                + "wrong. Getting this right is also what makes the round trip meaningful: re-emitting a parsed "
                + "file and re-parsing it has to give back the same values, or the DOM is not a safe place to "
                + "edit. Dialect conversion then falls out for free, since the delimiter is a writer option "
                + "rather than part of the tree.",
            expect: "The re-parsed document has the same shape and the same quoted value, so nothing was lost "
                + "in the round trip. Looking at the re-emitted row, only the field containing a comma carries "
                + "quotes - the others are bare. The TSV output is the same records with a different separator, "
                + "produced by a write rather than by a conversion step.");

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
        Console.WriteLine($"  round trip: shape preserved -> {sameShape}, quoted comma field preserved -> {sameQuoted}"
            + "  (expected True twice - parse, emit and re-parse must agree, or the DOM is not a safe place to edit a file)");

        // Only fields that need quoting get quotes - here the one containing a comma.
        var lastLine = formatted.TrimEnd().Split('\n')[^1].TrimEnd();
        Console.WriteLine($"  last row re-emitted: {lastLine}"
            + "  (only the field containing a comma is quoted - quoting everything would be valid but would diff badly against the source)");

        // Dialect conversion: write the same tree with a tab delimiter.
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new Utf8DelimitedWriter(buffer, new DelimitedWriterOptions { Delimiter = '\t' });
        records.WriteTo(ref writer);
        writer.Flush();

        var tsv = Encoding.UTF8.GetString(buffer.WrittenSpan);
        var tsvFirstRow = tsv.TrimEnd().Split('\n')[1].TrimEnd();
        Console.WriteLine($"  as TSV: {tsvFirstRow.Replace("\t", " <TAB> ")}"
            + "  (the same tree, a different writer option - the delimiter is not part of the parsed data)");

        Console.WriteLine();
    }
}
