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
        Console.WriteLine("--- Read-only DOM: TomlDocument / TomlElement ---");

        var toml = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Data", "server-config.toml"));
        using var document = TomlDocument.Parse(toml);
        var root = document.RootElement;

        // Drill down with GetProperty and read leaves with the typed getters.
        Console.WriteLine($"title         : {root.GetProperty("title").GetString()}");
        Console.WriteLine($"workers       : {root.GetProperty("workers").GetInt64()}");
        Console.WriteLine($"drain_timeout : {root.GetProperty("drain_timeout").GetTimeOnly():HH:mm:ss}");
        Console.WriteLine($"tls.enabled   : {root.GetProperty("tls").GetProperty("enabled").GetBoolean()}");

        // Enumerate a table without knowing its keys up front.
        Console.WriteLine("limits        :");
        foreach (var property in root.GetProperty("limits").EnumerateObject())
        {
            Console.WriteLine($"  {property.Name} = {property.Value.GetInt64()} ({property.Value.ValueKind})");
        }

        // Probe optional keys with TryGetProperty instead of catching exceptions.
        var hasProxy = root.TryGetProperty("proxy", out _);
        Console.WriteLine($"proxy present : {hasProxy}");

        Console.WriteLine();
    }
}
