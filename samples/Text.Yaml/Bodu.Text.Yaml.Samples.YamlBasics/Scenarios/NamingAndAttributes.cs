// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NamingAndAttributes.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Serialization;

namespace Bodu.Text.Yaml.Samples.YamlBasics.Scenarios;

/// <summary>
/// Demonstrates how wire names are chosen: a naming policy maps every property by convention, the
/// attribute family overrides it per member ([PropertyName], [Ignore], [Required]), and the
/// <see cref="YamlSerializerOptions.WriteEnumsAsStrings" /> knob switches enum output between the
/// member name and its numeric value.
/// </summary>
public static class NamingAndAttributes
{
    /// <summary>
    /// A small POCO exercised under different naming policies and enum settings.
    /// </summary>
    private sealed class JobSpec
    {
        public string JobName { get; set; } = "nightly-sync";

        public int TimeoutSeconds { get; set; } = 90;

        public DayOfWeek RunDay { get; set; } = DayOfWeek.Wednesday;
    }

    /// <summary>
    /// Serializes the same POCO under several policies, shows the attribute overrides, and
    /// contrasts string vs numeric enum output.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- Naming policies, attributes, and enum output ---");

        // 1. The same POCO, three policies - only the key spelling changes.
        var spec = new JobSpec();
        foreach (var (label, policy) in new (string, NamingPolicy)[]
        {
            ("CamelCase     ", NamingPolicy.CamelCase),
            ("SnakeCaseLower", NamingPolicy.SnakeCaseLower),
            ("KebabCaseLower", NamingPolicy.KebabCaseLower),
        })
        {
            var line = YamlSerializer
                .Serialize(spec, new YamlSerializerOptions { PropertyNamingPolicy = policy })
                .Split('\n')[0].TrimEnd();
            Console.WriteLine($"  {label}: {line}");
        }

        // 2. Attributes override the policy: EndpointConfig.Address writes as "url",
        //    and AppConfig.DisplayLabel ([Ignore]) never reaches the wire.
        var options = new YamlSerializerOptions { PropertyNamingPolicy = NamingPolicy.SnakeCaseLower };
        var yaml = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Data", "app-config.yaml"));
        AppConfig config = YamlSerializer.Deserialize<AppConfig>(yaml, options)!;
        var emitted = YamlSerializer.Serialize(config, options);

        Console.WriteLine($"  [PropertyName] : endpoints write 'url:' -> {emitted.Contains("url: ", StringComparison.Ordinal)}");
        Console.WriteLine($"  [Ignore]       : 'display_label' absent -> {!emitted.Contains("display_label", StringComparison.Ordinal)}");

        // 3. [Required]: a document missing service_name is rejected up front,
        //    instead of silently deserializing an empty string.
        try
        {
            YamlSerializer.Deserialize<AppConfig>("max_retries: 1\nenabled: true", options);
        }
        catch (YamlSerializationException ex)
        {
            Console.WriteLine($"  [Required]     : missing key rejected -> {ex.Message}");
        }

        // 4. Enums serialize as their .NET member name by default; setting WriteEnumsAsStrings
        //    to false emits the numeric value of the underlying enum instead.
        var asName = YamlSerializer.Serialize(spec, options);
        var asNumber = new YamlSerializerOptions
        {
            PropertyNamingPolicy = NamingPolicy.SnakeCaseLower,
            WriteEnumsAsStrings = false,
        };
        var numericForm = YamlSerializer.Serialize(spec, asNumber);

        Console.WriteLine($"  enum as string : {asName.Split('\n').First(l => l.StartsWith("run_day", StringComparison.Ordinal)).TrimEnd()}");
        Console.WriteLine($"  enum as number : {numericForm.Split('\n').First(l => l.StartsWith("run_day", StringComparison.Ordinal)).TrimEnd()}");

        Console.WriteLine();
    }
}
