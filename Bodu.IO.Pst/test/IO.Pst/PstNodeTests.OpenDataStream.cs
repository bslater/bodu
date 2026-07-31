// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstNodeTests.OpenDataStream.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Pst;

public partial class PstNodeTests
{
    /// <summary>
    /// Verifies that the stream is a seekable, read-only view over the same bytes as
    /// <see cref="PstNode.ReadAllBytes" />.
    /// </summary>
    [TestMethod]
    public void OpenDataStream_WhenMessageStoreNode_ShouldMatchReadAllBytes()
    {
        using PstFile file = PstFileTests.OpenSample1();
        PstNode node = file.GetNode(PstNodeId.MessageStore);

        using Stream stream = node.OpenDataStream();

        Assert.IsTrue(stream.CanSeek);
        Assert.IsFalse(stream.CanWrite);

        var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        CollectionAssert.AreEqual(node.ReadAllBytes(), buffer.ToArray());
    }
}
