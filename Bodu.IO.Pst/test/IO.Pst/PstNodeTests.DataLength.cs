// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstNodeTests.DataLength.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Pst.Internal;
using Bodu.Test;

namespace Bodu.IO.Pst;

public partial class PstNodeTests
{
    /// <summary>
    /// Verifies that <see cref="PstNode.DataLength" /> agrees with the materialized payload length for every node in
    /// the reference fixture, so the cheap length matches the ground truth.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Regression)]
    public void DataLength_WhenComparedToReadAllBytes_ShouldMatchForEveryCorpusNode()
    {
        using PstFile file = PstFileTests.OpenSample1();

        int verified = 0;
        foreach (PstNodeInfo info in file.EnumerateNodes())
        {
            PstNode node = file.GetNode(info.NodeId);
            Assert.AreEqual(node.ReadAllBytes().LongLength, node.DataLength,
                $"DataLength must match the materialized payload for node {info.NodeId}.");
            verified++;
        }

        Assert.IsTrue(verified > 0, "The corpus walk must visit at least one node.");
    }

    /// <summary>
    /// Verifies that <see cref="PstNode.DataLength" /> resolves a very large logical payload without reading any leaf
    /// block — the length of a ~255 MB node built from three physical blocks comes back from the tree metadata alone.
    /// </summary>
    [TestMethod]
    public void DataLength_WhenLogicalPayloadIsHuge_ShouldResolveWithoutMaterializing()
    {
        const int LeafRefsPerXBlock = 1021;
        const int XBlockRefs = 32;

        var payload = new byte[PstFixtureBuilder.MaxBlockPayload];
        var builder = new PstFixtureBuilder();
        ulong dataId = builder.AddDataBlock(payload);
        uint xBlockLength = (uint)(LeafRefsPerXBlock * payload.Length);
        ulong xBlockId = builder.AddXBlock(xBlockLength, [.. Enumerable.Repeat(dataId, LeafRefsPerXBlock)]);
        ulong xxBlockId = builder.AddXXBlock((uint)((long)XBlockRefs * xBlockLength), [.. Enumerable.Repeat(xBlockId, XBlockRefs)]);
        var nodeId = new PstNodeId(PstNodeType.NormalMessage, 43);
        builder.AddNode(nodeId.Value, xxBlockId);

        using PstFile file = PstFile.Open(builder.BuildStream(), new PstFileOptions());

        Assert.AreEqual((long)XBlockRefs * xBlockLength, file.GetNode(nodeId).DataLength);
    }
}
