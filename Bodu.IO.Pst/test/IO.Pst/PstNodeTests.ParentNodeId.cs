// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstNodeTests.ParentNodeId.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Pst;

public partial class PstNodeTests
{
    /// <summary>
    /// Verifies that every node of the file reports the parent identifier its directory entry records, the same value
    /// <see cref="PstNodeInfo.ParentNodeId" /> names.
    /// </summary>
    [TestMethod]
    public void ParentNodeId_WhenEveryNodeIsRetrieved_ShouldMatchDirectoryEntry()
    {
        using PstFile file = PstFileTests.OpenSample1();

        foreach (PstNodeInfo info in file.EnumerateNodes())
            Assert.AreEqual(info.ParentNodeId, file.GetNode(info.NodeId).ParentNodeId, info.ToString());
    }

    /// <summary>
    /// Verifies that a message node reports the folder that holds it as its parent.
    /// </summary>
    [TestMethod]
    public void ParentNodeId_WhenMessageNode_ShouldReturnContainingFolder()
    {
        using PstFile file = PstFileTests.OpenSample1();

        PstNode message = file.GetNode(Sample1Message);

        Assert.AreEqual(PstNodeType.NormalFolder, message.ParentNodeId.Type);
        Assert.IsTrue(file.TryGetNode(message.ParentNodeId, out _));
    }

    /// <summary>
    /// Verifies that a subnode reports the node that owns it as its parent, whether it is found by identifier or by
    /// type.
    /// </summary>
    [TestMethod]
    public void ParentNodeId_WhenSubnode_ShouldReturnOwningNode()
    {
        using PstFile file = PstFileTests.OpenSample1();
        PstNode message = file.GetNode(Sample1Message);

        Assert.IsTrue(message.TryGetSubnode(new PstNodeId(0x0692), out PstNode? byId));
        Assert.IsTrue(message.TryGetSubnodeOfType(PstNodeType.AttachmentTable, out PstNode? byType));

        Assert.AreEqual(Sample1Message, byId.ParentNodeId);
        Assert.AreEqual(Sample1Message, byType.ParentNodeId);
    }
}
