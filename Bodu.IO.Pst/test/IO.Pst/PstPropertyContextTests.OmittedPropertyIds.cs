// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstPropertyContextTests.OmittedPropertyIds.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;
using Bodu.IO.Pst.Internal;

namespace Bodu.IO.Pst;

/// <summary>
/// Verifies <see cref="PstPropertyContext.OmittedPropertyIds" /> and the context a session that omits unresolvable
/// values (<see cref="PstFileOptions.OmitUnresolvableValues" />) reads: which values are omitted, that an omitted value
/// is absent from every member, and that the default session still rejects them.
/// </summary>
public partial class PstPropertyContextTests
{
    /// <summary>A heap identifier past the fixture heap's last allocation (item 15 of block 0).</summary>
    private const uint DanglingHid = 0x000001E0;

    /// <summary>A subnode identifier the fixture node's subnode tree does not hold.</summary>
    private const uint MissingSubnodeId = 0x61;

    /// <summary>The heap identifier of the damaged fixture's four-byte item, too short for a fixed-width type.</summary>
    private static readonly uint ShortItem = PstLtpFixtureBuilder.Hid(0, 1);

    /// <summary>
    /// Verifies that a context read without omitting unresolvable values lists no omitted property.
    /// </summary>
    [TestMethod]
    public void OmittedPropertyIds_WhenSessionDoesNotOmit_ShouldBeEmpty()
    {
        (PstFile file, PstPropertyContext context) = OpenSharedContext();
        using (file)
        {
            Assert.AreEqual(0, context.OmittedPropertyIds.Count);
        }
    }

    /// <summary>
    /// Verifies that a context whose every reference resolves omits nothing when the session omits unresolvable values.
    /// </summary>
    [TestMethod]
    public void OmittedPropertyIds_WhenEveryReferenceResolves_ForOmittingSession_ShouldBeEmpty()
    {
        (PstFile file, PstPropertyContext context) = OpenSharedContext(omitUnresolvableValues: true);
        using (file)
        {
            Assert.AreEqual(0, context.OmittedPropertyIds.Count);
            Assert.AreEqual(9, context.Count);
        }
    }

    /// <summary>
    /// Verifies that a variable-size value whose heap identifier lies outside the heap is omitted, and the rest of the
    /// context stays readable.
    /// </summary>
    [TestMethod]
    public void OmittedPropertyIds_WhenHeapReferenceDoesNotResolve_ForOmittingSession_ShouldListTheProperty()
    {
        (PstFile file, PstPropertyContext context) = OpenDamagedContext(omitUnresolvableValues: true, (StringId, 0x001F, DanglingHid));
        using (file)
        {
            CollectionAssert.AreEqual(new ushort[] { StringId }, context.OmittedPropertyIds.ToArray());
            CollectionAssert.AreEqual(Encoding.Unicode.GetBytes("kept"), context.GetValue(BinaryId).RawData.ToArray());
        }
    }

    /// <summary>
    /// Verifies that a fixed-width value whose heap item is shorter than its type is omitted.
    /// </summary>
    [TestMethod]
    public void OmittedPropertyIds_WhenFixedWidthValueIsShort_ForOmittingSession_ShouldListTheProperty()
    {
        (PstFile file, PstPropertyContext context) = OpenDamagedContext(omitUnresolvableValues: true, (Int64Id, 0x0014, ShortItem));
        using (file)
        {
            CollectionAssert.AreEqual(new ushort[] { Int64Id }, context.OmittedPropertyIds.ToArray());
        }
    }

    /// <summary>
    /// Verifies that a fixed-width value whose reference is null is omitted, since it holds none of its type's bytes.
    /// </summary>
    [TestMethod]
    public void OmittedPropertyIds_WhenFixedWidthReferenceIsNull_ForOmittingSession_ShouldListTheProperty()
    {
        (PstFile file, PstPropertyContext context) = OpenDamagedContext(omitUnresolvableValues: true, (GuidId, 0x0048, 0));
        using (file)
        {
            CollectionAssert.AreEqual(new ushort[] { GuidId }, context.OmittedPropertyIds.ToArray());
        }
    }

    /// <summary>
    /// Verifies that a value naming a subnode the node does not hold is omitted.
    /// </summary>
    [TestMethod]
    public void OmittedPropertyIds_WhenSubnodeIsMissing_ForOmittingSession_ShouldListTheProperty()
    {
        (PstFile file, PstPropertyContext context) = OpenDamagedContext(omitUnresolvableValues: true, (SubnodeStringId, 0x001F, MissingSubnodeId));
        using (file)
        {
            CollectionAssert.AreEqual(new ushort[] { SubnodeStringId }, context.OmittedPropertyIds.ToArray());
        }
    }

    /// <summary>
    /// Verifies that a value naming a subnode whose data block does not exist is omitted, while the default session
    /// fails to read it.
    /// </summary>
    [TestMethod]
    public void OmittedPropertyIds_WhenSubnodeDataDoesNotResolve_ForOmittingSession_ShouldListTheProperty()
    {
        static (PstFile File, PstPropertyContext Context) Open(bool omitUnresolvableValues)
        {
            var builder = new PstFixtureBuilder();
            var ltp = new PstLtpFixtureBuilder();

            // The subnode exists, but the data block it names was never written.
            ulong subnodeTree = builder.AddSubnodeLeafBlock((SubnodeId, 0x7FF0, 0));
            _ = ltp.AddPropertyContext((Int32Id, 0x0003, 42), (SubnodeStringId, 0x001F, SubnodeId));
            ltp.AddHeapNode(builder, NodeId, subnodeTree);

            PstFile file = PstFile.Open(builder.BuildStream(), new PstFileOptions { OmitUnresolvableValues = omitUnresolvableValues });
            return (file, file.GetNode(new PstNodeId(NodeId)).ReadPropertyContext());
        }

        (PstFile omittingFile, PstPropertyContext omitting) = Open(omitUnresolvableValues: true);
        using (omittingFile)
        {
            CollectionAssert.AreEqual(new ushort[] { SubnodeStringId }, omitting.OmittedPropertyIds.ToArray());
            Assert.AreEqual(1, omitting.Count);
        }

        (PstFile defaultFile, PstPropertyContext context) = Open(omitUnresolvableValues: false);
        using (defaultFile)
        {
            _ = Assert.ThrowsExactly<PstFileFormatException>(() =>
            {
                _ = context.TryGetValue(SubnodeStringId, out _);
            });
        }
    }

    /// <summary>
    /// Verifies that several unresolvable values are all omitted, listed in ascending identifier order.
    /// </summary>
    [TestMethod]
    public void OmittedPropertyIds_WhenSeveralReferencesDoNotResolve_ForOmittingSession_ShouldListThemInAscendingOrder()
    {
        (PstFile file, PstPropertyContext context) = OpenDamagedContext(
            omitUnresolvableValues: true,
            (SubnodeStringId, 0x001F, MissingSubnodeId),
            (Int64Id, 0x0014, ShortItem),
            (StringId, 0x001F, DanglingHid));
        using (file)
        {
            CollectionAssert.AreEqual(new ushort[] { Int64Id, StringId, SubnodeStringId }, context.OmittedPropertyIds.ToArray());
        }
    }

    /// <summary>
    /// Verifies that an omitted property is absent from every member of the context: the count, containment, lookups,
    /// the wire type, the length, the stream, and enumeration.
    /// </summary>
    [TestMethod]
    public void OmittedPropertyIds_WhenAPropertyIsOmitted_ShouldBeAbsentFromEveryMember()
    {
        (PstFile file, PstPropertyContext context) = OpenDamagedContext(omitUnresolvableValues: true, (StringId, 0x001F, DanglingHid));
        using (file)
        {
            Assert.AreEqual(2, context.Count);
            Assert.IsFalse(context.Contains(StringId));
            Assert.IsFalse(context.TryGetValue(StringId, out _));
            Assert.IsFalse(context.TryGetWireType(StringId, out _));
            Assert.IsFalse(context.TryGetValueLength(StringId, out _));
            Assert.IsFalse(context.TryOpenValueStream(StringId, out _));
            CollectionAssert.AreEqual(new ushort[] { Int32Id, BinaryId }, context.EnumeratePropertyIds().ToArray());
            CollectionAssert.AreEqual(new ushort[] { Int32Id, BinaryId }, context.Select(static value => value.PropertyId).ToArray());

            PstFileException ex = Assert.ThrowsExactly<PstFileException>(() =>
            {
                _ = context.GetValue(StringId);
            });

            Assert.AreEqual(PstFileError.PropertyNotFound, ex.Error);
        }
    }

    /// <summary>
    /// Verifies that the default session still fails to read a value whose heap identifier lies outside the heap, with
    /// <see cref="PstFileError.InvalidHeap" />.
    /// </summary>
    [TestMethod]
    public void TryGetValue_WhenHeapReferenceDoesNotResolve_ShouldThrowPstFileFormatException()
    {
        (PstFile file, PstPropertyContext context) = OpenDamagedContext(omitUnresolvableValues: false, (StringId, 0x001F, DanglingHid));
        using (file)
        {
            Assert.AreEqual(0, context.OmittedPropertyIds.Count);

            PstFileFormatException ex = Assert.ThrowsExactly<PstFileFormatException>(() =>
            {
                _ = context.TryGetValue(StringId, out _);
            });

            Assert.AreEqual(PstFileError.InvalidHeap, ex.Error);
        }
    }

    /// <summary>
    /// Verifies that the default session still fails to read a fixed-width value shorter than its type, with
    /// <see cref="PstFileError.InvalidPropertyContext" />.
    /// </summary>
    [TestMethod]
    public void TryGetValue_WhenFixedWidthValueIsShort_ShouldThrowPstFileFormatException()
    {
        (PstFile file, PstPropertyContext context) = OpenDamagedContext(omitUnresolvableValues: false, (Int64Id, 0x0014, ShortItem));
        using (file)
        {
            PstFileFormatException ex = Assert.ThrowsExactly<PstFileFormatException>(() =>
            {
                _ = context.TryGetValue(Int64Id, out _);
            });

            Assert.AreEqual(PstFileError.InvalidPropertyContext, ex.Error);
        }
    }

    /// <summary>
    /// Verifies that the default session still fails to read a value naming a subnode the node does not hold, with
    /// <see cref="PstFileError.InvalidSubnodeTree" />.
    /// </summary>
    [TestMethod]
    public void TryGetValue_WhenSubnodeIsMissing_ShouldThrowPstFileFormatException()
    {
        (PstFile file, PstPropertyContext context) = OpenDamagedContext(omitUnresolvableValues: false, (SubnodeStringId, 0x001F, MissingSubnodeId));
        using (file)
        {
            PstFileFormatException ex = Assert.ThrowsExactly<PstFileFormatException>(() =>
            {
                _ = context.TryGetValue(SubnodeStringId, out _);
            });

            Assert.AreEqual(PstFileError.InvalidSubnodeTree, ex.Error);
        }
    }

    /// <summary>
    /// Verifies that omitting unresolvable values does not tolerate a structural fault in the heap: a node whose heap
    /// carries a table context still fails to read as a property context.
    /// </summary>
    [TestMethod]
    public void ReadPropertyContext_WhenHeapIsNotAPropertyContext_ForOmittingSession_ShouldThrowPstFileFormatException()
    {
        var builder = new PstFixtureBuilder();
        var ltp = new PstLtpFixtureBuilder();
        uint matrixHid = ltp.AddItem(new byte[8]);
        _ = ltp.AddTableContext([(0x67F40003, 0, 4, 0)], endOffset4: 4, endOffset2: 4, endOffset1: 4, rowWidth: 5, rowsHnid: matrixHid);
        ltp.AddHeapNode(builder, NodeId);

        using PstFile file = PstFile.Open(builder.BuildStream(), new PstFileOptions { OmitUnresolvableValues = true });
        PstNode node = file.GetNode(new PstNodeId(NodeId));

        PstFileFormatException ex = Assert.ThrowsExactly<PstFileFormatException>(() =>
        {
            _ = node.ReadPropertyContext();
        });

        Assert.AreEqual(PstFileError.InvalidPropertyContext, ex.Error);
    }

    /// <summary>
    /// Builds a context holding one inline value, one resolvable binary value, and the supplied damaged records, and
    /// opens it.
    /// </summary>
    /// <param name="omitUnresolvableValues">Whether the session omits unresolvable values.</param>
    /// <param name="damaged">The records whose references are under test.</param>
    /// <returns>The open file and the context; the caller disposes the file.</returns>
    private static (PstFile File, PstPropertyContext Context) OpenDamagedContext(
        bool omitUnresolvableValues,
        params (ushort PropertyId, ushort WireType, uint RawValue)[] damaged)
    {
        var builder = new PstFixtureBuilder();
        var ltp = new PstLtpFixtureBuilder();

        // Item 1 is a four-byte item that a fixed-width record can name to come up short; item 2 is a resolvable value.
        uint shortHid = ltp.AddItem([0x01, 0x02, 0x03, 0x04]);
        uint keptHid = ltp.AddItem(Encoding.Unicode.GetBytes("kept"));
        Assert.AreEqual(ShortItem, shortHid);

        (ushort, ushort, uint)[] records =
        [
            (Int32Id, 0x0003, 42),
            (BinaryId, 0x0102, keptHid),
            .. damaged,
        ];
        _ = ltp.AddPropertyContext([.. records.OrderBy(static record => record.Item1)]);
        ltp.AddHeapNode(builder, NodeId);

        PstFile file = PstFile.Open(builder.BuildStream(), new PstFileOptions { OmitUnresolvableValues = omitUnresolvableValues });
        return (file, file.GetNode(new PstNodeId(NodeId)).ReadPropertyContext());
    }
}
