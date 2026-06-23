// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExcelWorkbookPropertiesTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Formats.Excel;

/// <summary>
/// Verifies the behavior of <see cref="ExcelWorkbookProperties" />, the flattened document-properties view.
/// </summary>
[TestClass]
public class ExcelWorkbookPropertiesTests
{
    /// <summary>
    /// Verifies that the empty property view exposes <see langword="null" /> for every summary-information field.
    /// </summary>
    [TestMethod]
    public void Empty_WhenSummaryFieldsRead_ShouldAllBeNull()
    {
        ExcelWorkbookProperties properties = ExcelWorkbookProperties.Empty;

        Assert.IsNull(properties.Title);
        Assert.IsNull(properties.Subject);
        Assert.IsNull(properties.Author);
        Assert.IsNull(properties.Keywords);
        Assert.IsNull(properties.Comments);
        Assert.IsNull(properties.LastSavedBy);
        Assert.IsNull(properties.ApplicationName);
        Assert.IsNull(properties.Created);
        Assert.IsNull(properties.LastSaved);
        Assert.IsNull(properties.LastPrinted);
    }

    /// <summary>
    /// Verifies that the empty property view exposes <see langword="null" /> for every document-summary field.
    /// </summary>
    [TestMethod]
    public void Empty_WhenDocumentFieldsRead_ShouldAllBeNull()
    {
        ExcelWorkbookProperties properties = ExcelWorkbookProperties.Empty;

        Assert.IsNull(properties.Company);
        Assert.IsNull(properties.Manager);
        Assert.IsNull(properties.Category);
    }
}
