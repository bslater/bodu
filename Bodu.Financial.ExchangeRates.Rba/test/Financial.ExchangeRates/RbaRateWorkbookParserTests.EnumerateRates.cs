// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RbaRateWorkbookParserTests.EnumerateRates.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;

namespace Bodu.Financial.ExchangeRates;

public partial class RbaRateWorkbookParserTests
{
    /// <summary>
    /// Verifies that enumerated rates reproduce the rates the sample workbook holds on its first and last rows, the
    /// ones the tests and documentation name.
    /// </summary>
    [TestMethod]
    public void EnumerateRates_WhenSampleWorkbook_ShouldReproduceKnownAnchors()
    {
        var rates = ParseSample()
            .EnumerateRates()
            .ToDictionary(r => (r.To, r.Date), r => r.Rate);

        Assert.AreEqual(0.6828m, rates[(CurrencyCode.USD, new DateOnly(2023, 1, 3))]);
        Assert.AreEqual(0.7029m, rates[(CurrencyCode.USD, new DateOnly(2026, 6, 12))]);
        Assert.AreEqual(112.71m, rates[(CurrencyCode.JPY, new DateOnly(2026, 6, 12))]);
    }

    /// <summary>
    /// Verifies that blank cells produce no observation: on the first data row the USD rate is present while the UAE
    /// dirham column, which is blank on that row, yields no observation.
    /// </summary>
    [TestMethod]
    public void EnumerateRates_WhenCellIsBlank_ShouldNotEmitObservation()
    {
        var firstRow = new DateOnly(2023, 1, 3);
        var rates = ParseSample().EnumerateRates().ToList();

        Assert.Contains(r => r.To == CurrencyCode.USD && r.Date == firstRow, rates);
        Assert.DoesNotContain(r => r.To == CurrencyCode.AED && r.Date == firstRow, rates);
    }
}
