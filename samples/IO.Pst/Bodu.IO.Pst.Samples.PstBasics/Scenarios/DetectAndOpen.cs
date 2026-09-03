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
/// declared format and content encoding before any node is read.
/// </summary>
public static class DetectAndOpen
{
    /// <summary>
    /// Probes the sample file and some arbitrary bytes, then opens the sample and prints its header facts.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- Detection and the open handshake ---");

        using (FileStream source = File.OpenRead(Program.SamplePath))
        {
            Console.WriteLine($"sample1.pst : IsPstFile = {PstFile.IsPstFile(source)}");
        }

        using (var text = new MemoryStream("just some text, definitely not a node database"u8.ToArray()))
        {
            Console.WriteLine($"plain text  : IsPstFile = {PstFile.IsPstFile(text)}");
        }

        using PstFile file = PstFile.OpenRead(Program.SamplePath);
        Console.WriteLine($"format          : {file.Format}");
        Console.WriteLine($"content encoding: {file.CryptMethod}");

        // Census by node type - opening parsed only the header; this walk is the first real read.
        var census = file.EnumerateNodes()
            .GroupBy(info => info.NodeId.Type)
            .OrderBy(group => group.Key)
            .Select(group => $"{group.Key} x{group.Count()}");
        Console.WriteLine($"node census     : {string.Join(", ", census)}");

        Console.WriteLine();
    }
}
