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
        SampleConsole.Scenario(
            "Detection and the open handshake",
            what: "Detects whether a file is a PST before opening it, then opens one and reports the format "
                + "variant and the content encoding the header declared.",
            why: "PST is not one format. The Unicode and ANSI variants differ in the width of nearly every "
                + "field, so the layout has to be established from the header before anything else can be read - "
                + "and the 4 KiB-page OST variant is close enough to look openable while being structurally "
                + "different, which is why it is rejected explicitly rather than misread. The content encoding "
                + "matters just as much: block data is obfuscated by one of two schemes, and reading a block "
                + "without decoding it yields plausible-looking garbage rather than an error.",
            expect: "Detection answers without a full open. The format variant and the encoding scheme are read "
                + "from the file rather than assumed, which is what lets one reader handle both variants instead "
                + "of two.");

        // IsPstFile answers from the magic alone, so it says "yes" to either format (and to OST files,
        // which the open that follows rejects with PstUnsupportedFormatException).
        foreach (string path in new[] { Program.SamplePath, Program.AnsiSamplePath })
        {
            using FileStream source = File.OpenRead(path);
            Console.WriteLine($"  {Path.GetFileName(path)} : IsPstFile = {PstFile.IsPstFile(source)}");
        }

        using (var text = new MemoryStream("just some text, definitely not a node database"u8.ToArray()))
        {
            Console.WriteLine($"  plain text  : IsPstFile = {PstFile.IsPstFile(text)}");
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
        Console.WriteLine($"  {Path.GetFileName(path)}");
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
