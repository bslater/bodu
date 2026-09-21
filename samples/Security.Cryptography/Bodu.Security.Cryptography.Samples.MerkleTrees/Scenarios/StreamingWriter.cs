// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StreamingWriter.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography.Samples.MerkleTrees.Scenarios;

/// <summary>
/// Demonstrates <see cref="MerkleBlockAccumulator" />, the push-style writer-side counterpart to the blocked surface:
/// a writer that is already streaming bytes to storage feeds the same calls to
/// <see cref="MerkleBlockAccumulator.Append" /> and gets a root without a second pass over the input.
/// </summary>
public static class StreamingWriter
{
    /// <summary>The block size used throughout the scenario.</summary>
    private const int BlockSize = 256;

    /// <summary>The payload length: five whole blocks plus a 91-byte tail.</summary>
    private const int PayloadLength = (5 * BlockSize) + 91;

    /// <summary>
    /// Accumulates the same payload under several chunk patterns, then shows the bound root, reset, and path issuing.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- Write-time accumulation (MerkleBlockAccumulator) ---");

        var tree = new MerkleTree(SHA256.Create);
        var payload = SampleLog.Payload(PayloadLength);

        // The reference answer: what the pull-style API computes over the same bytes at the same block size.
        var expected = tree.ComputeRootOfBlocks(payload, BlockSize);
        Console.WriteLine($"  payload       : {PayloadLength} bytes, block size {BlockSize}");
        Console.WriteLine($"  ComputeRootOfBlocks: {Hex.ToHex(expected)}");

        // The central guarantee: the root is independent of how the input was split across Append calls. Append
        // re-blocks internally, so a writer passes bytes in whatever sizes they arrive - socket reads, buffer flushes,
        // a single array - and still lands on the same root.
        Console.WriteLine("  Append patterns (all must agree):");
        foreach (var (label, chunks) in ChunkPatterns())
        {
            using var accumulator = tree.CreateBlockAccumulator(BlockSize);
            var offset = 0;
            foreach (var chunk in chunks)
            {
                var take = Math.Min(chunk, PayloadLength - offset);
                accumulator.Append(payload.AsSpan(offset, take));
                offset += take;
            }

            var root = accumulator.Finish();
            Console.WriteLine(
                $"    {label,-22}: {Hex.ToShortHex(root)} {(Hex.ToHex(root) == Hex.ToHex(expected) ? "agrees" : "DIFFERS")}" +
                $"  (Length={accumulator.Length}, LeafCount={accumulator.LeafCount})");
        }

        // Memory is one block plus one pending hash per level - logarithmic in the leaf count - so this works on
        // inputs far larger than memory. Finish is idempotent: calling it again returns the same root, not a
        // different one, so a writer can ask for the root defensively.
        using var repeated = tree.CreateBlockAccumulator(BlockSize);
        repeated.Append(payload);
        var first = repeated.Finish();
        Console.WriteLine($"  Finish twice  : same root: {Hex.ToHex(first) == Hex.ToHex(repeated.Finish())}, IsFinished={repeated.IsFinished}");

        // Appending after a finish is a lifecycle error; Reset clears the finished state and starts over with the same
        // algorithm and buffer, so one accumulator can serve a sequence of objects.
        Console.WriteLine($"  Append after Finish: {Throws(() => repeated.Append([0x00]))}");
        repeated.Reset();
        Console.WriteLine($"  after Reset   : Length={repeated.Length}, LeafCount={repeated.LeafCount}, IsFinished={repeated.IsFinished}");

        // An empty input folds to the empty tree's root, H(), rather than failing - the degenerate case a writer hits
        // on a zero-byte object needs no special casing.
        using var empty = tree.CreateBlockAccumulator(BlockSize);
        Console.WriteLine($"  empty input   : {Hex.ToShortHex(empty.Finish())} == ComputeRoot([]): {Hex.ToHex(empty.Finish()) == Hex.ToHex(tree.ComputeRoot([]))}");

        // FinishBound is BindRoot applied to Finish and Length - the commitment to publish when a verifier will later
        // be told the length by a party it does not trust. The writer never has to track the length itself.
        using var bound = tree.CreateBlockAccumulator(BlockSize);
        bound.Append(payload);
        var boundRoot = bound.FinishBound();
        Console.WriteLine($"  FinishBound   : {Hex.ToShortHex(boundRoot)} == BindRoot(root, {PayloadLength}): {Hex.ToHex(boundRoot) == Hex.ToHex(tree.BindRoot(expected, PayloadLength))}");

        // By default leaf hashes are discarded as they are folded, which is what keeps memory logarithmic. A writer
        // that will need to answer challenges asks for them to be retained, and FinishComputation then hands back the
        // same MerkleBlockComputation the pull-style API produces - enough to issue authentication paths later.
        using var retaining = tree.CreateBlockAccumulator(BlockSize, retainLeafHashes: true);
        retaining.Append(payload);
        var computation = retaining.FinishComputation();

        Console.WriteLine($"  RetainsLeafHashes: {retaining.RetainsLeafHashes} -> LeafHashes={computation.LeafHashes.Count}");

        const long Challenge = 3;
        var path = SampleLog.ToProof(tree.AuthenticationPath([.. computation.LeafHashes], Challenge));
        var offset2 = (int)computation.BlockOffset(Challenge);
        var length2 = computation.BlockLength(Challenge);

        Console.WriteLine(
            $"  challenge block {Challenge}: {tree.VerifyBlockInclusion(boundRoot, PayloadLength, BlockSize, Challenge, payload.AsSpan(offset2, length2), path)}" +
            $" (answered from a write-time commitment)");

        Console.WriteLine();
    }

    /// <summary>
    /// Returns the chunk patterns the scenario appends the payload under.
    /// </summary>
    /// <returns>A label and the repeating chunk sizes, in bytes, for each pattern.</returns>
    /// <remarks>
    /// The patterns deliberately straddle the block size in both directions: smaller than a block, larger than a
    /// block, exactly one block, and an irregular cycle that never aligns with it.
    /// </remarks>
    private static IEnumerable<(string Label, int[] Chunks)> ChunkPatterns()
    {
        yield return ("one call", [PayloadLength]);
        yield return ("1 byte at a time", [.. Enumerable.Repeat(1, PayloadLength)]);
        yield return ("exactly one block", [.. Enumerable.Repeat(BlockSize, 6)]);
        yield return ("100-byte chunks", [.. Enumerable.Repeat(100, 14)]);
        yield return ("1000-byte chunks", [.. Enumerable.Repeat(1000, 2)]);
        yield return ("irregular 7/500/13", [.. Enumerable.Repeat(new[] { 7, 500, 13 }, 100).SelectMany(x => x)]);
    }

    /// <summary>
    /// Invokes an action and names the exception type it threw.
    /// </summary>
    /// <param name="action">The action to invoke.</param>
    /// <returns>The exception's type name, or a marker when the action completed.</returns>
    private static string Throws(Action action)
    {
        try
        {
            action();
            return "(did not throw)";
        }
        catch (Exception ex)
        {
            return $"{ex.GetType().Name} (as documented)";
        }
    }
}
