// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ParseAndDiagnostics.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Configuration;

namespace Bodu.Samples.Text.Configuration.ConfigCascade.Scenarios;

/// <summary>
/// Demonstrates the two parse entry points: <c>Parse</c> throws on the first structural error
/// (right for generated files), while <c>ParseWithDiagnostics</c> collects
/// <see cref="ConfigurationDiagnostic" /> rows and still returns a usable document — right for
/// user-authored files where an editor wants to show every problem at once.
/// </summary>
public static class ParseAndDiagnostics
{
    /// <summary>
    /// Loads the committed file, then parses flawed text with diagnostics collected.
    /// </summary>
    public static void Run()
    {
        SampleConsole.Scenario(
            "Parse versus ParseWithDiagnostics",
            what: "Loads the committed configuration file, then parses deliberately flawed text through the "
                + "diagnostic entry point and prints every problem it collected alongside the document it still "
                + "returned.",
            why: "These two entry points answer different questions about the same input. A generated or "
                + "machine-written file that does not parse is a bug upstream, and the right response is to stop "
                + "at the first error with a clear exception. A file a person typed is different: stopping at the "
                + "first error means they fix one line, re-run, and discover the next - so the parser collects "
                + "everything it can and still returns a usable document, which is what an editor needs to "
                + "underline every problem at once. Each diagnostic carries a stable code and a line number for "
                + "exactly that reason.",
            expect: "The flawed text yields a usable document rather than nothing, with the unparseable line "
                + "reported as a diagnostic naming its code and line number. The valid lines around it still "
                + "parsed - partial recovery is the point, since a document that collapses on one bad line "
                + "cannot drive an editor.");

        // The committed file parses clean.
        var document = ConfigurationDocument.Load(Path.Combine(AppContext.BaseDirectory, "Data", "sample.boduconfig"));
        Console.WriteLine($"  sample.boduconfig: root section + {document.Sections.Count} glob sections"
            + "  (the committed file parses clean, so Load's throw-on-error behaviour is the right entry point for it)");

        // User-authored text with problems: the Relaxed profile collects diagnostics
        // (DiagnosticMode.Collect) instead of throwing on the first error.
        var flawed = "root = true\n\n[*]\nindent_size = 4\nthis line has no separator\n";
        var result = ConfigurationDocument.ParseWithDiagnostics(flawed, ConfigurationParseOptions.Relaxed);

        Console.WriteLine($"  flawed text      : document usable = {result.Document is not null}, diagnostics = {result.Diagnostics.Length}"
            + "  (expected True - a document comes back despite the error, which is what lets an editor keep working on a file mid-edit)");
        foreach (var diagnostic in result.Diagnostics)
        {
            Console.WriteLine($"    [{diagnostic.Severity}] {diagnostic.Code} at line {diagnostic.Location.LineNumber}: {diagnostic.Message}");
        }

        Console.WriteLine("  (a stable code and a line number per diagnostic - enough for an editor to place a squiggle without re-parsing)");

        Console.WriteLine();
    }
}
