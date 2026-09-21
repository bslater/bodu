// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SpecAndStyles.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Yaml.Samples.YamlBasics.Scenarios;

/// <summary>
/// Demonstrates the parse- and emit-level knobs a consumer eventually needs: <see cref="YamlSpecVersion" />
/// gates the implicit typing of plain scalars (the YAML 1.1 "Norway problem"), the writer selects a
/// safe <see cref="YamlScalarStyle" /> per value, <see cref="YamlDuplicateKeyBehavior" /> resolves a
/// repeated mapping key, and <see cref="YamlMergeKeyBehavior" /> controls the merge key (<c>&lt;&lt;</c>).
/// </summary>
public static class SpecAndStyles
{
    /// <summary>
    /// Exercises spec-version typing, scalar-style selection, and the duplicate/merge-key policies.
    /// </summary>
    public static void Run()
    {
        SampleConsole.Scenario(
            "Spec version, scalar styles, duplicate keys and merge keys",
            what: "Resolves the same plain scalar under both YAML spec versions, emits two string values and "
                + "shows which one the writer quotes, rejects a repeated mapping key and then resolves it under "
                + "both lenient policies, and expands a merge key before disabling it.",
            why: "Each of these is a place where YAML is ambiguous enough that a library has to take a position. "
                + "The spec version is the famous one - under YAML 1.1 the plain scalar no resolves to a "
                + "boolean, which is why a country code for Norway can become false in a config file; 1.2 "
                + "narrows booleans to true and false, so it is the default here. Quoting on output is the same "
                + "problem in reverse: the writer has to quote any string that would resolve to a non-string on "
                + "the way back in, or the document does not round-trip. Duplicate keys are undefined behaviour "
                + "in practice - silently keeping one is how a config change appears to do nothing - so the "
                + "default is to refuse and the lenient modes are explicit about which occurrence wins. Merge "
                + "keys are a genuine feature, but one that makes a mapping depend on an anchor defined "
                + "elsewhere, so it can be turned off.",
            expect: "The same document yields a string under 1.2 and a boolean under 1.1 - one line of config, "
                + "two meanings, decided by a setting rather than by the file. The writer leaves an unambiguous "
                + "value plain and quotes the one that would otherwise come back as a boolean. The duplicate key "
                + "is an error by default and resolves differently under each lenient policy, and disabling the "
                + "merge key leaves it visible as an ordinary key rather than expanding it.");

        // 1. SpecVersion: under 1.2 (default) only true/false are booleans, so plain "yes" is a
        //    String; under 1.1 the broader schema resolves "yes" to Boolean true.
        var norway = "enabled: yes";
        var v12 = YamlSerializer.Deserialize<Dictionary<string, object?>>(norway)!;
        var v11Options = new YamlSerializerOptions { SpecVersion = YamlSpecVersion.V1_1 };
        var v11 = YamlSerializer.Deserialize<Dictionary<string, object?>>(norway, v11Options)!;
        Console.WriteLine($"  V1_2 (default): 'yes' -> {v12["enabled"]!.GetType().Name} ({v12["enabled"]})"
            + "  (1.2 narrows booleans to true/false, so the default does not reinterpret words an author wrote as text)");
        Console.WriteLine($"  V1_1          : 'yes' -> {v11["enabled"]!.GetType().Name} ({v11["enabled"]})"
            + "  (the broader 1.1 schema - the same rule that turns the country code NO into false)");

        // 2. Scalar style: the writer emits a plain scalar when unambiguous and double-quotes a
        //    value that would otherwise resolve to a non-string (YamlScalarStyle.Plain vs DoubleQuoted).
        var styled = YamlSerializer.Serialize(new Dictionary<string, string>
        {
            ["plain"] = "hello",
            ["quoted"] = "true",
        });
        foreach (var line in styled.Split('\n').Where(l => l.Length > 0))
        {
            Console.WriteLine($"  style: {line.TrimEnd()}");
        }

        Console.WriteLine("  (the writer quotes exactly what has to be quoted: leaving \"true\" plain would read back as a boolean)");

        // 3. DuplicateKeyBehavior: a repeated key is a parse error by default (Throw); the lenient
        //    modes keep the first or the last occurrence deterministically.
        var duplicate = "port: 80\nport: 443";
        try
        {
            YamlSerializer.Deserialize<Dictionary<string, object?>>(duplicate);
        }
        catch (YamlFormatException ex)
        {
            Console.WriteLine($"  Throw (default): duplicate rejected -> {ex.Message}"
                + "  (silently keeping one occurrence is how an edit to a config file appears to do nothing at all)");
        }

        var useFirst = YamlSerializer.Deserialize<Dictionary<string, object?>>(
            duplicate, new YamlSerializerOptions { DuplicateKeyBehavior = YamlDuplicateKeyBehavior.UseFirst })!;
        var useLast = YamlSerializer.Deserialize<Dictionary<string, object?>>(
            duplicate, new YamlSerializerOptions { DuplicateKeyBehavior = YamlDuplicateKeyBehavior.UseLast })!;
        Console.WriteLine($"  UseFirst       : port -> {useFirst["port"]}"
            + "  (deterministic, and explicit about which occurrence wins rather than leaving it to parse order)");
        Console.WriteLine($"  UseLast        : port -> {useLast["port"]}");

        // 4. MergeKeyBehavior: '<<' pulls an anchored mapping's keys in (Expand, default); Disabled
        //    leaves '<<' as an ordinary key, so it survives as a literal entry.
        var merge = """
            defaults: &defaults
              timeout: 30
              retries: 3
            service:
              <<: *defaults
              retries: 5
            """;
        var expanded = (Dictionary<string, object?>)YamlSerializer.Deserialize<Dictionary<string, object?>>(merge)!["service"]!;
        var disabled = (Dictionary<string, object?>)YamlSerializer.Deserialize<Dictionary<string, object?>>(
            merge, new YamlSerializerOptions { MergeKeyBehavior = YamlMergeKeyBehavior.Disabled })!["service"]!;
        Console.WriteLine($"  Expand (default): service keys -> [{string.Join(", ", expanded.Keys)}] (retries {expanded["retries"]})"
            + "  (timeout came from the anchor, and the local retries overrode the merged one)");
        Console.WriteLine($"  Disabled        : service keys -> [{string.Join(", ", disabled.Keys)}] ('<<' retained)"
            + "  (the merge key stays an ordinary key - the opt-out for documents where a mapping should not depend on an anchor elsewhere)");

        Console.WriteLine();
    }
}
