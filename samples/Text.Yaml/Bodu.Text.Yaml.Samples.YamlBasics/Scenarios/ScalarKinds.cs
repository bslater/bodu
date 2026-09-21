// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ScalarKinds.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Serialization;

namespace Bodu.Text.Yaml.Samples.YamlBasics.Scenarios;

/// <summary>
/// Demonstrates YAML's defining scalar feature: implicit typing. An unquoted (plain) scalar is
/// resolved to null, boolean, integer, float, or string by the active <see cref="YamlSpecVersion" />,
/// and the serializer surfaces each as the matching .NET runtime type. Quoting forces the string
/// interpretation. The <see cref="YamlNumberHandling" /> knob governs float-to-integer coercion.
/// </summary>
public static class ScalarKinds
{
    /// <summary>
    /// A payload POCO whose single member is an integer target for the number-handling demo.
    /// </summary>
    private sealed class Threshold
    {
        public int Count { get; set; }
    }

    /// <summary>
    /// Binds a document of plain and quoted scalars to a loose graph, then shows number handling.
    /// </summary>
    public static void Run()
    {
        SampleConsole.Scenario(
            "Implicit scalar typing and float-to-integer handling",
            what: "Binds a document of plain and quoted scalars to a loose dictionary and reports the runtime "
                + "type each one resolved to, then binds a non-integral float to an int property under the "
                + "strict default and again with float-to-integer coercion allowed.",
            why: "This is what makes YAML different from every other format here, and the source of most "
                + "surprises in it. An unquoted scalar has no declared type - the spec resolution rules decide "
                + "whether null, true and 3 are a null, a boolean and an integer or just text, and quoting is "
                + "how an author opts out. That is why this library keeps its scalar converters format-local "
                + "rather than sharing the token-strict ones its siblings use: they have to coerce across kinds, "
                + "which the shared converters cannot express. Number handling is the same question one layer "
                + "up - 3.7 into an int is either an error or a truncation, and choosing truncation silently is "
                + "how a configured value quietly becomes 3.",
            expect: "Five plain scalars resolve to five different runtime types with nothing declaring them, "
                + "while the quoted value stays a string despite looking like a number - the quotes are the "
                + "author type annotation. The strict binding then rejects 3.7 rather than truncating, and "
                + "truncation happens only when asked for explicitly.");

        // Binding to object surfaces the resolved runtime type of each plain scalar; a quoted
        // scalar stays a string even when its content looks like a number or a boolean.
        var document = """
            name: orders
            count: 3
            ratio: 0.25
            enabled: true
            note: null
            quoted: "42"
            """;

        var map = YamlSerializer.Deserialize<Dictionary<string, object?>>(document)!;
        foreach (var pair in map)
        {
            var typeName = pair.Value?.GetType().Name ?? "null";
            Console.WriteLine($"  {pair.Key,-8}-> {typeName,-8}: {pair.Value ?? "(null)"}");
        }

        Console.WriteLine("  (nothing declared these types - the spec resolution rules did, and the quoted value opted out by being quoted)");

        // NumberHandling gates float-to-integer coercion. Strict (default) rejects a non-integral
        // float bound to an int; AllowFloatToInteger truncates it toward zero. Both option sets
        // carry the snake_case policy so the wire key 'count' binds to the Count member.
        var strict = new YamlSerializerOptions { PropertyNamingPolicy = NamingPolicy.SnakeCaseLower };
        try
        {
            YamlSerializer.Deserialize<Threshold>("count: 3.7", strict);
        }
        catch (YamlSerializationException ex)
        {
            Console.WriteLine($"  Strict             : 3.7 -> int rejected -> {ex.Message}"
                + "  (the default refuses rather than truncating, because a configured value quietly becoming 3 is the worse failure)");
        }

        var lenient = new YamlSerializerOptions
        {
            PropertyNamingPolicy = NamingPolicy.SnakeCaseLower,
            NumberHandling = YamlNumberHandling.AllowFloatToInteger,
        };
        var truncated = YamlSerializer.Deserialize<Threshold>("count: 3.7", lenient)!;
        Console.WriteLine($"  AllowFloatToInteger: 3.7 -> int accepted -> {truncated.Count}"
            + "  (truncated toward zero, and only because it was asked for explicitly)");

        Console.WriteLine();
    }
}
