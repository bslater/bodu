// ---------------------------------------------------------------------------------------------------------------
// <copyright file="LintingRulePackText.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar;

namespace Bodu.Globalization.Calendar.Samples.ValidationLint.Scenarios;

/// <summary>
/// Demonstrates linting arbitrary rule-pack text: <c>NotableDateResourceLoader.TryLoad</c> accepts any input —
/// including malformed XML — and reports everything as diagnostics, which is the shape a build task or editor
/// integration wants.
/// </summary>
public static class LintingRulePackText
{
    /// <summary>
    /// Lints malformed, invalid, and valid rule-pack text through the collect-mode loader.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- NotableDateResourceLoader.TryLoad: linting rule-pack text ---");

        // Malformed XML never throws from TryLoad; it becomes a BODU-CAL-SYNTAX error diagnostic.
        LintAndReport("malformed input", "<NotableDateResource but not xml");

        // A well-formed document with a semantic error reports the same stable codes the throwing
        // Load overloads carry inside NotableDateValidationException.
        LintAndReport("unknown algorithm key", """
            <?xml version="1.0" encoding="utf-8"?>
            <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="pack.invalid">
              <NotableDates>
                <NotableDate id="mystery-day" displayName="Mystery Day" category="Observance">
                  <Rules><Rule id="x"><Strategy><Algorithm key="no-such-algorithm" /></Strategy></Rule></Rules>
                </NotableDate>
              </NotableDates>
            </NotableDateResource>
            """);

        // A valid pack loads with an empty diagnostic list and a ready resource.
        LintAndReport("valid pack", """
            <?xml version="1.0" encoding="utf-8"?>
            <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="pack.valid">
              <NotableDates>
                <NotableDate id="new-years-day" displayName="New Year's Day" category="PublicHoliday">
                  <Rules><Rule id="x"><Strategy><Fixed month="January" day="1" /></Strategy></Rule></Rules>
                </NotableDate>
              </NotableDates>
            </NotableDateResource>
            """);

        Console.WriteLine();
    }

    /// <summary>
    /// Lints one input and prints the outcome with its diagnostics.
    /// </summary>
    /// <param name="label">The scenario label.</param>
    /// <param name="content">The rule-pack XML to lint.</param>
    private static void LintAndReport(string label, string content)
    {
        bool loaded = NotableDateResourceLoader.TryLoad(content, _ => null, out NotableDateResource? resource, out var diagnostics);

        Console.WriteLine($"{label}: loaded={loaded}, resource={(resource is null ? "none" : resource.ResourceId)}");
        foreach (NotableDateValidationDiagnostic diagnostic in diagnostics)
            Console.WriteLine($"  {diagnostic}");
    }
}
