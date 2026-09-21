// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DiagnosticsTrace.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography.Samples.MerkleTrees.Scenarios;

/// <summary>
/// Demonstrates <see cref="MerkleTreeDiagnostics" />, the optional trace recorder: passing one to any root computation
/// records every leaf and internal node, so the tree can be walked level by level, re-validated independently, and
/// printed.
/// </summary>
public static class DiagnosticsTrace
{
    /// <summary>
    /// Records a four-entry tree, walks it by level, validates it, and shows a deliberately corrupted trace failing.
    /// </summary>
    public static void Run()
    {
        SampleConsole.Scenario(
            "Diagnostics trace",
            what: "Records every leaf and internal node of a four-entry tree into a MerkleTreeDiagnostics, walks it level by level, re-validates the recorded nodes with an independently created hash algorithm, then renders the whole trace.",
            why: "A trace turns an opaque 32-byte root into something reviewable - useful in a bug report or when reconciling a root against another implementation. Validate re-derives every internal node from its recorded children, so it proves the fold's internal consistency rather than merely echoing what was stored.",
            expect: "Four leaves fold to two internal nodes and one root, and the recorded root matches the value ComputeRoot returned. Validate passes with 0 errors; re-validating with SHA-512 fails with 3 mismatches, which is the proof that the check is real and not a method that always returns true.");

        var tree = new MerkleTree(SHA256.Create);

        // A deliberately small tree so the whole trace is legible. Recording costs memory proportional to the node
        // count, so this is a diagnostic aid rather than something to leave enabled in production.
        var entries = SampleLog.AsEntries().Take(4).ToArray();

        var diagnostics = new MerkleTreeDiagnostics();
        var root = tree.ComputeRoot(entries, diagnostics);

        Console.WriteLine($"  entries       : {entries.Length}");
        Console.WriteLine($"  root          : {Hex.ToShortHex(root)}");
        Console.WriteLine($"  levels        : {diagnostics.GetLevelCount()} (level 0 is the leaves)");
        Console.WriteLine($"  nodes         : {diagnostics.GetAllNodes().Count}");

        // Walking by level shows the fold: four leaves, two internal nodes, one root. Both GetLevel and GetAllNodes
        // sort their results, so a trace reads the same on every run even though recording is thread-safe and a
        // parallel computation may record out of order.
        for (var level = 0; level < diagnostics.GetLevelCount(); level++)
        {
            var nodes = diagnostics.GetLevel(level);
            var kind = nodes[0].IsLeaf ? "leaf" : "node";
            Console.WriteLine($"    level {level} ({kind,4}): {string.Join(", ", nodes.Select(n => $"[{n.Index}] {Hex.ToShortHex(n.Hash)}"))}");
        }

        // The Root property is the sole node at the highest level, and its hash is the value the computation returned.
        var recordedRoot = diagnostics.Root;
        Console.WriteLine($"  Root node     : level {recordedRoot!.Level}, index {recordedRoot.Index}, {recordedRoot.ChildHashes.Count} children");
        Console.WriteLine($"  matches return: {Hex.ToHex(recordedRoot.Hash) == Hex.ToHex(root)}  (expected True - the recorded root is the value ComputeRoot returned)");

        // Validate independently re-computes every internal node from its recorded children and compares. It is the
        // check that the fold wired the right children to the right parents - which a root value alone cannot show,
        // because any bug that produced a self-consistent wrong tree would still produce one root.
        var valid = diagnostics.Validate(SHA256.Create, out var errors);
        Console.WriteLine($"  Validate      : {valid} ({errors.Count} errors)  (expected True - every internal node re-derives from its recorded children)");

        // Leaf hashes are not re-validated against the original bytes, because the raw blocks are not retained - so a
        // trace proves the fold's internal consistency, not that the leaves were hashed from the input you think.

        // That the check is real, and not a method that returns true unconditionally, is visible by re-deriving with
        // the wrong algorithm: every internal node then fails to reproduce, and Validate names each one.
        var wrongAlgorithm = diagnostics.Validate(SHA512.Create, out var mismatches);
        Console.WriteLine($"  Validate(SHA512): {wrongAlgorithm} ({mismatches.Count} mismatch(es) - the nodes were folded with SHA-256)");

        // WriteTo renders the whole trace for a log or a bug report; passing the factory appends the validation summary.
        Console.WriteLine("  WriteTo(Console.Out):");
        using var writer = new StringWriter();
        diagnostics.WriteTo(writer, SHA256.Create);

        // Normalize the line endings before splitting: WriteTo separates its sections with blank lines, and dropping
        // empty entries would keep them on Windows (where the entry is "\r") and lose them everywhere else, so the
        // same trace would render differently per platform.
        var trace = writer.ToString().Replace("\r\n", "\n").Trim('\n');
        foreach (var line in trace.Split('\n'))
            Console.WriteLine(line.Length == 0 ? string.Empty : $"    {line}");

        Console.WriteLine();
    }
}
