// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Biff8WorkbookReaderTests.Properties.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Formats.Excel.Binary;

public partial class Biff8WorkbookReaderTests
{
    /// <summary>
    /// Verifies that opening the sample workbook exposes a document-properties view.
    /// </summary>
    [TestMethod]
    public void Properties_WhenSampleWorkbook_ShouldExposePropertiesView()
    {
        Biff8WorkbookReader reader = OpenSample();

        Assert.IsNotNull(reader.Properties);
    }

    /// <summary>
    /// Verifies that the sample workbook's summary-information property set is parsed and surfaced.
    /// </summary>
    [TestMethod]
    public void Properties_WhenSampleWorkbookHasSummaryStream_ShouldSurfaceSummary()
    {
        Biff8WorkbookReader reader = OpenSample();

        Assert.IsNotNull(reader.Properties.Summary);
    }
}
