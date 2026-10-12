// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RbaRateKnownAnswerTests.GetRates.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test;

namespace Bodu.Financial.ExchangeRates;

public partial class RbaRateKnownAnswerTests
{
    /// <summary>
    /// Verifies that the first, last, minimum, maximum and median rows selected for a workbook currency match the
    /// extremes and median of the series the provider reads for that currency, confirming both the selections and the
    /// completeness of the read series.
    /// </summary>
    /// <param name="sourceFileName">The era workbook file name.</param>
    /// <param name="currency">The ISO 4217 code of the quote currency.</param>
    /// <returns>A task that completes when the assertions have run.</returns>
    [TestMethod]
    [TestCategory(TestCategories.Regression)]
    [DynamicData(nameof(ClassificationGroups))]
    public async Task GetRates_WhenKnownAnswerSeries_ShouldMatchSelectedExtremes(string sourceFileName, string currency)
    {
        RbaRateProvider provider = await GetProviderAsync(sourceFileName);
        RbaEraWorkbook era = RbaEraWorkbook.Default.Single(e => string.Equals(e.FileName, sourceFileName, StringComparison.Ordinal));
        DateOnly end = era.End ?? new DateOnly(2100, 1, 1);

        IReadOnlyList<ExchangeRate> series = [.. await provider.GetRatesAsync("AUD", currency, era.Start, end)];
        Assert.IsNotEmpty(series);

        var byType = s_allRows
            .Where(row => row.SourceFileName == sourceFileName && string.Equals(row.Currency, currency, StringComparison.Ordinal))
            .ToDictionary(row => row.Type, StringComparer.Ordinal);
        decimal[] sorted = [.. series.Select(rate => rate.Rate).Order()];

        Assert.AreEqual(byType["First"].Date, series[0].Date);
        Assert.AreEqual(byType["First"].ExpectedRate, series[0].Rate);
        Assert.AreEqual(byType["Last"].Date, series[^1].Date);
        Assert.AreEqual(byType["Last"].ExpectedRate, series[^1].Rate);
        Assert.AreEqual(byType["Min"].ExpectedRate, sorted[0]);
        Assert.AreEqual(byType["Max"].ExpectedRate, sorted[^1]);
        Assert.AreEqual(byType["Median"].ExpectedRate, sorted[(sorted.Length - 1) / 2]);
    }
}
