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
        Console.WriteLine("--- Parse vs ParseWithDiagnostics ---");

        // The committed file parses clean.
        var document = ConfigurationDocument.Load(Path.Combine(AppContext.BaseDirectory, "Data", "sample.boduconfig"));
        Console.WriteLine($"sample.boduconfig: root section + {document.Sections.Count} glob sections");

        // User-authored text with problems: the Relaxed profile collects diagnostics
        // (DiagnosticMode.Collect) instead of throwing on the first error.
        var flawed = "root = true\n\n[*]\nindent_size = 4\nthis line has no separator\n";
        var result = ConfigurationDocument.ParseWithDiagnostics(flawed, ConfigurationParseOptions.Relaxed);

        Console.WriteLine($"flawed text      : document usable = {result.Document is not null}, diagnostics = {result.Diagnostics.Length}");
        foreach (var diagnostic in result.Diagnostics)
        {
            Console.WriteLine($"  [{diagnostic.Severity}] {diagnostic.Code} at line {diagnostic.Location.LineNumber}: {diagnostic.Message}");
        }

        Console.WriteLine();
    }
}
