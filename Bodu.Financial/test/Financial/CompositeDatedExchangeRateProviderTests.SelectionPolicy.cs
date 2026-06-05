// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompositeDatedExchangeRateProviderTests.SelectionPolicy.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial;

public partial class CompositeDatedExchangeRateProviderTests
{
    /// <summary>
    /// Verifies that <see cref="ExchangeRateProviderSelectionPolicy.ProviderPriorityFirst" /> (the v1.0 default)
    /// returns the first provider's successful answer without consulting later providers.
    /// </summary>
    [TestMethod]
    public void TryGetRate_WhenSelectionPolicyIsProviderPriorityFirst_ShouldUseFirstSuccess()
    {
        FixedDatedExchangeRateProvider first = new(new[]
        {
            new ExchangeRate("USD", "AUD", new DateOnly(2024, 1, 1), 1.5m, "RBA"),
        });
        FixedDatedExchangeRateProvider second = new(new[]
        {
            new ExchangeRate("USD", "AUD", new DateOnly(2024, 1, 1), 1.7m, "ECB"),
        });

        CompositeDatedExchangeRateProvider composite = new(
            new IDatedExchangeRateProvider[] { first, second },
            ExchangeRateProviderSelectionPolicy.ProviderPriorityFirst);

        var found = composite.TryGetRate("USD", "AUD", new DateOnly(2024, 1, 1), null, out ExchangeRateLookupResult result);

        Assert.IsTrue(found);
        Assert.AreEqual(1.5m, result.Rate.Rate);
        Assert.AreEqual("RBA", result.Rate.Provider);
    }

    /// <summary>
    /// Verifies that selection policies other than <see cref="ExchangeRateProviderSelectionPolicy.ProviderPriorityFirst" />
    /// are not yet supported and throw <see cref="NotSupportedException" />.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenSelectionPolicyIsExactDateBeforeProviderFallback_ShouldThrowNotSupported()
    {
        IDatedExchangeRateProvider stub = new FixedDatedExchangeRateProvider(new[]
        {
            new ExchangeRate("USD", "AUD", new DateOnly(2024, 1, 1), 1.5m, "RBA"),
        });

        _ = Assert.ThrowsExactly<NotSupportedException>(() =>
        {
            _ = new CompositeDatedExchangeRateProvider(
                new[] { stub },
                ExchangeRateProviderSelectionPolicy.ExactDateBeforeProviderFallback);
        });
    }

    /// <summary>
    /// Verifies that an undefined enum value passed to the selection policy parameter is rejected.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenSelectionPolicyIsUndefined_ShouldThrowArgumentOutOfRangeException()
    {
        IDatedExchangeRateProvider stub = new FixedDatedExchangeRateProvider(new[]
        {
            new ExchangeRate("USD", "AUD", new DateOnly(2024, 1, 1), 1.5m, "RBA"),
        });

        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new CompositeDatedExchangeRateProvider(
                new[] { stub },
                (ExchangeRateProviderSelectionPolicy)999);
        });
    }
}
