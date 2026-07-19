// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CollectionsAndDictionaries.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Serialization;

namespace Bodu.Text.Yaml.Samples.YamlBasics.Scenarios;

/// <summary>
/// Demonstrates how YAML's two container shapes bind to .NET collections: a block or flow
/// sequence maps to <see cref="List{T}" /> and arrays, a mapping maps to
/// <see cref="Dictionary{TKey, TValue}" />, and the two nest freely to any depth.
/// </summary>
public static class CollectionsAndDictionaries
{
    /// <summary>
    /// A POCO combining a sequence, a string-keyed dictionary, and a nested dictionary of lists.
    /// </summary>
    private sealed class Topology
    {
        public List<int> Ports { get; set; } = [];

        public Dictionary<string, int> Weights { get; set; } = [];

        public Dictionary<string, List<string>> Zones { get; set; } = [];
    }

    /// <summary>
    /// Deserializes each container shape, prints what bound, and round-trips the graph.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- Sequences -> List/array, mappings -> Dictionary ---");

        var yaml = """
            ports:
              - 8080
              - 8443
            weights:
              primary: 3
              backup: 1
            zones:
              us:
                - us-east
                - us-west
              eu:
                - eu-central
            """;

        var options = new YamlSerializerOptions { PropertyNamingPolicy = NamingPolicy.SnakeCaseLower };
        var topology = YamlSerializer.Deserialize<Topology>(yaml, options)!;

        Console.WriteLine($"  ports   -> List<int>                     : [{string.Join(", ", topology.Ports)}]");
        Console.WriteLine($"  weights -> Dictionary<string,int>        : {string.Join(", ", topology.Weights.Select(w => $"{w.Key}={w.Value}"))}");
        Console.WriteLine($"  zones   -> Dictionary<string,List<string>>: {string.Join("; ", topology.Zones.Select(z => $"{z.Key}=[{string.Join(",", z.Value)}]"))}");

        // The same flow sequence also binds to a fixed array target.
        var ports = YamlSerializer.Deserialize<int[]>("[8080, 8443, 9090]")!;
        Console.WriteLine($"  flow sequence -> int[]                   : [{string.Join(", ", ports)}]");

        // Round-trip the whole graph back through YAML and re-read it.
        var emitted = YamlSerializer.Serialize(topology, options);
        var reloaded = YamlSerializer.Deserialize<Topology>(emitted, options)!;
        Console.WriteLine($"  round trip: {reloaded.Ports.Count} ports, {reloaded.Weights.Count} weights, {reloaded.Zones.Count} zones (preserved)");

        Console.WriteLine();
    }
}
