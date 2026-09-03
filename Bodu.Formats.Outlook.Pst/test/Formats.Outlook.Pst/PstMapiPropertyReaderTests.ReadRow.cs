// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstMapiPropertyReaderTests.ReadRow.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Text;
using Bodu.IO.Pst;
using Bodu.IO.Pst.Internal;

namespace Bodu.Formats.Outlook.Pst;

public partial class PstMapiPropertyReaderTests
{
    /// <summary>The node identifier the fixtures use for the table under test.</summary>
    private const uint NodeId = 0x21;

    /// <summary>
    /// Opens a one-row table whose 32-bit integer column is declared two bytes wide, so the cell cannot carry the
    /// value its type promises.
    /// </summary>
    /// <returns>The open file and the row; the caller disposes the file.</returns>
    private static (PstFile File, PstTableRow Row) OpenRowWithNarrowInt32Cell()
    {
        var builder = new PstFixtureBuilder();
        var ltp = new PstLtpFixtureBuilder();

        // Row: id(4) | value(2) | bitmap(1).
        var row = new byte[7];
        BinaryPrimitives.WriteUInt32LittleEndian(row, 1);
        BinaryPrimitives.WriteUInt16LittleEndian(row.AsSpan(4), 0x0203);
        row[6] = 0b1100_0000;
        uint matrixHid = ltp.AddItem(row);

        _ = ltp.AddTableContext(
            [
                (((uint)MapiPropertyIds.LtpRowId << 16) | 0x0003, 0, 4, 0),
                (((uint)MapiPropertyIds.RecipientType << 16) | 0x0003, 4, 2, 1),
            ],
            endOffset4: 4,
            endOffset2: 6,
            endOffset1: 6,
            rowWidth: 7,
            rowsHnid: matrixHid,
            (1, 0));
        ltp.AddHeapNode(builder, NodeId);

        PstFile file = PstFile.Open(builder.BuildStream(), new PstFileOptions());
        return (file, file.GetNode(new PstNodeId(NodeId)).ReadTableContext().EnumerateRows().Single());
    }

    /// <summary>
    /// Verifies that a fixed-width cell narrower than its declared type is omitted under the tolerant levels rather
    /// than zero-extended into a fabricated value.
    /// </summary>
    [TestMethod]
    public void ReadRow_WhenFixedCellIsNarrowerThanItsType_ForCompatible_ShouldOmitProperty()
    {
        (PstFile file, PstTableRow row) = OpenRowWithNarrowInt32Cell();
        using (file)
        {
            MapiPropertyCollection properties = PstMapiPropertyReader.ReadRow(row, Encoding.Unicode, strict: false);

            Assert.IsNull(properties.GetInt32(MapiPropertyIds.RecipientType), "A two-byte PT_LONG cell must not decode to a value.");
            Assert.AreEqual(1, properties.GetInt32(MapiPropertyIds.LtpRowId));
        }
    }

    /// <summary>
    /// Verifies that a fixed-width cell narrower than its declared type is a format error under strict validation.
    /// </summary>
    [TestMethod]
    public void ReadRow_WhenFixedCellIsNarrowerThanItsType_ForStrict_ShouldThrowOutlookPstFormatException()
    {
        (PstFile file, PstTableRow row) = OpenRowWithNarrowInt32Cell();
        using (file)
        {
            _ = Assert.ThrowsExactly<OutlookPstFormatException>(() =>
            {
                _ = PstMapiPropertyReader.ReadRow(row, Encoding.Unicode, strict: true);
            });
        }
    }
}
