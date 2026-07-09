// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RbaRateKnownAnswerTests.GetRate.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test;
using Bodu.Test.Kat;

namespace Bodu.Financial.ExchangeRates;

public partial class RbaRateKnownAnswerTests
{
    /// <summary>
    /// Verifies that an exact-date lookup returns the rate RBA published for the row's currency.
    /// </summary>
    /// <param name="answer">The known-answer row under test.</param>
    /// <returns>A task that completes when the assertion has run.</returns>
    [TestMethod(UnfoldingStrategy = TestDataSourceUnfoldingStrategy.Unfold)]
    [TestCategory(TestCategories.Regression)]
    [DynamicData(nameof(KnownAnswers), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public async Task GetRate_WhenKnownAnswer_ShouldReturnPublishedRate(RbaRateKnownAnswer answer)
    {
        ArgumentNullException.ThrowIfNull(answer);

        RbaExchangeRateProvider provider = await GetProviderAsync(answer.SourceFileName);

        RateLookupResult result = provider.GetRate("AUD", ResolveCurrency(answer.Currency), answer.Date);

        Assert.AreEqual(answer.ExpectedRate, result.Rate.Rate);
    }
}
