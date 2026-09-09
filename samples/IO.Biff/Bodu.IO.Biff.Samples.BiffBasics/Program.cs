// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Biff.Samples.BiffBasics.Scenarios;
using Bodu.IO.Compound;

namespace Bodu.IO.Biff.Samples.BiffBasics;

/// <summary>
/// Entry point for the BIFF sample: the record codec beneath the Excel reader via <c>Bodu.IO.Biff</c> — a record
/// census over a real workbook stream, decoding cells and the shared string table, authoring a BIFF8 workbook
/// stream with <c>BiffWriter</c> and reading it back, and the codec's handling of malformed input. Everything runs
/// offline against the committed <c>Data/sample-biff8.xls</c> fixture (the same file the Excel reader's tests use),
/// whose <c>Workbook</c> stream is extracted with <c>Bodu.IO.Compound</c> — the codec itself has no container
/// dependency.
/// </summary>
public static class Program
{
    /// <summary>
    /// Gets the path of the committed BIFF8 sample workbook.
    /// </summary>
    internal static string SamplePath { get; } =
        Path.Combine(AppContext.BaseDirectory, "Data", "sample-biff8.xls");

    /// <summary>
    /// Runs every scenario in order.
    /// </summary>
    public static void Main()
    {
        Console.WriteLine("Bodu.IO.Biff.Samples.BiffBasics");
        Console.WriteLine("===============================");
        Console.WriteLine();

        byte[] workbookStream = ReadWorkbookStream(SamplePath);

        RecordCensus.Run(workbookStream);
        CellsAndSharedStrings.Run(workbookStream);
        WriteAndReadBack.Run();
        MalformedInput.Run(workbookStream);

        Console.WriteLine("Done.");
    }

    /// <summary>
    /// Extracts the BIFF record stream from an <c>.xls</c> compound file. The stream is named <c>Workbook</c> in
    /// BIFF8 files and <c>Book</c> in BIFF5 files.
    /// </summary>
    /// <param name="path">The <c>.xls</c> path.</param>
    /// <returns>The record stream bytes.</returns>
    internal static byte[] ReadWorkbookStream(string path)
    {
        using CompoundFile container = CompoundFile.OpenRead(path);
        string name = container.RootStorage.TryOpenStream("Workbook", out CompoundStream? probe) ? "Workbook" : "Book";
        probe?.Dispose();

        using CompoundStream stream = container.RootStorage.OpenStream(name);
        byte[] bytes = new byte[stream.Length];
        stream.ReadExactly(bytes);
        return bytes;
    }
}
