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
        SampleConsole.Scenario(
            "Parsing and typed records over a CSV",
            what: "Parses the committed trades file through the read-only document, reports its headers and row "
                + "count, reads a field whose value contains a comma, then binds the whole file onto a typed "
                + "record class and aggregates over it.",
            why: "CSV is the format most often parsed by splitting on commas, and that works until a field "
                + "contains one. RFC 4180's answer is quoting, which means a correct reader is a small state "
                + "machine rather than a split - and getting that wrong shifts every later column in the row, "
                + "silently. The typed layer carries the other trap: every CSV value is text, and parsing a "
                + "decimal or a timestamp under the machine's current culture makes the same file mean different "
                + "things on different machines. Binding parses with invariant culture for exactly that reason, "
                + "once, rather than at each call site.",
            expect: "The quoted field comes back as one value with its comma intact and its quotes removed - the "
                + "quotes were structure, not content. The aggregate works on real decimals and timestamps "
                + "rather than strings, so summing and subtracting are ordinary arithmetic, and the result does "
                + "not depend on the machine's locale.");

        var path = Path.Combine(AppContext.BaseDirectory, "Data", "trades.csv");
        var csvBytes = File.ReadAllBytes(path);

        // Document surface: headers + records as objects keyed by header name.
        using var document = DelimitedDocument.Parse(csvBytes);
        var root = document.RootElement;
        Console.WriteLine($"  headers: {string.Join(", ", document.Headers)} ({root.GetArrayLength()} rows)"
            + "  (the header row names the fields, so each record is addressed by name rather than by position)");

        // Quoted fields arrive unwrapped: "F, ordinary" is one field despite its comma.
        var lastSymbol = root[root.GetArrayLength() - 1].GetProperty("symbol").GetString();
        Console.WriteLine($"  quoted field: '{lastSymbol}'"
            + "  (one field containing a comma, not two fields - splitting on commas would have shifted every later column)");

        // Serializer surface: the whole file as typed records via the snake_case naming policy.
        var options = new DelimitedSerializerOptions { PropertyNamingPolicy = NamingPolicy.SnakeCaseLower };
        var trades = DelimitedSerializer.Deserialize<Trade>(File.ReadAllText(path), options);

        var notional = trades.Sum(t => t.Quantity * t.Price);
        var span = trades[^1].ExecutedAt - trades[0].ExecutedAt;
        Console.WriteLine($"  total notional: {notional:N2} across {span.TotalMinutes:F0} minutes of trading"
            + "  (real decimals and timestamps, parsed once with invariant culture - the same file gives this answer on any machine)");

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
