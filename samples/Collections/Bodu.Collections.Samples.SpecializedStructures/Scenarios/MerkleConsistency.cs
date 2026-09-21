// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MerkleConsistency.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

using Bodu.Collections.Specialized;

namespace Bodu.Collections.Samples.SpecializedStructures.Scenarios;

/// <summary>
/// Demonstrates the append-only guarantee: <see cref="Rfc6962MerkleTree.ConsistencyProof" /> and
/// <see cref="Rfc6962MerkleTree.VerifyConsistency" /> prove that a later root extends an earlier one without
/// rewriting anything already committed.
/// </summary>
public static class MerkleConsistency
{
    /// <summary>
    /// Proves that the seven-entry log extends the four-entry log, then shows a rewritten history being rejected.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- Rfc6962MerkleTree: consistency (append-only) proofs ---");

        var tree = new Rfc6962MerkleTree(SHA256.Create);
        var entries = MerkleCommitments.Entries();

        // Two snapshots of the same log: as it stood at four entries, and as it stands now at seven.
        var oldEntries = entries.Take(4).ToArray();
        var oldRoot = tree.ComputeRoot(oldEntries);
        var newRoot = tree.ComputeRoot(entries);

        Console.WriteLine($"  root @ 4      : {Hex.Short(oldRoot)}");
        Console.WriteLine($"  root @ 7      : {Hex.Short(newRoot)}");

        // A consistency proof is computed over the *current* entry list plus the earlier size. It is the minimal set
        // of subtree hashes that lets a verifier rebuild both roots, so an auditor who saw only the old root can
        // confirm the new one is an extension of it - never a replacement.
        var proof = MerkleCommitments.ToPath(tree.ConsistencyProof(entries, firstSize: 4));
        var plural = proof.Count == 1 ? "step" : "steps";
        Console.WriteLine($"  proof 4 -> 7  : {proof.Count} {plural} ({string.Join(", ", proof.Select(step => Hex.Short(step.Span)))})");

        // The auditor holds both signed roots and both sizes; it has never seen the entries themselves.
        Console.WriteLine($"  verify 4 -> 7 : {tree.VerifyConsistency(oldRoot, 4, newRoot, 7, proof)}");

        // A proof for size n -> n is trivially empty and verifies, so a log that published twice without appending
        // needs no special casing by the auditor.
        Console.WriteLine($"  verify 7 -> 7 : {tree.VerifyConsistency(newRoot, 7, newRoot, 7, [])}");

        // Now the attack the proof exists to stop: the operator rewrites entry 1 and appends as if nothing happened.
        // The forged log's root is a perfectly valid Merkle root - it just is not an extension of what was published.
        var forged = entries.ToArray();
        forged[1] = MerkleCommitments.Utf8("2026-01-07 rotate  signing-key (backdated)");
        var forgedRoot = tree.ComputeRoot(forged);

        Console.WriteLine($"  forged root @7: {Hex.Short(forgedRoot)}");
        Console.WriteLine($"  forged proof  : {tree.VerifyConsistency(oldRoot, 4, forgedRoot, 7, MerkleCommitments.ToPath(tree.ConsistencyProof(forged, 4)))} (history was rewritten)");

        // A truncated log fails too: sizes must be ordered, and a shrinking log cannot be append-only.
        Console.WriteLine($"  verify 7 -> 4 : {tree.VerifyConsistency(newRoot, 7, oldRoot, 4, proof)} (sizes out of order)");

        // As with inclusion, verification is total - a proof with a step of the wrong width is rejected, not thrown.
        Console.WriteLine($"  mis-sized step: {tree.VerifyConsistency(oldRoot, 4, newRoot, 7, [.. proof.Take(proof.Count - 1), (ReadOnlyMemory<byte>)new byte[16]])}");

        Console.WriteLine();
    }
}
