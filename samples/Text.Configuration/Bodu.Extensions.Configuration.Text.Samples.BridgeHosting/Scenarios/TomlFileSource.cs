// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlFileSource.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Extensions.Configuration.Text;
using Microsoft.Extensions.Configuration;

namespace Bodu.Samples.Extensions.Configuration.Text.BridgeHosting.Scenarios;

/// <summary>
/// Demonstrates <c>AddTomlFile</c>: a TOML document becomes an <see cref="IConfiguration" />
/// source, its tables flattened to the standard colon-separated keys — <c>[server.limits]</c>
/// surfaces as <c>server:limits:*</c> — alongside any other provider in the same builder.
/// </summary>
public static class TomlFileSource
{
    /// <summary>
    /// Builds configuration from <c>Data/settings.toml</c> and reads flattened keys.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- AddTomlFile: TOML -> IConfiguration ---");

        // AddTomlFile reads the path directly (absolute, or relative to the working
        // directory) - it does not consult the builder's file provider, so anchor it.
        var tomlPath = Path.Combine(AppContext.BaseDirectory, "Data", "settings.toml");

        IConfiguration configuration = new ConfigurationBuilder()
            .AddTomlFile(tomlPath)
            .Build();

        Console.WriteLine($"title                    : {configuration["title"]}");
        Console.WriteLine($"server:host              : {configuration["server:host"]}");
        Console.WriteLine($"server:port              : {configuration["server:port"]}");
        Console.WriteLine($"server:limits:max_connections : {configuration["server:limits:max_connections"]}");

        // GetSection composes like any other provider's tree.
        var server = configuration.GetSection("server");
        Console.WriteLine($"GetSection(\"server\")     : {server.GetChildren().Count()} children");

        // Optional files are skipped instead of throwing.
        var withOptional = new ConfigurationBuilder()
            .AddTomlFile(Path.Combine(AppContext.BaseDirectory, "Data", "does-not-exist.toml"), optional: true)
            .Build();
        Console.WriteLine($"optional missing file    : builds clean ({withOptional.GetChildren().Count()} keys)");

        Console.WriteLine();
    }
}
