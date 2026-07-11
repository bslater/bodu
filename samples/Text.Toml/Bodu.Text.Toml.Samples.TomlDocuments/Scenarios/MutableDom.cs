// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MutableDom.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;
using Bodu.Text.Toml.Nodes;

namespace Bodu.Text.Toml.Samples.TomlDocuments.Scenarios;

/// <summary>
/// Demonstrates the mutable <see cref="TomlNode" /> DOM — the <c>JsonNode</c>-style layer for
/// edit-in-place workflows: parse a document into a tree, read and rewrite values with indexers,
/// graft new tables in, and emit the result, all without defining a POCO.
/// </summary>
public static class MutableDom
{
    /// <summary>
    /// Parses <c>Data/server-config.toml</c> into a node tree, edits it, and re-emits TOML.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- Mutable DOM: TomlNode / TomlObject / TomlValue ---");

        var bytes = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Data", "server-config.toml"));
        var root = (TomlObject)TomlNode.Parse(bytes)!;

        // Read through indexers - each hop is a TomlNode, GetValue<T>() unwraps the leaf.
        Console.WriteLine($"title            : {root["title"]!.GetValue<string>()}");
        Console.WriteLine($"tls.certificate  : {root["tls"]!["certificate"]!.GetValue<string>()}");

        // Edit in place: assign a new leaf over an existing key.
        root["workers"] = TomlValue.Create(16);

        // Graft a whole new table in - built bottom-up from nodes.
        var logging = new TomlObject
        {
            ["level"] = TomlValue.Create("warning"),
            ["sinks"] = new TomlArray(TomlValue.Create("console"), TomlValue.Create("file")),
        };
        root.Add("logging", logging);

        // Emit the edited tree. ToUtf8Bytes() round-trips through Utf8TomlWriter.
        var emitted = Encoding.UTF8.GetString(root.ToUtf8Bytes());
        Console.WriteLine("edited document  :");
        foreach (var line in emitted.Split('\n').Where(l => l.Length > 0))
        {
            Console.WriteLine($"  | {line.TrimEnd()}");
        }

        Console.WriteLine();
    }
}
