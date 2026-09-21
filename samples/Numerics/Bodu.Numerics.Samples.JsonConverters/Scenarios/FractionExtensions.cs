// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FractionExtensions.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Numerics;
using Bodu.Numerics.Serialization.Json;

namespace Bodu.Numerics.Samples.JsonConverters.Scenarios;

/// <summary>
/// Demonstrates the <see cref="Bodu.Numerics.Serialization.Json.FractionJsonExtensions" /> convenience
/// helpers: <c>ToJson</c> and <c>FromJson</c> serialize a single <see cref="Fraction{T}" /> to and
/// from JSON in one call, building the registered options internally so no
/// <see cref="System.Text.Json.JsonSerializerOptions" /> plumbing is needed at the call site.
/// </summary>
public static class FractionExtensions
{
    /// <summary>
    /// Round-trips a fraction through the one-call JSON helpers under two policies.
    /// </summary>
    public static void Run()
    {
        SampleConsole.Scenario(
            "FractionJsonExtensions - one-call helpers",
            what: "Uses ToJson and FromJson on a Fraction under both policies, and confirms both routes restore " +
                  "the original value.",
            why: "The extensions exist for the case where one value crosses a boundary and building a " +
                 "JsonSerializerOptions would be ceremony - a log line, a cache key, a single column. They are a " +
                 "convenience over the same converters, not a second implementation, which is what keeps the two " +
                 "routes from drifting. For a whole object graph the registration is still the right approach, " +
                 "since options should be built once and reused rather than per call.",
            expect: "Both policies round-trip 22/7, and both restore it equal to the original. Same converters " +
                    "underneath, so the output matches the registered route exactly.");

        var value = new Fraction<int>(22, 7);

        // ToJson serializes under the default Strict policy - the self-describing object shape.
        var strictJson = value.ToJson();
        Console.WriteLine($"  ToJson() strict         : {strictJson}  (identical to the registered route - one converter implementation, two ways to reach it)");

        // The same helper takes a policy, so Compact gives the terse string shape.
        var compactJson = value.ToJson(NumericsJsonPolicy.Compact);
        Console.WriteLine($"  ToJson(Compact)         : {compactJson}  (the policy is an argument here rather than options state, which is what makes this a one-liner)");

        // FromJson is the inverse; it parses whichever shape matches the supplied policy.
        var fromStrict = FractionJsonExtensions.FromJson<int>(strictJson);
        var fromCompact = FractionJsonExtensions.FromJson<int>(compactJson, NumericsJsonPolicy.Compact);
        Console.WriteLine($"  FromJson(strict)        : {fromStrict}  (for a single value crossing a boundary - a log line, a cache key, one column)");
        Console.WriteLine($"  FromJson(Compact)       : {fromCompact}  (the same value from the other shape)");
        Console.WriteLine($"  both restore original   : {fromStrict == value && fromCompact == value}  (expected True - for a whole graph still prefer the registration, since options should be built once and reused)");

        Console.WriteLine();
    }
}
