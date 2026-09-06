// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DetectAndOpen.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Pst;

namespace Bodu.IO.Pst.Samples.PstBasics.Scenarios;

/// <summary>
/// Demonstrates format detection and the open handshake: <see cref="PstFile.IsPstFile(Stream)" /> answers
/// "is this a PST of any variant?" from the magic without a full parse, and an open session reports the
/// declared format and content encoding before any node is read. Both PST formats go through the same
/// surface: the Unicode file (<c>wVer</c> 23, 64-bit structures) and the ANSI file (<c>wVer</c> 14, 32-bit
/// structures) differ only in what <see cref="PstFile.Format" /> reports.
/// </summary>
public static class DetectAndOpen
{
    /// <summary>
    /// Probes both sample files and some arbitrary bytes, then opens each sample and prints its header facts.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- Detection and the open handshake ---");

        // IsPstFile answers from the magic alone, so it says "yes" to either format (and to OST files,
        // which the open that follows rejects with PstUnsupportedFormatException).
        foreach (string path in new[] { Program.SamplePath, Program.AnsiSamplePath })
        {
            using FileStream source = File.OpenRead(path);
            Console.WriteLine($"{Path.GetFileName(path)} : IsPstFile = {PstFile.IsPstFile(source)}");
        }

        using (var text = new MemoryStream("just some text, definitely not a node database"u8.ToArray()))
        {
            Console.WriteLine($"plain text  : IsPstFile = {PstFile.IsPstFile(text)}");
        }

        Console.WriteLine();

        // The same open call and the same session surface for both formats; only Format differs. The
        // 32-bit versus 64-bit structure widths are an internal layout choice made from the header.
        Describe(Program.SamplePath);
        Describe(Program.AnsiSamplePath);

        Console.WriteLine();
    }

    /// <summary>
    /// Opens one sample and prints its declared format, content encoding, and a node census.
    /// </summary>
    /// <param name="path">The PST file to open.</param>
    private static void Describe(string path)
    {
        using PstFile file = PstFile.OpenRead(path);
        Console.WriteLine($"{Path.GetFileName(path)}");
        Console.WriteLine($"  format          : {file.Format}");
        Console.WriteLine($"  content encoding: {file.CryptMethod}");

        // Census by node type - opening parsed only the header; this walk is the first real read.
        var census = file.EnumerateNodes()
            .GroupBy(info => info.NodeId.Type)
            .OrderBy(group => group.Key)
            .Select(group => $"{group.Key} x{group.Count()}");
        Console.WriteLine($"  node census     : {string.Join(", ", census)}");
    }
}
