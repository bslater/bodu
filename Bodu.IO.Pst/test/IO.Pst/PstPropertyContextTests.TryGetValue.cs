// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstPropertyContextTests.TryGetValue.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

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
}
