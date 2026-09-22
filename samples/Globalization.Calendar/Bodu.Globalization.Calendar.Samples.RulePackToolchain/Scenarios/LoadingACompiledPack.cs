// ---------------------------------------------------------------------------------------------------------------
// <copyright file="LoadingACompiledPack.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar;

namespace Bodu.Globalization.Calendar.Samples.RulePackToolchain.Scenarios;

/// <summary>
/// Demonstrates the output side of the rule-pack toolchain: loading the sealed <c>.bcal</c> pack the build produced,
/// and comparing it against parsing the same rules from XML at runtime.
/// </summary>
public static class LoadingACompiledPack
{
    /// <summary>
    /// Loads the compiled pack, resolves a year from it, and shows that the XML source resolves identically.
    /// </summary>
    /// <param name="packPath">The compiled <c>.bcal</c> pack beside the application.</param>
    /// <param name="xmlPath">The rule document the pack was compiled from.</param>
    public static void Run(string packPath, string xmlPath)
    {
        SampleConsole.Scenario(
            "Loading the .bcal pack the build compiled",
            what: "Loads the sealed binary pack that the MSBuild task produced from rules/company-holidays.xml, "
                + "resolves a year from it, then parses the same XML at runtime and shows both paths resolving the "
                + "same dates. Prints the size of each representation.",
            why: "The XML is the authoring format and the pack is the deployment format, and they are different jobs. "
                + "Parsing XML at startup means shipping the schema, validating every document, and paying that cost "
                + "on every cold start - which a serverless function or a CLI pays on every invocation. Compiling at "
                + "build time moves the validation to the developer's machine, where a broken rule fails the build "
                + "instead of the deployment, and leaves the runtime a sealed file to read. The point of the MSBuild "
                + "integration is that this happens without anyone remembering to run a tool: the item group in the "
                + "csproj is the whole opt-in.",
            expect: "Both representations resolve the same three holidays for 2026, and the comparison line reports "
                + "that they match. The pack is the smaller of the two and needs no schema validation to read. If "
                + "the pack were missing, this sample would have said so and stopped before here - its presence is "
                + "itself the evidence that the task ran during the build.");

        // The pack is what a deployed application reads: no XML parse, no schema, no validation pass.
        using var packStream = File.OpenRead(packPath);
        var fromPack = NotableDateResourceLoader.LoadBinary(packStream);
        var packService = new NotableDateService(fromPack);

        // The same rules parsed from source, for comparison only - a deployment would not do this.
        var fromXml = NotableDateResourceLoader.Load(File.ReadAllText(xmlPath));
        var xmlService = new NotableDateService(fromXml);

        var packDates = packService.Resolve(2026, "AU");
        var xmlDates = xmlService.Resolve(2026, "AU");

        Console.WriteLine($"  Compiled pack           : {Path.GetFileName(packPath)}, {new FileInfo(packPath).Length} bytes");
        Console.WriteLine($"  XML source              : {Path.GetFileName(xmlPath)}, {new FileInfo(xmlPath).Length} bytes");
        Console.WriteLine();

        Console.WriteLine("  Resolved 2026 from the compiled pack:");
        foreach (NotableDate date in packDates)
            Console.WriteLine($"    {date.Date:yyyy-MM-dd ddd}  {date.DisplayName,-18} {date.Category}");

        Console.WriteLine();

        // Comparing the two proves the compile step is faithful, not just smaller.
        var identical = packDates.Count == xmlDates.Count
            && packDates.Zip(xmlDates).All(static pair =>
                pair.First.Date == pair.Second.Date
                && string.Equals(pair.First.DisplayName, pair.Second.DisplayName, StringComparison.Ordinal));

        Console.WriteLine($"  pack matches XML source : {identical}   ({packDates.Count} occurrences either way - compiling changes the representation, not the answer)");
        Console.WriteLine();
    }
}
