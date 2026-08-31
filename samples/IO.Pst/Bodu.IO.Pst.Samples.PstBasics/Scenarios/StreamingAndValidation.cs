// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StreamingAndValidation.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Pst;

namespace Bodu.IO.Pst.Samples.PstBasics.Scenarios;

/// <summary>
/// Demonstrates memory-bounded payload access and the validation ladder: <see cref="PstNode.DataLength" />
/// prices a payload without reading it, <see cref="PstNode.OpenDataStream" /> streams it one leaf block at a
/// time, strict validation enforces every checksum, and corruption always surfaces as the
/// <see cref="PstFileException" /> family.
/// </summary>
public static class StreamingAndValidation
{
    /// <summary>
    /// Streams the largest node's payload, reopens the file strictly, and classifies a truncated copy.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- Streaming, validation levels, and failure classification ---");

        var options = new PstFileOptions
        {
            ValidationLevel = PstValidationLevel.Strict,   // every CRC, trailer signature, and page invariant
            BlockCacheSize = 512,                          // decoded-block LRU entries (default 256, 0 disables)
        };

        using (PstFile file = PstFile.Open(File.OpenRead(Program.SamplePath), options))
        {
            // Price payloads first, then stream the largest one without materializing it.
            PstNodeInfo largest = file.EnumerateNodes().MaxBy(info => info.DataLength)!;
            PstNode node = file.GetNode(largest.NodeId);
            Console.WriteLine($"largest node {largest.NodeId} holds {node.DataLength} bytes");

            long total = 0;
            var buffer = new byte[16 * 1024];
            using Stream data = node.OpenDataStream();
            for (int read; (read = data.Read(buffer)) > 0;)
                total += read;

            Console.WriteLine($"streamed {total} bytes in 16 KiB chunks under Strict validation");
        }

        // Corruption surfaces as the PstFileException family - never anything else.
        byte[] truncated = File.ReadAllBytes(Program.SamplePath).AsSpan(0, 4096).ToArray();
        try
        {
            using PstFile broken = PstFile.Open(new MemoryStream(truncated), new PstFileOptions());
            _ = broken.EnumerateNodes().Count();
        }
        catch (PstFileException ex)
        {
            Console.WriteLine($"truncated copy rejected: {ex.GetType().Name} ({ex.Error})");
        }

        Console.WriteLine();
    }
}
