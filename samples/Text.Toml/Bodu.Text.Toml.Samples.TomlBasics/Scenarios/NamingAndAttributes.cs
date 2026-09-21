// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NamingAndAttributes.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Serialization;
using Bodu.Text.Toml;
using Bodu.Text.Toml.Serialization;

namespace Bodu.Text.Toml.Samples.TomlBasics.Scenarios;

/// <summary>
/// Demonstrates how wire names are chosen: a naming policy maps every property by convention, the
/// attribute family overrides it per member ([PropertyName], [Ignore], [Required]),
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
        SampleConsole.Scenario(
            "Naming policies, attributes, and enum converters",
            what: "Serializes one POCO under three naming policies, shows a property renamed by attribute and "
                + "another excluded entirely, provokes the missing-required-key failure, then contrasts default "
                + "enum output with a policy-cased converter.",
            why: "The wire name and the C# name answer to different audiences. TOML files are hand-edited, so "
                + "their keys follow the format's conventions rather than .NET's, and a policy handles that for "
                + "the whole type at once instead of an attribute per property. Attributes then exist for the "
                + "cases a convention cannot reach: a key whose spelling is fixed by an external contract, a "
                + "computed property that should never reach the wire, and a key whose absence is an error "
                + "rather than a default. That last one matters most - without it a missing service_name "
                + "deserializes to an empty string and the failure surfaces somewhere unrelated, long after the "
                + "config was read.",
            expect: "One type produces three different key spellings with no change to the class. The renamed "
                + "property appears under its wire name and the ignored one is absent from the output entirely. "
                + "The missing required key is rejected at deserialize time naming the key, rather than "
                + "producing an object with a silently empty field. The enum rows show the same value spelled "
                + "two ways depending on the converter.");

        // 1. The same POCO, three policies - only the key spelling changes.
        var spec = new JobSpec();
        foreach (var (label, policy) in new (string, NamingPolicy)[]
        {
            ("CamelCase     ", NamingPolicy.CamelCase),
            ("SnakeCaseLower", NamingPolicy.SnakeCaseLower),
            ("KebabCaseLower", NamingPolicy.KebabCaseLower),
        })
        {
            var line = TomlSerializer
                .Serialize(spec, new TomlSerializerOptions { PropertyNamingPolicy = policy })
                .Split('\n')[0].TrimEnd();
            Console.WriteLine($"  {label}: {line}");
        }

        Console.WriteLine("  (one class, three key spellings - the convention belongs to the file format, not to the C# type)");

        // 2. Attributes override the policy: EndpointConfig.Address writes as "url",
        //    and AppConfig.DisplayLabel ([Ignore]) never reaches the wire.
        var options = new TomlSerializerOptions { PropertyNamingPolicy = NamingPolicy.SnakeCaseLower };
        var toml = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Data", "app-config.toml"));
        AppConfig config = TomlSerializer.Deserialize<AppConfig>(toml, options);
        var emitted = TomlSerializer.Serialize(config, options);

        Console.WriteLine($"  [PropertyName] : endpoints write 'url =' -> {emitted.Contains("url = ", StringComparison.Ordinal)}"
            + "  (expected True - the attribute overrides the policy where an external contract fixes the spelling)");
        Console.WriteLine($"  [Ignore]       : 'display_label' absent  -> {!emitted.Contains("display_label", StringComparison.Ordinal)}"
            + "  (expected True - a computed property has no business on the wire, where it would look editable)");

        // 3. [Required]: a document missing service_name is rejected up front,
        //    instead of silently deserializing an empty string.
        try
        {
            TomlSerializer.Deserialize<AppConfig>("max_retries = 1\nenabled = true", options);
        }
        catch (TomlSerializationException ex)
        {
            Console.WriteLine($"  [Required]     : missing key rejected   -> {ex.Message}"
                + "  (without this the field would deserialize to an empty string and fail somewhere unrelated, much later)");
        }

        // 4. Enums serialize as their .NET member name by default; TomlStringEnumConverter
        //    re-cases them through its own naming policy (and can reject integer input).
        var defaultForm = TomlSerializer.Serialize(spec, options);
        var cased = new TomlSerializerOptions { PropertyNamingPolicy = NamingPolicy.SnakeCaseLower };
        cased.Converters.Add(new TomlStringEnumConverter(NamingPolicy.SnakeCaseLower, allowIntegerValues: false));
        var casedForm = TomlSerializer.Serialize(spec, cased);

        Console.WriteLine($"  enum default       : {defaultForm.Split('\n').First(l => l.StartsWith("run_day", StringComparison.Ordinal)).TrimEnd()}"
            + "  (the .NET member name verbatim)");
        Console.WriteLine($"  enum policy-cased  : {casedForm.Split('\n').First(l => l.StartsWith("run_day", StringComparison.Ordinal)).TrimEnd()}"
            + "  (re-cased through its own policy, and configured to reject integer input so a stray number is an error rather than a silent enum value)");

        Console.WriteLine();
    }
}
