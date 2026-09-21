// ---------------------------------------------------------------------------------------------------------------
// <copyright file="JsonRoundTrip.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar;
using Bodu.Globalization.Calendar.Builder;

namespace Bodu.Globalization.Calendar.Samples.CustomCalendar.Scenarios;

/// <summary>
/// Demonstrates the same persistence round trip as <see cref="XmlRoundTrip" /> against the documented
/// JSON subset: author fluently, save to <c>.json</c>, reload through both the builder and the plain
/// resource loader (<see cref="NotableDateResourceLoader.LoadJson(string, Microsoft.Extensions.Logging.ILogger?)" />),
/// and serve it. XML and JSON are two encodings of one document model.
/// </summary>
public static class JsonRoundTrip
{
    /// <summary>
    /// Saves an authored calendar to JSON in the output directory, reloads it, and proves equivalence.
    /// </summary>
    public static void Run()
    {
        SampleConsole.Scenario(
            "JSON round trip - the same document in the other encoding",
            what: "Repeats the previous scenario against the documented JSON subset: saves to .json, reloads "
                + "through the builder and through the JSON loader entry point, resolves a date, and compares the "
                + "two paths.",
            why: "XML and JSON here are two encodings of one document model rather than two formats with "
                + "separate semantics, which is why the file extension is enough to select one on save. JSON "
                + "earns its place because a calendar is often authored or edited by something other than a .NET "
                + "tool - a script, a web editor, a configuration pipeline - and XML is a poor fit for those. It "
                + "is a documented subset rather than a full equivalent, which is the honest framing: the "
                + "constructs it covers behave identically, and the loader has its own entry point rather than "
                + "sniffing the content.",
            expect: "The same authored calendar produces a working JSON document, reloads through a loader that "
                + "knows nothing about the builder, and resolves the same date as the XML path did.");

        NotableDateDocumentBuilder builder = NotableDateDocumentBuilder.Create("contoso-roundtrip")
            .WithMetadata("Contoso round-trip calendar")
            .AddNotableDate("founding-day", "Contoso Founding Day", NotableDateCategory.Other, c => c
                .AsNonWorkingByDefault()
                .AddRule("fixed", r => r.Fixed(3, 12)));

        // Save: the extension picks the format (.json here selects the documented JSON subset).
        var path = Path.Combine(AppContext.BaseDirectory, "contoso-holidays.json");
        builder.Save(path);
        Console.WriteLine($"  Saved: {Path.GetFileName(path)} ({new FileInfo(path).Length} bytes)"
            + "  (the file on disk is the distributable artifact - the builder is only one way to produce it)");

        // Load path 1 - the builder: reload for further editing, then materialize.
        NotableDateResource viaBuilder = NotableDateDocumentBuilder.Load(path).Build();

        // Load path 2 - the plain loader: what a consumer without the Builder package does. JSON has
        // its own loader entry point (LoadJson) alongside the XML-accepting Load.
        NotableDateResource viaLoader = NotableDateResourceLoader.LoadJson(File.ReadAllText(path));

        var service = new NotableDateService(viaLoader);
        NotableDate founding = service.Resolve(2024, "AU").Single();
        Console.WriteLine($"  Reloaded and resolved: {founding.Date:yyyy-MM-dd} {founding.DisplayName}"
            + "  (loaded by the plain resource loader - the path a consumer without the Builder package takes)");
        Console.WriteLine($"  Builder and loader agree: {viaBuilder.ResourceId == viaLoader.ResourceId}"
            + "  (expected True - authoring and consuming are two views of one document, not two representations to keep in sync)");

        Console.WriteLine();
    }
}
