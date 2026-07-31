// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstNodeTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Pst;

/// <summary>
/// Verifies the behavior of <see cref="PstNode" />, the per-node data and subnode surface.
/// </summary>
[TestClass]
public partial class PstNodeTests
{
    /// <summary>The identifier of the sole message node in the primary Unicode fixture.</summary>
    internal static readonly PstNodeId Sample1Message = new(0x00200024);

    /// <summary>
    /// Verifies the identity facts the node projects from its directory entry: identifier, parent, subnode flag, and
    /// the diagnostic string.
    /// </summary>
    [TestMethod]
    public void Id_WhenMessageNode_ShouldExposeDirectoryFacts()
    {
        using PstFile file = PstFileTests.OpenSample1();

        PstNode message = file.GetNode(Sample1Message);
        PstNode store = file.GetNode(PstNodeId.MessageStore);

        Assert.AreEqual(Sample1Message, message.Id);
        Assert.AreEqual(PstNodeType.NormalMessage, message.Id.Type);
        Assert.IsTrue(message.HasSubnodes);
        Assert.IsFalse(store.HasSubnodes);
        Assert.AreEqual("0x00200024 (NormalMessage)", message.ToString());
    }
}
