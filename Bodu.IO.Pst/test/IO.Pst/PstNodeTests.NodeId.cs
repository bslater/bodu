// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstNodeTests.NodeId.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Pst;

public partial class PstNodeTests
{
    /// <summary>
    /// Verifies that a message node reports its own identifier, with the node type the identifier encodes.
    /// </summary>
    [TestMethod]
    public void NodeId_WhenMessageNode_ShouldReturnMessageIdentifier()
    {
        using PstFile file = PstFileTests.OpenSample1();

        PstNode message = file.GetNode(Sample1Message);

        Assert.AreEqual(Sample1Message, message.NodeId);
        Assert.AreEqual(PstNodeType.NormalMessage, message.NodeId.Type);
    }

    /// <summary>
    /// Verifies that every node of the file reports the identifier its directory entry records, the same value
    /// <see cref="PstNodeInfo.NodeId" /> names.
    /// </summary>
    [TestMethod]
    public void NodeId_WhenEveryNodeIsRetrieved_ShouldMatchDirectoryEntry()
    {
        using PstFile file = PstFileTests.OpenSample1();

        foreach (PstNodeInfo info in file.EnumerateNodes())
            Assert.AreEqual(info.NodeId, file.GetNode(info.NodeId).NodeId, info.ToString());
    }

    /// <summary>
    /// Verifies that a subnode reports its own identifier, the same value its subnode directory entry names.
    /// </summary>
    [TestMethod]
    public void NodeId_WhenSubnode_ShouldReturnSubnodeIdentifier()
    {
        using PstFile file = PstFileTests.OpenSample1();
        PstNode message = file.GetNode(Sample1Message);
        var recipientTable = new PstNodeId(0x0692);

        Assert.IsTrue(message.TryGetSubnode(recipientTable, out PstNode? subnode));
        Assert.AreEqual(recipientTable, subnode.NodeId);
        Assert.AreEqual(message.EnumerateSubnodes().Single(s => s.NodeId == recipientTable).NodeId, subnode.NodeId);
    }
}
