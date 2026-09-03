// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstNodeTests.OpenDataStream.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Pst.Internal;
using Bodu.Test;

namespace Bodu.IO.Pst;

public partial class PstNodeTests
{
    /// <summary>
    /// Verifies the streaming-first invariant (exploration doc §4, risk R4): reading a node whose logical payload is
    /// hundreds of megabytes through <see cref="PstNode.OpenDataStream" /> in small chunks stays under a memory
    /// ceiling a materializing implementation cannot meet. The fixture is physically tiny — one 8176-byte data block
    /// referenced 1021 times by an <c>XBLOCK</c> that an <c>XXBLOCK</c> references 32 times, giving a ~255 MB logical
    /// payload from three distinct blocks — so only the reader's buffering strategy is measured.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Regression)]
    public void OpenDataStream_WhenLogicalPayloadIsHuge_ShouldStreamUnderMemoryCeiling()
    {
        const int LeafRefsPerXBlock = 1021;
        const int XBlockRefs = 32;
        const long CeilingBytes = 96L * 1024 * 1024;

        var payload = new byte[PstFixtureBuilder.MaxBlockPayload];
        for (int i = 0; i < payload.Length; i++) payload[i] = (byte)(i * 31);

        var builder = new PstFixtureBuilder();
        ulong dataId = builder.AddDataBlock(payload);
        uint xBlockLength = (uint)(LeafRefsPerXBlock * payload.Length);
        ulong xBlockId = builder.AddXBlock(xBlockLength, [.. Enumerable.Repeat(dataId, LeafRefsPerXBlock)]);
        ulong xxBlockId = builder.AddXXBlock((uint)((long)XBlockRefs * xBlockLength), [.. Enumerable.Repeat(xBlockId, XBlockRefs)]);
        var nodeId = new PstNodeId(PstNodeType.NormalMessage, 42);
        builder.AddNode(nodeId.Value, xxBlockId);

        long expectedLength = (long)XBlockRefs * xBlockLength;
        using PstFile file = PstFile.Open(builder.BuildStream(), new PstFileOptions());
        PstNode node = file.GetNode(nodeId);

        GC.Collect();
        GC.WaitForPendingFinalizers();
        long baseline = GC.GetTotalMemory(forceFullCollection: true);

        long totalRead = 0;
        long maxDelta = 0;
        var chunk = new byte[64 * 1024];
        using (Stream stream = node.OpenDataStream())
        {
            int chunkIndex = 0;
            int read;
            while ((read = stream.Read(chunk, 0, chunk.Length)) > 0)
            {
                if (totalRead == 0)
                    Assert.AreEqual(payload[0], chunk[0], "The first streamed byte must match the leaf payload.");

                totalRead += read;

                if ((chunkIndex++ & 63) == 0)
                    maxDelta = Math.Max(maxDelta, GC.GetTotalMemory(forceFullCollection: false) - baseline);
            }
        }

        Assert.AreEqual(expectedLength, totalRead, "The stream must yield the full logical payload.");
        Assert.IsTrue(maxDelta < CeilingBytes,
            $"Streaming a {expectedLength / (1024 * 1024)} MB logical payload must stay under the " +
            $"{CeilingBytes / (1024 * 1024)} MB ceiling; observed a {maxDelta / (1024 * 1024)} MB peak — " +
            "the payload is being materialized instead of streamed.");
    }

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
