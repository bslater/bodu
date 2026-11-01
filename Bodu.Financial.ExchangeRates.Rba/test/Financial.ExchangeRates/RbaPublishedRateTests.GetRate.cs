// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RbaPublishedRateTests.GetRate.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test;
using Bodu.Test.Kat;

namespace Bodu.Financial.ExchangeRates;

public partial class RbaPublishedRateTests
{
    /// <summary>
    /// Verifies that an exact-date lookup, with the row's era downloaded from the RBA, returns the rate the RBA
    /// published for the row's currency, dated on the row's day and not a neighboring one.
    /// </summary>
    /// <param name="answer">The published-rate row under test.</param>
    /// <returns>A task that completes when the assertion has run.</returns>
    [TestMethod(UnfoldingStrategy = TestDataSourceUnfoldingStrategy.Unfold)]
    [TestCategory(TestCategories.Stress)]
    [DynamicData(nameof(PublishedRates), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public async Task GetRate_WhenRbaPublishedTheRate_ShouldReturnItFromTheLiveWorkbook(RbaPublishedRateKnownAnswer answer)
    {
        ArgumentNullException.ThrowIfNull(answer);
        if (!IsLiveTestEnabled())
        {
            Assert.Inconclusive($"Set {LiveEnvVar} to a non-empty value to run the live RBA published-rate test.");
            return;
        }

        RbaRateProvider provider = await GetProviderAsync();

        RateLookupResult result = provider.GetRate("AUD", answer.Currency, answer.Date);

        Assert.AreEqual(answer.Date, result.Rate.Date);
        Assert.AreEqual(answer.ExpectedRate, result.Rate.Rate);
    }
}
