// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StreamingReaderWriter.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Text.Delimited;

namespace Bodu.Samples.Text.Formats.DelimitedData.Scenarios;

/// <summary>
/// Demonstrates the streaming surface for files too large to materialize: the forward-only
/// <see cref="DelimitedReader" /> holds one row at a time, and <see cref="DelimitedWriter" />
/// emits rows as they are produced — here composed into a filter pipeline that never builds a
/// document.
/// </summary>
public static class StreamingReaderWriter
{
    /// <summary>
    /// Streams <c>Data/trades.csv</c> through a filter into a new CSV without materializing it.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- Streaming: DelimitedReader -> filter -> DelimitedWriter ---");

        using var input = File.OpenText(Path.Combine(AppContext.BaseDirectory, "Data", "trades.csv"));
        using var reader = Delimited.CreateReader(input);

        var output = new StringWriter();
        var writer = Delimited.CreateWriter(output);

        // One row in memory at a time: read, filter, write.
        var seen = 0;
        var kept = 0;
        decimal buyNotional = 0;
        var headerWritten = false;

        while (reader.Read())
        {
            if (!headerWritten)
            {
                writer.WriteHeader(reader.Headers);
                headerWritten = true;
            }

            seen++;

            // Fields is the current row: [2] = side, [3] = quantity, [4] = price.
            if (reader.Fields[2] == "Buy")
            {
                kept++;
                buyNotional += int.Parse(reader.Fields[3], CultureInfo.InvariantCulture)
                    * decimal.Parse(reader.Fields[4], CultureInfo.InvariantCulture);
                writer.WriteRow(reader.Fields);
            }
        }

        Console.WriteLine($"streamed {seen} rows, kept {kept} buys (notional {buyNotional:N2}); reader stopped at line {reader.LineNumber}");

        // The writer re-quotes on the way out - note 'F, ordinary' kept its quotes.
        Console.WriteLine("filtered output:");
        foreach (var line in output.ToString().TrimEnd().Split('\n'))
        {
            Console.WriteLine($"  | {line.TrimEnd()}");
        }

        Console.WriteLine();
    }
}
