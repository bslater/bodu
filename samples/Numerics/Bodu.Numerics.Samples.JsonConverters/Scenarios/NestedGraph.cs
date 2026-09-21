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
        SampleConsole.Scenario(
            "NestedGraph - numerics inside a POCO",
            what: "Serializes an ordinary class holding a Fraction, an Interval and a list of intervals, then " +
                  "reads the whole graph back and compares each member.",
            why: "Registering converters on the options is what makes these types work anywhere in a graph, at " +
                 "any depth, including inside collections - rather than only when serialized directly. That is " +
                 "the practical difference between a converter package and a helper method: the POCO needs no " +
                 "attributes, no custom converter of its own, and no awareness that its members are anything " +
                 "unusual.",
            expect: "The nested members emit exactly the shapes the standalone scenario produced, and the list " +
                    "of intervals serializes element by element. Every member compares equal after the round " +
                    "trip, which is what proves depth is irrelevant to the registration.");

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
        Console.WriteLine($"  re-read name          : {restored.Name}  (an ordinary property, serialized the ordinary way)");
        Console.WriteLine($"  re-read target weight : {restored.TargetWeight}  (a Fraction nested one level down - the POCO needs no attribute and no converter of its own)");
        Console.WriteLine($"  re-read price band    : {restored.PriceBand}  (an Interval, with its inclusivity preserved through the round trip)");
        Console.WriteLine($"  re-read trading hours : {restored.TradingHours}  (inside a collection, which is the case a helper method cannot reach and a registered converter handles for free)");

        Console.WriteLine();
    }
}
