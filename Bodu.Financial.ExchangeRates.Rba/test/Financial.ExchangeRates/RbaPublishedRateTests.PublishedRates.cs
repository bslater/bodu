// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RbaPublishedRateTests.PublishedRates.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;

namespace Bodu.Financial.ExchangeRates;

public partial class RbaPublishedRateTests
{
    /// <summary>
    /// Verifies that every published-rate row cites the era workbook of the default catalog that covers its date, or
    /// the F11.1 CSV for a date of the current era, and names a currency the exchange-rate types model, so that the
    /// live test downloads the file each constant was typed from and can ask the provider for it.
    /// </summary>
    [TestMethod]
    public void PublishedRates_ShouldEachCiteTheFileOfTheEraThatCoversTheirDate()
    {
        Assert.IsNotEmpty(s_rows);

        foreach (RbaPublishedRateKnownAnswer row in s_rows)
        {
            RbaEraWorkbook? era = RbaEraWorkbook.ForDate(row.Date, RbaEraWorkbook.Default);

            Assert.IsNotNull(era, row.Name);
            Assert.IsTrue(
                string.Equals(row.SourceFile, era.FileName, StringComparison.Ordinal) ||
                (era.End is null && string.Equals(row.SourceFile, CsvFile, StringComparison.Ordinal)),
                row.Name);
            Assert.IsTrue(CurrencyInfo.TryGetCurrencyCode(row.Currency, out _), row.Name);
        }
    }
}
