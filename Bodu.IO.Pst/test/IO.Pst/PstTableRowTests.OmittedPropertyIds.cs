// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstTableRowTests.OmittedPropertyIds.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Text;
using Bodu.IO.Pst.Internal;

namespace Bodu.IO.Pst;

/// <summary>
/// Verifies <see cref="PstTableRow.OmittedPropertyIds" /> and the row a session that omits unresolvable values
/// (<see cref="PstFileOptions.OmitUnresolvableValues" />) produces: a cell whose reference does not resolve is absent
/// from every member, and the default session still rejects it.
/// </summary>
public partial class PstTableRowTests
{
    /// <summary>The node identifier of the damaged table fixture.</summary>
    private const uint DamagedTableNodeId = 0x21;

    /// <summary>The property identifier of the damaged fixture's 32-bit column.</summary>
    private const ushort Int32ColumnId = 0x1111;

    /// <summary>The property identifier of the damaged fixture's first string column.</summary>
    private const ushort FirstStringColumnId = 0x2222;

    /// <summary>The property identifier of the damaged fixture's second string column.</summary>
    private const ushort SecondStringColumnId = 0x5555;

    /// <summary>A heap identifier past the fixture heap's last allocation (item 15 of block 0).</summary>
    private const uint DanglingHid = 0x000001E0;

    /// <summary>
    /// Verifies that a row read without omitting unresolvable values lists no omitted cell.
    /// </summary>
    [TestMethod]
    public void OmittedPropertyIds_WhenSessionDoesNotOmit_ShouldBeEmpty()
    {
        (PstFile file, PstTableContext context) = PstTableContextTests.OpenContextForRowTests();
        using (file)
        {
            Assert.AreEqual(0, context.EnumerateRows().First().OmittedPropertyIds.Count);
        }
    }

    /// <summary>
    /// Verifies that a row whose every present cell resolves omits nothing when the session omits unresolvable values.
    /// </summary>
    [TestMethod]
    public void OmittedPropertyIds_WhenEveryReferenceResolves_ForOmittingSession_ShouldBeEmpty()
    {
        (PstFile file, PstTableRow row) = OpenDamagedRow(omitUnresolvableValues: true, secondStringHnid: null);
        using (file)
        {
            Assert.AreEqual(0, row.OmittedPropertyIds.Count);
            Assert.IsTrue(row.TryGetCell(SecondStringColumnId, out _));
        }
    }

    /// <summary>
    /// Verifies that a cell whose heap identifier lies outside the heap is omitted, and the row's other cells stay
    /// readable.
    /// </summary>
    [TestMethod]
    public void OmittedPropertyIds_WhenCellReferenceDoesNotResolve_ForOmittingSession_ShouldListTheCell()
    {
        (PstFile file, PstTableRow row) = OpenDamagedRow(omitUnresolvableValues: true, secondStringHnid: DanglingHid);
        using (file)
        {
            CollectionAssert.AreEqual(new ushort[] { SecondStringColumnId }, row.OmittedPropertyIds.ToArray());

            Assert.IsTrue(row.TryGetCell(FirstStringColumnId, out PstPropertyValue first));
            Assert.AreEqual("first", first.GetString());
            Assert.IsTrue(row.TryGetCell(Int32ColumnId, out PstPropertyValue int32Value));
            Assert.AreEqual(7, int32Value.GetInt32());
        }
    }

    /// <summary>
    /// Verifies that an omitted cell is absent from every member of the row: lookup, length, stream, and enumeration.
    /// </summary>
    [TestMethod]
    public void OmittedPropertyIds_WhenACellIsOmitted_ShouldBeAbsentFromEveryMember()
    {
        (PstFile file, PstTableRow row) = OpenDamagedRow(omitUnresolvableValues: true, secondStringHnid: DanglingHid);
        using (file)
        {
            Assert.IsFalse(row.TryGetCell(SecondStringColumnId, out _));
            Assert.IsFalse(row.TryGetCellLength(SecondStringColumnId, out _));
            Assert.IsFalse(row.TryOpenCellStream(SecondStringColumnId, out _));
            CollectionAssert.DoesNotContain(row.EnumerateCells().Select(static cell => cell.PropertyId).ToArray(), SecondStringColumnId);
        }
    }

    /// <summary>
    /// Verifies that the default session still fails to read a cell whose heap identifier lies outside the heap, with
    /// <see cref="PstFileError.InvalidHeap" />.
    /// </summary>
    [TestMethod]
    public void TryGetCell_WhenCellReferenceDoesNotResolve_ShouldThrowPstFileFormatException()
    {
        (PstFile file, PstTableRow row) = OpenDamagedRow(omitUnresolvableValues: false, secondStringHnid: DanglingHid);
        using (file)
        {
            Assert.AreEqual(0, row.OmittedPropertyIds.Count);

            PstFileFormatException ex = Assert.ThrowsExactly<PstFileFormatException>(() =>
            {
                _ = row.TryGetCell(SecondStringColumnId, out _);
            });

            Assert.AreEqual(PstFileError.InvalidHeap, ex.Error);
        }
    }

    /// <summary>
    /// Builds a one-row table with a 32-bit cell and two string cells, the second naming the supplied reference, and
    /// opens its row.
    /// </summary>
    /// <param name="omitUnresolvableValues">Whether the session omits unresolvable values.</param>
    /// <param name="secondStringHnid">
    /// The second string cell's value reference; <see langword="null" /> for a resolvable one.
    /// </param>
    /// <returns>The open file and the row; the caller disposes the file.</returns>
    private static (PstFile File, PstTableRow Row) OpenDamagedRow(bool omitUnresolvableValues, uint? secondStringHnid)
    {
        var builder = new PstFixtureBuilder();
        var ltp = new PstLtpFixtureBuilder();

        uint firstHid = ltp.AddItem(Encoding.Unicode.GetBytes("first"));
        uint secondHid = ltp.AddItem(Encoding.Unicode.GetBytes("second"));

        // Row identifier, the 32-bit cell, the two string references, then the existence bitmap with all four bits set.
        var row = new byte[17];
        BinaryPrimitives.WriteUInt32LittleEndian(row, 0x100);
        BinaryPrimitives.WriteInt32LittleEndian(row.AsSpan(4), 7);
        BinaryPrimitives.WriteUInt32LittleEndian(row.AsSpan(8), firstHid);
        BinaryPrimitives.WriteUInt32LittleEndian(row.AsSpan(12), secondStringHnid ?? secondHid);
        row[16] = 0b1111_0000;
        uint matrixHid = ltp.AddItem(row);

        _ = ltp.AddTableContext(
            [
                (0x67F40003, 0, 4, 0),
                (((uint)Int32ColumnId << 16) | 0x0003, 4, 4, 1),
                (((uint)FirstStringColumnId << 16) | 0x001F, 8, 4, 2),
                (((uint)SecondStringColumnId << 16) | 0x001F, 12, 4, 3),
            ],
            endOffset4: 16,
            endOffset2: 16,
            endOffset1: 16,
            rowWidth: 17,
            rowsHnid: matrixHid,
            (0x100, 0));
        ltp.AddHeapNode(builder, DamagedTableNodeId);

        PstFile file = PstFile.Open(builder.BuildStream(), new PstFileOptions { OmitUnresolvableValues = omitUnresolvableValues });
        PstTableRow tableRow = file.GetNode(new PstNodeId(DamagedTableNodeId)).ReadTableContext().EnumerateRows().Single();
        return (file, tableRow);
    }
}
