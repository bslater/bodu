// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstNodeTests.TryGetSubnode.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Pst;

public partial class PstNodeTests
{
    /// <summary>
    /// Verifies that an existing subnode resolves as a node whose parent is the owner, with readable data.
    /// </summary>
    [TestMethod]
    public void TryGetSubnode_WhenSubnodeExists_ShouldReturnTrueAndNode()
    {
        using PstFile file = PstFileTests.OpenSample1();
        PstNode message = file.GetNode(Sample1Message);
        var recipientTable = new PstNodeId(0x0692);

        Assert.IsTrue(message.TryGetSubnode(recipientTable, out PstNode? subnode));
        Assert.AreEqual(recipientTable, subnode.Id);
        Assert.AreEqual(message.Id, subnode.ParentId);
        Assert.IsTrue(subnode.ReadAllBytes().Length > 0);
    }

    /// <summary>
    /// Verifies that an absent subnode identifier reports <see langword="false" /> with a <see langword="null" />
    /// node, both on a node with a subnode tree and on one without.
    /// </summary>
    [TestMethod]
    public void TryGetSubnode_WhenSubnodeAbsent_ShouldReturnFalse()
    {
        using PstFile file = PstFileTests.OpenSample1();
        var missing = new PstNodeId(PstNodeType.Ltp, 0x7FFFFFF);

        Assert.IsFalse(file.GetNode(Sample1Message).TryGetSubnode(missing, out PstNode? fromMessage));
        Assert.IsNull(fromMessage);

        Assert.IsFalse(file.GetNode(PstNodeId.MessageStore).TryGetSubnode(missing, out PstNode? fromStore));
        Assert.IsNull(fromStore);
    }
}
