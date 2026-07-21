// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SerializerRoundTrip.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Serialization;

namespace Bodu.Text.Yaml.Samples.YamlBasics.Scenarios;

/// <summary>
/// Demonstrates the serializer's core loop: deserialize a committed YAML file into a typed POCO
/// graph (a nested mapping, a block sequence of mappings), then serialize it back and read it
/// again — the System.Text.Json-shaped workflow, for YAML.
/// </summary>
public static class SerializerRoundTrip
{
    /// <summary>
    /// Deserializes <c>Data/app-config.yaml</c>, mutates the POCO, and round-trips it.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- YamlSerializer: file -> POCO -> YAML -> POCO ---");

        var yaml = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Data", "app-config.yaml"));

        // Deserialize with the snake_case policy so service_name binds to ServiceName.
        var options = new YamlSerializerOptions { PropertyNamingPolicy = NamingPolicy.SnakeCaseLower };
        AppConfig config = YamlSerializer.Deserialize<AppConfig>(yaml, options)!;

        Console.WriteLine($"Service   : {config.ServiceName} (enabled: {config.Enabled}, retries: {config.MaxRetries})");
        Console.WriteLine($"Database  : {config.Database.Host}:{config.Database.Port}");
        Console.WriteLine($"Endpoints : {string.Join(", ", config.Endpoints.Select(e => $"{e.Name} -> {e.Address}"))}");

        // Mutate and serialize back - the nested mapping and the sequence re-emit as block YAML.
        config.MaxRetries = 5;
        var emitted = YamlSerializer.Serialize(config, options);

        AppConfig reloaded = YamlSerializer.Deserialize<AppConfig>(emitted, options)!;
        Console.WriteLine($"Round trip: retries {reloaded.MaxRetries}, endpoints {reloaded.Endpoints.Count} (values preserved)");

        Console.WriteLine();
    }
}
