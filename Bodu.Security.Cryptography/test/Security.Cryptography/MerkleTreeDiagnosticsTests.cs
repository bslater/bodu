// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MerkleTreeDiagnosticsTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Direct unit tests for <see cref="MerkleTreeDiagnostics" /> and
/// <see cref="MerkleTreeDiagnosticNode" />. The diagnostic recorder is exercised indirectly
/// via <c>ParallelMerkleTreeHashTests.Diagnostics.cs</c>, but its inspection, validation, and
/// formatting APIs deserve targeted coverage that does not depend on the parallel pipeline
/// being correct.
/// </summary>
[TestClass]
public sealed class MerkleTreeDiagnosticsTests
{
    /// <summary>
    /// Verifies that a freshly-constructed <see cref="MerkleTreeDiagnostics" /> reports an empty
    /// node set, a zero level count, and a <see langword="null" /> root.
    /// </summary>
    [TestMethod]
    public void Empty_ShouldReportNoNodesNoLevelsAndNullRoot()
    {
        var diagnostics = new MerkleTreeDiagnostics();

        Assert.AreEqual(0, diagnostics.GetAllNodes().Count);
        Assert.AreEqual(0, diagnostics.GetLevelCount());
        Assert.IsNull(diagnostics.Root);
        Assert.AreEqual(0, diagnostics.GetLevel(0).Count);
    }

    /// <summary>
    /// Verifies that recording a single leaf node makes it the only node, the only entry at level 0,
    /// the root, and that <see cref="MerkleTreeDiagnostics.GetLevelCount" /> returns 1.
    /// </summary>
    [TestMethod]
    public void RecordLeaf_ShouldMakeLeafTheRoot()
    {
        var diagnostics = new MerkleTreeDiagnostics();
        var leafHash = new byte[] { 0xAA, 0xBB, 0xCC };

        diagnostics.RecordLeaf(index: 0, hash: leafHash);

        Assert.AreEqual(1, diagnostics.GetLevelCount());
        Assert.AreEqual(1, diagnostics.GetAllNodes().Count);
        Assert.IsNotNull(diagnostics.Root);
        Assert.AreEqual(0, diagnostics.Root!.Level);
        Assert.AreEqual(0, diagnostics.Root.Index);
        Assert.IsTrue(diagnostics.Root.IsLeaf);
        CollectionAssert.AreEqual(leafHash, diagnostics.Root.Hash);
    }

    /// <summary>
    /// Verifies that <see cref="MerkleTreeDiagnostics.RecordLeaf" /> stores a defensive copy of the
    /// supplied hash so that subsequent caller mutation cannot corrupt the recorded snapshot.
    /// </summary>
    [TestMethod]
    public void RecordLeaf_ShouldStoreDefensiveCopyOfHash()
    {
        var diagnostics = new MerkleTreeDiagnostics();
        var leafHash = new byte[] { 0x01, 0x02, 0x03 };

        diagnostics.RecordLeaf(index: 0, hash: leafHash);
        leafHash[0] = 0xFF;

        Assert.AreEqual(0x01, diagnostics.Root!.Hash[0],
            "RecordLeaf must take a defensive copy so caller mutation cannot affect the recorded node.");
    }

    /// <summary>
    /// Verifies that <see cref="MerkleTreeDiagnostics.GetAllNodes" /> returns nodes sorted by level
    /// ascending, then by index ascending — independent of the order in which they were recorded.
    /// </summary>
    [TestMethod]
    public void GetAllNodes_ShouldReturnNodesSortedByLevelThenIndex()
    {
        var diagnostics = new MerkleTreeDiagnostics();

        diagnostics.RecordInternal(level: 1, index: 1, childHashes: [[0x01], [0x02]], hash: [0xAA]);
        diagnostics.RecordLeaf(index: 2, hash: [0x33]);
        diagnostics.RecordInternal(level: 1, index: 0, childHashes: [[0x03], [0x04]], hash: [0xBB]);
        diagnostics.RecordLeaf(index: 0, hash: [0x11]);
        diagnostics.RecordLeaf(index: 1, hash: [0x22]);

        IReadOnlyList<MerkleTreeDiagnosticNode> nodes = diagnostics.GetAllNodes();
        var ordered = nodes.Select(n => (n.Level, n.Index)).ToList();

        var expected = new List<(int Level, int Index)>
        {
            (0, 0), (0, 1), (0, 2),
            (1, 0), (1, 1),
        };

        CollectionAssert.AreEqual(expected, ordered);
    }

    /// <summary>
    /// Verifies that <see cref="MerkleTreeDiagnostics.GetLevel" /> returns only the nodes at the
    /// requested level, sorted by index ascending, regardless of insertion order.
    /// </summary>
    [TestMethod]
    public void GetLevel_ShouldReturnOnlyMatchingLevelSortedByIndex()
    {
        var diagnostics = new MerkleTreeDiagnostics();

        diagnostics.RecordLeaf(index: 1, hash: [0x11]);
        diagnostics.RecordInternal(level: 1, index: 0, childHashes: [[0x01]], hash: [0xAA]);
        diagnostics.RecordLeaf(index: 0, hash: [0x10]);

        IReadOnlyList<MerkleTreeDiagnosticNode> level0 = diagnostics.GetLevel(0);
        IReadOnlyList<MerkleTreeDiagnosticNode> level1 = diagnostics.GetLevel(1);
        IReadOnlyList<MerkleTreeDiagnosticNode> level2 = diagnostics.GetLevel(2);

        Assert.AreEqual(2, level0.Count);
        Assert.AreEqual(0, level0[0].Index);
        Assert.AreEqual(1, level0[1].Index);
        Assert.AreEqual(1, level1.Count);
        Assert.AreEqual(0, level2.Count, "Levels above the highest recorded should be empty.");
    }

    /// <summary>
    /// Verifies that <see cref="MerkleTreeDiagnostics.Root" /> returns the unique node at the
    /// highest recorded level.
    /// </summary>
    [TestMethod]
    public void Root_ShouldReturnHighestLevelNode()
    {
        var diagnostics = new MerkleTreeDiagnostics();

        diagnostics.RecordLeaf(index: 0, hash: [0x10]);
        diagnostics.RecordLeaf(index: 1, hash: [0x11]);
        diagnostics.RecordInternal(level: 1, index: 0, childHashes: [[0x10], [0x11]], hash: [0xAA]);
        diagnostics.RecordInternal(level: 2, index: 0, childHashes: [[0xAA]], hash: [0xFF]);

        Assert.AreEqual(2, diagnostics.Root!.Level);
        Assert.AreEqual(0xFF, diagnostics.Root.Hash[0]);
    }

    /// <summary>
    /// Verifies that <see cref="MerkleTreeDiagnostics.Validate" /> succeeds when every internal
    /// node's stored hash matches the SHA-256 of the concatenation of its child hashes.
    /// </summary>
    [TestMethod]
    public void Validate_WhenInternalHashesMatchChildren_ShouldReturnTrue()
    {
        var diagnostics = new MerkleTreeDiagnostics();

        var leaf0 = new byte[] { 0x01 };
        var leaf1 = new byte[] { 0x02 };
        var expectedParent = ComputeSha256(Concat(leaf0, leaf1));

        diagnostics.RecordLeaf(0, leaf0);
        diagnostics.RecordLeaf(1, leaf1);
        diagnostics.RecordInternal(level: 1, index: 0, childHashes: [leaf0, leaf1], hash: expectedParent);

        var ok = diagnostics.Validate(SHA256.Create, out IReadOnlyList<string>? errors);

        Assert.IsTrue(ok, $"Validate should pass — got errors: {string.Join(", ", errors)}");
        Assert.AreEqual(0, errors.Count);
    }

    /// <summary>
    /// Verifies that <see cref="MerkleTreeDiagnostics.Validate" /> reports a mismatch when an
    /// internal node's stored hash does not match the SHA-256 of its child hashes.
    /// </summary>
    [TestMethod]
    public void Validate_WhenInternalHashIsCorrupted_ShouldReportMismatch()
    {
        var diagnostics = new MerkleTreeDiagnostics();

        var leaf0 = new byte[] { 0x01 };
        var leaf1 = new byte[] { 0x02 };
        var corruptedParent = new byte[] { 0xFF, 0xFF, 0xFF };

        diagnostics.RecordLeaf(0, leaf0);
        diagnostics.RecordLeaf(1, leaf1);
        diagnostics.RecordInternal(level: 1, index: 0, childHashes: [leaf0, leaf1], hash: corruptedParent);

        var ok = diagnostics.Validate(SHA256.Create, out IReadOnlyList<string>? errors);

        Assert.IsFalse(ok);
        Assert.AreEqual(1, errors.Count);
        Assert.IsTrue(errors[0].Contains("[1:0]", StringComparison.Ordinal),
            $"Validation error should reference the offending node by [level:index]; got '{errors[0]}'.");
    }

    /// <summary>
    /// Verifies that <see cref="MerkleTreeDiagnostics.Validate" /> succeeds trivially when no
    /// internal nodes have been recorded — leaves are not re-validated against input bytes.
    /// </summary>
    [TestMethod]
    public void Validate_WhenOnlyLeavesRecorded_ShouldReturnTrue()
    {
        var diagnostics = new MerkleTreeDiagnostics();
        diagnostics.RecordLeaf(0, [0x01]);
        diagnostics.RecordLeaf(1, [0x02]);

        var ok = diagnostics.Validate(SHA256.Create, out IReadOnlyList<string>? errors);

        Assert.IsTrue(ok);
        Assert.AreEqual(0, errors.Count);
    }

    /// <summary>
    /// Verifies that <see cref="MerkleTreeDiagnostics.Validate" /> rejects a <see langword="null" />
    /// algorithm factory.
    /// </summary>
    [TestMethod]
    public void Validate_WhenAlgorithmFactoryIsNull_ShouldThrowExactly()
    {
        var diagnostics = new MerkleTreeDiagnostics();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = diagnostics.Validate(null!, out _);
        });
    }

    /// <summary>
    /// Verifies that <see cref="MerkleTreeDiagnostics.WriteTo" /> rejects a <see langword="null" />
    /// writer.
    /// </summary>
    [TestMethod]
    public void WriteTo_WhenWriterIsNull_ShouldThrowExactly()
    {
        var diagnostics = new MerkleTreeDiagnostics();

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            diagnostics.WriteTo(null!);
        });
    }

    /// <summary>
    /// Verifies that <see cref="MerkleTreeDiagnostics.WriteTo" /> on an empty instance emits the
    /// documented "(no nodes recorded)" placeholder rather than throwing.
    /// </summary>
    [TestMethod]
    public void WriteTo_WhenEmpty_ShouldEmitPlaceholderAndNotThrow()
    {
        var diagnostics = new MerkleTreeDiagnostics();
        var writer = new StringWriter();

        diagnostics.WriteTo(writer);

        var output = writer.ToString();
        Assert.IsTrue(output.Contains("(no nodes recorded)", StringComparison.Ordinal),
            $"Expected the 'no nodes recorded' placeholder; got: {output}");
    }

    /// <summary>
    /// Verifies that <see cref="MerkleTreeDiagnostics.WriteTo" /> with a non-null algorithm factory
    /// appends a "Validation: PASS" line when every recorded internal node hashes correctly.
    /// </summary>
    [TestMethod]
    public void WriteTo_WhenValidationPasses_ShouldAppendPassLine()
    {
        var diagnostics = new MerkleTreeDiagnostics();
        var leaf0 = new byte[] { 0x01 };
        var leaf1 = new byte[] { 0x02 };

        diagnostics.RecordLeaf(0, leaf0);
        diagnostics.RecordLeaf(1, leaf1);
        diagnostics.RecordInternal(level: 1, index: 0, childHashes: [leaf0, leaf1],
            hash: ComputeSha256(Concat(leaf0, leaf1)));

        var writer = new StringWriter();
        diagnostics.WriteTo(writer, SHA256.Create);

        var output = writer.ToString();
        Assert.IsTrue(output.Contains("Validation: PASS", StringComparison.Ordinal),
            $"Expected 'Validation: PASS' summary; got: {output}");
    }

    /// <summary>
    /// Verifies that <see cref="MerkleTreeDiagnostics.WriteTo" /> with a non-null algorithm factory
    /// appends a "Validation: FAIL" line and the offending node identifier when an internal hash
    /// is inconsistent.
    /// </summary>
    [TestMethod]
    public void WriteTo_WhenValidationFails_ShouldAppendFailLineAndNodeReference()
    {
        var diagnostics = new MerkleTreeDiagnostics();

        diagnostics.RecordLeaf(0, [0x01]);
        diagnostics.RecordLeaf(1, [0x02]);
        diagnostics.RecordInternal(level: 1, index: 0, childHashes: [[0x01], [0x02]],
            hash: [0xFF, 0xFF, 0xFF]);

        var writer = new StringWriter();
        diagnostics.WriteTo(writer, SHA256.Create);

        var output = writer.ToString();
        Assert.IsTrue(output.Contains("Validation: FAIL", StringComparison.Ordinal),
            $"Expected 'Validation: FAIL' summary; got: {output}");
        Assert.IsTrue(output.Contains("[1:0]", StringComparison.Ordinal),
            $"Expected the failing node reference '[1:0]' in the validation summary; got: {output}");
    }

    /// <summary>
    /// Verifies <see cref="MerkleTreeDiagnosticNode" /> record equality: two records with the same
    /// fields are equal even though <see cref="byte" />[] uses reference equality by default. This
    /// is the documented record-with-init behaviour for value-equality.
    /// </summary>
    [TestMethod]
    public void DiagnosticNode_RecordsWithIdenticalFields_ShouldNotBeReferenceEqualByByteArray()
    {
        // Records with byte[] fields use reference equality on the array, so two records with
        // different array instances containing identical bytes are NOT equal — pin this contract
        // so consumers don't assume value-equality.
        var hashA = new byte[] { 0x01 };
        var hashB = new byte[] { 0x01 };

        var nodeA = new MerkleTreeDiagnosticNode(
            Level: 0, Index: 0, IsLeaf: true, Hash: hashA, ChildHashes: Array.Empty<byte[]>());
        var nodeB = new MerkleTreeDiagnosticNode(
            Level: 0, Index: 0, IsLeaf: true, Hash: hashB, ChildHashes: Array.Empty<byte[]>());

        Assert.AreNotEqual(nodeA, nodeB,
            "Records with byte[] members use reference equality on the array; identical-content but distinct instances must compare unequal.");
    }

    /// <summary>
    /// Verifies <see cref="MerkleTreeDiagnosticNode" /> record equality with the same array reference.
    /// </summary>
    [TestMethod]
    public void DiagnosticNode_RecordsSharingByteArrayReferences_ShouldBeEqual()
    {
        var hash = new byte[] { 0x01 };
        IReadOnlyList<byte[]> children = Array.Empty<byte[]>();

        var nodeA = new MerkleTreeDiagnosticNode(0, 0, true, hash, children);
        var nodeB = new MerkleTreeDiagnosticNode(0, 0, true, hash, children);

        Assert.AreEqual(nodeA, nodeB);
    }

    private static byte[] ComputeSha256(byte[] input)
    {
        using var sha = SHA256.Create();
        return sha.ComputeHash(input);
    }

    private static byte[] Concat(byte[] a, byte[] b)
    {
        var result = new byte[a.Length + b.Length];
        Buffer.BlockCopy(a, 0, result, 0, a.Length);
        Buffer.BlockCopy(b, 0, result, a.Length, b.Length);
        return result;
    }
}
