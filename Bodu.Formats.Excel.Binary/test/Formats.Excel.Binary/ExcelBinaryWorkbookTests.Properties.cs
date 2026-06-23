// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExcelBinaryWorkbookTests.Properties.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Formats.Excel.Binary;

public partial class ExcelBinaryWorkbookTests
{
    /// <summary>
    /// Verifies that opening the sample workbook exposes a document-properties view.
    /// </summary>
    [TestMethod]
    public void Properties_WhenSampleWorkbook_ShouldExposePropertiesView()
    {
        using ExcelBinaryWorkbook workbook = OpenSample();

        Assert.IsNotNull(workbook.Properties);
    }

    /// <summary>
    /// Verifies that disabling document-property reading yields an empty, non-null properties view.
    /// </summary>
    [TestMethod]
    public void Properties_WhenReadDocumentPropertiesDisabled_ShouldReturnEmptyView()
    {
        using ExcelBinaryWorkbook workbook = ExcelBinaryWorkbook.Open(
            ExcelBinaryFixtures.OpenStream(ExcelBinaryFixtures.SampleBiff8),
            new ExcelBinaryReaderOptions { ReadDocumentProperties = false });

        Assert.IsNotNull(workbook.Properties);
        Assert.IsNull(workbook.Properties.Title);
        Assert.IsNull(workbook.Properties.Author);
        Assert.IsNull(workbook.Properties.Company);
        Assert.IsNull(workbook.Properties.ApplicationName);
    }
}
