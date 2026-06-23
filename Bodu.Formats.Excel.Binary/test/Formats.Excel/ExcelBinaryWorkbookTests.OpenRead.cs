// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExcelBinaryWorkbookTests.OpenRead.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Linq;
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
            using ExcelBinaryWorkbook workbook = ExcelBinaryWorkbook.OpenRead(path);
            Assert.AreEqual(2, workbook.Worksheets.Count);
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

        using (ExcelBinaryWorkbook workbook = ExcelBinaryWorkbook.OpenRead(source, leaveOpen: true))
            Assert.AreEqual(2, workbook.Worksheets.Count);

        // A disposed MemoryStream throws on access; reading the length confirms it is still open.
        Assert.IsTrue(source.Length > 0);
        source.Dispose();
    }

    /// <summary>
    /// Verifies that opening with the default ownership disposes the source stream when the workbook is disposed.
    /// </summary>
    [TestMethod]
    public void OpenRead_WhenLeaveOpenFalse_ShouldDisposeSourceStream()
    {
        MemoryStream source = ExcelBinaryFixtures.OpenStream(ExcelBinaryFixtures.SampleBiff8);

        using (ExcelBinaryWorkbook workbook = ExcelBinaryWorkbook.OpenRead(source))
            Assert.AreEqual(2, workbook.Worksheets.Count);

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
        using (CompoundFile file = CompoundFile.Create(container, leaveOpen: true))
        {
            file.RootStorage.CreateStream("Book", new byte[] { 0x09, 0x08, 0x00, 0x00 });
            file.RootStorage.CreateStream("Workbook", workbookBytes);
            file.Commit();
        }

        container.Position = 0;
        using ExcelBinaryWorkbook workbook = ExcelBinaryWorkbook.OpenRead(container);
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
        using (CompoundFile file = CompoundFile.Create(container, leaveOpen: true))
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
    /// Verifies that a workbook whose globals declare a non-BIFF8 version throws
    /// <see cref="ExcelBinaryUnsupportedException" />.
    /// </summary>
    [TestMethod]
    public void OpenRead_WhenVersionNotBiff8_ShouldThrowUnsupportedException()
    {
        // Globals BOF declaring BIFF5 (0x0500), followed by EOF.
        byte[] bofPayload = new byte[16];
        BinaryPrimitives.WriteUInt16LittleEndian(bofPayload, 0x0500);
        BinaryPrimitives.WriteUInt16LittleEndian(bofPayload.AsSpan(2), Biff8TestWorkbook.BofGlobals);

        byte[] globals = [.. Biff8TestWorkbook.Record(0x0809, bofPayload), .. Biff8TestWorkbook.Eof()];
        using MemoryStream xls = Biff8TestWorkbook.WrapInCompoundFile(globals, "Workbook");

        _ = Assert.ThrowsExactly<ExcelBinaryUnsupportedException>(() =>
        {
            _ = ExcelBinaryWorkbook.OpenRead(xls);
        });
    }
}
