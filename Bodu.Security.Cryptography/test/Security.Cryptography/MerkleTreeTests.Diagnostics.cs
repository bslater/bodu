// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MerkleTreeTests.Diagnostics.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using static Bodu.Security.Cryptography.MerkleTestData;

namespace Bodu.Security.Cryptography;

/// <summary>
/// The recorder's interplay with the tree: what one computation records, per-call isolation, and validation over
/// uneven shapes.
/// </summary>
public partial class MerkleTreeTests
{
    /// <summary>
    /// Verifies that two full blocks record two leaves and one internal node whose children are those leaves.
    /// </summary>
    [TestMethod]
    public void Diagnostics_WhenTwoFullBlocksWithFanOutTwo_ShouldRecordTwoLeavesAndOneInternal()
    {
        var diagnostics = new MerkleTreeDiagnostics();
        _ = CreateAdditiveTree().ComputeRootOfBlocks(MakeData(8), 4, diagnostics);

        IReadOnlyList<MerkleTreeDiagnosticNode> leaves = diagnostics.GetLevel(0);
        var internals = diagnostics.GetAllNodes().Where(n => !n.IsLeaf).ToList();

        Assert.HasCount(2, leaves);
        Assert.HasCount(1, internals);
        Assert.IsTrue(leaves.All(n => n.IsLeaf && n.ChildHashes.Count == 0));
        CollectionAssert.AreEqual(leaves[0].Hash, internals[0].ChildHashes[0]);
        CollectionAssert.AreEqual(leaves[1].Hash, internals[0].ChildHashes[1]);
        Assert.AreEqual(2, diagnostics.GetLevelCount());
    }

    /// <summary>
    /// Verifies that each computation records only its own nodes into the recorder it was given, and records nothing
    /// into one it was not.
    /// </summary>
    [TestMethod]
    public void Diagnostics_WhenFreshInstancePerCall_ShouldIsolateEachComputationsTrace()
    {
        MerkleTree tree = CreateAdditiveTree();
        byte[] data = MakeData(8);
        var detached = new MerkleTreeDiagnostics();
        var first = new MerkleTreeDiagnostics();
        var second = new MerkleTreeDiagnostics();

        _ = tree.ComputeRootOfBlocks(data, 4);
        _ = tree.ComputeRootOfBlocks(data, 4, first);
        _ = tree.ComputeRootOfBlocks(data, 4, second);

        Assert.IsEmpty(detached.GetAllNodes());
        Assert.HasCount(3, first.GetAllNodes());
        Assert.HasCount(3, second.GetAllNodes(), "call state must not accumulate");
    }

    /// <summary>
    /// Verifies that a trace over an uneven shape validates, on the sequential and the parallel instance, and that a
    /// promoted leaf is recorded once.
    /// </summary>
    /// <param name="maxDegreeOfParallelism">The degree of parallelism.</param>
    [TestMethod]
    [DataRow(1)]
    [DataRow(-1)]
    public void Diagnostics_WhenTreeHasUnevenRemainder_ShouldValidateAndRecordThePromotedLeafOnce(int maxDegreeOfParallelism)
    {
        var diagnostics = new MerkleTreeDiagnostics();
        _ = CreateAdditiveTree(2, maxDegreeOfParallelism).ComputeRootOfBlocks(MakeData(12), 4, diagnostics);

        Assert.IsTrue(diagnostics.Validate(Factory, out IReadOnlyList<string> errors), string.Join("; ", errors));
        Assert.HasCount(3, diagnostics.GetLevel(0));
        Assert.HasCount(1, diagnostics.GetLevel(1), "one pair; the third leaf is promoted, not recorded again");
        Assert.HasCount(1, diagnostics.GetLevel(2));
    }
}
