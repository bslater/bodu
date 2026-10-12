// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DocumentSummaryInformationTests.ToPropertySet.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Compound.PropertySets;

public partial class DocumentSummaryInformationTests
{
    /// <summary>
    /// Verifies that a custom property added through <see cref="DocumentSummaryInformationBuilder" /> is written to a
    /// second section whose format identifier is <c>FMTID_UserDefinedProperties</c>,
    /// D5CDD505-2E9C-101B-9397-08002B2CF9AE.
    /// </summary>
    [TestMethod]
    public void ToPropertySet_WhenCustomPropertiesAdded_ShouldUseUserDefinedFormatId()
    {
        var builder = new DocumentSummaryInformationBuilder { Company = "Contoso" };
        builder.AddCustomProperty("Reviewed", OlePropertyValue.Create(true));

        OlePropertySet set = builder.ToPropertySet();

        Assert.HasCount(2, set.Sections);
        Assert.AreEqual(new Guid("D5CDD505-2E9C-101B-9397-08002B2CF9AE"), set.Sections[1].FormatId);
    }
}
