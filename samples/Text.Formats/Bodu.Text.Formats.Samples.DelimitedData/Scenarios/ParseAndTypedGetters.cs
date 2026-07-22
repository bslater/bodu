// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ParseAndTypedGetters.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Delimited;
using Bodu.Text.Delimited.Document;
using Bodu.Text.Serialization;

namespace Bodu.Samples.Text.Formats.DelimitedData.Scenarios;

/// <summary>
/// Demonstrates the read surfaces: <see cref="DelimitedDocument" /> gives JsonDocument-style access to a parsed CSV
/// (records as objects keyed by header), and <see cref="DelimitedSerializer" /> binds the same file straight onto a
/// typed record class — numbers and timestamps parsed with <c>InvariantCulture</c>, so nothing depends on the
/// machine's locale.
/// </summary>
public static class ParseAndTypedGetters
{
    /// <summary>
    /// Parses <c>Data/trades.csv</c> and aggregates it with typed record binding.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- Parse + typed records over Data/trades.csv ---");

        var path = Path.Combine(AppContext.BaseDirectory, "Data", "trades.csv");
        var csvBytes = File.ReadAllBytes(path);

        // Document surface: headers + records as objects keyed by header name.
        using var document = DelimitedDocument.Parse(csvBytes);
        var root = document.RootElement;
        Console.WriteLine($"headers: {string.Join(", ", document.Headers)} ({root.GetArrayLength()} rows)");

        // Quoted fields arrive unwrapped: "F, ordinary" is one field despite its comma.
        var lastSymbol = root[root.GetArrayLength() - 1].GetProperty("symbol").GetString();
        Console.WriteLine($"quoted field: '{lastSymbol}'");

        // Serializer surface: the whole file as typed records via the snake_case naming policy.
        var options = new DelimitedSerializerOptions { PropertyNamingPolicy = NamingPolicy.SnakeCaseLower };
        var trades = DelimitedSerializer.Deserialize<Trade>(File.ReadAllText(path), options);

        var notional = trades.Sum(t => t.Quantity * t.Price);
        var span = trades[^1].ExecutedAt - trades[0].ExecutedAt;
        Console.WriteLine($"total notional: {notional:N2} across {span.TotalMinutes:F0} minutes of trading");

        Console.WriteLine();
    }

    /// <summary>
    /// A trade record; the snake_case naming policy maps the CSV headers onto these properties.
    /// </summary>
    private sealed class Trade
    {
        public int TradeId { get; set; }

        public string? Symbol { get; set; }

        public string? Side { get; set; }

        public int Quantity { get; set; }

        public decimal Price { get; set; }

        public DateTimeOffset ExecutedAt { get; set; }
    }
}
