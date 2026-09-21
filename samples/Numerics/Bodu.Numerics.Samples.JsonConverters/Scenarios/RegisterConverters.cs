// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RegisterConverters.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json;
using Bodu.Numerics;
using Bodu.Numerics.Serialization.Json;

namespace Bodu.Numerics.Samples.JsonConverters.Scenarios;

/// <summary>
/// Demonstrates the one-call registration that teaches <see cref="JsonSerializer" /> the
/// <c>Bodu.Numerics</c> types: <see cref="NumericsJsonSerializerOptionsExtensions.AddNumericsJsonConverters" />
/// adds a coherent converter set to a <see cref="JsonSerializerOptions" />, after which
/// <see cref="Fraction{T}" />, <see cref="Interval{T}" />, <see cref="DiscreteInterval{T}" />, and
/// <see cref="IntervalSet{T}" /> all round-trip like any built-in type.
/// </summary>
public static class RegisterConverters
{
    /// <summary>
    /// Registers the converters once, then serializes and re-reads each numerics type.
    /// </summary>
    public static void Run()
    {
        SampleConsole.Scenario(
            "AddNumericsJsonConverters - round-trip every type",
            what: "Registers the converters once on a JsonSerializerOptions, then serializes and re-reads each " +
                  "numerics type, comparing the result against the original value.",
            why: "Bodu.Numerics deliberately takes no dependency on System.Text.Json, so the converters ship in a " +
                 "companion package - the pattern NodaTime uses. That keeps the core usable where the serializer " +
                 "is not, at the cost of one registration call. Without it these types serialize by their public " +
                 "properties, which for a Fraction means emitting whatever surface it happens to expose rather " +
                 "than a form that reads back.",
            expect: "Each type emits a documented shape and reads back equal to what went in - the match is the " +
                    "claim, not the text. IntervalSet emits an array because it is a union of pieces, so its " +
                    "normalized form survives the round trip rather than being flattened.");

        // A single call wires converters for all four types onto the options instance.
        // Strict is the default policy: canonical object shapes suitable for persistence.
        var options = new JsonSerializerOptions().AddNumericsJsonConverters();

        // Fraction<int> round-trips through its canonical numerator/denominator object.
        RoundTrip("Fraction<int>", new Fraction<int>(3, 4), options);

        // Interval<int> round-trips through its lower/upper/inclusive object.
        RoundTrip("Interval<int>", Interval<int>.ClosedOpen(1, 5), options);

        // DiscreteInterval<int> has its own converter registered by the same call.
        RoundTrip("DiscreteInterval<int>", DiscreteInterval<int>.Closed(10, 20), options);

        // IntervalSet<int> serializes as the JSON array of its normalized pieces.
        RoundTrip("IntervalSet<int>", IntervalSet<int>.Of(Interval<int>.Closed(0, 3), Interval<int>.Closed(8, 10)), options);

        Console.WriteLine();
    }

    /// <summary>
    /// Serializes a value, prints its JSON, deserializes it back, and confirms the round trip.
    /// </summary>
    /// <typeparam name="T">The value type being round-tripped.</typeparam>
    /// <param name="label">A human-readable label for the type under test.</param>
    /// <param name="value">The value to serialize and re-read.</param>
    /// <param name="options">The options carrying the registered numerics converters.</param>
    private static void RoundTrip<T>(string label, T value, JsonSerializerOptions options)
    {
        // Serialize -> JSON text -> deserialize, then compare the re-read value to the original.
        var json = JsonSerializer.Serialize(value, options);
        var restored = JsonSerializer.Deserialize<T>(json, options);

        var matches = EqualityComparer<T>.Default.Equals(value, restored);
        Console.WriteLine($"  {label,-22}: {json}");
        Console.WriteLine($"  {"  re-read",-22}: {restored}  (matches original: {matches} - the equality is the claim; the text above is just how it got there)");
    }
}
