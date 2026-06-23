// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExcelBinaryWorkbookTests.Open.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Linq;
using Bodu.Test;

namespace Bodu.Formats.Excel.Binary;

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
}
