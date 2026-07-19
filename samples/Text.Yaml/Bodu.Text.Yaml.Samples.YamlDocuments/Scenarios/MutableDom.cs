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
        Console.WriteLine("--- Mutable DOM: YamlNode / YamlObject / YamlValue ---");

        var yaml = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Data", "server-config.yaml"));
        var root = (YamlObject)YamlNode.Parse(yaml)!;

        // Read through indexers - each hop is a YamlNode; AsValue().GetValue<T>() unwraps the leaf.
        Console.WriteLine($"title           : {root["title"]!.AsValue().GetValue<string>()}");
        Console.WriteLine($"tls.certificate : {root["tls"]!["certificate"]!.AsValue().GetValue<string>()}");

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
        Console.WriteLine("edited document :");
        foreach (var line in root.ToYamlString().Split('\n').Where(l => l.Length > 0))
        {
            Console.WriteLine($"  | {line.TrimEnd()}");
        }

        Console.WriteLine();
    }
}
