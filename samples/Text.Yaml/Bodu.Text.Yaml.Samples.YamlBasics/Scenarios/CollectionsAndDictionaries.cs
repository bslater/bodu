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
        SampleConsole.Scenario(
            "Sequences to lists and arrays, mappings to dictionaries",
            what: "Binds a document containing a block sequence, a string-keyed mapping, and a mapping of "
                + "sequences onto one class, then binds a flow sequence to a fixed array and round-trips the "
                + "whole graph.",
            why: "YAML has exactly two container shapes, and the useful thing about them is that they compose "
                + "without limit - a mapping of sequences of mappings is ordinary YAML, not an extension. The "
                + "binding rules follow from that: a sequence is a list or an array, a mapping is a dictionary "
                + "or an object, and which one you get is decided by the target type rather than by anything in "
                + "the document. That is why a section whose keys are open-ended can bind to a dictionary and "
                + "one whose shape is fixed to a class, in the same file and on the same class.",
            expect: "Three different .NET shapes bound from three container expressions, including a nested "
                + "dictionary of lists that required no special handling. The flow sequence binds to an array "
                + "from the same rules that bound the block sequence to a list - block and flow are two "
                + "spellings of one structure, not two structures.");

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
        Console.WriteLine($"  zones   -> Dictionary<string,List<string>>: {string.Join("; ", topology.Zones.Select(z => $"{z.Key}=[{string.Join(",", z.Value)}]"))}"
            + "  (containers compose without limit - a mapping of sequences needs no special handling)");

        // The same flow sequence also binds to a fixed array target.
        var ports = YamlSerializer.Deserialize<int[]>("[8080, 8443, 9090]")!;
        Console.WriteLine($"  flow sequence -> int[]                   : [{string.Join(", ", ports)}]"
            + "  (flow and block are two spellings of one structure - the target type decides list or array, not the document)");

        // Round-trip the whole graph back through YAML and re-read it.
        var emitted = YamlSerializer.Serialize(topology, options);
        var reloaded = YamlSerializer.Deserialize<Topology>(emitted, options)!;
        Console.WriteLine($"  round trip: {reloaded.Ports.Count} ports, {reloaded.Weights.Count} weights, {reloaded.Zones.Count} zones (preserved)"
            + "  (all three shapes survive a re-emit and re-read, nesting included)");

        Console.WriteLine();
    }
}
