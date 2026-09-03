// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstPropertyContextTests.TryGetValue.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using Bodu.IO.Pst.Internal;

namespace Bodu.IO.Pst;

/// <summary>
/// Verifies <see cref="PstPropertyContext.TryGetValue" />: value resolution across the inline, heap-resident, and
/// subnode-resident storage classes, and miss reporting.
/// </summary>
public partial class PstPropertyContextTests
{
    /// <summary>
    /// Verifies that inline values resolve from the record's value dword with their natural widths.
    /// </summary>
    [TestMethod]
    public void TryGetValue_WhenValueIsInline_ShouldResolveFromTheRecordDword()
    {
        (PstFile file, PstPropertyContext context) = OpenSharedContext();
        using (file)
        {
            Assert.IsTrue(context.TryGetValue(Int16Id, out PstPropertyValue int16Value));
            Assert.AreEqual((short)0x1234, int16Value.GetInt16());

            Assert.IsTrue(context.TryGetValue(Int32Id, out PstPropertyValue int32Value));
            Assert.AreEqual(unchecked((int)0x89ABCDEF), int32Value.GetInt32());

            Assert.IsTrue(context.TryGetValue(BooleanId, out PstPropertyValue booleanValue));
            Assert.IsTrue(booleanValue.GetBoolean());
        }
    }

    /// <summary>
    /// Verifies that a null-typed value resolves with an empty payload.
    /// </summary>
    [TestMethod]
    public void TryGetValue_WhenValueIsNullTyped_ShouldResolveEmptyPayload()
    {
        (PstFile file, PstPropertyContext context) = OpenSharedContext();
        using (file)
        {
            Assert.IsTrue(context.TryGetValue(NullId, out PstPropertyValue value));
            Assert.AreEqual(0, value.RawData.Length);
        }
    }

    /// <summary>
    /// Verifies that fixed-size heap-resident values resolve from their heap items.
    /// </summary>
    [TestMethod]
    public void TryGetValue_WhenValueIsHeapResident_ShouldResolveFromTheHeapItem()
    {
        (PstFile file, PstPropertyContext context) = OpenSharedContext();
        using (file)
        {
            Assert.IsTrue(context.TryGetValue(Int64Id, out PstPropertyValue int64Value));
            Assert.AreEqual(0x1122334455667788, int64Value.GetInt64());

            Assert.IsTrue(context.TryGetValue(GuidId, out PstPropertyValue guidValue));
            Assert.AreEqual(KnownGuid, guidValue.GetGuid());

            Assert.IsTrue(context.TryGetValue(StringId, out PstPropertyValue stringValue));
            Assert.AreEqual("Sample1", stringValue.GetString());

            Assert.IsTrue(context.TryGetValue(BinaryId, out PstPropertyValue binaryValue));
            CollectionAssert.AreEqual(new byte[] { 0xDE, 0xAD, 0xBE, 0xEF }, binaryValue.GetBytes());
        }
    }

    /// <summary>
    /// Verifies that a value whose <c>HNID</c> is a subnode identifier resolves from the owning node's subnode data.
    /// </summary>
    [TestMethod]
    public void TryGetValue_WhenValueIsSubnodeResident_ShouldResolveFromTheSubnodeData()
    {
        (PstFile file, PstPropertyContext context) = OpenSharedContext();
        using (file)
        {
            Assert.IsTrue(context.TryGetValue(SubnodeStringId, out PstPropertyValue value));
            Assert.AreEqual("from-subnode", value.GetString());
        }
    }

    /// <summary>
    /// Verifies that an absent property reports a miss rather than throwing.
    /// </summary>
    [TestMethod]
    public void TryGetValue_WhenPropertyIsAbsent_ShouldReturnFalse()
    {
        (PstFile file, PstPropertyContext context) = OpenSharedContext();
        using (file)
        {
            Assert.IsFalse(context.TryGetValue(0x7FFF, out _));
        }
    }

    /// <summary>
    /// Verifies that every property of a context whose records are stored out of key order is still found under the
    /// tolerant levels: record ordering is enforced only under strict validation, so lookup must not assume it.
    /// </summary>
    [TestMethod]
    public void TryGetValue_WhenRecordsAreUnordered_ForCompatible_ShouldFindEveryProperty()
    {
        static byte[] Record(int value)
        {
            var data = new byte[6];
            BinaryPrimitives.WriteUInt16LittleEndian(data, 0x0003);
            BinaryPrimitives.WriteInt32LittleEndian(data.AsSpan(2), value);
            return data;
        }

        var builder = new PstFixtureBuilder();
        var ltp = new PstLtpFixtureBuilder();
        uint leaf = ltp.AddBthLeafItem(2, 6, (0x0030, Record(3)), (0x0010, Record(1)), (0x0020, Record(2)));
        uint header = ltp.AddBthHeaderItem(2, 6, indexLevels: 0, leaf);
        ltp.ClientSignature = 0xBC;
        ltp.UserRootHid = header;
        ltp.AddHeapNode(builder, NodeId);

        using PstFile file = PstFile.Open(builder.BuildStream(), PstFileOptions.Default);
        PstPropertyContext context = file.GetNode(new PstNodeId(NodeId)).ReadPropertyContext();

        foreach ((ushort id, int expected) in new[] { ((ushort)0x0010, 1), ((ushort)0x0020, 2), ((ushort)0x0030, 3) })
        {
            Assert.IsTrue(context.TryGetValue(id, out PstPropertyValue value), $"Property 0x{id:X4} must be found.");
            Assert.AreEqual(expected, value.GetInt32());
        }
    }
}
