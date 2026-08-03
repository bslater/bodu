// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstNodeTests.ReadAllBytes.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test;
using Bodu.Test.Kat;

namespace Bodu.IO.Pst;

public partial class PstNodeTests
{
    /// <summary>
    /// Verifies that the message store's payload decodes to a well-formed LTP heap: after the permute decoding, the
    /// heap-node header carries the <c>bSig</c> marker (<c>0xEC</c>) and the property-context client signature
    /// (<c>0xBC</c>) at their fixed offsets, and the length matches the value pinned during the P0 validation.
    /// </summary>
    [TestMethod]
    public void ReadAllBytes_WhenMessageStoreNode_ShouldDecodeHeapNodeHeader()
    {
        using PstFile file = PstFileTests.OpenSample1();

        byte[] data = file.GetNode(PstNodeId.MessageStore).ReadAllBytes();

        Assert.AreEqual(290, data.Length);
        Assert.AreEqual(0xEC, data[2]);
        Assert.AreEqual(0xBC, data[3]);
    }

    /// <summary>
    /// Verifies that a multi-block payload assembles to the logical length its data tree records — the message node's
    /// payload exceeds a single block.
    /// </summary>
    [TestMethod]
    public void ReadAllBytes_WhenMultiBlockPayload_ShouldAssembleRecordedLength()
    {
        using PstFile file = PstFileTests.OpenSample1();

        PstNodeInfo info = file.EnumerateNodes().Single(n => n.NodeId == Sample1Message);
        byte[] data = file.GetNode(Sample1Message).ReadAllBytes();

        Assert.AreEqual(4198, info.DataLength);
        Assert.AreEqual(info.DataLength, data.Length);
    }

    /// <summary>
    /// Verifies that every enumerated node's payload materializes to its reported directory length, under strict
    /// validation, for each Unicode fixture — the full-corpus data-tree sweep.
    /// </summary>
    /// <param name="kat">The fixture census row.</param>
    [TestMethod]
    [TestCategory(TestCategories.Regression)]
    [DynamicData(
        nameof(PstFileTests.NodeCensusRows),
        typeof(PstFileTests),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void ReadAllBytes_WhenEveryNodeUnderStrict_ShouldMatchReportedLength(PstNodeCensusKat kat)
    {
        using PstFile file = PstReferenceFixtures.OpenFile(kat.File, PstValidationLevel.Strict);

        foreach (PstNodeInfo info in file.EnumerateNodes().ToList())
        {
            byte[] data = file.GetNode(info.NodeId).ReadAllBytes();

            Assert.AreEqual(info.DataLength, data.Length, $"node {info.NodeId}");
        }
    }
}
