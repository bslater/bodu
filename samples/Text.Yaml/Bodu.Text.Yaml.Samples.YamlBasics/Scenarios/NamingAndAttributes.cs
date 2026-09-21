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
        SampleConsole.Scenario(
            "Naming policies, attributes, and enum output",
            what: "Serializes one POCO under three naming policies, shows a property renamed by attribute and "
                + "another excluded entirely, provokes the missing-required-key failure, then emits an enum as "
                + "a name and as a number.",
            why: "The wire name and the C# name answer to different audiences. YAML files are hand-edited, so "
                + "their keys follow the format conventions rather than .NET ones, and a policy settles that "
                + "for a whole type instead of an attribute per property. Attributes cover what a convention "
                + "cannot reach: a key whose spelling is fixed by an external contract, a computed property that "
                + "should never appear in an editable file, and a key whose absence is an error rather than a "
                + "default. The last is the one that earns its place - without it a missing service_name "
                + "deserializes to an empty string and fails somewhere unrelated, long after the config was "
                + "read.",
            expect: "One type produces three key spellings with no change to the class. The renamed property "
                + "appears under its wire name, the ignored one is absent entirely, and the missing required "
                + "key is rejected by name at deserialize time. The enum rows show why the string form is the "
                + "default: the name survives a renumbering of the enum, the number does not.");

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

        Console.WriteLine("  (one class, three key spellings - the convention belongs to the file, not to the C# type)");

        // 2. Attributes override the policy: EndpointConfig.Address writes as "url",
        //    and AppConfig.DisplayLabel ([Ignore]) never reaches the wire.
        var options = new YamlSerializerOptions { PropertyNamingPolicy = NamingPolicy.SnakeCaseLower };
        var yaml = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Data", "app-config.yaml"));
        AppConfig config = YamlSerializer.Deserialize<AppConfig>(yaml, options)!;
        var emitted = YamlSerializer.Serialize(config, options);

        Console.WriteLine($"  [PropertyName] : endpoints write 'url:' -> {emitted.Contains("url: ", StringComparison.Ordinal)}"
            + "  (expected True - the attribute overrides the policy where an external contract fixes the spelling)");
        Console.WriteLine($"  [Ignore]       : 'display_label' absent -> {!emitted.Contains("display_label", StringComparison.Ordinal)}"
            + "  (expected True - a computed property in an editable file looks like something a reader may change)");

        // 3. [Required]: a document missing service_name is rejected up front,
        //    instead of silently deserializing an empty string.
        try
        {
            YamlSerializer.Deserialize<AppConfig>("max_retries: 1\nenabled: true", options);
        }
        catch (YamlSerializationException ex)
        {
            Console.WriteLine($"  [Required]     : missing key rejected -> {ex.Message}"
                + "  (without this the field would bind to an empty string and fail somewhere unrelated, much later)");
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
        Console.WriteLine($"  enum as number : {numericForm.Split('\n').First(l => l.StartsWith("run_day", StringComparison.Ordinal)).TrimEnd()}"
            + "  (why the string form is the default: the name survives a renumbering of the enum, the number does not)");

        Console.WriteLine();
    }
}
