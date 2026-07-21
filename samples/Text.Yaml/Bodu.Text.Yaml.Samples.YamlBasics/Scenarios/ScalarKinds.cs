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
        Console.WriteLine("--- Implicit scalar typing and YamlNumberHandling ---");

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
            Console.WriteLine($"  Strict             : 3.7 -> int rejected -> {ex.Message}");
        }

        var lenient = new YamlSerializerOptions
        {
            PropertyNamingPolicy = NamingPolicy.SnakeCaseLower,
            NumberHandling = YamlNumberHandling.AllowFloatToInteger,
        };
        var truncated = YamlSerializer.Deserialize<Threshold>("count: 3.7", lenient)!;
        Console.WriteLine($"  AllowFloatToInteger: 3.7 -> int accepted -> {truncated.Count}");

        Console.WriteLine();
    }
}
