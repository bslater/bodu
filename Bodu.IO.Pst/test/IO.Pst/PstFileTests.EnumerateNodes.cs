// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstFileTests.EnumerateNodes.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Kat;

namespace Bodu.IO.Pst;

public partial class PstFileTests
{
    /// <summary>
    /// Verifies that the node census of each Unicode fixture — total, folder, and message node counts — matches the
    /// counts pinned during the P0 validation of the corpus.
    /// </summary>
    /// <param name="kat">The expected census row.</param>
    [TestMethod]
    [DynamicData(
        nameof(NodeCensusRows),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void EnumerateNodes_WhenUnicodeFixture_ShouldMatchNodeCensus(PstNodeCensusKat kat)
    {
        using PstFile file = PstReferenceFixtures.OpenFile(kat.File);

        var nodes = file.EnumerateNodes().ToList();

        Assert.AreEqual(kat.NodeCount, nodes.Count);
        Assert.AreEqual(kat.FolderNodeCount, nodes.Count(n => n.NodeId.Type == PstNodeType.NormalFolder));
        Assert.AreEqual(kat.MessageNodeCount, nodes.Count(n => n.NodeId.Type == PstNodeType.NormalMessage));
    }

    /// <summary>
    /// Verifies that the enumeration includes the fixed nodes every file carries: the message store, the
    /// named-property map, and the root folder.
    /// </summary>
    [TestMethod]
    public void EnumerateNodes_WhenUnicodeFixture_ShouldIncludeWellKnownNodes()
    {
        using PstFile file = OpenSample1();

        var ids = file.EnumerateNodes().Select(n => n.NodeId).ToHashSet();

        Assert.IsTrue(ids.Contains(PstNodeId.MessageStore));
        Assert.IsTrue(ids.Contains(PstNodeId.NameToIdMap));
        Assert.IsTrue(ids.Contains(PstNodeId.RootFolder));
    }

    /// <summary>
    /// Verifies that nodes stream out in ascending identifier order — the node B-tree's key order.
    /// </summary>
    [TestMethod]
    public void EnumerateNodes_WhenUnicodeFixture_ShouldYieldAscendingIdentifiers()
    {
        using PstFile file = OpenSample1();

        var values = file.EnumerateNodes().Select(n => n.NodeId.Value).ToList();

        CollectionAssert.AreEqual(values.OrderBy(v => v).ToList(), values);
    }

    /// <summary>
    /// Verifies that a disposed session refuses to enumerate.
    /// </summary>
    [TestMethod]
    public void EnumerateNodes_WhenDisposed_ShouldThrowObjectDisposedException()
    {
        PstFile file = OpenSample1();
        file.Dispose();

        _ = Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = file.EnumerateNodes().ToList();
        });
    }
}
