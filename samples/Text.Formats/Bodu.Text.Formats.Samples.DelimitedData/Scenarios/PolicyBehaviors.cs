// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PolicyBehaviors.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Delimited;

namespace Bodu.Samples.Text.Formats.DelimitedData.Scenarios;

/// <summary>
/// Demonstrates the policy knobs for real-world files that break the RFC 4180 contract: rows
/// with the wrong field count (<see cref="DelimitedFieldCountBehavior" />) and structurally
/// malformed records (<see cref="DelimitedMalformedRecordBehavior" />). The strict defaults
/// throw; the lenient settings let a clean import continue past dirty rows.
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
        var ragged = "sku,name,stock\nA1,Widget,12\nB2,Bolt\nC3,Nut,40,extra\n";

        try
        {
            Delimited.Parse(ragged);
        }
        catch (DelimitedFormatException ex)
        {
            Console.WriteLine($"Strict (default) : {ex.Message}");
        }

        var raggedDoc = Delimited.Parse(ragged, new DelimitedParseOptions
        {
            FieldCountBehavior = DelimitedFieldCountBehavior.Ragged,
        });
        Console.WriteLine($"Ragged           : accepted {raggedDoc.Rows.Count} rows with field counts [{string.Join(", ", raggedDoc.Rows.Select(r => r.Count))}]");

        // A quoted field followed by stray characters is structurally malformed.
        var malformed = "sku,name,stock\nA1,Widget,12\nB2,\"Bolt\"x,9\nC3,Nut,40\n";

        try
        {
            Delimited.Parse(malformed);
        }
        catch (DelimitedFormatException ex)
        {
            Console.WriteLine($"Throw (default)  : {ex.Message}");
        }

        // SkipRecord discards the rest of the malformed record but keeps the fields parsed so
        // far, leaving a short row - so lenient ingestion pairs it with Ragged.
        var skipped = Delimited.Parse(malformed, new DelimitedParseOptions
        {
            MalformedRecordBehavior = DelimitedMalformedRecordBehavior.SkipRecord,
            FieldCountBehavior = DelimitedFieldCountBehavior.Ragged,
        });
        Console.WriteLine($"SkipRecord+Ragged: kept {skipped.Rows.Count} rows, field counts [{string.Join(", ", skipped.Rows.Select(r => r.Count))}] (malformed row truncated)");

        Console.WriteLine();
    }
}
