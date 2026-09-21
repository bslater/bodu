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
/// anything already committed — and show precisely which entries that promise covers.
/// </summary>
public static class Consistency
{
    /// <summary>
    /// Proves that the seven-entry log extends the four-entry log, then shows a rewrite inside the published
    /// snapshot being rejected and a rewrite after it being accepted.
    /// </summary>
    public static void Run()
    {
        SampleConsole.Scenario(
            "Consistency (append-only) proofs",
            what: "Publishes the log's root at 4 entries and again at 7, proves the second extends the first, then " +
                  "rewrites one entry inside the published snapshot and one entry after it and re-runs the proof.",
            why: "An inclusion proof answers \"is this entry in the log?\". A consistency proof answers the harder " +
                 "question \"is this the same log I saw last time, only longer?\" - the one that stops an operator " +
                 "quietly rewriting history. The auditor holds only the two signed roots and the two sizes; it never " +
                 "sees an entry.",
            expect: "The honest proof verifies. Rewriting entry 1 - inside the 4-entry snapshot - fails, because the " +
                    "old root the auditor holds no longer folds to the claimed new root. Rewriting entry 5 - after " +
                    "the snapshot - still verifies, and that is the guarantee working as defined, not a hole: a " +
                    "4 -> 7 proof attests to the first four entries only.");

        var tree = new MerkleTree(SHA256.Create);
        var entries = SampleLog.AsEntries();

        // Two snapshots of the same log: as it stood at four entries, and as it stands now at seven.
        var oldRoot = tree.ComputeRoot(entries.Take(4).ToArray());
        var newRoot = tree.ComputeRoot(entries);

        Console.WriteLine($"  root @ 4       : {Hex.ToShortHex(oldRoot)}  (the snapshot the auditor already holds, signed)");
        Console.WriteLine($"  root @ 7       : {Hex.ToShortHex(newRoot)}  (what the log publishes today)");

        // The proof is computed over the *current* entry list plus the earlier size. Four is a power of two and
        // exactly RFC 6962's split point for seven, so the seven-entry root is H(0x01 || MTH(e0..e3) || MTH(e4..e6)):
        // the auditor is missing only the right subtree, and the proof is that single hash.
        var proof = SampleLog.ToProof(tree.ConsistencyProof(entries, firstSize: 4));
        var plural = proof.Count == 1 ? "step" : "steps";
        Console.WriteLine(
            $"  proof 4 -> 7   : {proof.Count} {plural} ({string.Join(", ", proof.Select(step => Hex.ToShortHex(step.Span)))})" +
            " - the Merkle head of entries 4..6, the only subtree the auditor has not seen");

        // The verifier folds the old root it already trusts with the proof steps and compares the result to the new
        // root. Nothing else about the first four entries enters the calculation, which is what makes the check binding.
        Console.WriteLine($"  verify 4 -> 7  : {tree.VerifyConsistency(oldRoot, 4, newRoot, 7, proof)}  (expected True - H(0x01 || root@4 || step) reproduces root@7)");

        // A proof for n -> n is trivially empty and verifies, so a log that published twice without appending needs
        // no special casing by the auditor.
        Console.WriteLine($"  verify 7 -> 7  : {tree.VerifyConsistency(newRoot, 7, newRoot, 7, [])}  (expected True - republishing without appending needs an empty proof)");

        // The attack the proof exists to stop: the operator backdates an entry that was already published, then
        // appends as if nothing happened. The forged log's root is a perfectly valid Merkle root - it simply is not an
        // extension of the root the auditor holds.
        Console.WriteLine();
        Console.WriteLine("  Rewrite INSIDE the snapshot - entry 1 is backdated, the other six entries left alone:");

        var forged = entries.ToArray();
        forged[1] = SampleLog.Utf8("2026-01-07 rotate  signing-key (backdated)");
        var forgedRoot = tree.ComputeRoot(forged);
        var forgedProof = SampleLog.ToProof(tree.ConsistencyProof(forged, 4));

        Console.WriteLine($"    forged root @7 : {Hex.ToShortHex(forgedRoot)}  (a valid root over the rewritten log - just not an extension of root @ 4)");
        Console.WriteLine($"    proof step     : {Hex.ToShortHex(forgedProof[0].Span)}  (unchanged: entries 4..6 were not touched, so the operator cannot move it)");
        Console.WriteLine($"    verify 4 -> 7  : {tree.VerifyConsistency(oldRoot, 4, forgedRoot, 7, forgedProof)}  (expected False - the fold still yields {Hex.ToShortHex(newRoot)}, which is not the root claimed)");

        // The mirror case, and the one that shows what a consistency proof does *not* promise: an entry appended
        // after the auditor's snapshot is outside what the 4 -> 7 proof attests to, so amending it still verifies.
        // Catching that needs an auditor holding a signed root at size 7 - which is why auditors keep every root.
        Console.WriteLine();
        Console.WriteLine("  Rewrite AFTER the snapshot - entry 5 is backdated, entries 0..3 left intact:");

        var amended = entries.ToArray();
        amended[5] = SampleLog.Utf8("2026-02-14 rotate  signing-key (backdated)");
        var amendedRoot = tree.ComputeRoot(amended);
        var amendedProof = SampleLog.ToProof(tree.ConsistencyProof(amended, 4));

        Console.WriteLine($"    amended root @7: {Hex.ToShortHex(amendedRoot)}  (a different root again)");
        Console.WriteLine($"    verify 4 -> 7  : {tree.VerifyConsistency(oldRoot, 4, amendedRoot, 7, amendedProof)}  (expected True - the proof commits to entries 0..3 only; a signed root at 7 is what catches this)");
        Console.WriteLine();

        // A shrinking log cannot be append-only, so the sizes must be ordered.
        Console.WriteLine($"  verify 7 -> 4  : {tree.VerifyConsistency(newRoot, 7, oldRoot, 4, proof)}  (expected False - sizes out of order; a log cannot shrink)");

        // As with inclusion, verification is total - a proof step of the wrong width is rejected, not thrown.
        Console.WriteLine($"  mis-sized step : {tree.VerifyConsistency(oldRoot, 4, newRoot, 7, [.. proof.Take(proof.Count - 1), (ReadOnlyMemory<byte>)new byte[16]])}  (expected False - a 16-byte step is rejected, not thrown)");

        // ConsistencyProofOfLeafHashes is the same proof for a caller that already holds the leaf hashes - an operator
        // keeping a running list of leaves need not retain the entries to answer an auditor.
        var leafHashes = SampleLog.Entries.Select(entry => tree.HashLeaf(SampleLog.Utf8(entry))).ToArray();
        var fromLeaves = SampleLog.ToProof(tree.ConsistencyProofOfLeafHashes(leafHashes, 4));
        Console.WriteLine($"  from leaf hashes: identical proof: {fromLeaves.Select(s => Hex.ToHex(s.Span)).SequenceEqual(proof.Select(s => Hex.ToHex(s.Span)))}  (expected True - leaves are all the proof needs)");

        Console.WriteLine();
    }
}
