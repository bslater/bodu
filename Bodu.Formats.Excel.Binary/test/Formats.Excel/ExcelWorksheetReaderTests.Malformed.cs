// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExcelWorksheetReaderTests.Malformed.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Kat;

namespace Bodu.Formats.Excel;

public partial class ExcelWorksheetReaderTests
{
    /// <summary>
    /// Gets the malformed-record known-answer rows, each wrapped as a single-element <c>object[]</c> for
    /// <c>[DynamicData]</c>.
    /// </summary>
    /// <returns>A sequence of rows, each carrying one <see cref="MalformedRecordKat" />.</returns>
    public static IEnumerable<object[]> MalformedRecords
    {
        get
        {
            foreach (MalformedRecordKat kat in MalformedCatalogue)
                yield return [kat];
        }
    }

    /// <summary>The immutable catalogue of malformed-record rows.</summary>
    private static IReadOnlyList<MalformedRecordKat> MalformedCatalogue { get; } =
    [
        new("truncated NUMBER record", [Biff8TestWorkbook.Record(0x0203, new byte[8])]),
        new("truncated RK record", [Biff8TestWorkbook.Record(0x027E, new byte[6])]),
        new("truncated LABELSST record", [Biff8TestWorkbook.Record(0x00FD, new byte[6])]),
        new("truncated BOOLERR record", [Biff8TestWorkbook.Record(0x0205, new byte[4])]),
        new("truncated FORMULA record", [Biff8TestWorkbook.Record(0x0006, new byte[8])]),

        // 6 header bytes + 5 run bytes is not a whole number of (ixfe + rk) entries.
        new("MULRK run not a whole number of entries", [Biff8TestWorkbook.Record(0x00BD, new byte[11])]),

        // The reader's shared string table is empty, so index 0 is out of range.
        new("LABELSST index out of range", [Biff8TestWorkbook.LabelSst(0, 0, 0)]),
    ];

    /// <summary>
    /// Verifies that a malformed cell record is rejected with <see cref="ExcelBinaryFormatException" />.
    /// </summary>
    /// <param name="kat">The malformed-record row under test.</param>
    [TestMethod]
    [DynamicData(
        nameof(MalformedRecords),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void TryReadCell_WhenRecordMalformed_ShouldThrowFormatException(MalformedRecordKat kat)
    {
        _ = Assert.ThrowsExactly<ExcelBinaryFormatException>(() =>
        {
            _ = ReadGrid(kat.Records);
        });
    }

    /// <summary>
    /// Verifies that a substream ending in a trailing fragment too short to form a record header throws
    /// <see cref="ExcelBinaryFormatException" />.
    /// </summary>
    [TestMethod]
    public void TryReadCell_WhenTrailingBytesAfterLastRecord_ShouldThrowFormatException()
    {
        byte[] substream = Biff8TestWorkbook.Concat(
            Biff8TestWorkbook.Bof(Biff8TestWorkbook.BofWorksheet),
            Biff8TestWorkbook.Number(0, 0, 1.0),
            new byte[] { 0x01, 0x02 });

        using ExcelWorksheetReader reader = Biff8TestWorkbook.OpenWorksheetReaderRaw(substream);

        Assert.IsTrue(reader.TryReadCell(out _));
        _ = Assert.ThrowsExactly<ExcelBinaryFormatException>(() =>
        {
            _ = reader.TryReadCell(out _);
        });
    }
}
