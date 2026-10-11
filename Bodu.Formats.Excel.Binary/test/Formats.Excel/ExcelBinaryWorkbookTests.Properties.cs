// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExcelBinaryWorkbookTests.Properties.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Compound.Builders;
using Bodu.IO.Compound.PropertySets;

namespace Bodu.Formats.Excel;

public partial class ExcelBinaryWorkbookTests
{
    /// <summary>The format identifier of the document summary section, <c>FMTID_DocSummaryInformation</c>.</summary>
    private static readonly Guid DocumentSummaryInformationFormatId = new("D5CDD502-2E9C-101B-9397-08002B2CF9AE");

    /// <summary>The format identifier of the user-defined section, <c>FMTID_UserDefinedProperties</c>.</summary>
    private static readonly Guid UserDefinedPropertiesFormatId = new("D5CDD505-2E9C-101B-9397-08002B2CF9AE");

    /// <summary>
    /// Verifies that opening the sample workbook exposes a document-properties view holding its summary information.
    /// </summary>
    [TestMethod]
    public void Properties_WhenSampleWorkbook_ShouldExposePropertiesView()
    {
        using ExcelBinaryWorkbook workbook = OpenSample();

        Assert.IsNotNull(workbook.Properties);
        Assert.AreEqual(ExcelBinaryFixtures.Title, workbook.Properties.Title);
    }

    /// <summary>
    /// Verifies that disabling document-property reading yields an empty, non-null properties view.
    /// </summary>
    [TestMethod]
    public void Properties_WhenReadDocumentPropertiesDisabled_ShouldReturnEmptyView()
    {
        using var workbook = ExcelBinaryWorkbook.Open(
            ExcelBinaryFixtures.OpenSample(),
            new ExcelBinaryReaderOptions { ReadDocumentProperties = false });

        Assert.IsNotNull(workbook.Properties);
        Assert.IsNull(workbook.Properties.Title);
        Assert.IsNull(workbook.Properties.Author);
        Assert.IsNull(workbook.Properties.Company);
        Assert.IsNull(workbook.Properties.ApplicationName);
    }

    /// <summary>
    /// Verifies that a workbook whose document summary carries a user-defined section with PID_LOCALE (0x80000000), as
    /// Office writes it, opens with its document properties.
    /// </summary>
    [TestMethod]
    public void Properties_WhenDocumentSummaryCarriesLocale_ShouldReadThem()
    {
        var document = new OlePropertySection(DocumentSummaryInformationFormatId, 1252);
        document.Set(15, OlePropertyValue.Create("Bodu"));
        var custom = new OlePropertySection(UserDefinedPropertiesFormatId, 1252);
        custom.Set(unchecked((int)0x80000000), new OlePropertyValue { Type = OlePropertyType.UInt32, Value = 0x0409u });
        var set = new OlePropertySet(DocumentSummaryInformationFormatId, Guid.Empty, 1252);
        set.AddSection(document);
        set.AddSection(custom);

        var root = CompoundStorageBuilder.CreateRoot();
        _ = root.AddStream("Workbook", Biff8TestWorkbook.BuildWorkbookStream([], new Biff8TestWorkbook.SheetSpec("Sheet1", 0, 0, [])));
        _ = root.AddStream(DocumentSummaryInformation.StreamName, set.ToArray());
        using var xls = new MemoryStream(root.ToArray());

        using var workbook = ExcelBinaryWorkbook.OpenRead(xls);

        Assert.AreEqual("Bodu", workbook.Properties.Company);
    }
}
