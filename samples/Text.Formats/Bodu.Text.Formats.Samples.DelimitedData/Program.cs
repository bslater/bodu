// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Samples.Text.Formats.DelimitedData.Scenarios;

namespace Bodu.Samples.Text.Formats.DelimitedData;

/// <summary>
/// Entry point for the delimited-data sample: RFC 4180 CSV/TSV via <c>Bodu.Text.Delimited</c> —
/// parsing into a document with typed field access, the policy knobs for ragged and malformed
/// real-world input, formatting a document back to text, and the forward-only streaming
/// reader/writer for large files. Everything runs offline against the committed
/// <c>Data/trades.csv</c>.
/// </summary>
public static class Program
{
    /// <summary>
    /// Runs every scenario in order.
    /// </summary>
    public static void Main()
    {
        Console.WriteLine("Bodu.Text.Formats.Samples.DelimitedData");
        Console.WriteLine("=======================================");
        Console.WriteLine();

        ParseAndTypedGetters.Run();
        PolicyBehaviors.Run();
        FormatAndRoundTrip.Run();
        StreamingReaderWriter.Run();

        Console.WriteLine("Done.");
    }
}
