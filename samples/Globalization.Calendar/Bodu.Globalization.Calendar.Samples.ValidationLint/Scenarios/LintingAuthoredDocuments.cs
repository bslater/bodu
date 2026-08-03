// ---------------------------------------------------------------------------------------------------------------
// <copyright file="LintingAuthoredDocuments.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar;
using Bodu.Globalization.Calendar.Builder;

namespace Bodu.Globalization.Calendar.Samples.ValidationLint.Scenarios;

/// <summary>
/// Demonstrates linting a fluently authored document: <c>Validate()</c> returns every diagnostic the canonical
/// loader would produce — without throwing — and <c>TryBuild(...)</c> is the non-throwing <c>Build()</c>.
/// </summary>
public static class LintingAuthoredDocuments
{
    /// <summary>
    /// Lints a clean document, a document with a semantic error, and a structurally incomplete document.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- NotableDateDocumentBuilder.Validate / TryBuild: linting authored documents ---");

        // A clean document lints empty, and a clean lint guarantees Build() succeeds - both run the
        // same validation pipeline.
        NotableDateDocumentBuilder clean = NotableDateDocumentBuilder.Create("corp.holidays")
            .AddNotableDate("company-day", "Company Day", NotableDateCategory.Observance, d => d
                .AddRule("default", r => r.Fixed(3, 14)));

        Console.WriteLine($"Clean document diagnostics: {clean.Validate().Count}");

        // A semantic problem - here a rule referencing an algorithm key that does not exist - surfaces
        // as an error diagnostic with a stable code instead of an exception.
        NotableDateDocumentBuilder unknownKey = NotableDateDocumentBuilder.Create("corp.broken")
            .AddNotableDate("mystery-day", "Mystery Day", NotableDateCategory.Observance, d => d
                .AddRule("default", r => r.Algorithm("no-such-algorithm")));

        foreach (NotableDateValidationDiagnostic diagnostic in unknownKey.Validate())
            Console.WriteLine($"  {diagnostic}");

        // A document too incomplete to even serialize (a concept with no rules) is reported the same
        // way - as a diagnostic - rather than as the InvalidOperationException that Build() would throw.
        NotableDateDocumentBuilder incomplete = NotableDateDocumentBuilder.Create("corp.incomplete")
            .AddNotableDate("empty-concept", "Empty Concept", NotableDateCategory.Observance, d => { });

        foreach (NotableDateValidationDiagnostic diagnostic in incomplete.Validate())
            Console.WriteLine($"  {diagnostic}");

        // TryBuild is the non-throwing Build: a false return hands back the diagnostics, a true return
        // hands back the ready-to-use resource.
        if (clean.TryBuild(out NotableDateResource? resource, out _))
        {
            var service = new NotableDateService(resource!);
            var occurrences = service.Resolve(2026, "XX");
            Console.WriteLine($"TryBuild succeeded: {resource!.ResourceId} resolves {occurrences.Count} occurrence(s) in 2026");
        }

        Console.WriteLine();
    }
}
