// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstFileTests.GetNode.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Pst;

public partial class PstFileTests
{
    /// <summary>
    /// Verifies that each well-known node resolves and reports its own identifier.
    /// </summary>
    [TestMethod]
    public void GetNode_WhenWellKnownIdentifier_ShouldReturnNode()
    {
        using PstFile file = OpenSample1();

        foreach (PstNodeId id in new[] { PstNodeId.MessageStore, PstNodeId.NameToIdMap, PstNodeId.RootFolder })
        {
            PstNode node = file.GetNode(id);

            Assert.AreEqual(id, node.Id);
        }
    }

    /// <summary>
    /// Verifies that an absent identifier raises the library's key-not-found error carrying the identifier.
    /// </summary>
    [TestMethod]
    public void GetNode_WhenIdentifierAbsent_ShouldThrowPstNodeNotFoundException()
    {
        using PstFile file = OpenSample1();
        var missing = new PstNodeId(PstNodeType.NormalMessage, 0x7FFFFFF);

        var ex = Assert.ThrowsExactly<PstNodeNotFoundException>(() =>
        {
            _ = file.GetNode(missing);
        });

        Assert.IsTrue(ex.Message.Contains(missing.ToString(), StringComparison.Ordinal));
        Assert.AreEqual(PstFileError.NodeNotFound, ex.Error);
    }

    /// <summary>
    /// Verifies that a disposed session refuses the lookup.
    /// </summary>
    [TestMethod]
    public void GetNode_WhenDisposed_ShouldThrowObjectDisposedException()
    {
        PstFile file = OpenSample1();
        file.Dispose();

        _ = Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = file.GetNode(PstNodeId.MessageStore);
        });
    }
}
