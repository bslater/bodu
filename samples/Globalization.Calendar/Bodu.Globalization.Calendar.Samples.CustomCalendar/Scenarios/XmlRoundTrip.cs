// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XmlRoundTrip.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar;
using Bodu.Globalization.Calendar.Builder;

namespace Bodu.Globalization.Calendar.Samples.CustomCalendar.Scenarios;

/// <summary>
/// Demonstrates the persistence round trip: author fluently, save the document to XML, load it back
/// (with the plain resource loader — the path any non-builder consumer uses), and serve it. The
/// document on disk is the distributable artifact; the builder is just one way to produce it.
/// </summary>
public static class XmlRoundTrip
{
    /// <summary>
    /// Saves an authored calendar to the output directory, reloads it, and proves equivalence.
    /// </summary>
    public static void Run()
    {
        SampleConsole.Scenario(
            "XML round trip - author, save, load, serve",
            what: "Authors a small calendar, saves it to XML, reloads it through both the builder and the plain "
                + "resource loader, resolves a date from the loaded resource, and compares the two load paths.",
            why: "The builder is a convenience, not the product - what ships is the document. Proving the round "
                + "trip matters because the two load paths have different audiences: a tool that edits calendars "
                + "reloads through the builder, while an application that only consumes them uses the plain "
                + "loader and does not reference the Builder package at all. If those disagreed, an authored "
                + "calendar could work in the tool that produced it and fail in the application that consumes it, "
                + "which is the worst possible place to find out.",
            expect: "The saved file reloads and resolves correctly through the loader that knows nothing about "
                + "the builder, and both load paths agree on what they read - authoring and consuming are two "
                + "views of one document rather than two representations to keep in sync.");

        NotableDateDocumentBuilder builder = NotableDateDocumentBuilder.Create("contoso-roundtrip")
            .WithMetadata("Contoso round-trip calendar")
            .AddNotableDate("founding-day", "Contoso Founding Day", NotableDateCategory.Other, c => c
                .AsNonWorkingByDefault()
                .AddRule("fixed", r => r.Fixed(3, 12)));

        // Save: the extension picks the format (.xml here; .json is the documented subset).
        var path = Path.Combine(AppContext.BaseDirectory, "contoso-holidays.xml");
        builder.Save(path);
        Console.WriteLine($"  Saved: {Path.GetFileName(path)} ({new FileInfo(path).Length} bytes)"
            + "  (the file on disk is the distributable artifact - the builder is only one way to produce it)");

        // Load path 1 - the builder: reload for further editing, then materialize.
        NotableDateResource viaBuilder = NotableDateDocumentBuilder.Load(path).Build();

        // Load path 2 - the plain loader: what a consumer without the Builder package does.
        NotableDateResource viaLoader = NotableDateResourceLoader.Load(File.ReadAllText(path));

        var service = new NotableDateService(viaLoader);
        NotableDate founding = service.Resolve(2024, "AU").Single();
        Console.WriteLine($"  Reloaded and resolved: {founding.Date:yyyy-MM-dd} {founding.DisplayName}"
            + "  (loaded by the plain resource loader - the path a consumer without the Builder package takes)");
        Console.WriteLine($"  Builder and loader agree: {viaBuilder.ResourceId == viaLoader.ResourceId}"
            + "  (expected True - authoring and consuming are two views of one document, not two representations to keep in sync)");

        Console.WriteLine();
    }
}
