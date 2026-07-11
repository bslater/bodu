// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NamingAndAttributes.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Toml;
using Bodu.Text.Toml.Serialization;

namespace Bodu.Text.Toml.Samples.TomlBasics.Scenarios;

/// <summary>
/// Demonstrates how wire names are chosen: a naming policy maps every property by convention, the
/// attribute family overrides it per member ([TomlPropertyName], [TomlIgnore], [TomlRequired]),
/// and <c>TomlStringEnumConverter</c> re-cases enum strings through a naming policy of their own.
/// </summary>
public static class NamingAndAttributes
{
    /// <summary>
    /// A small POCO exercised under different naming policies and converters.
    /// </summary>
    private sealed class JobSpec
    {
        public string JobName { get; set; } = "nightly-sync";

        public int TimeoutSeconds { get; set; } = 90;

        public DayOfWeek RunDay { get; set; } = DayOfWeek.Wednesday;
    }

    /// <summary>
    /// Serializes the same POCO under several policies, shows the attribute overrides, and
    /// contrasts default vs policy-cased enum output.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- Naming policies, attributes, and enum converters ---");

        // 1. The same POCO, three policies - only the key spelling changes.
        var spec = new JobSpec();
        foreach (var (label, policy) in new (string, TomlNamingPolicy)[]
        {
            ("CamelCase     ", TomlNamingPolicy.CamelCase),
            ("SnakeCaseLower", TomlNamingPolicy.SnakeCaseLower),
            ("KebabCaseLower", TomlNamingPolicy.KebabCaseLower),
        })
        {
            var line = TomlSerializer
                .Serialize(spec, new TomlSerializerOptions { PropertyNamingPolicy = policy })
                .Split('\n')[0].TrimEnd();
            Console.WriteLine($"  {label}: {line}");
        }

        // 2. Attributes override the policy: EndpointConfig.Address writes as "url",
        //    and AppConfig.DisplayLabel ([TomlIgnore]) never reaches the wire.
        var options = new TomlSerializerOptions { PropertyNamingPolicy = TomlNamingPolicy.SnakeCaseLower };
        var toml = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Data", "app-config.toml"));
        AppConfig config = TomlSerializer.Deserialize<AppConfig>(toml, options);
        var emitted = TomlSerializer.Serialize(config, options);

        Console.WriteLine($"  [TomlPropertyName] : endpoints write 'url =' -> {emitted.Contains("url = ", StringComparison.Ordinal)}");
        Console.WriteLine($"  [TomlIgnore]       : 'display_label' absent  -> {!emitted.Contains("display_label", StringComparison.Ordinal)}");

        // 3. [TomlRequired]: a document missing service_name is rejected up front,
        //    instead of silently deserializing an empty string.
        try
        {
            TomlSerializer.Deserialize<AppConfig>("max_retries = 1\nenabled = true", options);
        }
        catch (TomlSerializationException ex)
        {
            Console.WriteLine($"  [TomlRequired]     : missing key rejected   -> {ex.Message}");
        }

        // 4. Enums serialize as their .NET member name by default; TomlStringEnumConverter
        //    re-cases them through its own naming policy (and can reject integer input).
        var defaultForm = TomlSerializer.Serialize(spec, options);
        var cased = new TomlSerializerOptions { PropertyNamingPolicy = TomlNamingPolicy.SnakeCaseLower };
        cased.Converters.Add(new TomlStringEnumConverter(TomlNamingPolicy.SnakeCaseLower, allowIntegerValues: false));
        var casedForm = TomlSerializer.Serialize(spec, cased);

        Console.WriteLine($"  enum default       : {defaultForm.Split('\n').First(l => l.StartsWith("run_day", StringComparison.Ordinal)).TrimEnd()}");
        Console.WriteLine($"  enum policy-cased  : {casedForm.Split('\n').First(l => l.StartsWith("run_day", StringComparison.Ordinal)).TrimEnd()}");

        Console.WriteLine();
    }
}
