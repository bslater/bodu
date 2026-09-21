// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ShapeAndParallelism.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography.Samples.MerkleTrees.Scenarios;

/// <summary>
/// Demonstrates the two constructor knobs: <see cref="MerkleTree.MaxDegreeOfParallelism" />, which is an optimisation
/// that must never change the answer, and <see cref="MerkleTree.FanOut" />, which does — a wider fan-out is an explicit
/// non-RFC mode on which every proof member throws.
/// </summary>
public static class ShapeAndParallelism
{
    /// <summary>
    /// Compares roots across degrees of parallelism, then across fan-outs, and shows the proof members refusing.
    /// </summary>
    public static void Run()
    {
        SampleConsole.Scenario(
            "Fan-out and parallelism",
            what: "Computes the same two commitments at four degrees of parallelism, then computes the entry root at fan-outs 2, 3, 4 and 8, and asks a wide tree for a proof.",
            why: "The two constructor knobs differ in kind. MaxDegreeOfParallelism is an optimisation that must never change the answer - leaves hash in parallel while the fold stays ordered on the caller thread. FanOut changes the tree's shape, so it changes the root, and a fan-out above two is no longer RFC 6962's tree.",
            expect: "Every degree of parallelism agrees on both roots - a differing row would be a defect. Each fan-out prints a different root, and that is correct, not a mismatch: they are different trees. Proof members on a wide tree throw NotSupportedException, because the proofs are defined for the binary tree only.");

        var entries = SampleLog.AsEntries();
        var payload = SampleLog.Payload(64 * 1024);

        // Parallelism is an optimisation, never a different answer: leaves are independent, so they can be hashed in
        // parallel, while the fold and any observer stay on the caller thread and in order. 1 is sequential, -1 is
        // unbounded, >= 2 is a bounded worker count.
        Console.WriteLine("  MaxDegreeOfParallelism (must all agree):");
        var sequentialRoot = Hex.ToHex(new MerkleTree(SHA256.Create).ComputeRoot(entries));

        foreach (var degree in new[] { 1, 2, 4, -1 })
        {
            var tree = new MerkleTree(SHA256.Create, maxDegreeOfParallelism: degree);
            var entryRoot = Hex.ToHex(tree.ComputeRoot(entries));
            var blockRoot = Hex.ToHex(tree.ComputeRootOfBlocks(payload, 1024));

            Console.WriteLine(
                $"    degree {degree,2}     : entries {Hex.ToShortHex(Convert.FromHexString(entryRoot))} " +
                $"{(entryRoot == sequentialRoot ? "agrees" : "DIFFERS")}, 64 KiB blocked {Hex.ToShortHex(Convert.FromHexString(blockRoot))}");
        }

        // Fan-out is different in kind: it changes the tree's shape, so it changes the root. Two is RFC 6962's tree and
        // the default; a wider fan-out hashes k children per node, H(0x01 || child0 || ... || child_k-1).
        Console.WriteLine("  FanOut (each shape is a different tree):");
        foreach (var fanOut in new[] { 2, 3, 4, 8 })
        {
            var tree = new MerkleTree(SHA256.Create, fanOut);
            Console.WriteLine(
                $"    fan-out {fanOut}     : {Hex.ToShortHex(tree.ComputeRoot(entries))} (IsBinary={tree.IsBinary})");
        }

        // Because RFC 6962's proof formats are defined only for a binary tree, every proof member on a wider tree
        // throws NotSupportedException rather than silently inventing a non-interoperable proof. Root computation still
        // works - a wide tree is a legitimate choice when only a digest is wanted.
        var wide = new MerkleTree(SHA256.Create, fanOut: 4);
        Console.WriteLine($"  wide tree root         : {Hex.ToShortHex(wide.ComputeRoot(entries))} (computing a root is fine)");
        Console.WriteLine($"  wide AuthenticationPath: {Throws(() => wide.AuthenticationPath(entries, 0))}");
        Console.WriteLine($"  wide ConsistencyProof  : {Throws(() => wide.ConsistencyProof(entries, 4))}");

        // The two knobs compose: a wide tree can still hash its leaves in parallel.
        var wideParallel = new MerkleTree(SHA256.Create, fanOut: 4, maxDegreeOfParallelism: 2);
        Console.WriteLine(
            $"  fan-out 4 + degree 2   : {Hex.ToShortHex(wideParallel.ComputeRoot(entries))} " +
            $"== sequential fan-out 4: {Hex.ToHex(wideParallel.ComputeRoot(entries)) == Hex.ToHex(wide.ComputeRoot(entries))}");

        Console.WriteLine();
    }

    /// <summary>
    /// Invokes a function and names the exception type it threw.
    /// </summary>
    /// <param name="action">The function to invoke.</param>
    /// <returns>The exception's type name, or a marker when the call completed.</returns>
    private static string Throws(Func<object> action)
    {
        try
        {
            _ = action();
            return "(did not throw)";
        }
        catch (Exception ex)
        {
            return $"{ex.GetType().Name} (proofs are RFC 6962 binary only)";
        }
    }
}
