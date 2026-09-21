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
        SampleConsole.Scenario(
            "Validate and TryBuild - linting a document you are authoring",
            what: "Lints a clean document, one whose rule names an algorithm that does not exist, and one whose "
                + "concept has no rules at all, then builds the clean one through the non-throwing overload.",
            why: "Authoring rule data is the part of this library a consumer is most likely to get wrong, because "
                + "the mistakes are semantic rather than syntactic - a misspelled algorithm key is perfectly "
                + "well-formed. Reporting those as exceptions would be the wrong shape for the tools that need "
                + "them: a build task wants every problem in one pass so a developer fixes them together, and an "
                + "editor wants to underline them all rather than the first. So Validate collects instead of "
                + "throwing, runs the same pipeline the loader does - which is what makes a clean lint a real "
                + "guarantee rather than a weaker check - and gives each diagnostic a stable code so a build can "
                + "suppress one without pattern-matching on a message.",
            expect: "The clean document reports nothing and then builds. The two broken ones report diagnostics "
                + "rather than throwing, including the structurally incomplete document that Build itself would "
                + "have rejected with an exception - the whole point being that a linter must survive input worse "
                + "than the loader accepts.");

        // A clean document lints empty, and a clean lint guarantees Build() succeeds - both run the
        // same validation pipeline.
        NotableDateDocumentBuilder clean = NotableDateDocumentBuilder.Create("corp.holidays")
            .AddNotableDate("company-day", "Company Day", NotableDateCategory.Observance, d => d
                .AddRule("default", r => r.Fixed(3, 14)));

        Console.WriteLine($"  Clean document diagnostics: {clean.Validate().Count}"
            + "  (expected 0 - and because Validate runs the loader's own pipeline, an empty result guarantees Build will succeed)");

        // A semantic problem - here a rule referencing an algorithm key that does not exist - surfaces
        // as an error diagnostic with a stable code instead of an exception.
        NotableDateDocumentBuilder unknownKey = NotableDateDocumentBuilder.Create("corp.broken")
            .AddNotableDate("mystery-day", "Mystery Day", NotableDateCategory.Observance, d => d
                .AddRule("default", r => r.Algorithm("no-such-algorithm")));

        foreach (NotableDateValidationDiagnostic diagnostic in unknownKey.Validate())
            Console.WriteLine($"    {diagnostic}");

        Console.WriteLine("  (a stable code, not a message to pattern-match - the document is well-formed XML, so only validation catches this)");

        // A document too incomplete to even serialize (a concept with no rules) is reported the same
        // way - as a diagnostic - rather than as the InvalidOperationException that Build() would throw.
        NotableDateDocumentBuilder incomplete = NotableDateDocumentBuilder.Create("corp.incomplete")
            .AddNotableDate("empty-concept", "Empty Concept", NotableDateCategory.Observance, d => { });

        foreach (NotableDateValidationDiagnostic diagnostic in incomplete.Validate())
            Console.WriteLine($"    {diagnostic}");

        Console.WriteLine("  (Build would have thrown on this one - a linter has to survive input worse than the loader accepts)");

        // TryBuild is the non-throwing Build: a false return hands back the diagnostics, a true return
        // hands back the ready-to-use resource.
        if (clean.TryBuild(out NotableDateResource? resource, out _))
        {
            var service = new NotableDateService(resource!);
            var occurrences = service.Resolve(2026, "XX");
            Console.WriteLine($"  TryBuild succeeded: {resource!.ResourceId} resolves {occurrences.Count} occurrence(s) in 2026"
                + "  (a true return hands back a ready resource; a false one hands back the diagnostics instead)");
        }

        Console.WriteLine();
    }
}
