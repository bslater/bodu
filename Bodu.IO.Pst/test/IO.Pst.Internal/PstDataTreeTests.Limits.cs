// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstDataTreeTests.Limits.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Pst;
using Bodu.Test;

namespace Bodu.IO.Pst.Internal;

public partial class PstDataTreeTests
{
    /// <summary>
    /// Builds a node whose data tree declares a logical payload of roughly 318 MB from a single physical 8 KB block
    /// referenced 1021 times per <c>XBLOCK</c> across 40 <c>XBLOCK</c>s — above the default materialization limit,
    /// but a perfectly ordinary size for a streamed read.
    /// </summary>
    /// <param name="expectedLength">Receives the declared logical length.</param>
    /// <returns>The container builder with the node declared.</returns>
    private static PstFixtureBuilder BuildOversizedTree(out long expectedLength)
    {
        const int LeafRefsPerXBlock = 1021;
        const int XBlockRefs = 40;

        var builder = new PstFixtureBuilder();
        ulong dataId = builder.AddDataBlock(new byte[PstFixtureBuilder.MaxBlockPayload]);
        uint xBlockLength = (uint)(LeafRefsPerXBlock * PstFixtureBuilder.MaxBlockPayload);
        ulong xBlockId = builder.AddXBlock(xBlockLength, [.. Enumerable.Repeat(dataId, LeafRefsPerXBlock)]);
        ulong xxBlockId = builder.AddXXBlock((uint)((long)XBlockRefs * xBlockLength), [.. Enumerable.Repeat(xBlockId, XBlockRefs)]);
        builder.AddNode(NodeId, xxBlockId);

        expectedLength = (long)XBlockRefs * xBlockLength;
        return builder;
    }

    /// <summary>
    /// Verifies that materializing a node whose declared payload exceeds the default materialization limit fails
    /// with <see cref="PstFileFormatException" /> before the payload is assembled, rather than allocating hundreds of
    /// megabytes on the strength of a few kilobytes of file.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Regression)]
    public void ReadAllBytes_WhenLogicalPayloadExceedsMaterializationLimit_ShouldThrowPstFileFormatException()
    {
        PstFixtureBuilder builder = BuildOversizedTree(out _);

        using PstFile file = PstFile.Open(builder.BuildStream(), PstFileOptions.Default);
        PstNode node = file.GetNode(new PstNodeId(NodeId));

        var ex = Assert.ThrowsExactly<PstFileFormatException>(() =>
        {
            _ = node.ReadAllBytes();
        });

        Assert.AreEqual(PstFileError.LimitExceeded, ex.Error);
    }

    /// <summary>
    /// Verifies that a caller-supplied materialization limit is honoured: a node above it is refused, one below it
    /// reads normally.
    /// </summary>
    [TestMethod]
    public void ReadAllBytes_WhenCallerLowersMaterializationLimit_ShouldHonourIt()
    {
        var builder = new PstFixtureBuilder();
        builder.AddNode(NodeId, builder.AddXBlock(2000, builder.AddDataBlock(Payload(1000, 1)), builder.AddDataBlock(Payload(1000, 2))));

        using (PstFile permissive = PstFile.Open(builder.BuildStream(), new PstFileOptions { MaxNodeDataLength = 2000 }))
        {
            Assert.AreEqual(2000, permissive.GetNode(new PstNodeId(NodeId)).ReadAllBytes().Length);
        }

        using PstFile strict = PstFile.Open(builder.BuildStream(), new PstFileOptions { MaxNodeDataLength = 1999 });
        PstNode node = strict.GetNode(new PstNodeId(NodeId));

        var ex = Assert.ThrowsExactly<PstFileFormatException>(() => _ = node.ReadAllBytes());

        Assert.AreEqual(PstFileError.LimitExceeded, ex.Error);
    }

    /// <summary>
    /// Verifies that a data tree referencing more leaf blocks than the fan-out limit allows is refused before any
    /// leaf payload is read, for the streaming and buffered paths alike.
    /// </summary>
    [TestMethod]
    public void OpenDataStream_WhenLeafCountExceedsLimit_ShouldThrowBeforeReadingLeaves()
    {
        var builder = new PstFixtureBuilder();
        ulong leaf = builder.AddDataBlock(Payload(64, 1));
        builder.AddNode(NodeId, builder.AddXBlock(64 * 200, [.. Enumerable.Repeat(leaf, 200)]));

        using PstFile file = PstFile.Open(builder.BuildStream(), new PstFileOptions { MaxDataTreeLeaves = 100 });
        PstNode node = file.GetNode(new PstNodeId(NodeId));

        var streaming = Assert.ThrowsExactly<PstFileFormatException>(() => _ = node.OpenDataStream());
        Assert.AreEqual(PstFileError.LimitExceeded, streaming.Error);

        var buffered = Assert.ThrowsExactly<PstFileFormatException>(() => _ = node.ReadAllBytes());
        Assert.AreEqual(PstFileError.LimitExceeded, buffered.Error);
    }

    /// <summary>
    /// Verifies that the same oversized node still streams in full through <see cref="PstNode.OpenDataStream" />:
    /// the materialization limit governs buffered reads only, never the streaming path.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Regression)]
    public void OpenDataStream_WhenLogicalPayloadExceedsMaterializationLimit_ShouldStillStream()
    {
        PstFixtureBuilder builder = BuildOversizedTree(out long expectedLength);

        using PstFile file = PstFile.Open(builder.BuildStream(), PstFileOptions.Default);
        PstNode node = file.GetNode(new PstNodeId(NodeId));

        long total = 0;
        var chunk = new byte[64 * 1024];
        using (Stream stream = node.OpenDataStream())
        {
            int read;
            while ((read = stream.Read(chunk, 0, chunk.Length)) > 0)
                total += read;
        }

        Assert.AreEqual(expectedLength, total);
    }
}
