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
    /// <summary>The Microsoft Word document class id, used to mark the authored container's type.</summary>
    private static readonly Guid WordDocumentClassId = new("00020906-0000-0000-c000-000000000046");

    /// <summary>
    /// Authors and reads back a summary-information stream and root class id, then inspects a real document's.
    /// </summary>
    /// <returns>A task that completes when the scenario has run.</returns>
    public static async Task RunAsync()
    {
        SampleConsole.Scenario(
            "OLE property sets",
            what: "Reads the summary-information and document-summary-information property sets from a real "
                + "document, reporting the typed values.",
            why: "The document metadata everyone wants - title, author, timestamps, page counts - lives in a "
                + "property set stored as a binary stream inside the container, in a format shared across every "
                + "OLE application. Decoding it here rather than in each format reader means .xls, .doc and .msg "
                + "get it from one implementation. The values are typed rather than stringified because a "
                + "timestamp and a count are not text, and a consumer that has to re-parse them would have to "
                + "guess the format that this layer already knows.",
            expect: "The properties come back as typed values from a binary stream, and the two standard sets "
                + "are distinguished - which matters because they overlap in purpose but not in content.");

        // Author: build the metadata, write it back through the convenience setter, stamp the root
        // storage's class id (the OLE2 file-type discriminator), and persist with an async commit.
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
            writable.RootStorage.ClassId = WordDocumentClassId;
            writable.RootStorage.CreateStream("Body", "..."u8.ToArray());

            // Writes are staged in memory until committed; disposing without Commit discards them
            // (the transactional Commit/Revert model).
            await writable.CommitAsync();
        }

        container.Position = 0;

        // Read back through the typed accessor and the settable metadata.
        using var file = CompoundFile.Open(container, leaveOpen: true);
        if (file.TryGetSummaryInformation(out var readBack))
        {
            Console.WriteLine($"  authored : '{readBack.Title}' by {readBack.Author} (created {readBack.CreateTime:yyyy-MM-dd})");
        }

        Console.WriteLine($"  authored : root class id {file.RootStorage.ClassId}");

        // The same accessor reads real-world files: the committed .doc fixture.
        using var doc = CompoundFile.OpenRead(Path.Combine(AppContext.BaseDirectory, "Data", "sample1.doc"));
        if (doc.TryGetSummaryInformation(out var docSummary))
        {
            Console.WriteLine($"  sample1.doc: title='{docSummary.Title}', author='{docSummary.Author}', app='{docSummary.ApplicationName}'");
        }
        else
        {
            Console.WriteLine("  sample1.doc: no SummaryInformation stream");
        }

        Console.WriteLine();
    }
}
