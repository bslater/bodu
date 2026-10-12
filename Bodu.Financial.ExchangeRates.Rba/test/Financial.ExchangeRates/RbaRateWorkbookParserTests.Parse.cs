// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RbaRateWorkbookParserTests.Parse.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

public partial class RbaRateWorkbookParserTests
{
    /// <summary>
    /// Verifies that a data sheet whose name differs from <c>Data</c> only in case, here <c>data</c>, is read as a
    /// <c>Data</c> sheet is, rather than failing with <see cref="KeyNotFoundException" />.
    /// </summary>
    [TestMethod]
    public void Parse_WhenDataSheetNameDiffersInCase_ShouldReadSheet()
    {
        RbaTestSheet sheet = CreateSheet() with { Name = "data" };

        RbaRateTable table = Parse(sheet);

        RbaRateSeries usd = table.Series.Single();
        Assert.AreEqual("USD", usd.CurrencyCode);
        Assert.AreEqual("FXRUSD", usd.SeriesId);
        CollectionAssert.AreEqual(sheet.Dates.ToArray(), table.Rows.Select(row => row.Date).ToArray());
        Assert.AreEqual(0.6812m, table.Rows[0].Values[0]);
    }
}
