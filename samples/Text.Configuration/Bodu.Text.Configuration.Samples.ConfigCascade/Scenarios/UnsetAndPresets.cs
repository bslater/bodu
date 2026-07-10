// ---------------------------------------------------------------------------------------------------------------
// <copyright file="UnsetAndPresets.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Configuration;

namespace Bodu.Samples.Text.Configuration.ConfigCascade.Scenarios;

/// <summary>
/// Demonstrates the dialect knobs: <see cref="ConfigurationUnsetValueMode" /> decides whether a
/// literal <c>unset</c> value removes the inherited value from the view (EditorConfig semantics)
/// or stays a plain string, and the <c>Presets</c> switch the whole pipeline's dialect in one
/// assignment instead of five.
/// </summary>
public static class UnsetAndPresets
{
    /// <summary>
    /// Resolves the test-tree target under both unset modes and via presets.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- 'unset' handling and dialect presets ---");

        var document = ConfigurationDocument.Load(Path.Combine(AppContext.BaseDirectory, "Data", "sample.boduconfig"));

        // [test/**.cs] sets max_line_length = unset. The two modes disagree about what that means.
        var literal = document.Resolve("test/AppTests/ProgramTests.cs", new ConfigurationResolveOptions
        {
            UnsetValueMode = ConfigurationUnsetValueMode.TreatAsLiteral,
        });
        var removed = document.Resolve("test/AppTests/ProgramTests.cs", new ConfigurationResolveOptions
        {
            UnsetValueMode = ConfigurationUnsetValueMode.RemoveEffectiveValue,
        });

        Console.WriteLine($"TreatAsLiteral      : max_line_length = '{literal.GetString("max_line_length", "(absent)")}'");
        Console.WriteLine($"RemoveEffectiveValue: max_line_length = '{removed.GetString("max_line_length", "(absent)")}'");

        // Canonical profile option sets bundle coherent defaults: EditorConfigCompatible aligns
        // the whole pipeline (including RemoveEffectiveValue) with EditorConfig 0.17.2.
        var compat = document.Resolve("test/AppTests/ProgramTests.cs", ConfigurationResolveOptions.EditorConfigCompatible);
        Console.WriteLine($"EditorConfig preset : max_line_length present = {compat.TryGetInt32("max_line_length", out _)}");

        Console.WriteLine();
    }
}
