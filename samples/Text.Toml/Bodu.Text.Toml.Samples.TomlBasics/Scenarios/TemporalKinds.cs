// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TemporalKinds.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Toml;

namespace Bodu.Text.Toml.Samples.TomlBasics.Scenarios;

/// <summary>
/// Demonstrates TOML's headline feature over JSON: four native date-time kinds. An offset
/// date-time is an exact instant; a local date-time, local date, and local time deliberately carry
/// no zone — and the serializer maps each to the matching .NET type instead of forcing everything
/// through <see cref="DateTime" />.
/// </summary>
public static class TemporalKinds
{
    /// <summary>
    /// Round-trips each temporal kind and shows the wire form it produces.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- The four native temporal kinds ---");

        var options = new TomlSerializerOptions { PropertyNamingPolicy = TomlNamingPolicy.SnakeCaseLower };
        var toml = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Data", "app-config.toml"));
        AppConfig config = TomlSerializer.Deserialize<AppConfig>(toml, options);

        // Each TOML kind arrived as its natural .NET type - no invented midnights or UTC guesses.
        Console.WriteLine($"released_on        -> DateOnly       : {config.ReleasedOn}");
        Console.WriteLine($"maintenance_window -> TimeOnly       : {config.MaintenanceWindow}");
        Console.WriteLine($"build_stamp        -> DateTimeOffset : {config.BuildStamp} (offset {config.BuildStamp.Offset})");

        // And each .NET type serializes back to its distinct TOML kind.
        var emitted = TomlSerializer.Serialize(config, options);
        foreach (var line in emitted.Split('\n').Where(l =>
            l.StartsWith("released_on", StringComparison.Ordinal) ||
            l.StartsWith("maintenance_window", StringComparison.Ordinal) ||
            l.StartsWith("build_stamp", StringComparison.Ordinal)))
        {
            Console.WriteLine($"  wire: {line.TrimEnd()}");
        }

        Console.WriteLine();
    }
}
