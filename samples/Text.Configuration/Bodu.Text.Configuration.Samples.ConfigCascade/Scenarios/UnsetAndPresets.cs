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
        SampleConsole.Scenario(
            "The 'unset' value and dialect presets",
            what: "Resolves the same test-tree path twice with opposite unset-value modes, then resolves it once "
                + "more through the EditorConfig-compatible preset.",
            why: "A cascade can only add values, which leaves no way to say a deeper section should stop "
                + "inheriting one - so EditorConfig gives the literal text 'unset' that meaning. It is a genuine "
                + "dialect decision rather than an obvious one: a file that legitimately wants the string "
                + "\"unset\" as a value needs the other mode, so the library makes it explicit instead of "
                + "guessing. Presets exist because a dialect is not one switch but several that have to agree; "
                + "setting them individually is how a configuration reader ends up almost-compatible with the "
                + "format it claims to read.",
            expect: "The same key, the same path, two different answers - literal text under one mode, absent "
                + "under the other. The preset then reports the key as absent without naming any individual "
                + "switch, because it carries the whole EditorConfig dialect including this one.");

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

        Console.WriteLine($"  TreatAsLiteral      : max_line_length = '{literal.GetString("max_line_length", "(absent)")}'"
            + "  (the word is just a value here - the mode a file that genuinely stores the text \"unset\" needs)");
        Console.WriteLine($"  RemoveEffectiveValue: max_line_length = '{removed.GetString("max_line_length", "(absent)")}'"
            + "  (EditorConfig semantics - the key is gone from the view, which is the only way a cascade can un-inherit)");

        // Canonical profile option sets bundle coherent defaults: EditorConfigCompatible aligns
        // the whole pipeline (including RemoveEffectiveValue) with EditorConfig 0.17.2.
        var compat = document.Resolve("test/AppTests/ProgramTests.cs", ConfigurationResolveOptions.EditorConfigCompatible);
        Console.WriteLine($"  EditorConfig preset : max_line_length present = {compat.TryGetInt32("max_line_length", out _)}"
            + "  (expected False - the preset carries the whole dialect, so the switches cannot drift out of agreement)");

        Console.WriteLine();
    }
}
