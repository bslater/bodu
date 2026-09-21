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
        SampleConsole.Scenario(
            "NumericsJsonPolicy - object vs string shapes",
            what: "Writes the same Fraction and Interval under the strict object policy and the compact string " +
                  "policy, then reads a compact value back.",
            why: "The two shapes trade legibility against tooling. The object form is self-describing, so a " +
                 "consumer that has never seen a Fraction can still read it, and a JSON Schema can validate it. " +
                 "The compact form is a single string - far easier for a human to read in a config file or a log " +
                 "line, and much smaller in bulk - but its meaning lives in a parser rather than in the document. " +
                 "Pick the object form for an API contract and the compact one for storage you control.",
            expect: "The same values in both shapes: an object with named fields, and \"3/4\" or \"[1, 5)\" as a " +
                    "string. The compact form reads back to an equal value, so the choice is about the document " +
                    "rather than about fidelity.");

        // Two options instances differing only in the policy passed to the registration call.
        var strict = new JsonSerializerOptions().AddNumericsJsonConverters(NumericsJsonPolicy.Strict);
        var compact = new JsonSerializerOptions().AddNumericsJsonConverters(NumericsJsonPolicy.Compact);

        var fraction = new Fraction<int>(3, 4);
        var interval = Interval<int>.ClosedOpen(1, 5);

        // Strict renders a fraction as a numerator/denominator object - unambiguous and self-describing.
        Console.WriteLine($"  Fraction strict  : {JsonSerializer.Serialize(fraction, strict)}  (self-describing: a consumer that has never seen a Fraction can still read it, and a schema can validate it)");

        // Compact renders the same fraction as a single "3/4" string - terse for logs and query strings.
        Console.WriteLine($"  Fraction compact : {JsonSerializer.Serialize(fraction, compact)}  (one string - far smaller in bulk and readable in a config file, but its meaning lives in a parser rather than the document)");

        // Strict renders an interval as a full endpoint object.
        Console.WriteLine($"  Interval strict  : {JsonSerializer.Serialize(interval, strict)}  (the inclusivity flags are explicit fields, so no reader has to know the bracket convention)");

        // Compact renders the interval in ISO 31-11 bracket notation as a string.
        Console.WriteLine($"  Interval compact : {JsonSerializer.Serialize(interval, compact)}  (the same information carried by the brackets - compact, but only if the reader knows what [ and ) mean)");

        // Each policy reads back its own shape, so a compact string restores the same value.
        var restored = JsonSerializer.Deserialize<Fraction<int>>("\"3/4\"", compact);
        Console.WriteLine($"  compact read-back: \"3/4\" -> {restored}  (the compact form loses nothing: the choice is about the document, not about fidelity)");

        Console.WriteLine();
    }
}
