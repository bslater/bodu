// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StreamingReaderWriter.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Globalization;
using System.Text;
using Bodu.Text.Delimited;
using Bodu.Text.Delimited.Reader;
using Bodu.Text.Delimited.Writer;

namespace Bodu.Samples.Text.Formats.DelimitedData.Scenarios;

/// <summary>
/// Demonstrates the token surface for row-at-a-time processing: the forward-only
/// <see cref="Utf8DelimitedReader" /> walks the UTF-8 bytes one token at a time, and
/// <see cref="Utf8DelimitedWriter" /> emits records as they are produced — here composed into a filter pipeline that
/// never builds a document.
/// </summary>
public static class StreamingReaderWriter
{
    /// <summary>
    /// Streams <c>Data/trades.csv</c> through a filter into a new CSV without materializing it.
    /// </summary>
    public static void Run()
    {
        SampleConsole.Scenario(
            "Streaming - reader to filter to writer, with no document in between",
            what: "Walks the trades file one token at a time, accumulating each row's fields, and writes only the "
                + "Buy rows through to a new CSV while accumulating their notional - then prints the filtered "
                + "output.",
            why: "This is the shape for a file bigger than memory, and the reason the reader and writer share a "
                + "token model rather than each having their own. Only one row is held at a time, so the cost of "
                + "the pipeline is fixed regardless of how long the file is - which is the difference between a "
                + "job that runs on a large export and one that runs out of memory partway through. The writer "
                + "re-applies the quoting rules independently on the way out, so a field that needed quotes in "
                + "the source still has them in the output even though the pipeline only ever saw its unquoted "
                + "value.",
            expect: "Fewer rows out than in, because only Buy rows pass the filter, with the notional accumulated "
                + "during the same single pass. The reader reports the line it finished on, which is what makes "
                + "a failure mid-stream diagnosable. In the output, the symbol containing a comma is quoted "
                + "again - the writer decided that, not the reader.");

        var csvBytes = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Data", "trades.csv"));
        var reader = new Utf8DelimitedReader(csvBytes);

        // The writer shares the reader's token model: a document is one array of record objects.
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new Utf8DelimitedWriter(buffer);
        writer.WriteStartArray();

        // One record in memory at a time: read its fields, filter, write.
        var fields = new List<string>();
        var seen = 0;
        var kept = 0;
        decimal buyNotional = 0;

        while (reader.Read())
        {
            switch (reader.TokenType)
            {
                case DelimitedTokenType.StartObject:
                    fields.Clear();
                    break;

                case DelimitedTokenType.String:
                    fields.Add(reader.GetString());
                    break;

                case DelimitedTokenType.EndObject:
                    seen++;

                    // Fields is the current row: [2] = side, [3] = quantity, [4] = price.
                    if (fields[2] == "Buy")
                    {
                        kept++;
                        buyNotional += int.Parse(fields[3], CultureInfo.InvariantCulture)
                            * decimal.Parse(fields[4], CultureInfo.InvariantCulture);

                        writer.WriteStartObject();
                        for (var i = 0; i < fields.Count; i++)
                        {
                            writer.WritePropertyName(reader.Headers[i]);
                            writer.WriteString(fields[i]);
                        }

                        writer.WriteEndObject();
                    }

                    break;

                default:
                    break;
            }
        }

        writer.WriteEndArray();
        writer.Flush();

        Console.WriteLine($"  streamed {seen} rows, kept {kept} buys (notional {buyNotional:N2}); reader stopped at line {reader.LineNumber}"
            + "  (one row held at a time, so the cost is fixed no matter how long the file is)");

        // The writer re-quotes on the way out - note 'F, ordinary' kept its quotes.
        Console.WriteLine("  filtered output (note 'F, ordinary' was re-quoted by the writer, which never saw the source quotes):");
        foreach (var line in Encoding.UTF8.GetString(buffer.WrittenSpan).TrimEnd().Split('\n'))
        {
            Console.WriteLine($"  | {line.TrimEnd()}");
        }

        Console.WriteLine();
    }
}
