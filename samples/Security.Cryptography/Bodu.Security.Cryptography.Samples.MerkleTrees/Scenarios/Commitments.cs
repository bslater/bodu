// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Commitments.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography.Samples.MerkleTrees.Scenarios;

/// <summary>
/// Demonstrates the core RFC 6962 commitment: hashing a list of entries into a single root with
/// <see cref="MerkleTree.ComputeRoot" />, issuing an audit path with
/// <see cref="MerkleTree.AuthenticationPath(IReadOnlyList{ReadOnlyMemory{byte}}, long)" />, and checking it with
/// <see cref="MerkleTree.VerifyInclusion" />.
/// </summary>
public static class Commitments
{
    /// <summary>
    /// Publishes a root over the log, proves one entry's membership against it, then tampers with the proof.
    /// </summary>
    public static void Run()
    {
        SampleConsole.Scenario(
            "Roots and inclusion proofs",
            what: "Commits a seven-entry audit log to one 32-byte root, issues the audit path for entry 3, then verifies that path honestly and against a tampered entry, a wrong index, and three malformed inputs.",
            why: "The root is what a log operator publishes and signs. An audit path lets a verifier confirm one entry belongs to that root while seeing none of the other six, and RFC 6962's 0x00 leaf / 0x01 node prefixes stop an interior node being passed off as a logged entry.",
            expect: "HashLeaf(e[0]) differs from a plain SHA256(e[0]) because of the 0x00 prefix; a one-entry tree's root IS that leaf hash; seven entries give a 3-step path (ceil(log2 7)). Honest checks print True, every corruption prints False - and the malformed inputs return False rather than throwing.");

        // The tree takes a hash-algorithm *factory*, not an instance. That is what makes MerkleTree immutable,
        // stateless, and safe to share across threads: each operation creates and disposes its own hasher, and the
        // type is deliberately not IDisposable.
        var tree = new MerkleTree(SHA256.Create);
        var entries = SampleLog.AsEntries();

        Console.WriteLine($"  hash length   : {tree.HashLength} bytes (SHA-256)");
        Console.WriteLine($"  fan-out       : {tree.FanOut} (IsBinary={tree.IsBinary} - RFC 6962's tree)");
        Console.WriteLine($"  entries       : {entries.Count}");

        // The Merkle Tree Hash of the whole list. This single value is what a log operator publishes and signs.
        var root = tree.ComputeRoot(entries);
        Console.WriteLine($"  root          : {Hex.ToHex(root)}");

        // RFC 6962 domain-separates leaves from interior nodes: a leaf is H(0x00 || entry) and an interior node is
        // H(0x01 || left || right). Without that separation an attacker could present an interior node as a leaf and
        // claim a subtree's hash was itself a logged entry - the second-preimage attack the prefixes close.
        var leafHash = tree.HashLeaf(SampleLog.Utf8(SampleLog.Entries[0]));
        var rawHash = SHA256.HashData(SampleLog.Utf8(SampleLog.Entries[0]));
        Console.WriteLine($"  HashLeaf(e[0]): {Hex.ToShortHex(leafHash)}");
        Console.WriteLine($"  SHA256(e[0])  : {Hex.ToShortHex(rawHash)} (differs - the 0x00 leaf prefix)");
        Console.WriteLine($"  HashNode pair : {Hex.ToShortHex(tree.HashNode(leafHash, leafHash))} (the 0x01 node prefix)");

        // The empty tree's root is the hash of the empty string, as the RFC specifies; a one-entry tree's root is that
        // entry's leaf hash, with no node hashing at all.
        Console.WriteLine($"  empty root    : {Hex.ToShortHex(tree.ComputeRoot([]))}  (the SHA-256 of the empty string, as the RFC specifies)");
        var single = tree.ComputeRoot(entries.Take(1).ToArray());
        Console.WriteLine($"  1-entry root  : {Hex.ToShortHex(single)} == HashLeaf(e[0]): {Hex.ToHex(single) == Hex.ToHex(leafHash)}");

        // An audit path is the sibling hashes needed to recompute the root from one leaf: ceil(log2 n) steps, so
        // proving membership in a seven-entry log costs three hashes rather than re-reading the log.
        const int Index = 3;
        var path = SampleLog.ToProof(tree.AuthenticationPath(entries, Index));
        Console.WriteLine($"  path for e[{Index}] : {path.Count} steps ({string.Join(", ", path.Select(step => Hex.ToShortHex(step.Span)))})");

        // A verifier holding only the signed root, the entry, its index and the tree size can now confirm membership.
        // It never sees the other six entries.
        Console.WriteLine($"  verify e[{Index}]   : {tree.VerifyInclusion(root, entries.Count, Index, SampleLog.Utf8(SampleLog.Entries[Index]), path)}  (expected True - the three steps fold the entry back to the published root)");

        // Tampering with the entry breaks the walk: the recomputed head no longer equals the published root.
        var tampered = SampleLog.Utf8(SampleLog.Entries[Index]);
        tampered[^1] ^= 0x01;
        Console.WriteLine($"  tampered entry: {tree.VerifyInclusion(root, entries.Count, Index, tampered, path)}  (expected False - one flipped bit in the entry changes its leaf hash)");

        // So does claiming the right entry at the wrong position - the index drives the left/right decisions.
        Console.WriteLine($"  wrong index   : {tree.VerifyInclusion(root, entries.Count, 2, SampleLog.Utf8(SampleLog.Entries[Index]), path)}  (expected False - the index decides which side each step is hashed on)");

        // VerifyInclusionOfLeafHash is the same check for a verifier that was given the leaf hash rather than the
        // entry - useful when the entry itself is large or must not be disclosed.
        var leaf3 = tree.HashLeaf(SampleLog.Utf8(SampleLog.Entries[Index]));
        Console.WriteLine($"  by leaf hash  : {tree.VerifyInclusionOfLeafHash(root, entries.Count, Index, leaf3, path)}  (expected True - the same check for a verifier never shown the entry)");

        // Verification is total: every malformed input returns false rather than throwing, so a verifier can be fed
        // attacker-supplied values without a try/catch. Only a null path argument is an exception.
        Console.WriteLine($"  short root    : {tree.VerifyInclusion(root.AsSpan(0, 16), entries.Count, Index, SampleLog.Utf8(SampleLog.Entries[Index]), path)}  (expected False - a 16-byte root is rejected, not thrown)");
        Console.WriteLine($"  path too long : {tree.VerifyInclusion(root, entries.Count, Index, SampleLog.Utf8(SampleLog.Entries[Index]), [.. path, (ReadOnlyMemory<byte>)root])}  (expected False - a fourth step cannot belong to a seven-leaf tree)");
        Console.WriteLine($"  index >= size : {tree.VerifyInclusion(root, entries.Count, 99, SampleLog.Utf8(SampleLog.Entries[Index]), path)}  (expected False - entry 99 is outside the tree)");

        Console.WriteLine();
    }
}
