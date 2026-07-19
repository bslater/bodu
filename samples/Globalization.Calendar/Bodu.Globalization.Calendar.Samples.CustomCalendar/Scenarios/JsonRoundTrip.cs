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
        Console.WriteLine("--- JSON round trip: author -> save -> load -> serve ---");

        NotableDateDocumentBuilder builder = NotableDateDocumentBuilder.Create("contoso-roundtrip")
            .WithMetadata("Contoso round-trip calendar")
            .AddNotableDate("founding-day", "Contoso Founding Day", NotableDateCategory.Other, c => c
                .AsNonWorkingByDefault()
                .AddRule("fixed", r => r.Fixed(3, 12)));

        // Save: the extension picks the format (.json here selects the documented JSON subset).
        var path = Path.Combine(AppContext.BaseDirectory, "contoso-holidays.json");
        builder.Save(path);
        Console.WriteLine($"Saved: {Path.GetFileName(path)} ({new FileInfo(path).Length} bytes)");

        // Load path 1 - the builder: reload for further editing, then materialize.
        NotableDateResource viaBuilder = NotableDateDocumentBuilder.Load(path).Build();

        // Load path 2 - the plain loader: what a consumer without the Builder package does. JSON has
        // its own loader entry point (LoadJson) alongside the XML-accepting Load.
        NotableDateResource viaLoader = NotableDateResourceLoader.LoadJson(File.ReadAllText(path));

        var service = new NotableDateService(viaLoader);
        NotableDate founding = service.Resolve(2024, "AU").Single();
        Console.WriteLine($"Reloaded and resolved: {founding.Date:yyyy-MM-dd} {founding.DisplayName}");
        Console.WriteLine($"Builder and loader agree: {viaBuilder.ResourceId == viaLoader.ResourceId}");

        Console.WriteLine();
    }
}
