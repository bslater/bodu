// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MerkleBlockedInputs.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

using Bodu.Collections.Specialized;

namespace Bodu.Collections.Samples.SpecializedStructures.Scenarios;

/// <summary>
/// Demonstrates the blocked surface, which treats one large input as a sequence of fixed-size leaves:
/// <see cref="MerkleBlocks" /> block arithmetic, <see cref="Rfc6962MerkleTree.ComputeBlocked(Stream, int, CancellationToken)" />
/// single-pass streaming, the parallel leaf-hashing variant, and
/// <see cref="Rfc6962MerkleTree.VerifyBlockInclusion" /> as the fail-closed possession check.
/// </summary>
public static class MerkleBlockedInputs
{
    /// <summary>The block size used throughout the scenario.</summary>
    private const int BlockSize = 1024;

    /// <summary>The payload length: three whole blocks plus a 300-byte tail.</summary>
    private const int PayloadLength = (3 * BlockSize) + 300;

    /// <summary>
    /// Computes a blocked commitment three ways — in memory, streaming, and in parallel — then proves possession of
    /// one block.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- Rfc6962MerkleTree: blocked and streaming inputs ---");

        var tree = new Rfc6962MerkleTree(SHA256.Create);
        var payload = MerkleSizeBinding.Payload(PayloadLength);

        // MerkleBlocks is the shared arithmetic every blocked overload uses. Exposing it means a caller slicing the
        // input itself computes the same offsets the library does, rather than re-deriving (and mis-deriving) them.
        var count = MerkleBlocks.BlockCount(PayloadLength, BlockSize);
        Console.WriteLine($"  payload       : {PayloadLength} bytes, block size {BlockSize}");
        Console.WriteLine($"  BlockCount    : {count} (the final block is short, not padded)");

        for (long index = 0; index < count; index++)
        {
            Console.WriteLine(
                $"    block {index}     : offset {MerkleBlocks.BlockOffset(index, BlockSize),5}, " +
                $"length {MerkleBlocks.BlockLength(PayloadLength, index, BlockSize),4}");
        }

        // ComputeBlocked over a span: one MerkleComputation carries the root, the input length, the block size, and
        // every leaf hash - so a caller that needs paths later does not have to re-read the input.
        var inMemory = tree.ComputeBlocked(payload, BlockSize);
        Console.WriteLine($"  root          : {Hex.Full(inMemory.Root)}");
        Console.WriteLine($"  computation   : InputLength={inMemory.InputLength}, BlockSize={inMemory.BlockSize}, LeafHashes={inMemory.LeafHashes.Count}");

        // The stream overload is a single pass with a logarithmic-memory fold: it never holds the input, only the
        // O(log n) spine of pending subtree hashes. Streaming and in-memory must agree byte for byte.
        using var stream = new MemoryStream(payload, writable: false);
        var streamed = tree.ComputeBlocked(stream, BlockSize);
        Console.WriteLine($"  streamed root : {Hex.Short(streamed.Root)} == in-memory: {Hex.Full(streamed.Root) == Hex.Full(inMemory.Root)}");

        // Parallel leaf hashing is an optimisation, never a different answer: leaves are independent, and the
        // reduction stays ordered. A fixed degree of parallelism keeps the sample deterministic in wall-clock terms
        // as well as in output.
        var parallel = tree.ComputeBlockedParallel(payload.AsMemory(), BlockSize, maxDegreeOfParallelism: 2);
        Console.WriteLine($"  parallel root : {Hex.Short(parallel.Root)} == serial: {Hex.Full(parallel.Root) == Hex.Full(inMemory.Root)}");

        // ComputeRootOfLeafHashes rebuilds the same root from the leaf hashes alone - the entries are no longer
        // needed once they have been hashed.
        var fromLeaves = tree.ComputeRootOfLeafHashes([.. inMemory.LeafHashes]);
        Console.WriteLine($"  from leaves   : {Hex.Short(fromLeaves)} == in-memory: {Hex.Full(fromLeaves) == Hex.Full(inMemory.Root)}");

        // Now the possession check. The published commitment is the *bound* root, so the byte length is authenticated
        // and VerifyBlockInclusion derives the leaf count itself rather than trusting a claimed tree size - the
        // fail-closed alternative to VerifyInclusion for exactly the reason MerkleSizeBinding demonstrates.
        var boundRoot = tree.BindRoot(inMemory.Root, PayloadLength);
        const long Challenge = 2;

        var path = MerkleCommitments.ToPath(tree.AuthenticationPath([.. inMemory.LeafHashes], Challenge));
        var offset = (int)MerkleBlocks.BlockOffset(Challenge, BlockSize);
        var length = MerkleBlocks.BlockLength(PayloadLength, Challenge, BlockSize);

        Console.WriteLine($"  bound root    : {Hex.Short(boundRoot)}");
        Console.WriteLine($"  challenge     : block {Challenge} ({length} bytes at offset {offset}), path of {path.Count} steps");
        Console.WriteLine(
            $"  answer        : {tree.VerifyBlockInclusion(boundRoot, PayloadLength, BlockSize, Challenge, payload.AsSpan(offset, length), path)}");

        // The path alone is not the answer: a holder that kept the proof but discarded the block cannot pass, because
        // the block is re-hashed as part of verification.
        var lost = payload.AsSpan(offset, length).ToArray();
        lost[^1] ^= 0x01;
        Console.WriteLine(
            $"  wrong block   : {tree.VerifyBlockInclusion(boundRoot, PayloadLength, BlockSize, Challenge, lost, path)} (one flipped byte)");

        // And a holder who understates the length gets a different derived tree - the ambiguity stays closed.
        Console.WriteLine(
            $"  wrong length  : {tree.VerifyBlockInclusion(boundRoot, PayloadLength - BlockSize, BlockSize, Challenge, payload.AsSpan(offset, length), path)}");

        Console.WriteLine();
    }

    /// <summary>
    /// Cuts a payload into <paramref name="blockSize" />-byte blocks using the shipped
    /// <see cref="MerkleBlocks" /> arithmetic, so a caller-side slice and a library-side slice cannot diverge.
    /// </summary>
    /// <param name="payload">The payload to cut.</param>
    /// <param name="blockSize">The block size, in bytes.</param>
    /// <returns>The blocks, in order; the final block is short rather than padded.</returns>
    internal static IReadOnlyList<ReadOnlyMemory<byte>> Cut(byte[] payload, int blockSize)
    {
        var count = MerkleBlocks.BlockCount(payload.Length, blockSize);
        var blocks = new ReadOnlyMemory<byte>[count];

        for (long index = 0; index < count; index++)
        {
            blocks[index] = payload.AsMemory(
                (int)MerkleBlocks.BlockOffset(index, blockSize),
                MerkleBlocks.BlockLength(payload.Length, index, blockSize));
        }

        return blocks;
    }
}
