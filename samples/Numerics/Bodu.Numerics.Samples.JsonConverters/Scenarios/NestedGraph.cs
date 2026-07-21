// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NestedGraph.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json;
using Bodu.Numerics;
using Bodu.Numerics.Serialization.Json;

namespace Bodu.Numerics.Samples.JsonConverters.Scenarios;

/// <summary>
/// Demonstrates that the registered numerics converters compose inside a larger object graph: a
/// <see cref="Portfolio" /> POCO mixing a string with a <see cref="Fraction{T}" />, an
/// <see cref="Interval{T}" />, and an <see cref="IntervalSet{T}" /> serializes and re-reads in one
/// call, with each numerics property rendered by its own converter — no per-property attributes.
/// </summary>
public static class NestedGraph
{
    /// <summary>
    /// Serializes a mixed POCO, prints its indented JSON, and re-reads it to confirm the round trip.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- NestedGraph: numerics inside a POCO ---");

        // Register the converters once; WriteIndented makes the composed shape easy to read.
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
        }.AddNumericsJsonConverters();

        var portfolio = new Portfolio
        {
            Name = "Growth",
            TargetWeight = new Fraction<int>(2, 5),
            PriceBand = Interval<int>.Closed(90, 110),
            TradingHours = IntervalSet<int>.Of(Interval<int>.Closed(9, 12), Interval<int>.Closed(13, 17)),
        };

        // One Serialize call handles the whole graph - the numerics props use their own converters.
        var json = JsonSerializer.Serialize(portfolio, options);
        Console.WriteLine(json);

        // Deserialize reconstructs every property, including the normalized interval set.
        var restored = JsonSerializer.Deserialize<Portfolio>(json, options)!;
        Console.WriteLine($"re-read name          : {restored.Name}");
        Console.WriteLine($"re-read target weight : {restored.TargetWeight}");
        Console.WriteLine($"re-read price band    : {restored.PriceBand}");
        Console.WriteLine($"re-read trading hours : {restored.TradingHours}");

        Console.WriteLine();
    }
}
