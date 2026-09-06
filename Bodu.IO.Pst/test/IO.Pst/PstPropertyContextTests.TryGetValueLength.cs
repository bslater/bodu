// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstPropertyContextTests.TryGetValueLength.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;
using Bodu.IO.Pst.Internal;

namespace Bodu.IO.Pst;

public partial class PstPropertyContextTests
{
    /// <summary>The property identifier of the large subnode-resident binary value.</summary>
    private const ushort LargeBinaryId = 0x000A;

    /// <summary>
    /// Opens a property context whose only out-of-line value is a binary property backed by an XXBLOCK tree of
    /// 40 XBLOCKs that each reference one 8 KB block 1,021 times — about 318 MB of logical payload above the default
    /// materialization limit, so only a streaming path can serve it.
    /// </summary>
    /// <param name="expectedLength">When this method returns, the logical payload length.</param>
    /// <returns>The open session and context.</returns>
    private static (PstFile File, PstPropertyContext Context) OpenLargeSubnodeContext(out long expectedLength)
    {
        const int LeafRefsPerXBlock = 1021;
        const int XBlockRefs = 40;

        var builder = new PstFixtureBuilder();
        var ltp = new PstLtpFixtureBuilder();

        ulong dataId = builder.AddDataBlock(new byte[PstFixtureBuilder.MaxBlockPayload]);
        uint xBlockLength = (uint)(LeafRefsPerXBlock * PstFixtureBuilder.MaxBlockPayload);
        ulong xBlockId = builder.AddXBlock(xBlockLength, [.. Enumerable.Repeat(dataId, LeafRefsPerXBlock)]);
        ulong xxBlockId = builder.AddXXBlock((uint)((long)XBlockRefs * xBlockLength), [.. Enumerable.Repeat(xBlockId, XBlockRefs)]);
        ulong subnodeTree = builder.AddSubnodeLeafBlock((SubnodeId, xxBlockId, 0));

        _ = ltp.AddPropertyContext(
            (Int32Id, 0x0003, 7),
            (LargeBinaryId, 0x0102, SubnodeId));
        ltp.AddHeapNode(builder, NodeId, subnodeTree);

        expectedLength = (long)XBlockRefs * xBlockLength;
        PstFile file = PstFile.Open(builder.BuildStream(), new PstFileOptions());
        return (file, file.GetNode(new PstNodeId(NodeId)).ReadPropertyContext());
    }

    /// <summary>
    /// Verifies that an inline value reports the width of its wire type.
    /// </summary>
    [TestMethod]
    public void TryGetValueLength_WhenValueIsInline_ShouldReturnInlineWidth()
    {
        (PstFile file, PstPropertyContext context) = OpenSharedContext();
        using (file)
        {
            Assert.IsTrue(context.TryGetValueLength(Int32Id, out long length));
            Assert.AreEqual(4L, length);
            Assert.IsTrue(context.TryGetValueLength(Int16Id, out length));
            Assert.AreEqual(2L, length);
        }
    }

    /// <summary>
    /// Verifies that a heap-resident value reports its heap item length.
    /// </summary>
    [TestMethod]
    public void TryGetValueLength_WhenValueIsHeapResident_ShouldReturnItemLength()
    {
        (PstFile file, PstPropertyContext context) = OpenSharedContext();
        using (file)
        {
            Assert.IsTrue(context.TryGetValueLength(BinaryId, out long length));
            Assert.AreEqual(4L, length);
            Assert.IsTrue(context.TryGetValueLength(StringId, out length));
            Assert.AreEqual(Encoding.Unicode.GetByteCount("Sample1"), length);
        }
    }

    /// <summary>
    /// Verifies that a subnode-resident value reports the length of its data tree.
    /// </summary>
    [TestMethod]
    public void TryGetValueLength_WhenValueIsSubnodeResident_ShouldReturnDataLength()
    {
        (PstFile file, PstPropertyContext context) = OpenSharedContext();
        using (file)
        {
            Assert.IsTrue(context.TryGetValueLength(SubnodeStringId, out long length));
            Assert.AreEqual(Encoding.Unicode.GetByteCount("from-subnode"), length);
        }
    }

    /// <summary>
    /// Verifies that an absent property reports <see langword="false" /> with a zero length.
    /// </summary>
    [TestMethod]
    public void TryGetValueLength_WhenPropertyAbsent_ShouldReturnFalse()
    {
        (PstFile file, PstPropertyContext context) = OpenSharedContext();
        using (file)
        {
            Assert.IsFalse(context.TryGetValueLength(0x0FFF, out long length));
            Assert.AreEqual(0L, length);
        }
    }

    /// <summary>
    /// Verifies that the length of a subnode-resident value above the materialization limit is reported from the
    /// tree blocks alone: no leaf payload is read and no limit is applied.
    /// </summary>
    [TestMethod]
    [TestCategory(Bodu.Test.TestCategories.Regression)]
    public void TryGetValueLength_WhenSubnodeTreeExceedsMaterializationLimit_ShouldReportLengthWithoutReadingLeaves()
    {
        const long CeilingBytes = 8L * 1024 * 1024;
        (PstFile file, PstPropertyContext context) = OpenLargeSubnodeContext(out long expectedLength);
        using (file)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            long baseline = GC.GetTotalMemory(forceFullCollection: true);

            Assert.IsTrue(context.TryGetValueLength(LargeBinaryId, out long length));

            long delta = GC.GetTotalMemory(forceFullCollection: false) - baseline;
            Assert.AreEqual(expectedLength, length);
            Assert.IsTrue(delta < CeilingBytes, $"Measuring the value allocated {delta / (1024 * 1024)} MB — the payload is being materialized.");
        }
    }
}
