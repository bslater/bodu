// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ReadOnlyDom.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Yaml.Document;

namespace Bodu.Text.Yaml.Samples.YamlDocuments.Scenarios;

/// <summary>
/// Demonstrates the read-only <see cref="YamlDocument" /> DOM — the <c>JsonDocument</c>-style layer
/// for inspect-without-materializing workflows: one parse, cheap <see cref="YamlElement" /> cursors
/// over it, typed getters, and safe probing for optional keys. The document owns the parsed data,
/// so it is <see cref="IDisposable" />.
/// </summary>
public static class ReadOnlyDom
{
    /// <summary>
    /// Parses <c>Data/server-config.yaml</c> and walks it with element cursors.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- Read-only DOM: YamlDocument / YamlElement ---");

        var yaml = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Data", "server-config.yaml"));
        using var document = YamlDocument.Parse(yaml);
        var root = document.RootElement;

        // Drill down with GetProperty and read leaves with the typed getters.
        Console.WriteLine($"title         : {root.GetProperty("title").GetString()}");
        Console.WriteLine($"workers       : {root.GetProperty("workers").GetInt64()}");
        Console.WriteLine($"drain_timeout : {root.GetProperty("drain_timeout").GetInt64()}");
        Console.WriteLine($"tls.enabled   : {root.GetProperty("tls").GetProperty("enabled").GetBoolean()}");

        // Enumerate a mapping without knowing its keys up front; each value reports its ValueKind.
        Console.WriteLine("limits        :");
        foreach (var property in root.GetProperty("limits").EnumerateMapping())
        {
            Console.WriteLine($"  {property.Name} = {property.Value.GetInt64()} ({property.Value.ValueKind})");
        }

        // Probe optional keys with TryGetProperty instead of catching exceptions.
        var hasProxy = root.TryGetProperty("proxy", out _);
        Console.WriteLine($"proxy present : {hasProxy}");

        Console.WriteLine();
    }
}
