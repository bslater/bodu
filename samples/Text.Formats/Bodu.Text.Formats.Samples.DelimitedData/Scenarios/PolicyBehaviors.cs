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
        SampleConsole.Scenario(
            "Policy knobs for input that breaks the contract",
            what: "Parses a file with a short row and a long row under the strict defaults and again with ragged "
                + "field counts allowed, then does the same for a structurally malformed quoted field, first "
                + "strictly and then with the malformed record skipped.",
            why: "Real CSV files arrive with rows the header does not describe and quotes that do not close, and "
                + "there is no universally right response - which is why this is a policy rather than a "
                + "behaviour. A file from a known generator should fail loudly, because a ragged row there means "
                + "a bug upstream and continuing past it imports wrong data. A one-off file exported by hand is "
                + "the opposite case, where stopping on row 4,000 of 50,000 is not a service to anybody. Making "
                + "the strict behaviour the default matters: a reader that is quietly permissive turns a broken "
                + "file into a plausible-looking import, and the damage surfaces much later.",
            expect: "Both strict parses throw. The second reports a field-count mismatch rather than a quoting "
                + "error, because the stray characters after the closing quote collapse that row to two fields "
                + "and the count check fires first - the message names the symptom the reader reached, not the "
                + "root cause. The lenient parses accept the same input and keep the rows, with the field counts "
                + "showing exactly what was preserved. Note that skipping a malformed record keeps the fields "
                + "already parsed, leaving a short row - which is why that policy is paired with ragged rather "
                + "than used alone.");

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
            Console.WriteLine($"  Strict (default) : {ex.Message}"
                + "  (the default fails loudly - a ragged row from a known generator means a bug upstream, not data to import)");
        }

        using (var raggedDoc = DelimitedDocument.Parse(ragged, new DelimitedReaderOptions
        {
            NoHeader = true,
            FieldCountBehavior = DelimitedFieldCountBehavior.Ragged,
        }))
        {
            Console.WriteLine($"  Ragged           : accepted {raggedDoc.RootElement.GetArrayLength()} rows with field counts [{string.Join(", ", FieldCounts(raggedDoc))}]"
                + "  (the counts differ per row and are visible - the policy accepts the variance rather than hiding it)");
        }

        // A quoted field followed by stray characters is structurally malformed.
        var malformed = Encoding.UTF8.GetBytes("sku,name,stock\nA1,Widget,12\nB2,\"Bolt\"x,9\nC3,Nut,40\n");

        try
        {
            using var strict = DelimitedDocument.Parse(malformed);
        }
        catch (DelimitedFormatException ex)
        {
            Console.WriteLine($"  Throw (default)  : {ex.Message}"
                + "  (the stray characters after the closing quote collapse the row to two fields, so the field-count check is what rejects it first)");
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
            Console.WriteLine($"  SkipRecord+Ragged: kept {skipped.RootElement.GetArrayLength()} rows, field counts [{string.Join(", ", FieldCounts(skipped))}] (malformed row truncated)"
                + "  (skipping keeps the fields already parsed, so the row comes out short - which is why this policy is paired with Ragged)");
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
