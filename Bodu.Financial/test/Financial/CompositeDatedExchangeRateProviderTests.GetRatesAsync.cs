// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompositeDatedExchangeRateProviderTests.GetRatesAsync.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial;

public partial class CompositeDatedExchangeRateProviderTests
{
    /// <summary>
    /// Verifies that the range lookup returns the first inner provider's non-empty result.
    /// </summary>
    [TestMethod]
    public async Task GetRatesAsync_WhenFirstProviderHasRange_ShouldReturnIt()
    {
        FixedDatedExchangeRateProvider first = new(
        [
            new ExchangeRate("USD", "AUD", s_d1, 1.50m, "RBA"),
        ]);
        FixedDatedExchangeRateProvider second = new(
        [
            new ExchangeRate("USD", "AUD", s_d1, 1.60m, "ECB"),
        ]);
        CompositeDatedExchangeRateProvider composite = new([first, second]);

        IReadOnlyList<ExchangeRate> rates =
            await composite.GetRatesAsync("USD", "AUD", s_d1, s_d1);

        Assert.AreEqual(1, rates.Count);
        Assert.AreEqual("RBA", rates[0].Provider);
    }

    /// <summary>
    /// Verifies that an empty first provider falls through to the next provider's range.
    /// </summary>
    [TestMethod]
    public async Task GetRatesAsync_WhenFirstProviderEmpty_ShouldFallThroughToSecond()
    {
        FixedDatedExchangeRateProvider first = new([]);
        FixedDatedExchangeRateProvider second = new(
        [
            new ExchangeRate("USD", "AUD", s_d1, 1.60m, "ECB"),
        ]);
        CompositeDatedExchangeRateProvider composite = new([first, second]);

        IReadOnlyList<ExchangeRate> rates =
            await composite.GetRatesAsync("USD", "AUD", s_d1, s_d1);

        Assert.AreEqual(1, rates.Count);
        Assert.AreEqual("ECB", rates[0].Provider);
    }

    /// <summary>
    /// Verifies that the range lookup returns an empty list when no inner provider has data.
    /// </summary>
    [TestMethod]
    public async Task GetRatesAsync_WhenNoProviderHasData_ShouldReturnEmpty()
    {
        CompositeDatedExchangeRateProvider composite = new([new FixedDatedExchangeRateProvider([])]);

        IReadOnlyList<ExchangeRate> rates =
            await composite.GetRatesAsync("USD", "AUD", s_d1, s_d1);

        Assert.AreEqual(0, rates.Count);
    }
}
