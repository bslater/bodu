// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ParseAndTypedGetters.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Delimited;

namespace Bodu.Samples.Text.Formats.DelimitedData.Scenarios;

/// <summary>
/// Demonstrates the document surface: parse a CSV file once, then read fields by header name
/// with culture-safe typed getters — <c>GetValue&lt;T&gt;</c> parses any
/// <see cref="ISpanParsable{TSelf}" /> with <c>InvariantCulture</c>, so numbers and timestamps
/// never depend on the machine's locale.
/// </summary>
public static class ParseAndTypedGetters
{
    /// <summary>
    /// Parses <c>Data/trades.csv</c> and aggregates it with typed field access.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- Parse + typed getters over Data/trades.csv ---");

        var csv = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Data", "trades.csv"));
        var document = Delimited.Parse(csv);

        Console.WriteLine($"headers: {string.Join(", ", document.Headers)} ({document.Rows.Count} rows)");

        // Quoted fields arrive unwrapped: "F, ordinary" is one field despite its comma.
        var lastSymbol = document.Rows[^1]["symbol"];
        Console.WriteLine($"quoted field: '{lastSymbol}'");

        // Typed access by header name - int, decimal, DateTimeOffset all via ISpanParsable.
        decimal notional = 0;
        foreach (var row in document.Rows)
        {
            notional += row.GetValue<int>("quantity") * row.GetValue<decimal>("price");
        }

        var first = document.Rows[0].GetValue<DateTimeOffset>("executed_at");
        var last = document.Rows[^1].GetValue<DateTimeOffset>("executed_at");

        Console.WriteLine($"total notional: {notional:N2} across {(last - first).TotalMinutes:F0} minutes of trading");

        // TryGetValue for fields that may not parse - no exception on bad input.
        var ok = document.Rows[0].TryGetValue<int>("symbol", out _);
        Console.WriteLine($"TryGetValue<int>(\"symbol\") on 'AAPL' -> {ok}");

        Console.WriteLine();
    }
}
