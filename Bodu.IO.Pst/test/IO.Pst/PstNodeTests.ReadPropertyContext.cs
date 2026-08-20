// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstNodeTests.ReadPropertyContext.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Pst.Internal;

namespace Bodu.IO.Pst;

/// <summary>
/// Verifies <see cref="PstNode.ReadPropertyContext" />: the LTP property-bag entry point on a node.
/// </summary>
public partial class PstNodeTests
{
    /// <summary>
    /// Verifies that a node carrying a property context opens it and serves a property, exercising the primary LTP
    /// read path end to end.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void ReadPropertyContext_WhenNodeCarriesPropertyContext_ShouldServeProperties()
    {
        var builder = new PstFixtureBuilder();
        var ltp = new PstLtpFixtureBuilder();
        _ = ltp.AddPropertyContext((0x1001, 0x0003, 42));
        ltp.AddHeapNode(builder, 0x21);

        using PstFile file = PstFile.Open(builder.BuildStream(), PstFileOptions.Default);
        PstPropertyContext context = file.GetNode(new PstNodeId(0x21)).ReadPropertyContext();

        Assert.AreEqual(1, context.Count);
        Assert.AreEqual(42, context.GetValue(0x1001).GetInt32());
    }

    /// <summary>
    /// Verifies that a node whose heap declares a table context is rejected as a property context, because the
    /// client signature names a different structure.
    /// </summary>
    [TestMethod]
    public void ReadPropertyContext_WhenNodeCarriesTableContext_ShouldThrowPstFileFormatException()
    {
        var builder = new PstFixtureBuilder();
        var ltp = new PstLtpFixtureBuilder { ClientSignature = 0x7C };
        _ = ltp.AddItem([0x00]);
        ltp.AddHeapNode(builder, 0x21);

        using PstFile file = PstFile.Open(builder.BuildStream(), PstFileOptions.Default);
        PstNode node = file.GetNode(new PstNodeId(0x21));

        _ = Assert.ThrowsExactly<PstFileFormatException>(() => _ = node.ReadPropertyContext());
    }

    /// <summary>
    /// Verifies that a node whose data is not a heap at all is rejected.
    /// </summary>
    [TestMethod]
    public void ReadPropertyContext_WhenNodeDataIsNotAHeap_ShouldThrowPstFileFormatException()
    {
        var builder = new PstFixtureBuilder();
        builder.AddNode(0x21, builder.AddDataBlock([1, 2, 3, 4]));

        using PstFile file = PstFile.Open(builder.BuildStream(), PstFileOptions.Default);
        PstNode node = file.GetNode(new PstNodeId(0x21));

        _ = Assert.ThrowsExactly<PstFileFormatException>(() => _ = node.ReadPropertyContext());
    }

    /// <summary>
    /// Verifies that a property tree whose shape is not the property-context shape (a key width other than two) is
    /// rejected.
    /// </summary>
    [TestMethod]
    public void ReadPropertyContext_WhenTreeShapeIsNotPc_ShouldThrowPstFileFormatException()
    {
        var builder = new PstFixtureBuilder();
        var ltp = new PstLtpFixtureBuilder();
        uint headerHid = ltp.AddBthHeaderItem(keySize: 4, dataSize: 6, indexLevels: 0, rootHid: 0);
        ltp.ClientSignature = 0xBC;
        ltp.UserRootHid = headerHid;
        ltp.AddHeapNode(builder, 0x21);

        using PstFile file = PstFile.Open(builder.BuildStream(), PstFileOptions.Default);
        PstNode node = file.GetNode(new PstNodeId(0x21));

        _ = Assert.ThrowsExactly<PstFileFormatException>(() => _ = node.ReadPropertyContext());
    }

    /// <summary>
    /// Verifies that an unrecognized wire type is surfaced raw under <see cref="PstValidationLevel.Compatible" /> but
    /// rejected under <see cref="PstValidationLevel.Strict" />.
    /// </summary>
    [TestMethod]
    public void ReadPropertyContext_WhenWireTypeIsUnknown_ShouldThrowOnlyUnderStrict()
    {
        static PstFixtureBuilder BuildFixture()
        {
            var builder = new PstFixtureBuilder();
            var ltp = new PstLtpFixtureBuilder();
            _ = ltp.AddPropertyContext((0x1001, 0x0F0F, 0x11223344));
            ltp.AddHeapNode(builder, 0x21);
            return builder;
        }

        using (PstFile file = PstFile.Open(BuildFixture().BuildStream(), PstFileOptions.Default))
        {
            PstPropertyValue value = file.GetNode(new PstNodeId(0x21)).ReadPropertyContext().GetValue(0x1001);

            Assert.AreEqual(0x0F0F, value.WireType);
            CollectionAssert.AreEqual(new byte[] { 0x44, 0x33, 0x22, 0x11 }, value.GetBytes());
        }

        using (PstFile file = PstFile.Open(BuildFixture().BuildStream(), new PstFileOptions { ValidationLevel = PstValidationLevel.Strict }))
        {
            PstNode node = file.GetNode(new PstNodeId(0x21));

            _ = Assert.ThrowsExactly<PstFileFormatException>(() => _ = node.ReadPropertyContext());
        }
    }
}
