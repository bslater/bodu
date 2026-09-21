// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MerkleCommitments.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;
using System.Text;

using Bodu.Collections.Specialized;

namespace Bodu.Collections.Samples.SpecializedStructures.Scenarios;

/// <summary>
/// Demonstrates the core RFC 6962 commitment: hashing a list of entries into a single root with
/// <see cref="Rfc6962MerkleTree.ComputeRoot" />, issuing an audit path with
/// <see cref="Rfc6962MerkleTree.AuthenticationPath(IReadOnlyList{ReadOnlyMemory{byte}}, long)" />, and checking it
/// with <see cref="Rfc6962MerkleTree.VerifyInclusion" />.
/// </summary>
public static class MerkleCommitments
{
    /// <summary>The audit-log entries committed to by every scenario in this sample.</summary>
    internal static readonly string[] LogEntries =
    [
        "2026-01-04 deploy  v1.0.0",
        "2026-01-07 rotate  signing-key",
        "2026-01-11 deploy  v1.0.1",
        "2026-01-19 revoke  cert-4417",
        "2026-02-02 deploy  v1.1.0",
        "2026-02-14 rotate  signing-key",
        "2026-03-01 deploy  v1.2.0",
    ];

    /// <summary>
    /// Publishes a root over the log, proves one entry's membership against it, then tampers with the proof.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- Rfc6962MerkleTree: roots and inclusion proofs ---");

        // The tree takes a hash-algorithm *factory*, not an instance: each operation creates and disposes its own
        // hasher, which is what makes the parallel surface safe and the type reusable across calls.
        var tree = new Rfc6962MerkleTree(SHA256.Create);
        var entries = Entries();

        Console.WriteLine($"  hash length   : {tree.HashLength} bytes (SHA-256)");
        Console.WriteLine($"  entries       : {entries.Count}");

        // The Merkle Tree Hash of the whole list. This single value is what a log operator publishes and signs.
        var root = tree.ComputeRoot(entries);
        Console.WriteLine($"  root          : {Hex.Full(root)}");

        // RFC 6962 domain-separates leaves from interior nodes: a leaf hash is H(0x00 || entry) and an interior node
        // is H(0x01 || left || right). Without that separation an attacker could present an interior node as a leaf
        // and claim a subtree's hash was itself a logged entry - the "second preimage" attack the prefixes close.
        var leafHash = tree.HashLeaf(Utf8(LogEntries[0]));
        var rawHash = SHA256.HashData(Utf8(LogEntries[0]));
        Console.WriteLine($"  HashLeaf(e[0]): {Hex.Short(leafHash)}");
        Console.WriteLine($"  SHA256(e[0])  : {Hex.Short(rawHash)} (differs - the 0x00 leaf prefix)");
        Console.WriteLine($"  HashNode pair : {Hex.Short(tree.HashNode(leafHash, leafHash))} (the 0x01 node prefix)");

        // The empty tree's root is the hash of the empty string, as the RFC specifies; a one-entry tree's root is
        // that entry's leaf hash with no node hashing at all.
        Console.WriteLine($"  empty root    : {Hex.Short(tree.ComputeRoot([]))}");
        var single = tree.ComputeRoot(entries.Take(1).ToArray());
        Console.WriteLine($"  1-entry root  : {Hex.Short(single)} == HashLeaf(e[0]): {Hex.Full(single) == Hex.Full(leafHash)}");

        // An audit path is the sibling hashes needed to recompute the root from one leaf: ceil(log2(n)) steps, so
        // proving membership in a seven-entry log costs three hashes rather than re-reading the log.
        const int Index = 3;
        var path = ToPath(tree.AuthenticationPath(entries, Index));
        Console.WriteLine($"  path for e[{Index}] : {path.Count} steps ({string.Join(", ", path.Select(step => Hex.Short(step.Span)))})");

        // A verifier holding only the signed root, the entry, its index, and the tree size can now confirm
        // membership. It never sees the other six entries.
        Console.WriteLine($"  verify e[{Index}]   : {tree.VerifyInclusion(root, entries.Count, Index, Utf8(LogEntries[Index]), path)}");

        // Tampering with the entry breaks the walk: the recomputed head no longer equals the published root.
        var tampered = Utf8(LogEntries[Index]);
        tampered[^1] ^= 0x01;
        Console.WriteLine($"  tampered entry: {tree.VerifyInclusion(root, entries.Count, Index, tampered, path)}");

        // So does claiming the right entry at the wrong position - the index drives the left/right decisions.
        Console.WriteLine($"  wrong index   : {tree.VerifyInclusion(root, entries.Count, 2, Utf8(LogEntries[Index]), path)}");

        // Verification is total: every malformed input returns false rather than throwing, so a verifier can feed it
        // attacker-supplied values without a try/catch. Only a null path argument is an exception.
        Console.WriteLine($"  short root    : {tree.VerifyInclusion(root.AsSpan(0, 16), entries.Count, Index, Utf8(LogEntries[Index]), path)}");
        Console.WriteLine($"  path too long : {tree.VerifyInclusion(root, entries.Count, Index, Utf8(LogEntries[Index]), [.. path, (ReadOnlyMemory<byte>)root])}");
        Console.WriteLine($"  index >= size : {tree.VerifyInclusion(root, entries.Count, 99, Utf8(LogEntries[Index]), path)}");

        Console.WriteLine();
    }

    /// <summary>
    /// Returns <see cref="LogEntries" /> as the read-only memory list the entry-mode surface accepts.
    /// </summary>
    /// <returns>The UTF-8 encoded log entries, in order.</returns>
    internal static IReadOnlyList<ReadOnlyMemory<byte>> Entries() =>
        [.. LogEntries.Select(entry => (ReadOnlyMemory<byte>)Utf8(entry))];

    /// <summary>
    /// Converts the <c>byte[][]</c> a proof method returns into the list shape a verify method accepts.
    /// </summary>
    /// <param name="steps">The proof steps.</param>
    /// <returns>The same steps as a read-only memory list.</returns>
    internal static IReadOnlyList<ReadOnlyMemory<byte>> ToPath(byte[][] steps) =>
        [.. steps.Select(step => (ReadOnlyMemory<byte>)step)];

    /// <summary>
    /// Encodes a log entry as UTF-8.
    /// </summary>
    /// <param name="text">The text to encode.</param>
    /// <returns>A fresh byte array holding the encoded text.</returns>
    internal static byte[] Utf8(string text) =>
        Encoding.UTF8.GetBytes(text);
}
