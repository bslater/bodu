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

    /// <summary>
    /// Writes one record of every typed kind the writer supports under the version, in a plausible workbook order.
    /// </summary>
    /// <param name="version">The version.</param>
    /// <returns>The stream bytes.</returns>
    private static byte[] WriteEveryRecord(BiffVersion version)
    {
        var output = new ArrayBufferWriter<byte>();
        var writer = new BiffWriter(output, new BiffWriterOptions { Version = version, CodePage = 1252 });
        Assert.IsTrue(BiffRk.TryEncode(-0.25, out uint rk));

        writer.WriteBof(BiffSubstreamType.WorkbookGlobals, build: 1, year: 1997);
        writer.WriteCodePage(1252);
        writer.WriteDateMode(true);
        writer.WriteFont(240, 0x0002, 0x7FFF, 700, 1, 2, 3, 4, "Tahoma");
        writer.WriteFormat(200, "#,##0.00");
        writer.WriteXf(new BiffXfRecord(1, 200, 0x0011));
        writer.WriteBoundSheet(0x1000, BiffSheetState.Hidden, BiffSheetType.Worksheet, "Données");
        if (version == BiffVersion.Biff8)
            writer.WriteSst(["shared", "日本"], totalReferenceCount: 5);
        writer.WriteEof();

        writer.WriteBof(BiffSubstreamType.Worksheet);
        writer.WriteDimensions(new BiffDimensionsRecord(1, 6, 2, 9));
        writer.WriteRow(new BiffRowRecord(1, 2, 9, 300, 0x0140, 0x0001));
        writer.WriteNumber(1, 2, 1, 1234.5678);
        writer.WriteRk(1, 3, 1, rk);
        writer.WriteMulRk(1, 4, [new BiffRkCell(1, 0x06), new BiffRkCell(1, 0x0A)]);
        writer.WriteBlank(1, 6, 1);
        writer.WriteMulBlank(1, 7, [1, 1]);
        writer.WriteBoolean(2, 2, 1, true);
        writer.WriteError(2, 3, 1, 0x1D);
        writer.WriteLabel(2, 4, 1, "inline é");
        if (version == BiffVersion.Biff8)
            writer.WriteLabelSst(2, 5, 1, 1);
        writer.WriteFormula(3, 2, 1, 42.0, [0x1E, 0x2A, 0x00], flags: 0x0002);
        writer.WriteFormula(3, 3, 1, BiffCachedResultKind.String, 0, default);
        writer.WriteString("cached");
        writer.WriteFormula(3, 4, 1, BiffCachedResultKind.Boolean, 1, default);
        writer.WriteFormula(3, 5, 1, BiffCachedResultKind.Error, 0x07, default);
        writer.WriteFormula(3, 6, 1, BiffCachedResultKind.Empty, 0, default);
        writer.WriteEof();

        Assert.AreEqual(0, writer.OpenSubstreamDepth);
        Assert.AreEqual(output.WrittenCount, writer.BytesCommitted);
        return output.WrittenSpan.ToArray();
    }

    /// <summary>
    /// Verifies that every typed record round-trips through the writer and reader under both versions, with the
    /// version-specific layouts and text encodings applied.
    /// </summary>
    /// <param name="version">The version.</param>
    [TestMethod]
    [DataRow(BiffVersion.Biff5)]
    [DataRow(BiffVersion.Biff8)]
    public void RoundTrip_WhenEveryTypedRecord_ShouldDecodeBack(BiffVersion version)
    {
        byte[] stream = WriteEveryRecord(version);
        var reader = new BiffReader(stream);
        var seen = new List<BiffRecordType>();
        string[] shared = [];
        var texts = new List<string>();
        var kinds = new List<BiffCachedResultKind>();

        while (reader.Read())
        {
            seen.Add(reader.RecordType);
            switch (reader.RecordType)
            {
                case BiffRecordType.Bof:
                    Assert.AreEqual(version, reader.GetBof().Version);
                    break;
                case BiffRecordType.CodePage:
                    Assert.AreEqual(1252, reader.GetCodePage().CodePage);
                    break;
                case BiffRecordType.DateMode:
                    Assert.IsTrue(reader.GetDateMode().Is1904);
                    break;
                case BiffRecordType.Font:
                    BiffFontRecord font = reader.GetFont();
                    Assert.AreEqual("Tahoma", font.Name.GetString());
                    Assert.IsTrue(font.IsBold);
                    Assert.IsTrue(font.IsItalic);
                    break;
                case BiffRecordType.Format:
                    Assert.AreEqual("#,##0.00", reader.GetFormat().Code.GetString());
                    break;
                case BiffRecordType.Xf:
                    Assert.AreEqual(version == BiffVersion.Biff8 ? 20 : 16, reader.RecordLength);
                    Assert.AreEqual(200, reader.GetXf().FormatIndex);
                    break;
                case BiffRecordType.BoundSheet:
                    BiffBoundSheetRecord sheet = reader.GetBoundSheet();
                    Assert.AreEqual("Données", sheet.Name.GetString());
                    Assert.AreEqual(BiffSheetState.Hidden, sheet.State);
                    Assert.AreEqual(0x1000u, sheet.StreamOffset);
                    break;
                case BiffRecordType.Sst:
                    var table = new BiffSstReader(ref reader);
                    var list = new List<string>();
                    while (table.Read(ref reader))
                        list.Add(table.GetString());
                    shared = [.. list];
                    break;
                case BiffRecordType.Dimensions:
                    Assert.AreEqual(version == BiffVersion.Biff8 ? 14 : 10, reader.RecordLength);
                    Assert.AreEqual(new BiffDimensionsRecord(1, 6, 2, 9), reader.GetDimensions());
                    break;
                case BiffRecordType.Row:
                    Assert.AreEqual(300, reader.GetRow().Height);
                    Assert.IsTrue(reader.GetRow().HasCustomHeight);
                    break;
                case BiffRecordType.Number:
                    Assert.AreEqual(1234.5678, reader.GetNumber().Value);
                    break;
                case BiffRecordType.Rk:
                    Assert.AreEqual(-0.25, reader.GetRk().Value);
                    break;
                case BiffRecordType.MulRk:
                    Assert.AreEqual(2.0, reader.GetMulRk()[1].Value);
                    Assert.AreEqual(5, reader.GetMulRk().LastColumn);
                    break;
                case BiffRecordType.Blank:
                    Assert.AreEqual(6, reader.GetBlank().Column);
                    break;
                case BiffRecordType.MulBlank:
                    Assert.AreEqual(8, reader.GetMulBlank().LastColumn);
                    break;
                case BiffRecordType.BoolErr:
                    BiffBoolErrRecord flag = reader.GetBoolErr();
                    Assert.AreEqual(flag.IsError ? (byte)0x1D : (byte)1, flag.RawValue);
                    break;
                case BiffRecordType.Label:
                    texts.Add(reader.GetLabel().Text.GetString());
                    break;
                case BiffRecordType.LabelSst:
                    texts.Add(shared[reader.GetLabelSst().SstIndex]);
                    break;
                case BiffRecordType.Formula:
                    kinds.Add(reader.GetFormula().CachedResultKind);
                    break;
                case BiffRecordType.String:
                    texts.Add(reader.GetString().Text.GetString());
                    break;
                default:
                    break;
            }
        }

        Assert.AreEqual(version, reader.Version);
        Assert.AreEqual(2, seen.Count(t => t == BiffRecordType.Eof));
        Assert.AreEqual(5, kinds.Count);
        CollectionAssert.AreEqual(new[] { BiffCachedResultKind.Number, BiffCachedResultKind.String, BiffCachedResultKind.Boolean, BiffCachedResultKind.Error, BiffCachedResultKind.Empty }, kinds);
        if (version == BiffVersion.Biff8)
        {
            CollectionAssert.AreEqual(new[] { "shared", "日本" }, shared);
            CollectionAssert.AreEqual(new[] { "inline é", "日本", "cached" }, texts);
        }
        else
        {
            Assert.IsFalse(seen.Contains(BiffRecordType.Sst));
            Assert.IsFalse(seen.Contains(BiffRecordType.LabelSst));
            CollectionAssert.AreEqual(new[] { "inline é", "cached" }, texts);
        }
    }

    /// <summary>
    /// Verifies that a writer-produced workbook stream, including a shared string table with continuations, reads
    /// identically whatever the block size it is fed in.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void RoundTrip_WhenStreamIsFedInEveryChunkSize_ShouldReadIdentically()
    {
        var output = new ArrayBufferWriter<byte>();
        var writer = new BiffWriter(output, BiffVersion.Biff8);
        writer.WriteBof(BiffSubstreamType.WorkbookGlobals);
        writer.WriteSst([.. Enumerable.Range(0, 1200).Select(i => i % 5 == 0 ? $"日本{i}" : $"s{i}")]);
        writer.WriteEof();
        byte[] stream = output.WrittenSpan.ToArray();

        List<(ushort Id, int Length)> expected = ReadFramesInChunks(stream, stream.Length);
        Assert.IsGreaterThan(3, expected.Count, "The table spans continuation records.");

        for (int chunkSize = 1; chunkSize <= stream.Length; chunkSize += chunkSize < 64 ? 1 : 97)
            CollectionAssert.AreEqual(expected, ReadFramesInChunks(stream, chunkSize), $"Chunk size {chunkSize} changed the frames.");
    }

    /// <summary>
    /// Frames the stream through readers fed the given number of new bytes per pass.
    /// </summary>
    /// <param name="stream">The stream bytes.</param>
    /// <param name="chunkSize">The number of new bytes per pass.</param>
    /// <returns>The identifier and length of every record, in order.</returns>
    private static List<(ushort Id, int Length)> ReadFramesInChunks(byte[] stream, int chunkSize)
    {
        var frames = new List<(ushort, int)>();
        BiffReaderState state = default;
        int consumed = 0;
        int available = 0;
        while (consumed < stream.Length)
        {
            available = Math.Min(stream.Length, available + chunkSize);
            var reader = new BiffReader(stream.AsSpan(consumed, available - consumed), available == stream.Length, state);
            while (reader.Read())
                frames.Add((reader.RecordId, reader.RecordLength));

            consumed += reader.BytesConsumed;
            state = reader.CurrentState;
        }

        return frames;
    }
}
