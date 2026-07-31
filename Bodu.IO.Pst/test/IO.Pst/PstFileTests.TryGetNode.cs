// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstFileTests.TryGetNode.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Pst;

public partial class PstFileTests
{
    /// <summary>
    /// Verifies that an existing identifier resolves with the node's directory facts intact.
    /// </summary>
    [TestMethod]
    public void TryGetNode_WhenIdentifierExists_ShouldReturnTrueAndNode()
    {
        using PstFile file = OpenSample1();

        Assert.IsTrue(file.TryGetNode(PstNodeId.RootFolder, out PstNode? node));
        Assert.AreEqual(PstNodeId.RootFolder, node.Id);
        Assert.AreEqual(PstNodeType.NormalFolder, node.Id.Type);
    }

    /// <summary>
    /// Verifies that an absent identifier reports <see langword="false" /> with a <see langword="null" /> node.
    /// </summary>
    [TestMethod]
    public void TryGetNode_WhenIdentifierAbsent_ShouldReturnFalse()
    {
        using PstFile file = OpenSample1();

        Assert.IsFalse(file.TryGetNode(new PstNodeId(PstNodeType.NormalMessage, 0x7FFFFFF), out PstNode? node));
        Assert.IsNull(node);
    }

    /// <summary>
    /// Verifies that a disposed session refuses the lookup.
    /// </summary>
    [TestMethod]
    public void TryGetNode_WhenDisposed_ShouldThrowObjectDisposedException()
    {
        PstFile file = OpenSample1();
        file.Dispose();

        _ = Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = file.TryGetNode(PstNodeId.MessageStore, out _);
        });
    }
}
