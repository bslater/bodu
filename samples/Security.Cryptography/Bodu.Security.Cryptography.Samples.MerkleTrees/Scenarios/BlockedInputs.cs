// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BlockedInputs.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography.Samples.MerkleTrees.Scenarios;

/// <summary>
/// Demonstrates the blocked surface, which treats one large input as a sequence of fixed-size leaves: the static block
/// arithmetic, <see cref="MerkleTree.ComputeBlocked(Stream, int, MerkleTreeDiagnostics?, CancellationToken)" /> and its
/// async counterpart, <see cref="MerkleBlockComputation" />, and
/// <see cref="MerkleTree.VerifyBlockInclusion" /> as the fail-closed possession check.
/// </summary>
public static class BlockedInputs
{
    /// <summary>The block size used throughout the scenario.</summary>
    private const int BlockSize = 1024;

    /// <summary>The payload length: three whole blocks plus a 300-byte tail.</summary>
    private const int PayloadLength = (3 * BlockSize) + 300;

    /// <summary>
    /// Computes a blocked commitment four ways, then proves possession of one block.
    /// </summary>
    /// <returns>A task that completes when the scenario has finished.</returns>
    public static async Task RunAsync()
    {
        Console.WriteLine("--- Blocked and streaming inputs ---");

        var tree = new MerkleTree(SHA256.Create);
        var payload = SampleLog.Payload(PayloadLength);

        // The block arithmetic is static on MerkleTree, so a caller slicing the input itself computes the same offsets
        // the library does rather than re-deriving (and mis-deriving) them.
        var count = MerkleTree.BlockCount(PayloadLength, BlockSize);
        Console.WriteLine($"  payload       : {PayloadLength} bytes, block size {BlockSize}");
        Console.WriteLine($"  BlockCount    : {count} (the final block is short, not padded)");

        for (long index = 0; index < count; index++)
        {
            Console.WriteLine(
                $"    block {index}     : offset {MerkleTree.BlockOffset(index, BlockSize),5}, " +
                $"length {MerkleTree.BlockLength(PayloadLength, index, BlockSize),4}");
        }

        // ComputeBlocked over a span: one MerkleBlockComputation carries the root, the input length, the block size and
        // every leaf hash - so a caller that needs paths later does not have to re-read the input. It also exposes the
        // same block arithmetic as instance members, already bound to this computation's length and block size.
        var inMemory = tree.ComputeBlocked(payload, BlockSize);
        Console.WriteLine($"  root          : {Hex.ToHex(inMemory.Root)}");
        Console.WriteLine($"  computation   : InputLength={inMemory.InputLength}, BlockSize={inMemory.BlockSize}, BlockCount={inMemory.BlockCount}, LeafHashes={inMemory.LeafHashes.Count}");
        Console.WriteLine($"  block 3 via computation: offset {inMemory.BlockOffset(3)}, length {inMemory.BlockLength(3)}");

        // The Stream overload is a single pass: it never holds the whole input, only the O(log n) spine of pending
        // subtree hashes, so it scales to inputs far larger than memory. It must agree byte for byte with the span
        // overload.
        using var stream = new MemoryStream(payload, writable: false);
        var streamed = tree.ComputeBlocked(stream, BlockSize);
        Console.WriteLine($"  streamed root : {Hex.ToShortHex(streamed.Root)} == in-memory: {Hex.ToHex(streamed.Root) == Hex.ToHex(inMemory.Root)}");

        // ComputeBlockedAsync is the same fold with asynchronous reads, for a network or file stream. Same answer.
        using var asyncStream = new MemoryStream(payload, writable: false);
        var asyncComputation = await tree.ComputeBlockedAsync(asyncStream, BlockSize).ConfigureAwait(false);
        Console.WriteLine($"  async root    : {Hex.ToShortHex(asyncComputation.Root)} == in-memory: {Hex.ToHex(asyncComputation.Root) == Hex.ToHex(inMemory.Root)}");

        // ComputeRootOfBlocks is the same computation when only the root is wanted - it does not retain leaf hashes,
        // so it is the cheaper call for a writer that will never issue a proof.
        using var rootOnlyStream = new MemoryStream(payload, writable: false);
        var rootOnly = await tree.ComputeRootOfBlocksAsync(rootOnlyStream, BlockSize).ConfigureAwait(false);
        Console.WriteLine($"  root-only     : {Hex.ToShortHex(rootOnly)} == in-memory: {Hex.ToHex(rootOnly) == Hex.ToHex(inMemory.Root)}");

        // ComputeRootOfLeafHashes rebuilds the same root from the leaf hashes alone - the blocks are no longer needed
        // once they have been hashed.
        var fromLeaves = tree.ComputeRootOfLeafHashes([.. inMemory.LeafHashes]);
        Console.WriteLine($"  from leaves   : {Hex.ToShortHex(fromLeaves)} == in-memory: {Hex.ToHex(fromLeaves) == Hex.ToHex(inMemory.Root)}");

        // Now the possession check. The published commitment is the *bound* root, so the byte length is authenticated
        // and VerifyBlockInclusion derives the leaf count itself rather than trusting a claimed tree size - the
        // fail-closed alternative to VerifyInclusion, for the reason the SizeBinding scenario demonstrates.
        var boundRoot = tree.BindRoot(inMemory.Root, PayloadLength);
        const long Challenge = 2;

        var path = SampleLog.ToProof(tree.AuthenticationPath([.. inMemory.LeafHashes], Challenge));
        var offset = (int)MerkleTree.BlockOffset(Challenge, BlockSize);
        var length = MerkleTree.BlockLength(PayloadLength, Challenge, BlockSize);

        Console.WriteLine($"  bound root    : {Hex.ToShortHex(boundRoot)}");
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
}
