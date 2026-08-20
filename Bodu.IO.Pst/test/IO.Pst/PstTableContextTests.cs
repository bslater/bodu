// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstTableContextTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Text;
using Bodu.IO.Pst.Internal;

namespace Bodu.IO.Pst;

/// <summary>
/// Verifies <see cref="PstTableContext" />, the LTP table. This root holds the shared fixture — a two-row table with
/// fixed, variable, and absent cells over a heap-resident row matrix — and the member partials assert each surface's
/// contract over it.
/// </summary>
[TestClass]
public partial class PstTableContextTests
{
    /// <summary>The node identifier the fixtures use for the node under test.</summary>
    private const uint NodeId = 0x21;

    /// <summary>The row-identifier column's tag.</summary>
    private const uint RowIdTag = 0x67F40003;

    /// <summary>The 32-bit integer column's tag.</summary>
    private const uint Int32Tag = 0x11110003;

    /// <summary>The UTF-16LE string column's tag.</summary>
    private const uint StringTag = 0x2222001F;

    /// <summary>The 16-bit integer column's tag.</summary>
    private const uint Int16Tag = 0x33330002;

    /// <summary>The Boolean column's tag.</summary>
    private const uint BooleanTag = 0x4444000B;

    /// <summary>The identifier of the first fixture row.</summary>
    private const uint FirstRowId = 0x100;

    /// <summary>The identifier of the second fixture row.</summary>
    private const uint SecondRowId = 0x200;

    /// <summary>The shared fixture's row width: three dwords, one 2-byte cell, one 1-byte cell, and the bitmap byte.</summary>
    private const int RowWidth = 16;

    /// <summary>
    /// Builds one 16-byte fixture row.
    /// </summary>
    /// <param name="rowId">The row identifier.</param>
    /// <param name="int32Value">The 32-bit cell value.</param>
    /// <param name="stringHnid">The string cell's value reference.</param>
    /// <param name="int16Value">The 16-bit cell value.</param>
    /// <param name="booleanValue">The Boolean cell byte.</param>
    /// <param name="bitmap">The existence bitmap byte (bits are most-significant first).</param>
    /// <returns>The row bytes.</returns>
    private static byte[] Row(uint rowId, int int32Value, uint stringHnid, short int16Value, byte booleanValue, byte bitmap)
    {
        var row = new byte[RowWidth];
        BinaryPrimitives.WriteUInt32LittleEndian(row, rowId);
        BinaryPrimitives.WriteInt32LittleEndian(row.AsSpan(4), int32Value);
        BinaryPrimitives.WriteUInt32LittleEndian(row.AsSpan(8), stringHnid);
        BinaryPrimitives.WriteInt16LittleEndian(row.AsSpan(12), int16Value);
        row[14] = booleanValue;
        row[15] = bitmap;
        return row;
    }

    /// <summary>
    /// Opens the shared table fixture for the row tests, which assert cell semantics over the same two-row table.
    /// </summary>
    /// <returns>The open file and the context; the caller disposes the file.</returns>
    internal static (PstFile File, PstTableContext Context) OpenContextForRowTests() =>
        OpenSharedContext();

    /// <summary>
    /// Builds the shared fixture and opens the table context of the node under test: two rows over a heap-resident
    /// matrix, with the second row's string cell marked absent.
    /// </summary>
    /// <returns>The open file and the context; the caller disposes the file.</returns>
    private static (PstFile File, PstTableContext Context) OpenSharedContext()
    {
        var builder = new PstFixtureBuilder();
        var ltp = new PstLtpFixtureBuilder();

        uint alphaHid = ltp.AddItem(Encoding.Unicode.GetBytes("alpha"));

        byte[] matrix =
        [
            .. Row(FirstRowId, 7, alphaHid, 0x0102, 1, 0b1111_1000),
            .. Row(SecondRowId, 9, 0, 0x0304, 0, 0b1101_1000),
        ];
        uint matrixHid = ltp.AddItem(matrix);

        _ = ltp.AddTableContext(
            [
                (RowIdTag, 0, 4, 0),
                (Int32Tag, 4, 4, 1),
                (StringTag, 8, 4, 2),
                (Int16Tag, 12, 2, 3),
                (BooleanTag, 14, 1, 4),
            ],
            endOffset4: 12,
            endOffset2: 14,
            endOffset1: 15,
            rowWidth: RowWidth,
            rowsHnid: matrixHid,
            (FirstRowId, 0),
            (SecondRowId, 1));

        ltp.AddHeapNode(builder, NodeId);

        PstFile file = PstFile.Open(builder.BuildStream(), PstFileOptions.Default);
        return (file, file.GetNode(new PstNodeId(NodeId)).ReadTableContext());
    }
}
