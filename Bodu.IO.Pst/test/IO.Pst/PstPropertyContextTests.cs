// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstPropertyContextTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Text;
using Bodu.IO.Pst.Internal;

namespace Bodu.IO.Pst;

/// <summary>
/// Verifies <see cref="PstPropertyContext" />, the LTP property bag. This root holds the shared fixture: a property
/// context carrying inline, heap-resident, and subnode-resident values; the member partials assert each accessor's
/// contract over it.
/// </summary>
[TestClass]
public partial class PstPropertyContextTests
{
    /// <summary>The node identifier the fixtures use for the node under test.</summary>
    private const uint NodeId = 0x21;

    /// <summary>The subnode identifier a subnode-resident value uses; its low type bits make it an NID, not an HID.</summary>
    private const uint SubnodeId = 0x41;

    /// <summary>The identifier of the fixture's inline 16-bit integer property.</summary>
    private const ushort Int16Id = 0x0001;

    /// <summary>The identifier of the fixture's inline 32-bit integer property.</summary>
    private const ushort Int32Id = 0x0002;

    /// <summary>The identifier of the fixture's inline Boolean property.</summary>
    private const ushort BooleanId = 0x0003;

    /// <summary>The identifier of the fixture's null property.</summary>
    private const ushort NullId = 0x0004;

    /// <summary>The identifier of the fixture's heap-resident 64-bit integer property.</summary>
    private const ushort Int64Id = 0x0005;

    /// <summary>The identifier of the fixture's heap-resident GUID property.</summary>
    private const ushort GuidId = 0x0006;

    /// <summary>The identifier of the fixture's heap-resident UTF-16LE string property.</summary>
    private const ushort StringId = 0x0007;

    /// <summary>The identifier of the fixture's heap-resident binary property.</summary>
    private const ushort BinaryId = 0x0008;

    /// <summary>The identifier of the fixture's subnode-resident string property.</summary>
    private const ushort SubnodeStringId = 0x0009;

    /// <summary>The GUID value the shared fixture stores.</summary>
    private static readonly Guid KnownGuid = new("8b4f19a2-1f7e-4e4c-9c93-2d0305a6f0aa");

    /// <summary>
    /// Builds the shared fixture and opens the property context of the node under test.
    /// </summary>
    /// <param name="validationLevel">The validation level to open under.</param>
    /// <returns>The open file and the context; the caller disposes the file.</returns>
    private static (PstFile File, PstPropertyContext Context) OpenSharedContext(
        PstValidationLevel validationLevel = PstValidationLevel.Compatible)
    {
        var builder = new PstFixtureBuilder();
        var ltp = new PstLtpFixtureBuilder();

        // Heap-resident fixed and variable values are added first so their HIDs exist for the records.
        var int64Item = new byte[8];
        BinaryPrimitives.WriteInt64LittleEndian(int64Item, 0x1122334455667788);
        uint int64Hid = ltp.AddItem(int64Item);
        uint guidHid = ltp.AddItem(KnownGuid.ToByteArray());
        uint stringHid = ltp.AddItem(Encoding.Unicode.GetBytes("Sample1"));
        uint binaryHid = ltp.AddItem([0xDE, 0xAD, 0xBE, 0xEF]);

        // A subnode-resident string: the record's HNID is the subnode's NID.
        ulong subnodeData = builder.AddDataBlock(Encoding.Unicode.GetBytes("from-subnode"));
        ulong subnodeTree = builder.AddSubnodeLeafBlock((SubnodeId, subnodeData, 0));

        _ = ltp.AddPropertyContext(
            (Int16Id, 0x0002, 0x1234),
            (Int32Id, 0x0003, 0x89ABCDEF),
            (BooleanId, 0x000B, 1),
            (NullId, 0x0001, 0),
            (Int64Id, 0x0014, int64Hid),
            (GuidId, 0x0048, guidHid),
            (StringId, 0x001F, stringHid),
            (BinaryId, 0x0102, binaryHid),
            (SubnodeStringId, 0x001F, SubnodeId));

        ltp.AddHeapNode(builder, NodeId, subnodeTree);

        PstFile file = PstFile.Open(builder.BuildStream(), new PstFileOptions { ValidationLevel = validationLevel });
        return (file, file.GetNode(new PstNodeId(NodeId)).ReadPropertyContext());
    }
}
