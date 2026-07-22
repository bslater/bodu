// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IniReadTypedValues.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Ini;
using Bodu.Text.Ini.Document;
using Bodu.Text.Serialization;

namespace Bodu.Samples.Text.Formats.ConfigFiles.Scenarios;

/// <summary>
/// Demonstrates reading an INI file: the global (sectionless) keys hoisted onto the root object, named sections as
/// nested objects via the read-only <see cref="IniDocument" />, and typed access by binding the whole file onto a
/// settings class with <see cref="IniSerializer" /> and the snake_case naming policy. Note the parser deliberately
/// treats <c>;</c> after a value as value content — values may legitimately contain semicolons.
/// </summary>
public static class IniReadTypedValues
{
    /// <summary>
    /// Parses <c>Data/app.ini</c> and reads global, section, and typed data.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- INI: global section, named sections, typed values ---");

        var path = Path.Combine(AppContext.BaseDirectory, "Data", "app.ini");
        var iniBytes = File.ReadAllBytes(path);
        using var document = IniDocument.Parse(iniBytes);
        var root = document.RootElement;

        // Keys before the first [section] hoist onto the root object; sections follow as objects.
        Console.WriteLine($"global      : environment = {root.GetProperty("environment").GetString()}");

        var sections = new List<string>();
        foreach (var property in root.EnumerateObject())
        {
            if (property.Value.ValueKind == IniValueKind.Object)
            {
                sections.Add(property.Name);
            }
        }

        Console.WriteLine($"sections    : {string.Join(", ", sections)}");

        // Element access into a section.
        var server = root.GetProperty("server");
        Console.WriteLine($"[server]    : {server.GetProperty("host").GetString()}:{server.GetProperty("port").GetString()}");

        // Typed access: bind the whole document onto a settings class via snake_case names.
        var options = new IniSerializerOptions { PropertyNamingPolicy = NamingPolicy.SnakeCaseLower };
        var config = IniSerializer.Deserialize<AppConfig>(File.ReadAllText(path), options);

        Console.WriteLine($"typed       : port = {config.Server!.Port}, timeout = {config.Server.RequestTimeout}s");
        Console.WriteLine($"dictionary  : [logging] level = {config.Logging!["level"]}, keys = {config.Logging.Count}");

        Console.WriteLine();
    }

    /// <summary>
    /// A typed view of the sample file: scalar members bind global keys; object members bind sections.
    /// </summary>
    private sealed class AppConfig
    {
        public string? Environment { get; set; }

        public ServerSection? Server { get; set; }

        public Dictionary<string, string>? Logging { get; set; }
    }

    /// <summary>
    /// The <c>[server]</c> section as a typed POCO.
    /// </summary>
    private sealed class ServerSection
    {
        public string? Host { get; set; }

        public int Port { get; set; }

        public int RequestTimeout { get; set; }
    }
}
