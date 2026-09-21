// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Collections.Samples.BitSets.Scenarios;

namespace Bodu.Collections.Samples.BitSets;

/// <summary>
/// Entry point for the bit-set sample: a tour of <see cref="Bodu.Collections.Specialized.BitSet" />, the packed,
/// growable set of non-negative integers that is the sole member of <c>Bodu.Collections.Specialized</c> — the
/// namespace for types that serve a specialised purpose rather than acting as general-purpose containers.
/// Everything runs offline and deterministically.
/// </summary>
public static class Program
{
    /// <summary>
    /// Runs every scenario in order.
    /// </summary>
    public static void Main()
    {
        Console.WriteLine("Bodu.Collections.Samples.BitSets");
        Console.WriteLine("================================");
        Console.WriteLine();

        BitSetAddressing.Run();
        BitSetAlgebra.Run();

        Console.WriteLine("Done.");
    }
}
