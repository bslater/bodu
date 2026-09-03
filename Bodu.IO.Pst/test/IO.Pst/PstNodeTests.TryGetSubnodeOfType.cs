// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstNodeTests.TryGetSubnodeOfType.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Pst;

public partial class PstNodeTests
{
    /// <summary>
    /// Verifies that the first subnode of a type is found in one pass over the subnode directory — the reference
    /// message carries its recipient and attachment tables there.
    /// </summary>
    [TestMethod]
    public void TryGetSubnodeOfType_WhenTypePresent_ShouldReturnFirstMatch()
    {
        using PstFile file = PstFileTests.OpenSample1();
        PstNode message = file.GetNode(Sample1Message);

        Assert.IsTrue(message.TryGetSubnodeOfType(PstNodeType.RecipientTable, out PstNode? recipients));
        Assert.AreEqual(0x692u, recipients.Id.Value);
        Assert.AreEqual(Sample1Message, recipients.ParentId);

        Assert.IsTrue(message.TryGetSubnodeOfType(PstNodeType.AttachmentTable, out PstNode? attachments));
        Assert.AreEqual(0x671u, attachments.Id.Value);
    }

    /// <summary>
    /// Verifies that an absent type, and a node without a subnode tree, both report a miss.
    /// </summary>
    [TestMethod]
    public void TryGetSubnodeOfType_WhenTypeAbsent_ShouldReturnFalse()
    {
        using PstFile file = PstFileTests.OpenSample1();

        Assert.IsFalse(file.GetNode(Sample1Message).TryGetSubnodeOfType(PstNodeType.SearchFolder, out _));
        Assert.IsFalse(file.GetNode(PstNodeId.MessageStore).TryGetSubnodeOfType(PstNodeType.RecipientTable, out _));
    }
}
