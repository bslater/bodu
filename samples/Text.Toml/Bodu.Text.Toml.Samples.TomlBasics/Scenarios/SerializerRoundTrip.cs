// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SerializerRoundTrip.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Toml;

namespace Bodu.Text.Toml.Samples.TomlBasics.Scenarios;

/// <summary>
/// Demonstrates the serializer's core loop: deserialize a committed TOML file into a typed POCO
/// graph (nested tables, an array of tables), then serialize it back and read it again — the
/// System.Text.Json-shaped workflow, for TOML.
/// </summary>
public static class SerializerRoundTrip
{
    /// <summary>
    /// Deserializes <c>Data/app-config.toml</c>, mutates the POCO, and round-trips it.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- TomlSerializer: file -> POCO -> TOML -> POCO ---");

        var toml = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Data", "app-config.toml"));

        // Deserialize with the snake_case policy so service_name binds to ServiceName.
        var options = new TomlSerializerOptions { PropertyNamingPolicy = TomlNamingPolicy.SnakeCaseLower };
        AppConfig config = TomlSerializer.Deserialize<AppConfig>(toml, options);

        Console.WriteLine($"Service   : {config.ServiceName} (enabled: {config.Enabled}, retries: {config.MaxRetries})");
        Console.WriteLine($"Database  : {config.Database.Host}:{config.Database.Port}");
        Console.WriteLine($"Endpoints : {string.Join(", ", config.Endpoints.Select(e => $"{e.Name} -> {e.Address}"))}");

        // Mutate and serialize back - nested tables and the array of tables re-emit as TOML.
        config.MaxRetries = 5;
        var emitted = TomlSerializer.Serialize(config, options);

        AppConfig reloaded = TomlSerializer.Deserialize<AppConfig>(emitted, options);
        Console.WriteLine($"Round trip: retries {reloaded.MaxRetries}, endpoints {reloaded.Endpoints.Count} (values preserved)");

        Console.WriteLine();
    }
}
