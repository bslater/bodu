// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Consistency.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography.Samples.MerkleTrees.Scenarios;

/// <summary>
/// Demonstrates the append-only guarantee: <see cref="MerkleTree.ConsistencyProof" /> and
/// <see cref="MerkleTree.VerifyConsistency" /> prove that a later root extends an earlier one without rewriting
/// anything already committed.
/// </summary>
public static class Consistency
{
    /// <summary>
    /// Proves that the seven-entry log extends the four-entry log, then shows a rewritten history being rejected.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- Consistency (append-only) proofs ---");

        var tree = new MerkleTree(SHA256.Create);
        var entries = SampleLog.AsEntries();

        // Two snapshots of the same log: as it stood at four entries, and as it stands now at seven.
        var oldRoot = tree.ComputeRoot(entries.Take(4).ToArray());
        var newRoot = tree.ComputeRoot(entries);

        Console.WriteLine($"  root @ 4      : {Hex.ToShortHex(oldRoot)}");
        Console.WriteLine($"  root @ 7      : {Hex.ToShortHex(newRoot)}");

        // An inclusion proof answers "is this entry in the log?". A consistency proof answers the harder question
        // "is this the same log I saw last time, only longer?" - which is what stops an operator rewriting history.
        // It is computed over the *current* entry list plus the earlier size.
        var proof = SampleLog.ToProof(tree.ConsistencyProof(entries, firstSize: 4));
        var plural = proof.Count == 1 ? "step" : "steps";
        Console.WriteLine($"  proof 4 -> 7  : {proof.Count} {plural} ({string.Join(", ", proof.Select(step => Hex.ToShortHex(step.Span)))})");

        // The auditor holds both signed roots and both sizes; it has never seen the entries themselves.
        Console.WriteLine($"  verify 4 -> 7 : {tree.VerifyConsistency(oldRoot, 4, newRoot, 7, proof)}");

        // A proof for n -> n is trivially empty and verifies, so a log that published twice without appending needs
        // no special casing by the auditor.
        Console.WriteLine($"  verify 7 -> 7 : {tree.VerifyConsistency(newRoot, 7, newRoot, 7, [])}");

        // Now the attack the proof exists to stop: the operator rewrites entry 1 and appends as if nothing happened.
        // The forged log's root is a perfectly valid Merkle root - it just is not an extension of what was published.
        var forged = entries.ToArray();
        forged[1] = SampleLog.Utf8("2026-01-07 rotate  signing-key (backdated)");
        var forgedRoot = tree.ComputeRoot(forged);

        Console.WriteLine($"  forged root @7: {Hex.ToShortHex(forgedRoot)}");
        Console.WriteLine($"  forged proof  : {tree.VerifyConsistency(oldRoot, 4, forgedRoot, 7, SampleLog.ToProof(tree.ConsistencyProof(forged, 4)))} (history was rewritten)");

        // A shrinking log cannot be append-only, so the sizes must be ordered.
        Console.WriteLine($"  verify 7 -> 4 : {tree.VerifyConsistency(newRoot, 7, oldRoot, 4, proof)} (sizes out of order)");

        // As with inclusion, verification is total - a proof step of the wrong width is rejected, not thrown.
        Console.WriteLine($"  mis-sized step: {tree.VerifyConsistency(oldRoot, 4, newRoot, 7, [.. proof.Take(proof.Count - 1), (ReadOnlyMemory<byte>)new byte[16]])}");

        // ConsistencyProofOfLeafHashes is the same proof for a caller that already holds the leaf hashes - an operator
        // keeping a running list of leaves need not retain the entries to answer an auditor.
        var leafHashes = SampleLog.Entries.Select(entry => tree.HashLeaf(SampleLog.Utf8(entry))).ToArray();
        var fromLeaves = SampleLog.ToProof(tree.ConsistencyProofOfLeafHashes(leafHashes, 4));
        Console.WriteLine($"  from leaf hashes: identical proof: {fromLeaves.Select(s => Hex.ToHex(s.Span)).SequenceEqual(proof.Select(s => Hex.ToHex(s.Span)))}");

        Console.WriteLine();
    }
}
