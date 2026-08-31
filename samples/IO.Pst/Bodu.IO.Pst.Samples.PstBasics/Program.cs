// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Pst.Samples.PstBasics.Scenarios;

namespace Bodu.IO.Pst.Samples.PstBasics;

/// <summary>
/// Entry point for the PST sample: the Outlook personal-folders container via <c>Bodu.IO.Pst</c> —
/// format detection, the raw node database and its property/table views, streaming payload access
/// with validation levels — and the mail-store view via <c>Bodu.Formats.Outlook.Pst</c>: folders,
/// messages, recipients, attachments, and bodies. Everything runs offline against the committed
/// <c>Data/sample1.pst</c> fixture (see <c>Data/NOTICE.md</c> for provenance).
/// </summary>
public static class Program
{
    /// <summary>
    /// Gets the path of the committed sample PST.
    /// </summary>
    internal static string SamplePath { get; } =
        Path.Combine(AppContext.BaseDirectory, "Data", "sample1.pst");

    /// <summary>
    /// Runs every scenario in order.
    /// </summary>
    public static void Main()
    {
        Console.WriteLine("Bodu.IO.Pst.Samples.PstBasics");
        Console.WriteLine("=============================");
        Console.WriteLine();

        DetectAndOpen.Run();
        NodesAndProperties.Run();
        StreamingAndValidation.Run();
        ReadMailStore.Run();

        Console.WriteLine("Done.");
    }
}
