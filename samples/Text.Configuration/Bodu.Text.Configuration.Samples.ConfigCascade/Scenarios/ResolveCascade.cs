// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ResolveCascade.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Configuration;

namespace Bodu.Samples.Text.Configuration.ConfigCascade.Scenarios;

/// <summary>
/// Demonstrates the heart of the library: path-targeted resolution. Every section whose glob
/// matches the target path contributes its keys, later sections overriding earlier ones — the
/// EditorConfig cascade — so one file expresses defaults plus per-tree exceptions.
/// </summary>
public static class ResolveCascade
{
    /// <summary>
    /// Resolves the same document for three target paths and compares the effective values.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- Resolve(targetPath): the cascade ---");

        var document = ConfigurationDocument.Load(Path.Combine(AppContext.BaseDirectory, "Data", "sample.boduconfig"));

        // Three targets: a production source file, a test file, and a non-.cs file.
        foreach (var target in new[] { "src/App/Program.cs", "test/AppTests/ProgramTests.cs", "README.md" })
        {
            var view = document.Resolve(target);

            var indent = view.GetInt32("indent_size");
            var lineLength = view.TryGetInt32("max_line_length", out var max) ? max.ToString() : "(unset)";
            Console.WriteLine($"{target,-32} indent_size = {indent}, max_line_length = {lineLength}");
        }

        // Typed getters parse the effective value; enums and fallbacks included.
        var src = document.Resolve("src/App/Program.cs");
        Console.WriteLine($"typed: start_day = {src.GetEnum<DayOfWeek>("start_day")}, strict_nullability = {src.GetBoolean("strict_nullability")}, theme = {src.GetString("theme", "(default)")}");

        Console.WriteLine();
    }
}
