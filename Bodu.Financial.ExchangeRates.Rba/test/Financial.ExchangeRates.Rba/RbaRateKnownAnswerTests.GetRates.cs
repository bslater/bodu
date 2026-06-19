// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RbaRateKnownAnswerTests.GetRates.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Linq;
using Bodu.Test;

namespace Bodu.Financial.ExchangeRates.Rba;

public partial class RbaRateKnownAnswerTests
{
    /// <summary>
    /// Verifies that the first, last, minimum, and maximum rows selected for a workbook currency genuinely match the
    /// extremes of that currency's full series, confirming both the selections and the completeness of the read series.
    /// </summary>
    /// <param name="sourceFileName">The RBA workbook file name.</param>
    /// <param name="currency">The RBA currency label.</param>
    /// <returns>A task that completes when the assertions have run.</returns>
    /// <remarks>
    /// The closest-to-median row is intentionally not recomputed here (its selection is algorithm-dependent); it is
    /// still verified as a point value by <see cref="GetRate_WhenKnownAnswer_ShouldReturnPublishedRate" />.
    /// </remarks>
    [TestMethod]
    [TestCategory(TestCategories.Regression)]
    [DynamicData(nameof(ClassificationGroups))]
    public async Task GetRates_WhenKnownAnswerSeries_ShouldMatchSelectedExtremes(string sourceFileName, string currency)
    {
        RbaExchangeRateProvider provider = await GetProviderAsync(sourceFileName);
        RbaEra era = RbaEra.Default.Single(e => string.Equals(e.FileName, sourceFileName, StringComparison.Ordinal));
        DateOnly end = era.End ?? new DateOnly(2100, 1, 1);

        IReadOnlyList<ExchangeRate> series = [.. await provider.GetRatesAsync("AUD", ResolveCurrency(currency), era.Start, end)];
        Assert.IsNotEmpty(series);

        var byType = s_allRows
            .Where(row => row.SourceFileName == sourceFileName && string.Equals(row.Currency, currency, StringComparison.Ordinal))
            .ToDictionary(row => row.Type, StringComparer.OrdinalIgnoreCase);

        if (byType.TryGetValue("First", out RbaRateKnownAnswer? first))
        {
            Assert.AreEqual(first.Date, series[0].Date);
            Assert.AreEqual(first.ExpectedRate, series[0].Rate);
        }

        if (byType.TryGetValue("Last", out RbaRateKnownAnswer? last))
        {
            Assert.AreEqual(last.Date, series[^1].Date);
            Assert.AreEqual(last.ExpectedRate, series[^1].Rate);
        }

        if (byType.TryGetValue("Min", out RbaRateKnownAnswer? min))
            Assert.AreEqual(min.ExpectedRate, series.Min(rate => rate.Rate));

        if (byType.TryGetValue("Max", out RbaRateKnownAnswer? max))
            Assert.AreEqual(max.ExpectedRate, series.Max(rate => rate.Rate));
    }
}
