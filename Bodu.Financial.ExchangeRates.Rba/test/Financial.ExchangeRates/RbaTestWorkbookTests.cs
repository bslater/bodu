// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RbaTestWorkbookTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using Bodu.IO.Biff;
using Bodu.IO.Compound;

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Verifies that <see cref="RbaTestWorkbook" /> generates workbooks laid out and encoded as the RBA's F11.1 files are,
/// and plants the cells the parser must cope with, so that the tests reading them exercise what they claim to.
/// </summary>
[TestClass]
public partial class RbaTestWorkbookTests
{
    /// <summary>The substream index of the data sheet in a generated workbook stream, after the globals.</summary>
    private const int DataSubstream = 1;

    /// <summary>
    /// Gets the label of each era of the default catalogue, as dynamic-data rows.
    /// </summary>
    /// <value>One object array holding an era label per era.</value>
    public static IEnumerable<object[]> Eras =>
        RbaEraWorkbook.Default.Select(static era => new object[] { era.Label });

    /// <summary>
    /// Returns the era of the default catalogue that has a label.
    /// </summary>
    /// <param name="label">The era label.</param>
    /// <returns>The era.</returns>
    private static RbaEraWorkbook GetEra(string label) =>
        RbaEraWorkbook.Default.Single(era => string.Equals(era.Label, label, StringComparison.Ordinal));

    /// <summary>
    /// Reads the workbook stream of a generated compound file.
    /// </summary>
    /// <param name="file">The bytes of the compound file.</param>
    /// <returns>The bytes of its <c>Workbook</c> stream.</returns>
    private static byte[] ReadWorkbookStream(byte[] file)
    {
        using CompoundFile compound = CompoundFile.Open(new MemoryStream(file, writable: false));
        using CompoundStream stream = compound.RootStorage.OpenStream(RbaTestWorkbook.WorkbookStreamName);
        using MemoryStream buffer = new();
        stream.CopyTo(buffer);

        return buffer.ToArray();
    }

    /// <summary>
    /// Lists the records of one substream of a workbook stream, with the first cell each cell record addresses.
    /// </summary>
    /// <param name="workbookStream">The workbook stream.</param>
    /// <param name="substream">The zero-based index of the substream: 0 the globals, then each sheet in order.</param>
    /// <returns>
    /// Each record's type, row and column, the row and column being those of the record's first cell, or -1 for a
    /// record that addresses no cell.
    /// </returns>
    private static List<(BiffRecordType Type, int Row, int Column)> ReadRecords(byte[] workbookStream, int substream)
    {
        List<(BiffRecordType Type, int Row, int Column)> records = [];
        BiffReader reader = new(workbookStream);
        int index = -1;
        while (reader.Read())
        {
            if (reader.RecordType == BiffRecordType.Bof)
                index++;

            if (index != substream)
                continue;

            ReadOnlySpan<byte> payload = reader.ValueSpan;
            bool isCell = reader.RecordType is BiffRecordType.LabelSst or BiffRecordType.Number or BiffRecordType.Rk
                or BiffRecordType.MulRk or BiffRecordType.Blank or BiffRecordType.MulBlank or BiffRecordType.Formula;
            records.Add(isCell
                ? (reader.RecordType, BinaryPrimitives.ReadUInt16LittleEndian(payload), BinaryPrimitives.ReadUInt16LittleEndian(payload[2..]))
                : (reader.RecordType, -1, -1));
        }

        return records;
    }

    /// <summary>
    /// Reads the code page the globals of a workbook stream declare.
    /// </summary>
    /// <param name="workbookStream">The workbook stream.</param>
    /// <returns>The code page of the first <c>CODEPAGE</c> record, or -1 when there is none.</returns>
    private static int ReadCodePage(byte[] workbookStream)
    {
        BiffReader reader = new(workbookStream);
        while (reader.Read())
        {
            if (reader.RecordType == BiffRecordType.CodePage)
                return reader.GetCodePage().CodePage;
        }

        return -1;
    }
}
