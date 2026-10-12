// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SummaryInformationTests.Title.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Compound.PropertySets;

public partial class SummaryInformationTests
{
    /// <summary>
    /// Verifies that a title stored as something other than a string, here a <c>VT_BLOB</c>, reads as
    /// <see langword="null" /> without an exception, as <see cref="OlePropertyValue.AsString" /> documents.
    /// </summary>
    [TestMethod]
    public void Title_WhenStoredAsBlob_ShouldReturnNull()
    {
        byte[] bytes = PropertySetBytes.Single(
            PropertySetBytes.SummaryInformation,
            (1, PropertySetBytes.CodePage(1252)),
            (2, PropertySetBytes.Typed(0x0041, PropertySetBytes.UInt32(4), [0x41, 0x42, 0x43, 0x44])));

        var summary = new SummaryInformation(OlePropertySet.Parse(bytes));

        Assert.IsNull(summary.Title);
    }
}
