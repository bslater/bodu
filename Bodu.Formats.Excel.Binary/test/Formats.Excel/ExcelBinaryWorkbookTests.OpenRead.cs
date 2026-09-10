// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExcelBinaryWorkbookTests.OpenRead.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Biff;
using Bodu.IO.Compound;
using Bodu.Test;

namespace Bodu.Formats.Excel;

public partial class ExcelBinaryWorkbookTests
{
    /// <summary>
    /// Verifies that opening the sample workbook exposes its two worksheets in order.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Smoke)]
    public void OpenRead_WhenSampleWorkbook_ShouldExposeDataAndNotesSheets()
    {
        using ExcelBinaryWorkbook workbook = OpenSample();

        var names = workbook.Worksheets.Select(s => s.Name).ToList();

        CollectionAssert.AreEqual(new[] { "Data", "Notes" }, names);
        Assert.IsTrue(workbook.Worksheets.All(s => s.IsVisible));
    }

    /// <summary>
    /// Verifies that opening a <see langword="null" /> stream throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void OpenRead_WhenStreamIsNull_ShouldThrowArgumentNullException()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = ExcelBinaryWorkbook.OpenRead((Stream)null!);
        });

        Assert.AreEqual("stream", ex.ParamName);
    }

    /// <summary>
    /// Verifies that opening a <see langword="null" /> path throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void OpenRead_WhenPathIsNull_ShouldThrowArgumentNullException()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = ExcelBinaryWorkbook.OpenRead((string)null!);
        });

        Assert.AreEqual("path", ex.ParamName);
    }

    /// <summary>
    /// Verifies that opening data that is not a compound file throws
    /// <see cref="IO.Compound.CompoundFileFormatException" />.
    /// </summary>
    [TestMethod]
    public void OpenRead_WhenStreamIsNotCompoundFile_ShouldThrowCompoundFileFormatException()
    {
        using MemoryStream stream = new(new byte[600]);

        _ = Assert.ThrowsExactly<IO.Compound.CompoundFileFormatException>(() =>
        {
            _ = ExcelBinaryWorkbook.OpenRead(stream);
        });
    }

    /// <summary>
    /// Verifies that opening a workbook from a file path reads its sheets.
    /// </summary>
    [TestMethod]
    public void OpenRead_WhenPath_ShouldReadWorkbook()
    {
        string path = Path.Combine(Path.GetTempPath(), $"bodu-xls-{Guid.NewGuid():N}.xls");
        using (MemoryStream source = ExcelBinaryFixtures.OpenStream(ExcelBinaryFixtures.SampleBiff8))
            File.WriteAllBytes(path, source.ToArray());

        try
        {
            using var workbook = ExcelBinaryWorkbook.OpenRead(path);
            Assert.HasCount(2, workbook.Worksheets);
        }
        finally
        {
            File.Delete(path);
        }
    }

    /// <summary>
    /// Verifies that opening with <c>leaveOpen: true</c> leaves the source stream open after the workbook is disposed.
    /// </summary>
    [TestMethod]
    public void OpenRead_WhenLeaveOpenTrue_ShouldNotDisposeSourceStream()
    {
        MemoryStream source = ExcelBinaryFixtures.OpenStream(ExcelBinaryFixtures.SampleBiff8);

        using (var workbook = ExcelBinaryWorkbook.OpenRead(source, leaveOpen: true))
            Assert.HasCount(2, workbook.Worksheets);

        // A disposed MemoryStream throws on access; reading the length confirms it is still open.
        Assert.IsGreaterThan(0, source.Length);
        source.Dispose();
    }

    /// <summary>
    /// Verifies that opening with the default ownership disposes the source stream when the workbook is disposed.
    /// </summary>
    [TestMethod]
    public void OpenRead_WhenLeaveOpenFalse_ShouldDisposeSourceStream()
    {
        MemoryStream source = ExcelBinaryFixtures.OpenStream(ExcelBinaryFixtures.SampleBiff8);

        using (var workbook = ExcelBinaryWorkbook.OpenRead(source))
            Assert.HasCount(2, workbook.Worksheets);

        _ = Assert.ThrowsExactly<ObjectDisposedException>(() => _ = source.Length);
    }

    /// <summary>
    /// Verifies that a workbook whose globals contain a file-pass record is reported as encrypted.
    /// </summary>
    [TestMethod]
    public void OpenRead_WhenFilePassPresent_ShouldThrowEncryptedWorkbookException()
    {
        using MemoryStream xls = Biff8TestWorkbook.BuildWorkbook(
            [Biff8TestWorkbook.FilePass()],
            new Biff8TestWorkbook.SheetSpec("Sheet1", 0, 0, [Biff8TestWorkbook.Dimensions(0, 1, 0, 1)]));

        _ = Assert.ThrowsExactly<ExcelBinaryEncryptedWorkbookException>(() =>
        {
            _ = ExcelBinaryWorkbook.OpenRead(xls);
        });
    }

    /// <summary>
    /// Verifies that the unencrypted sample workbook opens without being reported as encrypted.
    /// </summary>
    [TestMethod]
    public void OpenRead_WhenSampleWorkbook_ShouldNotThrowEncryptedWorkbookException()
    {
        using ExcelBinaryWorkbook workbook = OpenSample();

        Assert.IsNotNull(workbook);
    }

    /// <summary>
    /// Verifies that a compatibility-save workbook carrying both <c>Book</c> and <c>Workbook</c> streams reads the
    /// BIFF8 <c>Workbook</c> stream.
    /// </summary>
    [TestMethod]
    public void OpenRead_WhenBothBookAndWorkbookStreams_ShouldReadWorkbookStream()
    {
        byte[] workbookBytes = Biff8TestWorkbook.BuildWorkbookStream(
            [Biff8TestWorkbook.Sst("Only")],
            new Biff8TestWorkbook.SheetSpec("Sheet1", 0, 0, [Biff8TestWorkbook.Dimensions(0, 1, 0, 1), Biff8TestWorkbook.LabelSst(0, 0, 0)]));

        MemoryStream container = new();
        using (var file = CompoundFile.Create(container, leaveOpen: true))
        {
            file.RootStorage.CreateStream("Book", new byte[] { 0x09, 0x08, 0x00, 0x00 });
            file.RootStorage.CreateStream("Workbook", workbookBytes);
            file.Commit();
        }

        container.Position = 0;
        using var workbook = ExcelBinaryWorkbook.OpenRead(container);
        Dictionary<(int Row, int Column), ExcelCell> grid = ReadCellGrid(workbook, "Sheet1");

        Assert.AreEqual("Only", grid[(0, 0)].StringValue);
    }

    /// <summary>
    /// Verifies that a compound file without a workbook stream throws
    /// <see cref="ExcelBinaryWorkbookStreamNotFoundException" />.
    /// </summary>
    [TestMethod]
    public void OpenRead_WhenNoWorkbookStream_ShouldThrowWorkbookStreamNotFoundException()
    {
        MemoryStream container = new();
        using (var file = CompoundFile.Create(container, leaveOpen: true))
        {
            file.RootStorage.CreateStream("NotAWorkbook", new byte[] { 1, 2, 3, 4 });
            file.Commit();
        }

        container.Position = 0;

        _ = Assert.ThrowsExactly<ExcelBinaryWorkbookStreamNotFoundException>(() =>
        {
            _ = ExcelBinaryWorkbook.OpenRead(container);
        });
    }

    /// <summary>
    /// Verifies that a workbook stream whose first BOF opens a worksheet rather than the workbook globals is
    /// rejected, with the substream type in the message.
    /// </summary>
    [TestMethod]
    public void OpenRead_WhenFirstBofIsNotGlobals_ShouldThrowFormatException()
    {
        byte[] stream = Biff8TestWorkbook.Concat(Biff8TestWorkbook.Bof(Biff8TestWorkbook.BofWorksheet), Biff8TestWorkbook.Eof());
        using MemoryStream xls = Biff8TestWorkbook.WrapInCompoundFile(stream, "Workbook");

        var ex = Assert.ThrowsExactly<ExcelBinaryFormatException>(() =>
        {
            _ = ExcelBinaryWorkbook.OpenRead(xls);
        });

        Assert.IsTrue(ex.Message.Contains("0010", StringComparison.OrdinalIgnoreCase), ex.Message);
    }

    /// <summary>
    /// Verifies that a workbook stream whose first record is not a BOF is rejected.
    /// </summary>
    [TestMethod]
    public void OpenRead_WhenFirstRecordIsNotBof_ShouldThrowFormatException()
    {
        byte[] stream = Biff8TestWorkbook.Concat(Biff8TestWorkbook.DateMode(false), Biff8TestWorkbook.Bof(Biff8TestWorkbook.BofGlobals), Biff8TestWorkbook.Eof());
        using MemoryStream xls = Biff8TestWorkbook.WrapInCompoundFile(stream, "Workbook");

        _ = Assert.ThrowsExactly<ExcelBinaryFormatException>(() =>
        {
            _ = ExcelBinaryWorkbook.OpenRead(xls);
        });
    }

    /// <summary>
    /// Verifies that an empty workbook stream is rejected as malformed rather than opening with no sheets.
    /// </summary>
    [TestMethod]
    public void OpenRead_WhenWorkbookStreamIsEmpty_ShouldThrowFormatException()
    {
        using MemoryStream xls = Biff8TestWorkbook.WrapInCompoundFile([], "Workbook");

        _ = Assert.ThrowsExactly<ExcelBinaryFormatException>(() =>
        {
            _ = ExcelBinaryWorkbook.OpenRead(xls);
        });
    }

    /// <summary>
    /// Verifies that a globals substream that ends before its EOF, whether inside a record or between records, is
    /// rejected.
    /// </summary>
    /// <param name="cut">The number of bytes to keep.</param>
    [TestMethod]
    [DataRow(20)]
    [DataRow(23)]
    [DataRow(26)]
    public void OpenRead_WhenGlobalsTruncatedBeforeEof_ShouldThrowFormatException(int cut)
    {
        byte[] stream = Biff8TestWorkbook.Concat(Biff8TestWorkbook.Bof(Biff8TestWorkbook.BofGlobals), Biff8TestWorkbook.DateMode(true), Biff8TestWorkbook.Eof());
        using MemoryStream xls = Biff8TestWorkbook.WrapInCompoundFile(stream.AsSpan(0, cut).ToArray(), "Workbook");

        _ = Assert.ThrowsExactly<ExcelBinaryFormatException>(() =>
        {
            _ = ExcelBinaryWorkbook.OpenRead(xls);
        });
    }

    /// <summary>
    /// Verifies that a sheet substream that ends before its EOF is rejected when the sheet is opened.
    /// </summary>
    [TestMethod]
    public void OpenWorksheet_WhenSheetTruncatedBeforeEof_ShouldThrowFormatException()
    {
        byte[] stream = Biff8TestWorkbook.BuildWorkbookStream(
            [],
            new Biff8TestWorkbook.SheetSpec("Sheet1", 0, 0, [Biff8TestWorkbook.Number(0, 0, 1.0)]));
        using MemoryStream xls = Biff8TestWorkbook.WrapInCompoundFile(stream.AsSpan(0, stream.Length - 4).ToArray(), "Workbook");

        using var workbook = ExcelBinaryWorkbook.OpenRead(xls);
        _ = Assert.ThrowsExactly<ExcelBinaryFormatException>(() =>
        {
            _ = workbook.OpenWorksheet(0);
        });
    }

    /// <summary>
    /// Verifies that a sheet substream encoded in a different BIFF version from the globals is rejected when the
    /// sheet is opened, with the codec's exception preserved.
    /// </summary>
    [TestMethod]
    public void OpenWorksheet_WhenSheetBofVersionDisagrees_ShouldThrowFormatException()
    {
        var output = new System.Buffers.ArrayBufferWriter<byte>();
        var biff5 = new BiffWriter(output, BiffVersion.Biff5);
        biff5.WriteBof(BiffSubstreamType.Worksheet);
        biff5.WriteNumber(0, 0, 0, 1.0);
        biff5.WriteEof();
        byte[] sheet = output.WrittenSpan.ToArray();

        byte[] globals = Biff8TestWorkbook.Concat(Biff8TestWorkbook.Bof(Biff8TestWorkbook.BofGlobals), Biff8TestWorkbook.BoundSheet(0, 0, 0, "Mixed"), Biff8TestWorkbook.Eof());
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(globals.AsSpan(20 + 4), (uint)globals.Length);
        using MemoryStream xls = Biff8TestWorkbook.WrapInCompoundFile([.. globals, .. sheet], "Workbook");

        using var workbook = ExcelBinaryWorkbook.OpenRead(xls);
        var ex = Assert.ThrowsExactly<ExcelBinaryFormatException>(() =>
        {
            using ExcelWorksheetReader reader = workbook.OpenWorksheet(0);
            _ = reader.TryReadCell(out _);
        });

        Assert.IsInstanceOfType<BiffFormatException>(ex.InnerException);
    }

    /// <summary>
    /// Verifies that an unsupported version is reported with the codec's exception as the inner exception.
    /// </summary>
    [TestMethod]
    public void OpenRead_WhenVersionUnsupported_ShouldPreserveCodecException()
    {
        byte[] bofPayload = new byte[16];
        System.Buffers.Binary.BinaryPrimitives.WriteUInt16LittleEndian(bofPayload, 0x0700);
        System.Buffers.Binary.BinaryPrimitives.WriteUInt16LittleEndian(bofPayload.AsSpan(2), Biff8TestWorkbook.BofGlobals);
        byte[] stream = Biff8TestWorkbook.Concat(Biff8TestWorkbook.Record(0x0809, bofPayload), Biff8TestWorkbook.Eof());
        using MemoryStream xls = Biff8TestWorkbook.WrapInCompoundFile(stream, "Workbook");

        var ex = Assert.ThrowsExactly<ExcelBinaryUnsupportedException>(() =>
        {
            _ = ExcelBinaryWorkbook.OpenRead(xls);
        });

        Assert.IsInstanceOfType<BiffUnsupportedVersionException>(ex.InnerException);
        Assert.AreEqual((ushort)0x0700, ((BiffUnsupportedVersionException)ex.InnerException).RawVersion);
    }

    /// <summary>
    /// Verifies that a workbook opening with a BIFF2, BIFF3, or BIFF4 beginning-of-file record is reported as
    /// unsupported rather than malformed.
    /// </summary>
    /// <param name="legacyBof">The legacy BOF identifier.</param>
    [TestMethod]
    [DataRow((ushort)0x0009)]
    [DataRow((ushort)0x0209)]
    [DataRow((ushort)0x0409)]
    public void OpenRead_WhenLegacyBofOpensStream_ShouldThrowUnsupportedException(ushort legacyBof)
    {
        byte[] stream = Biff8TestWorkbook.Concat(Biff8TestWorkbook.Record(legacyBof, new byte[4]), Biff8TestWorkbook.Eof());
        using MemoryStream xls = Biff8TestWorkbook.WrapInCompoundFile(stream, "Book");

        _ = Assert.ThrowsExactly<ExcelBinaryUnsupportedException>(() =>
        {
            _ = ExcelBinaryWorkbook.OpenRead(xls);
        });
    }

    /// <summary>
    /// Verifies that an XF record too short even for its two indices is skipped, while one carrying only the
    /// indices still feeds number-format resolution, so the XF indices of later records stay aligned.
    /// </summary>
    [TestMethod]
    public void OpenRead_WhenXfRecordsAreShort_ShouldSkipUnusableAndKeepIndexAligned()
    {
        byte[] shortXf = Biff8TestWorkbook.Record(0x00E0, [0, 0]);
        byte[] indicesOnly = Biff8TestWorkbook.Record(0x00E0, [0, 0, 0xA4, 0x00]);
        using MemoryStream xls = Biff8TestWorkbook.BuildWorkbook(
            [Biff8TestWorkbook.Record(0x041E, [0xA4, 0x00, .. Biff8TestWorkbook.CompressedString("0.000").AsSpan(4)]), shortXf, indicesOnly],
            new Biff8TestWorkbook.SheetSpec("Sheet1", 0, 0, [Biff8TestWorkbook.Number(0, 0, 1.0, xfIndex: 0)]));

        using var workbook = ExcelBinaryWorkbook.OpenRead(xls);
        ExcelCell cell = ReadCellGrid(workbook, "Sheet1")[(0, 0)];

        Assert.AreEqual(164, cell.FormatIndex, "The unusable XF was skipped, so the indices-only XF is XF 0.");
        Assert.AreEqual("0.000", workbook.GetNumberFormatCode(cell.FormatIndex));
    }

    /// <summary>
    /// Verifies that a shared string table spanning several CONTINUE records resolves every cell reference through
    /// the workbook, including strings split mid-characters.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void OpenRead_WhenSharedStringTableSpansContinues_ShouldResolveEveryReference()
    {
        string[] strings = [.. Enumerable.Range(0, 2500).Select(i => i == 1234 ? new string('日', 6000) : $"value-{i}")];
        byte[][] body = [Biff8TestWorkbook.LabelSst(0, 0, 0), Biff8TestWorkbook.LabelSst(0, 1, 1234), Biff8TestWorkbook.LabelSst(0, 2, 2499)];
        using MemoryStream xls = Biff8TestWorkbook.BuildWorkbook(
            [Biff8TestWorkbook.Sst(strings)],
            new Biff8TestWorkbook.SheetSpec("Sheet1", 0, 0, body));

        using var workbook = ExcelBinaryWorkbook.OpenRead(xls);
        Dictionary<(int Row, int Column), ExcelCell> grid = ReadCellGrid(workbook, "Sheet1");

        Assert.AreEqual("value-0", grid[(0, 0)].StringValue);
        Assert.AreEqual(strings[1234], grid[(0, 1)].StringValue);
        Assert.AreEqual("value-2499", grid[(0, 2)].StringValue);
    }

    /// <summary>
    /// Verifies that a workbook with an empty shared string table rejects any shared-string cell.
    /// </summary>
    [TestMethod]
    public void OpenRead_WhenSharedStringTableIsEmpty_ShouldRejectSharedStringCells()
    {
        using MemoryStream xls = Biff8TestWorkbook.BuildWorkbook(
            [Biff8TestWorkbook.Sst()],
            new Biff8TestWorkbook.SheetSpec("Sheet1", 0, 0, [Biff8TestWorkbook.LabelSst(0, 0, 0)]));

        using var workbook = ExcelBinaryWorkbook.OpenRead(xls);

        _ = Assert.ThrowsExactly<ExcelBinaryFormatException>(() =>
        {
            _ = ReadCellGrid(workbook, "Sheet1");
        });
    }

    /// <summary>
    /// Verifies that a malformed shared string table fails when the workbook is opened, since every sheet depends
    /// on it.
    /// </summary>
    [TestMethod]
    public void OpenRead_WhenSharedStringTableIsMalformed_ShouldThrowFormatException()
    {
        using MemoryStream xls = Biff8TestWorkbook.BuildWorkbook(
            [Biff8TestWorkbook.Record(0x00FC, [2, 0, 0, 0, 2, 0, 0, 0, 0x01, 0x00, 0x00, (byte)'a'])],
            new Biff8TestWorkbook.SheetSpec("Sheet1", 0, 0, [Biff8TestWorkbook.Dimensions(0, 1, 0, 1)]));

        var ex = Assert.ThrowsExactly<ExcelBinaryFormatException>(() =>
        {
            _ = ExcelBinaryWorkbook.OpenRead(xls);
        });

        Assert.IsInstanceOfType<BiffFormatException>(ex.InnerException);
    }

    /// <summary>
    /// Verifies that a CODEPAGE record in a BIFF8 workbook is accepted and does not affect Unicode text.
    /// </summary>
    [TestMethod]
    public void OpenRead_WhenBiff8DeclaresCodePage_ShouldStillDecodeUnicode()
    {
        using MemoryStream xls = Biff8TestWorkbook.BuildWorkbook(
            [Biff8TestWorkbook.Record(0x0042, [0xB0, 0x04]), Biff8TestWorkbook.Sst("日本")],
            new Biff8TestWorkbook.SheetSpec("Sheet1", 0, 0, [Biff8TestWorkbook.LabelSst(0, 0, 0), Biff8TestWorkbook.Label(0, 1, "café")]));

        using var workbook = ExcelBinaryWorkbook.OpenRead(xls);
        Dictionary<(int Row, int Column), ExcelCell> grid = ReadCellGrid(workbook, "Sheet1");

        Assert.AreEqual("日本", grid[(0, 0)].StringValue);
        Assert.AreEqual("café", grid[(0, 1)].StringValue);
    }

    /// <summary>
    /// Verifies that a workbook's globals may carry records after the bound sheets and before EOF without
    /// disturbing the sheet directory.
    /// </summary>
    [TestMethod]
    public void OpenRead_WhenGlobalsHaveTrailingRecords_ShouldStillDescribeSheets()
    {
        byte[] stream = Biff8TestWorkbook.BuildWorkbookStream(
            [],
            new Biff8TestWorkbook.SheetSpec("Sheet1", 0, 0, [Biff8TestWorkbook.Number(0, 0, 1.0)]));

        // Insert an unknown record just before the globals EOF and shift the bound-sheet offset accordingly.
        int globalsEof = Biff8TestWorkbook.Bof(Biff8TestWorkbook.BofGlobals).Length + Biff8TestWorkbook.BoundSheet(0, 0, 0, "Sheet1").Length;
        byte[] extra = Biff8TestWorkbook.Record(0x00FF, new byte[10]);
        byte[] patched = [.. stream.AsSpan(0, globalsEof), .. extra, .. stream.AsSpan(globalsEof)];
        uint offset = System.Buffers.Binary.BinaryPrimitives.ReadUInt32LittleEndian(patched.AsSpan(20 + 4));
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(patched.AsSpan(20 + 4), offset + (uint)extra.Length);
        using MemoryStream xls = Biff8TestWorkbook.WrapInCompoundFile(patched, "Workbook");

        using var workbook = ExcelBinaryWorkbook.OpenRead(xls);

        Assert.AreEqual(1.0, ReadCellGrid(workbook, "Sheet1")[(0, 0)].NumberValue);
    }
}
