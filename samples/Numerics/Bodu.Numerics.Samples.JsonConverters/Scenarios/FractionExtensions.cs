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
        Console.WriteLine("--- FractionJsonExtensions: one-call helpers ---");

        var value = new Fraction<int>(22, 7);

        // ToJson serializes under the default Strict policy - the self-describing object shape.
        var strictJson = value.ToJson();
        Console.WriteLine($"ToJson() strict         : {strictJson}");

        // The same helper takes a policy, so Compact gives the terse string shape.
        var compactJson = value.ToJson(NumericsJsonPolicy.Compact);
        Console.WriteLine($"ToJson(Compact)         : {compactJson}");

        // FromJson is the inverse; it parses whichever shape matches the supplied policy.
        var fromStrict = FractionJsonExtensions.FromJson<int>(strictJson);
        var fromCompact = FractionJsonExtensions.FromJson<int>(compactJson, NumericsJsonPolicy.Compact);
        Console.WriteLine($"FromJson(strict)        : {fromStrict}");
        Console.WriteLine($"FromJson(Compact)       : {fromCompact}");
        Console.WriteLine($"both restore original   : {fromStrict == value && fromCompact == value}");

        Console.WriteLine();
    }
}
