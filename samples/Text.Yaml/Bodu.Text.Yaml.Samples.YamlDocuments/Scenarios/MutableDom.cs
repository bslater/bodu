// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MutableDom.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Yaml.Nodes;

namespace Bodu.Text.Yaml.Samples.YamlDocuments.Scenarios;

/// <summary>
/// Demonstrates the mutable <see cref="YamlNode" /> DOM — the <c>JsonNode</c>-style layer for
/// edit-in-place workflows: parse a document into a tree, read and rewrite values with indexers,
/// graft new mappings in, and emit the result, all without defining a POCO.
/// </summary>
public static class MutableDom
{
    /// <summary>
    /// Parses <c>Data/server-config.yaml</c> into a node tree, edits it, and re-emits YAML.
    /// </summary>
    public static void Run()
    {
        SampleConsole.Scenario(
            "The mutable DOM - editing a document without a POCO",
            what: "Parses the config file into a node tree, reads two values through indexers, overwrites a "
                + "leaf, grafts a whole new mapping built from nodes, and emits the edited tree as YAML.",
            why: "A typed model is right when the shape is known and stable. This layer is for when it is not: "
                + "a tool that edits one key in whatever file it is handed, a migration that adds a section to "
                + "documents it does not otherwise understand, a fixture built programmatically. Declaring a "
                + "POCO for those means enumerating a schema you do not care about and that the next file will "
                + "violate. The tree is mutable and self-describing instead, and the cost is that nothing "
                + "validates the shape - the right trade only when you genuinely do not know it.",
            expect: "The edited value and the grafted mapping both appear in the emitted document, with the "
                + "untouched parts unchanged. The new mapping emits with correct block indentation and its "
                + "sequence intact, so a tree built by hand produces the same wire form as a parsed one.");

        var yaml = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Data", "server-config.yaml"));
        var root = (YamlObject)YamlNode.Parse(yaml)!;

        // Read through indexers - each hop is a YamlNode; AsValue().GetValue<T>() unwraps the leaf.
        Console.WriteLine($"  title           : {root["title"]!.AsValue().GetValue<string>()}"
            + "  (each indexer hop returns a node; GetValue<T> unwraps the leaf to a CLR value)");
        Console.WriteLine($"  tls.certificate : {root["tls"]!["certificate"]!.AsValue().GetValue<string>()}"
            + "  (two hops into a nested mapping, with no type declared anywhere for its shape)");

        // Edit in place: assign a new leaf over an existing key.
        root["workers"] = YamlValue.Create(16);

        // Graft a whole new mapping in - built bottom-up from nodes.
        var logging = new YamlObject
        {
            ["level"] = YamlValue.Create("warning"),
            ["sinks"] = new YamlArray { YamlValue.Create("console"), YamlValue.Create("file") },
        };
        root.Add("logging", logging);

        // Emit the edited tree. ToYamlString() round-trips through Utf8YamlWriter.
        Console.WriteLine("  edited document (workers overwritten, logging grafted in, everything else untouched):");
        foreach (var line in root.ToYamlString().Split('\n').Where(l => l.Length > 0))
        {
            Console.WriteLine($"  | {line.TrimEnd()}");
        }

        Console.WriteLine();
    }
}
