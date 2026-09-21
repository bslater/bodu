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
        SampleConsole.Scenario(
            "Resolve(targetPath) - the cascade",
            what: "Resolves the same document against three target paths - a production source file, a test "
                + "file, and a markdown file - and prints the effective values each one sees, then reads typed "
                + "values through the enum, boolean and defaulted-string getters.",
            why: "This is the idea the whole library is built around, and it inverts how configuration usually "
                + "works. Rather than a file per directory, one file holds defaults plus glob-targeted "
                + "exceptions, and a value is only meaningful relative to the path you are asking about. Every "
                + "section whose glob matches contributes, with later sections overriding earlier ones, so "
                + "specificity is expressed by ordering rather than by a precedence algorithm nobody can predict. "
                + "The typed getters exist because the file format has exactly one value type - text - and every "
                + "consumer would otherwise reimplement the same invariant-culture parsing, differently.",
            expect: "Three paths, three different effective configurations, from one document and no per-"
                + "directory files. The test file picks up an override the source file does not, and the "
                + "markdown file falls through to the defaults because no .cs glob matches it. The typed row "
                + "shows a value absent from the file resolving to its supplied default rather than throwing.");

        var document = ConfigurationDocument.Load(Path.Combine(AppContext.BaseDirectory, "Data", "sample.boduconfig"));

        // Three targets: a production source file, a test file, and a non-.cs file.
        foreach (var target in new[] { "src/App/Program.cs", "test/AppTests/ProgramTests.cs", "README.md" })
        {
            var view = document.Resolve(target);

            var indent = view.GetInt32("indent_size");
            var lineLength = view.TryGetInt32("max_line_length", out var max) ? max.ToString() : "(unset)";
            Console.WriteLine($"  {target,-32} indent_size = {indent}, max_line_length = {lineLength}");
        }

        Console.WriteLine("  (one document, three effective configurations - the path is the input, and section order decides which override wins)");

        // Typed getters parse the effective value; enums and fallbacks included.
        var src = document.Resolve("src/App/Program.cs");
        Console.WriteLine($"  typed: start_day = {src.GetEnum<DayOfWeek>("start_day")}, strict_nullability = {src.GetBoolean("strict_nullability")}, theme = {src.GetString("theme", "(default)")}"
            + "  (every wire value is text; the getters parse with invariant culture, and 'theme' falls back because the file does not set it)");

        Console.WriteLine();
    }
}
