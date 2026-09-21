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
        SampleConsole.Scenario(
            "The mutable DOM - editing a document without a POCO",
            what: "Parses the config file into a node tree, reads two values through indexers, overwrites a leaf, "
                + "grafts a whole new table built from nodes, and emits the edited tree as TOML.",
            why: "A typed model is the right answer when the shape is known and stable. This layer is for the "
                + "cases where it is not: a tool that edits one key in whatever file it is handed, a migration "
                + "that adds a section to documents it does not otherwise understand, a test fixture built "
                + "programmatically. Defining a POCO for those means enumerating a schema you do not care about "
                + "and that will reject the next file. The tree is mutable and self-describing instead, and the "
                + "cost is that nothing validates the shape - which is the right trade only when you genuinely "
                + "do not know it.",
            expect: "The edited value and the grafted table both appear in the emitted document, and the parts "
                + "that were not touched come through unchanged. The new table emits as a proper [logging] "
                + "section with its array intact, so building a tree by hand produces the same wire form as "
                + "parsing one.");

        var bytes = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Data", "server-config.toml"));
        var root = (TomlObject)TomlNode.Parse(bytes)!;

        // Read through indexers - each hop is a TomlNode, GetValue<T>() unwraps the leaf.
        Console.WriteLine($"  title            : {root["title"]!.GetValue<string>()}"
            + "  (each indexer hop returns a node; GetValue<T> unwraps the leaf to a CLR value)");
        Console.WriteLine($"  tls.certificate  : {root["tls"]!["certificate"]!.GetValue<string>()}"
            + "  (two hops into a nested table, with no type declared anywhere for its shape)");

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
        Console.WriteLine("  edited document  (workers overwritten, [logging] grafted in, everything else untouched):");
        foreach (var line in emitted.Split('\n').Where(l => l.Length > 0))
        {
            Console.WriteLine($"  | {line.TrimEnd()}");
        }

        Console.WriteLine();
    }
}
