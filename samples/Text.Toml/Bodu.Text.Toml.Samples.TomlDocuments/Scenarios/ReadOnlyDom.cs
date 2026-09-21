// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ReadOnlyDom.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Toml.Document;

namespace Bodu.Text.Toml.Samples.TomlDocuments.Scenarios;

/// <summary>
/// Demonstrates the read-only <see cref="TomlDocument" /> DOM — the <c>JsonDocument</c>-style
/// layer for inspect-without-materializing workflows: one parse, cheap <see cref="TomlElement" />
/// cursors over it, typed getters, and safe probing for optional keys. The document owns the
/// parsed data, so it is <see cref="IDisposable" />.
/// </summary>
public static class ReadOnlyDom
{
    /// <summary>
    /// Parses <c>Data/server-config.toml</c> and walks it with element cursors.
    /// </summary>
    public static void Run()
    {
        SampleConsole.Scenario(
            "The read-only DOM - inspecting without materializing",
            what: "Parses the document once, reads four typed leaves including a nested one, enumerates a table "
                + "whose keys are not known in advance, and probes for an optional key that is not there.",
            why: "This is the cheapest way to read a document you are not going to change. One parse produces one "
                + "buffer, and the elements are cursors into it rather than objects built from it - so walking "
                + "the tree allocates nothing further, and reading two keys out of a large document does not "
                + "cost the whole document. Because the buffer is owned rather than borrowed the document is "
                + "disposable, which is the one thing to remember about this layer. TryGetProperty exists "
                + "because optional keys are the normal case in configuration, and exceptions are the wrong "
                + "mechanism for something expected.",
            expect: "The typed getters return CLR values rather than strings, including the local time, which "
                + "TOML models natively. Enumerating the table reports each value's kind alongside it, so a "
                + "consumer can branch on shape without a schema. The absent key reports False instead of "
                + "throwing - the probe is what optional keys should use.");

        var toml = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Data", "server-config.toml"));
        using var document = TomlDocument.Parse(toml);
        var root = document.RootElement;

        // Drill down with GetProperty and read leaves with the typed getters.
        Console.WriteLine($"  title         : {root.GetProperty("title").GetString()}");
        Console.WriteLine($"  workers       : {root.GetProperty("workers").GetInt64()}");
        Console.WriteLine($"  drain_timeout : {root.GetProperty("drain_timeout").GetTimeOnly():HH:mm:ss}"
            + "  (a native TOML local time, returned as a TimeOnly rather than as text to be re-parsed)");
        Console.WriteLine($"  tls.enabled   : {root.GetProperty("tls").GetProperty("enabled").GetBoolean()}"
            + "  (each GetProperty is a cursor move within the one parsed buffer, not a new object)");

        // Enumerate a table without knowing its keys up front.
        Console.WriteLine("  limits        :");
        foreach (var property in root.GetProperty("limits").EnumerateObject())
        {
            Console.WriteLine($"    {property.Name} = {property.Value.GetInt64()} ({property.Value.ValueKind})");
        }

        Console.WriteLine("  (each value reports its kind, so a consumer can branch on shape without a schema)");

        // Probe optional keys with TryGetProperty instead of catching exceptions.
        var hasProxy = root.TryGetProperty("proxy", out _);
        Console.WriteLine($"  proxy present : {hasProxy}"
            + "  (expected False - optional keys are the normal case in config, so probing beats catching)");

        Console.WriteLine();
    }
}
