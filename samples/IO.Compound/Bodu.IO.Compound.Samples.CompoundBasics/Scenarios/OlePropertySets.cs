// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OlePropertySets.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Compound;
using Bodu.IO.Compound.PropertySets;

namespace Bodu.IO.Compound.Samples.CompoundBasics.Scenarios;

/// <summary>
/// Demonstrates OLE property sets — the document metadata (title, author, timestamps) legacy
/// Office files carry in the well-known <c>SummaryInformation</c> stream: author one with
/// <see cref="SummaryInformationBuilder" />, read it back through
/// <see cref="CompoundFile.TryGetSummaryInformation" />, and inspect the metadata of a real
/// committed <c>.doc</c>.
/// </summary>
public static class OlePropertySets
{
    /// <summary>
    /// Authors and reads back a summary-information stream, then inspects a real document's.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- OLE property sets: SummaryInformation ---");

        // Author: build the metadata and write it back through the convenience setter.
        var summary = new SummaryInformationBuilder
        {
            Title = "Quarterly figures",
            Author = "Bodu Sample",
            ApplicationName = "Bodu.IO.Compound sample",
            CreateTime = new DateTimeOffset(2026, 7, 1, 9, 0, 0, TimeSpan.Zero),
        };

        using var container = new MemoryStream();
        using (var writable = CompoundFile.Create(container, leaveOpen: true))
        {
            writable.SetSummaryInformation(new SummaryInformation(summary.ToPropertySet()));
            writable.RootStorage.CreateStream("Body", "..."u8.ToArray());
            writable.Commit();
        }

        container.Position = 0;

        // Read back through the typed accessor.
        using var file = CompoundFile.Open(container, leaveOpen: true);
        if (file.TryGetSummaryInformation(out var readBack))
        {
            Console.WriteLine($"authored : '{readBack.Title}' by {readBack.Author} (created {readBack.CreateTime:yyyy-MM-dd})");
        }

        // The same accessor reads real-world files: the committed .doc fixture.
        using var doc = CompoundFile.OpenRead(Path.Combine(AppContext.BaseDirectory, "Data", "sample1.doc"));
        if (doc.TryGetSummaryInformation(out var docSummary))
        {
            Console.WriteLine($"sample1.doc: title='{docSummary.Title}', author='{docSummary.Author}', app='{docSummary.ApplicationName}'");
        }
        else
        {
            Console.WriteLine("sample1.doc: no SummaryInformation stream");
        }

        Console.WriteLine();
    }
}
