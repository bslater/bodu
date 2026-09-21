// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

using Bodu.Security.Cryptography.Samples.MerkleTrees.Scenarios;

namespace Bodu.Security.Cryptography.Samples.MerkleTrees;

/// <summary>
/// Entry point for the Merkle-tree sample: the RFC 6962 commitment surface — roots and inclusion proofs, the
/// length-bound root that closes the tree-size ambiguity, append-only consistency proofs, the blocked and streaming
/// surface with its async overloads, the write-time block accumulator, the fan-out and parallelism knobs, and the
/// optional diagnostics trace. Every input is fixed, so all output is deterministic.
/// </summary>
public static class Program
{
    /// <summary>
    /// Runs every scenario in order.
    /// </summary>
    /// <returns>A task that completes when every scenario has finished.</returns>
    public static async Task Main()
    {
        // The diagnostics trace renders box-drawing and arrow glyphs. A Windows console left on a legacy code page
        // maps the '\u2190' arrow onto 0x1B (ESC), and the terminal then swallows it together with the text that
        // follows as an escape sequence - so ask for UTF-8 before anything is written. A redirected stream may refuse
        // the change, which is harmless: only the glyphs suffer, never the values.
        try { Console.OutputEncoding = Encoding.UTF8; }
        catch (IOException) { }

        Console.WriteLine("Bodu.Security.Cryptography.Samples.MerkleTrees");
        Console.WriteLine("=============================================");
        Console.WriteLine();

        Commitments.Run();
        SizeBinding.Run();
        Consistency.Run();
        await BlockedInputs.RunAsync().ConfigureAwait(false);
        StreamingWriter.Run();
        ShapeAndParallelism.Run();
        DiagnosticsTrace.Run();

        Console.WriteLine("Done.");
    }
}
