// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiffRoundTripTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;

namespace Bodu.IO.Biff;

/// <summary>
/// Round-trip tests: a workbook-shaped stream written by <see cref="BiffWriter" /> reads back through
/// <see cref="BiffReader" /> without semantic change, under both versions.
/// </summary>
[TestClass]
public sealed class BiffRoundTripTests
{
    /// <summary>
    /// Verifies that a globals substream followed by a worksheet substream round-trips under BIFF8, including the
    /// shared string table and a bound-sheet offset computed from the writer's byte count.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void RoundTrip_WhenBiff8Workbook_ShouldReadBackEveryRecord()
    {
        var output = new ArrayBufferWriter<byte>();
        var writer = new BiffWriter(output, BiffVersion.Biff8);

        // Globals: the bound-sheet offset is patched after the sheet position is known.
        writer.WriteBof(BiffSubstreamType.WorkbookGlobals);
        writer.WriteCodePage(1200);
        writer.WriteDateMode(false);
        writer.WriteFont(200, 0, 0x7FFF, 400, 0, 0, 0, 0, "Arial");
        writer.WriteFormat(164, "0.00");
        writer.WriteXf(new BiffXfRecord(0, 164, 0x0001));
        long boundSheetOffset = writer.BytesCommitted;
        writer.WriteBoundSheet(0, BiffSheetState.Visible, BiffSheetType.Worksheet, "Sheet1");
        writer.WriteSst(["alpha", "beta"]);
        writer.WriteEof();

        long sheetOffset = writer.BytesCommitted;
        writer.WriteBof(BiffSubstreamType.Worksheet);
        writer.WriteDimensions(new BiffDimensionsRecord(0, 2, 0, 3));
        writer.WriteRow(new BiffRowRecord(0, 0, 3, 0x00FF, 0, 0));
        writer.WriteLabelSst(0, 0, 0, 1);
        writer.WriteNumber(0, 1, 0, 1.5);
        Assert.IsTrue(BiffRk.TryEncode(42, out uint rk));
        writer.WriteRk(0, 2, 0, rk);
        writer.WriteFormula(1, 0, 0, BiffCachedResultKind.String, 0, [0x17, 0x03, 0x00, (byte)'a', (byte)'b', (byte)'c']);
        writer.WriteString("abc");
        writer.WriteBoolean(1, 1, 0, true);
        writer.WriteEof();

        byte[] stream = output.WrittenSpan.ToArray();
        new BiffRecordHeader((ushort)BiffRecordType.BoundSheet, 0).WriteTo(stream.AsSpan((int)boundSheetOffset));
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(stream.AsSpan((int)boundSheetOffset + 4), (uint)sheetOffset);
        System.Buffers.Binary.BinaryPrimitives.WriteUInt16LittleEndian(stream.AsSpan((int)boundSheetOffset + 2), (ushort)(6 + 1 + 1 + 6));

        var reader = new BiffReader(stream);
        var seen = new List<BiffRecordType>();
        string[]? sharedStrings = null;
        uint? declaredSheetOffset = null;
        while (reader.Read())
        {
            seen.Add(reader.RecordType);
            switch (reader.RecordType)
            {
                case BiffRecordType.BoundSheet:
                    BiffBoundSheetRecord sheet = reader.GetBoundSheet();
                    declaredSheetOffset = sheet.StreamOffset;
                    Assert.AreEqual("Sheet1", sheet.Name.GetString());
                    break;
                case BiffRecordType.Sst:
                    var strings = new BiffSstReader(ref reader);
                    var list = new List<string>();
                    while (strings.Read(ref reader))
                        list.Add(strings.GetString());
                    sharedStrings = [.. list];
                    break;
                case BiffRecordType.LabelSst:
                    Assert.AreEqual("beta", sharedStrings![reader.GetLabelSst().SstIndex]);
                    break;
                case BiffRecordType.Number:
                    Assert.AreEqual(1.5, reader.GetNumber().Value);
                    break;
                case BiffRecordType.Rk:
                    Assert.AreEqual(42.0, reader.GetRk().Value);
                    break;
                case BiffRecordType.String:
                    Assert.AreEqual("abc", reader.GetString().Text.GetString());
                    break;
                case BiffRecordType.Format:
                    Assert.AreEqual("0.00", reader.GetFormat().Code.GetString());
                    break;
                case BiffRecordType.Font:
                    Assert.AreEqual("Arial", reader.GetFont().Name.GetString());
                    break;
                default:
                    break;
            }
        }

        Assert.AreEqual((uint)sheetOffset, declaredSheetOffset);
        Assert.AreEqual(BiffVersion.Biff8, reader.Version);
        Assert.AreEqual(1200, reader.CodePage);
        CollectionAssert.AreEqual(
            new[]
            {
                BiffRecordType.Bof, BiffRecordType.CodePage, BiffRecordType.DateMode, BiffRecordType.Font, BiffRecordType.Format,
                BiffRecordType.Xf, BiffRecordType.BoundSheet, BiffRecordType.Sst, BiffRecordType.Eof, BiffRecordType.Bof,
                BiffRecordType.Dimensions, BiffRecordType.Row, BiffRecordType.LabelSst, BiffRecordType.Number, BiffRecordType.Rk,
                BiffRecordType.Formula, BiffRecordType.String, BiffRecordType.BoolErr, BiffRecordType.Eof,
            },
            seen);
    }

    /// <summary>
    /// Verifies that a BIFF5 workbook with code-page text round-trips, with the reader picking the code page up from
    /// the CODEPAGE record the writer emitted.
    /// </summary>
    [TestMethod]
    public void RoundTrip_WhenBiff5Workbook_ShouldReadBackCodePageText()
    {
        var output = new ArrayBufferWriter<byte>();
        var writer = new BiffWriter(output, new BiffWriterOptions { Version = BiffVersion.Biff5, CodePage = 1252 });

        writer.WriteBof(BiffSubstreamType.WorkbookGlobals);
        writer.WriteCodePage(1252);
        writer.WriteBoundSheet(0, BiffSheetState.Visible, BiffSheetType.Worksheet, "Feuille");
        writer.WriteFormat(164, "0.00€");
        writer.WriteEof();
        writer.WriteBof(BiffSubstreamType.Worksheet);
        writer.WriteDimensions(new BiffDimensionsRecord(0, 1, 0, 2));
        writer.WriteLabel(0, 0, 0, "café");
        writer.WriteFormula(0, 1, 0, BiffCachedResultKind.String, 0, default);
        writer.WriteString("été");
        writer.WriteEof();

        var reader = new BiffReader(output.WrittenSpan);
        var texts = new List<string>();
        while (reader.Read())
        {
            switch (reader.RecordType)
            {
                case BiffRecordType.BoundSheet:
                    texts.Add(reader.GetBoundSheet().Name.GetString());
                    break;
                case BiffRecordType.Format:
                    texts.Add(reader.GetFormat().Code.GetString());
                    break;
                case BiffRecordType.Label:
                    texts.Add(reader.GetLabel().Text.GetString());
                    break;
                case BiffRecordType.String:
                    texts.Add(reader.GetString().Text.GetString());
                    break;
                case BiffRecordType.Dimensions:
                    Assert.AreEqual(10, reader.RecordLength);
                    break;
                default:
                    break;
            }
        }

        Assert.AreEqual(BiffVersion.Biff5, reader.Version);
        CollectionAssert.AreEqual(new[] { "Feuille", "0.00€", "café", "été" }, texts);
    }

    /// <summary>
    /// Verifies that a stream copied record by record through the raw surface is byte-identical, including records
    /// the codec does not name.
    /// </summary>
    [TestMethod]
    public void RoundTrip_WhenCopyingRawRecords_ShouldBeByteIdentical()
    {
        byte[] original = BiffTestRecords.Stream(
            BiffTestRecords.Bof8(),
            BiffTestRecords.Record(0x0FFE, [1, 2, 3]),
            BiffTestRecords.Sst(1, 1, BiffTestRecords.UnicodeString("s")),
            BiffTestRecords.Continue(9, 9),
            BiffTestRecords.Eof());

        var output = new ArrayBufferWriter<byte>();
        var writer = new BiffWriter(output, BiffVersion.Biff8);
        var reader = new BiffReader(original);
        while (reader.Read())
            writer.WriteRecord(reader.RecordId, reader.ValueSpan);

        CollectionAssert.AreEqual(original, output.WrittenSpan.ToArray());
    }
}
