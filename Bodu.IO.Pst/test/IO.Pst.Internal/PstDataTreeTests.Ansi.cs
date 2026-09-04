// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstDataTreeTests.Ansi.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Pst.Internal;

public partial class PstDataTreeTests
{
    /// <summary>
    /// Verifies that an ANSI <c>XBLOCK</c> (32-bit child identifiers) resolves to the concatenated leaf payloads.
    /// </summary>
    [TestMethod]
    public void ReadAllBytes_WhenAnsiNodeHasAnXBlock_ShouldConcatenateLeaves()
    {
        var builder = new PstFixtureBuilder { Format = PstFileFormat.Ansi };
        byte[] first = Payload(3000, 1);
        byte[] second = Payload(2000, 2);
        byte[] third = Payload(100, 3);
        ulong xBlock = builder.AddXBlock(5100, builder.AddDataBlock(first), builder.AddDataBlock(second), builder.AddDataBlock(third));
        builder.AddNode(NodeId, xBlock);

        using PstFile file = PstFile.Open(builder.BuildStream(), new PstFileOptions { ValidationLevel = PstValidationLevel.Strict });
        PstNode node = file.GetNode(new PstNodeId(NodeId));

        CollectionAssert.AreEqual((byte[])[.. first, .. second, .. third], node.ReadAllBytes());
        Assert.AreEqual(5100L, node.DataLength);
    }

    /// <summary>
    /// Verifies that an ANSI <c>XXBLOCK</c> over two <c>XBLOCK</c>s streams and materializes identically.
    /// </summary>
    [TestMethod]
    public void OpenDataStream_WhenAnsiNodeHasAnXXBlock_ShouldMatchReadAllBytes()
    {
        var builder = new PstFixtureBuilder { Format = PstFileFormat.Ansi };
        byte[] a = Payload(1000, 4);
        byte[] b = Payload(1000, 5);
        ulong left = builder.AddXBlock(1000, builder.AddDataBlock(a));
        ulong right = builder.AddXBlock(1000, builder.AddDataBlock(b));
        builder.AddNode(NodeId, builder.AddXXBlock(2000, left, right));

        using PstFile file = PstFile.Open(builder.BuildStream(), PstFileOptions.Default);
        PstNode node = file.GetNode(new PstNodeId(NodeId));

        var buffer = new MemoryStream();
        using (Stream stream = node.OpenDataStream())
            stream.CopyTo(buffer);

        CollectionAssert.AreEqual((byte[])[.. a, .. b], node.ReadAllBytes());
        CollectionAssert.AreEqual(node.ReadAllBytes(), buffer.ToArray());
    }
}
