// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PolicyBehaviors.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;
using Bodu.Text.Delimited;
using Bodu.Text.Delimited.Document;
using Bodu.Text.Delimited.Reader;

namespace Bodu.Samples.Text.Formats.DelimitedData.Scenarios;

/// <summary>
/// Demonstrates the policy knobs for real-world files that break the RFC 4180 contract: rows with the wrong field
/// count (<see cref="DelimitedFieldCountBehavior" />) and structurally malformed records
/// (<see cref="DelimitedMalformedRecordBehavior" />). The strict defaults throw; the lenient settings let a clean
/// import continue past dirty rows. Positional (headerless) mode keeps the field counts visible.
/// </summary>
public static class PolicyBehaviors
{
    /// <summary>
    /// Parses dirty input under the strict defaults and again under the lenient policies.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- Policy knobs for dirty input ---");

        // Row 2 is short (2 fields), row 3 is long (4 fields).
        var ragged = Encoding.UTF8.GetBytes("sku,name,stock\nA1,Widget,12\nB2,Bolt\nC3,Nut,40,extra\n");

        // Strict field counts are enforced against the header row, so the strict demo parses in
        // header mode.
        try
        {
            using var strict = DelimitedDocument.Parse(ragged);
        }
        catch (DelimitedFormatException ex)
        {
            Console.WriteLine($"Strict (default) : {ex.Message}");
        }

        using (var raggedDoc = DelimitedDocument.Parse(ragged, new DelimitedReaderOptions
        {
            NoHeader = true,
            FieldCountBehavior = DelimitedFieldCountBehavior.Ragged,
        }))
        {
            Console.WriteLine($"Ragged           : accepted {raggedDoc.RootElement.GetArrayLength()} rows with field counts [{string.Join(", ", FieldCounts(raggedDoc))}]");
        }

        // A quoted field followed by stray characters is structurally malformed.
        var malformed = Encoding.UTF8.GetBytes("sku,name,stock\nA1,Widget,12\nB2,\"Bolt\"x,9\nC3,Nut,40\n");

        try
        {
            using var strict = DelimitedDocument.Parse(malformed);
        }
        catch (DelimitedFormatException ex)
        {
            Console.WriteLine($"Throw (default)  : {ex.Message}");
        }

        // SkipRecord discards the rest of the malformed record but keeps the fields parsed so
        // far, leaving a short row - so lenient ingestion pairs it with Ragged.
        using (var skipped = DelimitedDocument.Parse(malformed, new DelimitedReaderOptions
        {
            NoHeader = true,
            MalformedRecordBehavior = DelimitedMalformedRecordBehavior.SkipRecord,
            FieldCountBehavior = DelimitedFieldCountBehavior.Ragged,
        }))
        {
            Console.WriteLine($"SkipRecord+Ragged: kept {skipped.RootElement.GetArrayLength()} rows, field counts [{string.Join(", ", FieldCounts(skipped))}] (malformed row truncated)");
        }

        Console.WriteLine();
    }

    /// <summary>
    /// Collects the per-record field counts of a positionally parsed document.
    /// </summary>
    /// <param name="document">The parsed document.</param>
    /// <returns>The field counts in record order.</returns>
    private static List<int> FieldCounts(DelimitedDocument document)
    {
        var root = document.RootElement;
        var counts = new List<int>(root.GetArrayLength());
        for (var i = 0; i < root.GetArrayLength(); i++)
        {
            counts.Add(root[i].GetArrayLength());
        }

        return counts;
    }
}
