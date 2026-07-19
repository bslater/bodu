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
        Console.WriteLine("--- SpecVersion, scalar styles, duplicate and merge keys ---");

        // 1. SpecVersion: under 1.2 (default) only true/false are booleans, so plain "yes" is a
        //    String; under 1.1 the broader schema resolves "yes" to Boolean true.
        var norway = "enabled: yes";
        var v12 = YamlSerializer.Deserialize<Dictionary<string, object?>>(norway)!;
        var v11Options = new YamlSerializerOptions { SpecVersion = YamlSpecVersion.V1_1 };
        var v11 = YamlSerializer.Deserialize<Dictionary<string, object?>>(norway, v11Options)!;
        Console.WriteLine($"  V1_2 (default): 'yes' -> {v12["enabled"]!.GetType().Name} ({v12["enabled"]})");
        Console.WriteLine($"  V1_1          : 'yes' -> {v11["enabled"]!.GetType().Name} ({v11["enabled"]})");

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

        // 3. DuplicateKeyBehavior: a repeated key is a parse error by default (Throw); the lenient
        //    modes keep the first or the last occurrence deterministically.
        var duplicate = "port: 80\nport: 443";
        try
        {
            YamlSerializer.Deserialize<Dictionary<string, object?>>(duplicate);
        }
        catch (YamlFormatException ex)
        {
            Console.WriteLine($"  Throw (default): duplicate rejected -> {ex.Message}");
        }

        var useFirst = YamlSerializer.Deserialize<Dictionary<string, object?>>(
            duplicate, new YamlSerializerOptions { DuplicateKeyBehavior = YamlDuplicateKeyBehavior.UseFirst })!;
        var useLast = YamlSerializer.Deserialize<Dictionary<string, object?>>(
            duplicate, new YamlSerializerOptions { DuplicateKeyBehavior = YamlDuplicateKeyBehavior.UseLast })!;
        Console.WriteLine($"  UseFirst       : port -> {useFirst["port"]}");
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
        Console.WriteLine($"  Expand (default): service keys -> [{string.Join(", ", expanded.Keys)}] (retries {expanded["retries"]})");
        Console.WriteLine($"  Disabled        : service keys -> [{string.Join(", ", disabled.Keys)}] ('<<' retained)");

        Console.WriteLine();
    }
}
