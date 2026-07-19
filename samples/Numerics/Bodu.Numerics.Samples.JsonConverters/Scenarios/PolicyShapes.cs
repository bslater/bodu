// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PolicyShapes.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json;
using Bodu.Numerics;
using Bodu.Numerics.Serialization.Json;

namespace Bodu.Numerics.Samples.JsonConverters.Scenarios;

/// <summary>
/// Demonstrates how <see cref="NumericsJsonPolicy" /> selects the on-the-wire shape: <c>Strict</c>
/// emits self-describing objects for persistence, while <c>Compact</c> emits the terse single-string
/// forms (<c>"3/4"</c>, <c>"[1, 5)"</c>). The same value serializes differently under each policy, and
/// each policy reads back its own shape.
/// </summary>
public static class PolicyShapes
{
    /// <summary>
    /// Serializes a fraction and an interval under the strict and compact policies.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- NumericsJsonPolicy: object vs string shapes ---");

        // Two options instances differing only in the policy passed to the registration call.
        var strict = new JsonSerializerOptions().AddNumericsJsonConverters(NumericsJsonPolicy.Strict);
        var compact = new JsonSerializerOptions().AddNumericsJsonConverters(NumericsJsonPolicy.Compact);

        var fraction = new Fraction<int>(3, 4);
        var interval = Interval<int>.ClosedOpen(1, 5);

        // Strict renders a fraction as a numerator/denominator object - unambiguous and self-describing.
        Console.WriteLine($"Fraction strict  : {JsonSerializer.Serialize(fraction, strict)}");

        // Compact renders the same fraction as a single "3/4" string - terse for logs and query strings.
        Console.WriteLine($"Fraction compact : {JsonSerializer.Serialize(fraction, compact)}");

        // Strict renders an interval as a full endpoint object.
        Console.WriteLine($"Interval strict  : {JsonSerializer.Serialize(interval, strict)}");

        // Compact renders the interval in ISO 31-11 bracket notation as a string.
        Console.WriteLine($"Interval compact : {JsonSerializer.Serialize(interval, compact)}");

        // Each policy reads back its own shape, so a compact string restores the same value.
        var restored = JsonSerializer.Deserialize<Fraction<int>>("\"3/4\"", compact);
        Console.WriteLine($"compact read-back: \"3/4\" -> {restored}");

        Console.WriteLine();
    }
}
