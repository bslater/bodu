// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MerkleTreeDiagnostics.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Captures the complete node-by-node trace of a <see cref="ParallelMerkleTreeHash" /> computation, and provides
/// structural inspection and independent hash re-validation.
/// </summary>
/// <remarks>
/// <para>
/// An instance is passed to a <see cref="ParallelMerkleTreeHash" /> <c>ComputeHash</c> call. As the tree is built, each
/// leaf and internal node is recorded concurrently by the level workers. Once the call returns, the complete trace is
/// available for inspection.
/// </para>
/// <para>
/// Storing child hash snapshots for every internal node incurs additional allocation proportional to the number of
/// internal nodes and the size of the hash output. This overhead is acceptable for diagnostic use but should not be
/// enabled in production paths.
/// </para>
/// <para>
/// The <see cref="Validate" /> method independently re-computes each internal node's hash from its recorded children
/// and confirms the result matches the value stored in the node. Leaf hashes are not re-validated against the original
/// input bytes, as raw blocks are not retained.
/// </para>
/// </remarks>
/// <example>
/// <code>
///<![CDATA[
/// var diagnostics = new MerkleTreeDiagnostics();
/// using var hasher = new ParallelMerkleTreeHash(() => SHA256.Create(), blockSize: 64, fanOut: 2);
/// byte[] root = hasher.ComputeHash(data, diagnostics);
///
/// diagnostics.WriteTo(Console.Out);
///
/// bool valid = diagnostics.Validate(() => SHA256.Create(), out var errors);
///]]>
/// </code>
/// </example>
public sealed class MerkleTreeDiagnostics
{
    /// <summary>The thread-safe collection of recorded leaf and internal nodes captured during the tree computation.</summary>
    private readonly ConcurrentBag<MerkleTreeDiagnosticNode> _nodes = new();

    // -----------------------------------------------------------------------------------------
    // Internal recording — called by ParallelMerkleTreeHash during computation
    // -----------------------------------------------------------------------------------------

    /// <summary>
    /// Records a leaf node produced from an input block.
    /// </summary>
    /// <param name="index">The zero-based leaf index.</param>
    /// <param name="hash">The computed leaf hash bytes.</param>
    internal void RecordLeaf(int index, byte[] hash) =>
        _nodes.Add(new MerkleTreeDiagnosticNode(
            Level: 0,
            Index: index,
            IsLeaf: true,
            Hash: (byte[])hash.Clone(),
            ChildHashes: Array.Empty<byte[]>()));

    /// <summary>
    /// Records an internal node produced by combining child hashes at the given source level.
    /// </summary>
    /// <param name="level">The level of the parent node (source level + 1).</param>
    /// <param name="index">The zero-based index of the parent node within its level.</param>
    /// <param name="childHashes">Snapshots of the child hash values used as input.</param>
    /// <param name="hash">The resulting parent hash.</param>
    internal void RecordInternal(int level, int index, byte[][] childHashes, byte[] hash) =>
        _nodes.Add(new MerkleTreeDiagnosticNode(
            Level: level,
            Index: index,
            IsLeaf: false,
            Hash: (byte[])hash.Clone(),
            ChildHashes: childHashes));

    // -----------------------------------------------------------------------------------------
    // Inspection
    // -----------------------------------------------------------------------------------------

    /// <summary>
    /// Returns all recorded nodes sorted by level ascending, then by index ascending.
    /// </summary>
    /// <returns>
    /// A list of all <see cref="MerkleTreeDiagnosticNode" /> instances recorded during the computation.
    /// </returns>
    public IReadOnlyList<MerkleTreeDiagnosticNode> GetAllNodes() =>
        _nodes.OrderBy(n => n.Level).ThenBy(n => n.Index).ToList();

    /// <summary>
    /// Gets the number of distinct levels recorded in the tree, including the leaf level.
    /// </summary>
    /// <returns>The total number of levels, or zero if no nodes have been recorded.</returns>
    public int GetLevelCount() =>
        _nodes.Count == 0 ? 0 : _nodes.Max(n => n.Level) + 1;

    /// <summary>
    /// Returns all nodes at the specified <paramref name="level" />, sorted by index ascending.
    /// </summary>
    /// <param name="level">The zero-based tree level to retrieve. Level 0 is the leaf level.</param>
    /// <returns>A list of nodes at <paramref name="level" />, or an empty list if none exist.</returns>
    public IReadOnlyList<MerkleTreeDiagnosticNode> GetLevel(int level) =>
        _nodes.Where(n => n.Level == level).OrderBy(n => n.Index).ToList();

    /// <summary>
    /// Gets the root node — the sole node at the highest recorded level — or <see langword="null" /> if no nodes have
    /// been recorded.
    /// </summary>
    public MerkleTreeDiagnosticNode? Root =>
        _nodes.IsEmpty ? null : _nodes.MaxBy(n => n.Level);

    // -----------------------------------------------------------------------------------------
    // Validation
    // -----------------------------------------------------------------------------------------

    /// <summary>
    /// Independently re-computes every internal node's hash from its recorded child hashes and verifies the result
    /// matches the value stored in the diagnostic node.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The same <paramref name="algorithmFactory" /> used for the original computation should be supplied so that the
    /// hash algorithm, output size, and combination strategy all match.
    /// </para>
    /// <para>
    /// Leaf nodes are not validated against original input bytes, as raw blocks are not retained.
    /// </para>
    /// </remarks>
    /// <param name="algorithmFactory">
    /// Factory returning a fresh <see cref="HashAlgorithm" /> per call. Must not be <see langword="null" />.
    /// </param>
    /// <param name="errors">
    /// On return, contains one entry per validation failure describing the node and the hash mismatch. Empty when the
    /// method returns <see langword="true" />.
    /// </param>
    /// <returns>
    /// <see langword="true" /> if all internal node hashes are consistent with their recorded children;
    /// <see langword="false" /> if any mismatch is found.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="algorithmFactory" /> is <see langword="null" />.
    /// </exception>
    public bool Validate(Func<HashAlgorithm> algorithmFactory, out IReadOnlyList<string> errors)
    {
        ArgumentNullException.ThrowIfNull(algorithmFactory);

        var issues = new List<string>();

        foreach (MerkleTreeDiagnosticNode? node in _nodes.Where(n => !n.IsLeaf).OrderBy(n => n.Level).ThenBy(n => n.Index))
        {
            byte[] recomputed = CombineHashes(node.ChildHashes, algorithmFactory);
            if (!recomputed.SequenceEqual(node.Hash))
            {
                issues.Add(
                    $"[{node.Level}:{node.Index}] hash mismatch — " +
                    $"expected {ToHex(node.Hash)}, recomputed {ToHex(recomputed)}");
            }
        }

        errors = issues;
        return issues.Count == 0;
    }

    // -----------------------------------------------------------------------------------------
    // Display
    // -----------------------------------------------------------------------------------------

    /// <summary>
    /// Writes a formatted representation of the recorded tree to <paramref name="writer" />, including per-node hashes,
    /// child references, and a validation summary.
    /// </summary>
    /// <remarks>
    /// The output is for diagnostic use only. Child references are resolved by matching recorded hash values, so an
    /// ambiguous match may occur if two nodes share the same hash.
    /// </remarks>
    /// <param name="writer">The destination writer. Must not be <see langword="null" />.</param>
    /// <param name="algorithmFactory">
    /// Optional factory used to run <see cref="Validate" /> and append a validation summary. Pass
    /// <see langword="null" /> to skip validation output.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="writer" /> is <see langword="null" />.</exception>
    public void WriteTo(TextWriter writer, Func<HashAlgorithm>? algorithmFactory = null)
    {
        ArgumentNullException.ThrowIfNull(writer);

        IReadOnlyList<MerkleTreeDiagnosticNode> allNodes = GetAllNodes();
        int levelCount = GetLevelCount();
        MerkleTreeDiagnosticNode? root = Root;

        // Build a reverse lookup from hex-encoded hash to node, used to annotate child
        // references in the tree display. Duplicate hashes resolve to the lowest-level match.
        var byHash = allNodes
            .GroupBy(n => ToHex(n.Hash))
            .ToDictionary(g => g.Key, g => g.OrderBy(n => n.Level).First(), StringComparer.OrdinalIgnoreCase);

        const string heavy = "════════════════════════════════════════════════════════════════════";
        const string light = "────────────────────────────────────────────────────────────────────";

        writer.WriteLine();
        writer.WriteLine(heavy);
        writer.WriteLine($"  Merkle Tree Diagnostic");

        if (root is not null)
        {
            writer.WriteLine($"  Levels: {levelCount}    Nodes: {allNodes.Count}    Root: {ToHex(root.Hash)}");
        }
        else
        {
            writer.WriteLine("  (no nodes recorded)");
        }

        writer.WriteLine(heavy);

        for (int level = 0; level < levelCount; level++)
        {
            IReadOnlyList<MerkleTreeDiagnosticNode> levelNodes = GetLevel(level);
            bool isRoot = level == levelCount - 1;
            string label = level == 0 ? "leaf" : "internal";
            string rootTag = isRoot ? "  ★  root" : string.Empty;

            writer.WriteLine();
            writer.WriteLine($"  Level {level}  —  {levelNodes.Count} {label} node{(levelNodes.Count == 1 ? string.Empty : "s")}{rootTag}");
            writer.WriteLine($"  {light}");

            foreach (MerkleTreeDiagnosticNode node in levelNodes)
            {
                if (node.ChildHashes.Count == 0)
                {
                    writer.WriteLine($"    [{node.Level}:{node.Index}]  {ToHex(node.Hash)}");
                }
                else
                {
                    IEnumerable<string> childRefs = node.ChildHashes
                        .Select(ch =>
                        {
                            string hex = ToHex(ch);
                            return byHash.TryGetValue(hex, out MerkleTreeDiagnosticNode? childNode)
                                ? $"[{childNode.Level}:{childNode.Index}] {hex}"
                                : hex;
                        });

                    writer.WriteLine($"    [{node.Level}:{node.Index}]  {ToHex(node.Hash)}  ←  {string.Join("  ·  ", childRefs)}");
                }
            }
        }

        writer.WriteLine();

        // Optional validation summary.
        if (algorithmFactory is not null)
        {
            bool valid = Validate(algorithmFactory, out IReadOnlyList<string>? validationErrors);
            int internalCount = allNodes.Count(n => !n.IsLeaf);

            writer.WriteLine(heavy);

            if (valid)
            {
                writer.WriteLine($"  Validation: PASS  ({internalCount} internal node{(internalCount == 1 ? string.Empty : "s")} verified)");
            }
            else
            {
                writer.WriteLine($"  Validation: FAIL  ({validationErrors.Count} error{(validationErrors.Count == 1 ? string.Empty : "s")})");
                foreach (string error in validationErrors)
                    writer.WriteLine($"    ✗  {error}");
            }

            writer.WriteLine(heavy);
        }

        writer.WriteLine();
    }

    // -----------------------------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------------------------

    /// <summary>
    /// Combines a list of child hashes using the same <see cref="HashAlgorithm.TransformBlock" /> strategy employed by
    /// <see cref="ParallelMerkleTreeHash" />, and returns the resulting hash.
    /// </summary>
    /// <param name="hashes">The ordered child hashes to concatenate and re-hash.</param>
    /// <param name="factory">A factory producing a fresh <see cref="HashAlgorithm" /> for this combination.</param>
    /// <returns>The combined parent hash.</returns>
    private static byte[] CombineHashes(IReadOnlyList<byte[]> hashes, Func<HashAlgorithm> factory)
    {
        using HashAlgorithm hasher = factory();

        // Internal-node domain separation: recompute H(0x01 || child₀ || … ) to match both hasher implementations.
        byte[] prefix = [MerkleTreeFormat.InternalNodePrefix];
        hasher.TransformBlock(prefix, 0, prefix.Length, null, 0);
        for (int i = 0; i < hashes.Count - 1; i++)
            hasher.TransformBlock(hashes[i], 0, hashes[i].Length, null, 0);
        hasher.TransformFinalBlock(hashes[^1], 0, hashes[^1].Length);
        return hasher.Hash!;
    }

    /// <summary>
    /// Returns the upper-case hexadecimal representation of <paramref name="bytes" /> for diagnostic display.
    /// </summary>
    /// <param name="bytes">The bytes to format.</param>
    /// <returns>A hex-encoded string.</returns>
    private static string ToHex(byte[] bytes) => Convert.ToHexString(bytes);
}
