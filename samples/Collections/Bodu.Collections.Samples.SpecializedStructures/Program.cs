// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Collections.Samples.SpecializedStructures.Scenarios;

namespace Bodu.Collections.Samples.SpecializedStructures;

/// <summary>
/// Entry point for the specialized-structures sample: a tour of <c>Bodu.Collections.Specialized</c> — the packed
/// <c>BitSet</c> and the RFC 6962 Merkle tree. These are the two members of the package that serve a specialised
/// purpose rather than acting as general-purpose containers, so they get their own sample rather than a slot in the
/// collection catalogue. Everything runs offline and deterministically.
/// </summary>
public static class Program
{
    /// <summary>
    /// Runs every scenario in order.
    /// </summary>
    public static void Main()
    {
        Console.WriteLine("Bodu.Collections.Samples.SpecializedStructures");
        Console.WriteLine("==============================================");
        Console.WriteLine();

        BitSetAddressing.Run();
        BitSetAlgebra.Run();
        MerkleCommitments.Run();
        MerkleSizeBinding.Run();
        MerkleConsistency.Run();
        MerkleBlockedInputs.Run();

        Console.WriteLine("Done.");
    }
}
