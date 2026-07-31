// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstNodeTests.EnumerateSubnodes.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Pst;

public partial class PstNodeTests
{
    /// <summary>
    /// Verifies that the message node's subnode tree surfaces its recipient and attachment tables — the private
    /// namespace the message layer will consume — with the owner recorded as each subnode's parent.
    /// </summary>
    [TestMethod]
    public void EnumerateSubnodes_WhenMessageNode_ShouldExposeRecipientAndAttachmentTables()
    {
        using PstFile file = PstFileTests.OpenSample1();
        PstNode message = file.GetNode(Sample1Message);

        var subnodes = message.EnumerateSubnodes().ToList();

        Assert.AreEqual(5, subnodes.Count);
        Assert.IsTrue(subnodes.Any(s => s.NodeId == new PstNodeId(0x0671) && s.NodeId.Type == PstNodeType.AttachmentTable));
        Assert.IsTrue(subnodes.Any(s => s.NodeId == new PstNodeId(0x0692) && s.NodeId.Type == PstNodeType.RecipientTable));
        Assert.IsTrue(subnodes.All(s => s.ParentNodeId == message.Id));
    }

    /// <summary>
    /// Verifies that a node without a subnode tree enumerates empty.
    /// </summary>
    [TestMethod]
    public void EnumerateSubnodes_WhenNodeHasNoSubnodes_ShouldYieldNothing()
    {
        using PstFile file = PstFileTests.OpenSample1();
        PstNode store = file.GetNode(PstNodeId.MessageStore);

        Assert.IsFalse(store.EnumerateSubnodes().Any());
    }
}
