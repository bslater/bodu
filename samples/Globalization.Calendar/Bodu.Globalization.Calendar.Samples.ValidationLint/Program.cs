// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar.Samples.ValidationLint.Scenarios;

namespace Bodu.Globalization.Calendar.Samples.ValidationLint;

/// <summary>
/// Entry point for the validation-lint sample: linting authored documents with
/// <c>NotableDateDocumentBuilder.Validate()</c> / <c>TryBuild(...)</c>, and linting arbitrary rule-pack
/// text with <c>NotableDateResourceLoader.TryLoad</c> — collecting stable <c>BODU-CAL-*</c> diagnostics
/// instead of catching exceptions.
/// </summary>
public static class Program
{
    /// <summary>
    /// Runs every scenario in order.
    /// </summary>
    public static void Main()
    {
        Console.WriteLine("Bodu.Globalization.Calendar.Samples.ValidationLint");
        Console.WriteLine("==================================================");
        Console.WriteLine();

        LintingAuthoredDocuments.Run();
        LintingRulePackText.Run();

        Console.WriteLine("Done.");
    }
}
