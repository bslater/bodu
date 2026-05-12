// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ParallelMerkleTreeHashTests.Diagnostics.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using static Bodu.Security.Cryptography.MerkleTestData;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Tests for the <see cref="MerkleTreeDiagnostics" /> capture and reporting facility — a
/// feature exposed only by <see cref="ParallelMerkleTreeHash" /> that records every node
/// produced during a computation so callers can inspect tree shape, validate hashes, and
/// render a human-readable trace.
/// </summary>
/// <remarks>
/// <para>
/// Scenarios covered here fall into seven groups: node-count and level-structure recording,
/// leaf vs internal node classification, index ordering, root exposure, child-hash accuracy,
/// structural validation (<c>Validate</c>), and textual output (<c>WriteTo</c>). Tests for
/// per-call isolation and computation-result invariance under diagnostic attachment live in
/// <c>ParallelMerkleTreeHashTests.ComputeHash.cs</c> and <c>ParallelMerkleTreeHashTests.Parity.cs</c>
/// respectively.
/// </para>
/// </remarks>
public partial class ParallelMerkleTreeHashTests
{
    // ═══════════════════════════════════════════════════════════════════════════════════════════
    // Recording — node counts and level structure
    // ═══════════════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Verifies that after a computation with two exact blocks and fanOut=2, exactly two leaf
    /// nodes and one internal node are recorded.
    /// </summary>
    [TestMethod]
    public void Diagnostics_WhenTwoFullBlocksWithFanOutTwo_ShouldRecordTwoLeavesAndOneInternal()
    {
        var diagnostics = new MerkleTreeDiagnostics();
        using var hasher = new ParallelMerkleTreeHash(Factory, blockSize: 4, fanOut: 2);
        hasher.ComputeHash(MakeData(8), diagnostics);

        IReadOnlyList<MerkleTreeDiagnosticNode> leaves = diagnostics.GetLevel(0);
        var internals = diagnostics.GetAllNodes().Where(n => !n.IsLeaf).ToList();

        Assert.AreEqual(2, leaves.Count, "Expected two leaf nodes.");
        Assert.AreEqual(1, internals.Count, "Expected one internal node.");
    }

    /// <summary>
    /// Verifies that diagnostic leaf nodes are marked as leaves and carry no child hashes.
    /// </summary>
    [TestMethod]
    public void Diagnostics_WhenLeafNodesRecorded_ShouldHaveIsLeafTrueAndNoChildren()
    {
        var diagnostics = new MerkleTreeDiagnostics();
        using var hasher = new ParallelMerkleTreeHash(Factory, blockSize: 4, fanOut: 2);
        hasher.ComputeHash(MakeData(8), diagnostics);

        foreach (MerkleTreeDiagnosticNode leaf in diagnostics.GetLevel(0))
        {
            Assert.IsTrue(leaf.IsLeaf, $"Node [{leaf.Level}:{leaf.Index}] is not marked as a leaf.");
            Assert.AreEqual(0, leaf.ChildHashes.Count,
                $"Leaf [{leaf.Level}:{leaf.Index}] must have no child hashes.");
        }
    }

    /// <summary>
    /// Verifies that internal nodes are not marked as leaves and carry the correct number of
    /// child hashes matching the fan-out.
    /// </summary>
    [TestMethod]
    public void Diagnostics_WhenInternalNodesRecorded_ShouldHaveIsLeafFalseAndCorrectChildCount()
    {
        var diagnostics = new MerkleTreeDiagnostics();
        using var hasher = new ParallelMerkleTreeHash(Factory, blockSize: 4, fanOut: 2);
        hasher.ComputeHash(MakeData(8), diagnostics); // 2 leaves → 1 internal with 2 children

        MerkleTreeDiagnosticNode node = diagnostics.GetAllNodes().Single(n => !n.IsLeaf);
        Assert.IsFalse(node.IsLeaf);
        Assert.AreEqual(2, node.ChildHashes.Count);
    }

    /// <summary>
    /// Verifies that leaf node indices are contiguous starting from zero.
    /// </summary>
    [TestMethod]
    public void Diagnostics_WhenLeafNodesRecorded_ShouldHaveSequentialIndicesFromZero()
    {
        const int blockSize = 4;
        const int dataLength = 16; // 4 full blocks
        var diagnostics = new MerkleTreeDiagnostics();
        using var hasher = new ParallelMerkleTreeHash(Factory, blockSize, fanOut: 2);
        hasher.ComputeHash(MakeData(dataLength), diagnostics);

        IReadOnlyList<MerkleTreeDiagnosticNode> leaves = diagnostics.GetLevel(0);
        for (var i = 0; i < leaves.Count; i++)
            Assert.AreEqual(i, leaves[i].Index,
                $"Leaf at position {i} has unexpected index {leaves[i].Index}.");
    }

    /// <summary>
    /// Verifies that the <c>Root</c> property returns the node at the highest recorded level
    /// and that its hash matches the value returned by <c>ComputeHash</c>.
    /// </summary>
    [TestMethod]
    public void Diagnostics_WhenComputationComplete_ShouldExposeRootNodeAtHighestLevel()
    {
        var diagnostics = new MerkleTreeDiagnostics();
        using var hasher = new ParallelMerkleTreeHash(Factory, blockSize: 4, fanOut: 2);
        var root = hasher.ComputeHash(MakeData(8), diagnostics);

        MerkleTreeDiagnosticNode? rootNode = diagnostics.Root;
        Assert.IsNotNull(rootNode);
        CollectionAssert.AreEqual(root, rootNode.Hash,
            "Root node hash does not match the hash returned by ComputeHash.");
    }

    /// <summary>
    /// Verifies that <c>GetLevelCount</c> returns the correct number of levels for a two-level tree.
    /// </summary>
    [TestMethod]
    public void Diagnostics_WhenTwoLevelTreeProduced_ShouldReturnCorrectLevelCount()
    {
        // 8 bytes, blockSize=4, fanOut=2 → 2 leaves at level 0 + 1 internal at level 1.
        var diagnostics = new MerkleTreeDiagnostics();
        using var hasher = new ParallelMerkleTreeHash(Factory, blockSize: 4, fanOut: 2);
        hasher.ComputeHash(MakeData(8), diagnostics);

        Assert.AreEqual(2, diagnostics.GetLevelCount());
    }

    /// <summary>
    /// Verifies that <c>GetLevel</c> returns an empty list for a level that does not exist.
    /// </summary>
    [TestMethod]
    public void Diagnostics_WhenLevelDoesNotExist_ShouldReturnEmptyList()
    {
        var diagnostics = new MerkleTreeDiagnostics();
        using var hasher = new ParallelMerkleTreeHash(Factory, blockSize: 4, fanOut: 2);
        hasher.ComputeHash(MakeData(8), diagnostics);

        Assert.AreEqual(0, diagnostics.GetLevel(99).Count);
    }

    /// <summary>
    /// Verifies that a diagnostics instance detached from any call records no nodes.
    /// </summary>
    [TestMethod]
    public void Diagnostics_WhenNullPassedPerCall_ShouldNotRecordAnyNodes()
    {
        var detached = new MerkleTreeDiagnostics();
        using var hasher = new ParallelMerkleTreeHash(Factory, blockSize: 4, fanOut: 2);
        hasher.ComputeHash(MakeData(8)); // no diagnostics argument

        Assert.AreEqual(0, detached.GetAllNodes().Count,
            "A detached diagnostics instance must record nothing.");
    }

    // ═══════════════════════════════════════════════════════════════════════════════════════════
    // Child hash accuracy
    // ═══════════════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Verifies that the child hashes stored in an internal diagnostic node exactly match the
    /// hashes stored in the corresponding leaf nodes.
    /// </summary>
    [TestMethod]
    public void Diagnostics_WhenInternalNodeRecorded_ChildHashesShouldMatchLeafHashes()
    {
        var diagnostics = new MerkleTreeDiagnostics();
        using var hasher = new ParallelMerkleTreeHash(Factory, blockSize: 4, fanOut: 2);
        hasher.ComputeHash(MakeData(8), diagnostics);

        var leaves = diagnostics.GetLevel(0).ToList();
        MerkleTreeDiagnosticNode internal_ = diagnostics.GetAllNodes().Single(n => !n.IsLeaf);

        Assert.AreEqual(leaves.Count, internal_.ChildHashes.Count);
        for (var i = 0; i < leaves.Count; i++)
            CollectionAssert.AreEqual(leaves[i].Hash, internal_.ChildHashes[i],
                $"Child hash {i} does not match leaf {i}.");
    }

    // ═══════════════════════════════════════════════════════════════════════════════════════════
    // Validate
    // ═══════════════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Verifies that <c>Validate</c> returns <see langword="true" /> and produces no errors
    /// after a correctly computed tree.
    /// </summary>
    [TestMethod]
    public void Diagnostics_Validate_WhenTreeIsCorrect_ShouldReturnTrueWithNoErrors()
    {
        var diagnostics = new MerkleTreeDiagnostics();
        using var hasher = new ParallelMerkleTreeHash(Factory, blockSize: 4, fanOut: 2);
        hasher.ComputeHash(MakeData(12), diagnostics);

        var valid = diagnostics.Validate(Factory, out IReadOnlyList<string>? errors);

        Assert.IsTrue(valid, "Validate must return true for a correctly computed tree.");
        Assert.AreEqual(0, errors.Count, "Validate must produce no errors for a correctly computed tree.");
    }

    /// <summary>
    /// Verifies that <c>Validate</c> with a <see langword="null" /> factory throws
    /// <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void Diagnostics_Validate_WhenFactoryIsNull_ShouldThrowExactly()
    {
        var diagnostics = new MerkleTreeDiagnostics();
        using var hasher = new ParallelMerkleTreeHash(Factory, blockSize: 4, fanOut: 2);
        hasher.ComputeHash(MakeData(4), diagnostics);

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            diagnostics.Validate(null!, out _);
        });
    }

    /// <summary>
    /// Verifies that <c>Validate</c> returns <see langword="false" /> and reports errors when
    /// an internal node hash is corrupted after the computation.
    /// </summary>
    [TestMethod]
    public void Diagnostics_Validate_WhenNodeHashCorrupted_ShouldReturnFalseWithErrors()
    {
        var diagnostics = new MerkleTreeDiagnostics();
        using var hasher = new ParallelMerkleTreeHash(Factory, blockSize: 4, fanOut: 2);
        hasher.ComputeHash(MakeData(8), diagnostics);

        // Corrupt the internal node's hash by flipping the first byte.
        MerkleTreeDiagnosticNode internalNode = diagnostics.GetAllNodes().First(n => !n.IsLeaf);
        internalNode.Hash[0] ^= 0xFF;

        var valid = diagnostics.Validate(Factory, out IReadOnlyList<string>? errors);
        Assert.IsFalse(valid, "Validate must return false when a node hash is corrupted.");
        Assert.IsTrue(errors.Count > 0, "Validate must report at least one error.");
    }

    /// <summary>
    /// Verifies that <c>Validate</c> passes for a tree with three leaves and an uneven
    /// remainder, confirming that single-child internal nodes are correctly handled.
    /// </summary>
    [TestMethod]
    public void Diagnostics_Validate_WhenTreeHasUnevenRemainder_ShouldPassValidation()
    {
        var diagnostics = new MerkleTreeDiagnostics();
        using var hasher = new ParallelMerkleTreeHash(Factory, blockSize: 4, fanOut: 2);
        hasher.ComputeHash(MakeData(12), diagnostics); // 3 leaves → remainder of 1

        var valid = diagnostics.Validate(Factory, out IReadOnlyList<string>? errors);
        Assert.IsTrue(valid, string.Join("; ", errors));
    }

    // ═══════════════════════════════════════════════════════════════════════════════════════════
    // WriteTo
    // ═══════════════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Verifies that <c>WriteTo</c> with a <see langword="null" /> writer throws
    /// <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void Diagnostics_WriteTo_WhenWriterIsNull_ShouldThrowExactly()
    {
        var diagnostics = new MerkleTreeDiagnostics();
        using var hasher = new ParallelMerkleTreeHash(Factory, blockSize: 4, fanOut: 2);
        hasher.ComputeHash(MakeData(4), diagnostics);

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            diagnostics.WriteTo(null!);
        });
    }

    /// <summary>
    /// Verifies that <c>WriteTo</c> produces output that references at least one node hash and
    /// the root.
    /// </summary>
    [TestMethod]
    public void Diagnostics_WriteTo_WhenTreeIsComplete_ShouldProduceNonEmptyOutput()
    {
        var diagnostics = new MerkleTreeDiagnostics();
        using var hasher = new ParallelMerkleTreeHash(Factory, blockSize: 4, fanOut: 2);
        var root = hasher.ComputeHash(MakeData(8), diagnostics);

        using var writer = new StringWriter();
        diagnostics.WriteTo(writer);

        var output = writer.ToString();
        Assert.IsTrue(output.Length > 0, "WriteTo should produce non-empty output.");
        StringAssert.Contains(output, Convert.ToHexString(root),
            "WriteTo output should include the root hash.");
    }

    /// <summary>
    /// Verifies that <c>WriteTo</c> with a non-<see langword="null" /> factory appends a
    /// validation summary that confirms the tree is valid.
    /// </summary>
    [TestMethod]
    public void Diagnostics_WriteTo_WhenFactoryProvided_ShouldAppendValidationSummary()
    {
        var diagnostics = new MerkleTreeDiagnostics();
        using var hasher = new ParallelMerkleTreeHash(Factory, blockSize: 4, fanOut: 2);
        hasher.ComputeHash(MakeData(8), diagnostics);

        using var writer = new StringWriter();
        diagnostics.WriteTo(writer, Factory);

        StringAssert.Contains(writer.ToString(), "PASS",
            "WriteTo output should contain a PASS validation summary.");
    }

    // ═══════════════════════════════════════════════════════════════════════════════════════════
    // GetAllNodes ordering
    // ═══════════════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Verifies that <c>GetAllNodes</c> returns nodes in level-ascending, index-ascending order.
    /// </summary>
    [TestMethod]
    public void Diagnostics_GetAllNodes_WhenCalled_ShouldReturnNodesSortedByLevelThenIndex()
    {
        var diagnostics = new MerkleTreeDiagnostics();
        using var hasher = new ParallelMerkleTreeHash(Factory, blockSize: 4, fanOut: 2);
        hasher.ComputeHash(MakeData(16), diagnostics); // 4 leaves → 2 internal → 1 root

        IReadOnlyList<MerkleTreeDiagnosticNode> nodes = diagnostics.GetAllNodes();
        for (var i = 1; i < nodes.Count; i++)
        {
            var ordered = nodes[i].Level > nodes[i - 1].Level ||
                           (nodes[i].Level == nodes[i - 1].Level && nodes[i].Index >= nodes[i - 1].Index);
            Assert.IsTrue(ordered,
                $"Node [{nodes[i].Level}:{nodes[i].Index}] is out of order " +
                $"after [{nodes[i - 1].Level}:{nodes[i - 1].Index}].");
        }
    }

    // ═══════════════════════════════════════════════════════════════════════════════════════════
    // Root / GetLevelCount before computation
    // ═══════════════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Verifies that the <c>Root</c> property on a freshly constructed diagnostics instance
    /// is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Diagnostics_Root_WhenNoNodesRecorded_ShouldBeNull()
    {
        Assert.IsNull(new MerkleTreeDiagnostics().Root);
    }

    /// <summary>
    /// Verifies that <c>GetLevelCount</c> returns zero when no nodes have been recorded.
    /// </summary>
    [TestMethod]
    public void Diagnostics_GetLevelCount_WhenNoNodesRecorded_ShouldReturnZero()
    {
        Assert.AreEqual(0, new MerkleTreeDiagnostics().GetLevelCount());
    }

    // ═══════════════════════════════════════════════════════════════════════════════════════════
    // Per-call isolation — multi-use with diagnostics
    // ═══════════════════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Verifies that supplying a fresh diagnostics instance per call results in each instance
    /// holding only the nodes from its own computation.
    /// </summary>
    [TestMethod]
    public void Diagnostics_WhenFreshInstancePerCall_ShouldIsolateEachComputationsTrace()
    {
        var data = MakeData(8); // 2 leaves + 1 internal = 3 nodes per call

        using var hasher = new ParallelMerkleTreeHash(Factory, blockSize: 4, fanOut: 2);

        var diag1 = new MerkleTreeDiagnostics();
        var diag2 = new MerkleTreeDiagnostics();
        var diag3 = new MerkleTreeDiagnostics();

        hasher.ComputeHash(data, diag1);
        hasher.ComputeHash(data, diag2);
        hasher.ComputeHash(data, diag3);

        Assert.AreEqual(3, diag1.GetAllNodes().Count, "diag1 must hold exactly 3 nodes.");
        Assert.AreEqual(3, diag2.GetAllNodes().Count, "diag2 must hold exactly 3 nodes.");
        Assert.AreEqual(3, diag3.GetAllNodes().Count, "diag3 must hold exactly 3 nodes.");
    }

    /// <summary>
    /// Verifies that each isolated diagnostics instance passes independent structural validation.
    /// </summary>
    [TestMethod]
    public void Diagnostics_WhenFreshInstancePerCall_EachShouldPassValidation()
    {
        var data = MakeData(12);

        using var hasher = new ParallelMerkleTreeHash(Factory, blockSize: 4, fanOut: 2);

        for (var i = 0; i < 3; i++)
        {
            var diag = new MerkleTreeDiagnostics();
            hasher.ComputeHash(data, diag);

            var valid = diag.Validate(Factory, out IReadOnlyList<string>? errors);
            Assert.IsTrue(valid, $"Call {i + 1} diagnostics failed validation: {string.Join("; ", errors)}");
        }
    }
}
