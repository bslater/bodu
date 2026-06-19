// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Biff8WorkbookReaderTests.Open.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Linq;
using Bodu.Test;

namespace Bodu.Formats.Excel.Binary;

public partial class Biff8WorkbookReaderTests
{
    /// <summary>
    /// Verifies that opening the sample workbook exposes its two worksheets in order.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Smoke)]
    public void Open_WhenSampleWorkbook_ShouldExposeDataAndNotesSheets()
    {
        Biff8WorkbookReader reader = OpenSample();

        var names = reader.Sheets.Select(s => s.Name).ToList();

        CollectionAssert.AreEqual(new[] { "Data", "Notes" }, names);
        Assert.IsTrue(reader.Sheets.All(s => s.IsVisible));
    }

    /// <summary>
    /// Verifies that opening a <see langword="null" /> stream throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void Open_WhenStreamIsNull_ShouldThrowArgumentNullException()
    {
        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = Biff8WorkbookReader.Open(null!);
        });

        Assert.AreEqual("stream", ex.ParamName);
    }

    /// <summary>
    /// Verifies that opening data that is not a compound file throws <see cref="IO.Compound.CompoundFileFormatException" />.
    /// </summary>
    [TestMethod]
    public void Open_WhenStreamIsNotCompoundFile_ShouldThrowCompoundFileFormatException()
    {
        using MemoryStream stream = new(new byte[600]);

        _ = Assert.ThrowsExactly<IO.Compound.CompoundFileFormatException>(() =>
        {
            _ = Biff8WorkbookReader.Open(stream);
        });
    }
}
