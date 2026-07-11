// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Compound.Samples.CompoundBasics.Scenarios;

namespace Bodu.IO.Compound.Samples.CompoundBasics;

/// <summary>
/// Entry point for the compound-file sample: the OLE2 / Compound File Binary (CFB) container via
/// <c>Bodu.IO.Compound</c> — authoring a container with the staged builder API and reading it
/// back, OLE property sets (the metadata legacy Office files carry), format detection and the
/// v3/v4 version knob, and walking a real committed <c>.doc</c>'s storage tree. Everything runs
/// offline against in-memory containers and the committed <c>Data/</c> fixtures.
/// </summary>
public static class Program
{
    /// <summary>
    /// Runs every scenario in order.
    /// </summary>
    public static void Main()
    {
        Console.WriteLine("Bodu.IO.Compound.Samples.CompoundBasics");
        Console.WriteLine("=======================================");
        Console.WriteLine();

        AuthorAndReadBack.Run();
        OlePropertySets.Run();
        DetectAndVersion.Run();
        StreamsAndEntries.Run();

        Console.WriteLine("Done.");
    }
}
