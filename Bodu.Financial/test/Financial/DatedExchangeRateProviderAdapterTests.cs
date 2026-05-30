// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DatedExchangeRateProviderAdapterTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Assertions;

namespace Bodu.Financial;

[TestClass]
public partial class DatedExchangeRateProviderAdapterTests
{
    private static readonly DateOnly s_d1 = new(2024, 1, 3);

    /// <summary>
    /// Verifies that the adapter forwards lookups to the inner dated provider with the pinned date and options, and
    /// returns the raw rate value through <see cref="IExchangeRateProvider.GetRate" />.
    /// </summary>
    [TestMethod]
    public void GetRate_WhenInnerHasRate_ShouldReturnRateValue()
    {
        FixedDatedExchangeRateTable inner = new(
        [
            new ExchangeRate("USD", "AUD", s_d1, 1.50m, "RBA"),
        ]);
        DatedExchangeRateProviderAdapter adapter = new(inner, s_d1, ExchangeRateLookupOptions.Exact);

        Assert.AreEqual(1.50m, adapter.GetRate("USD", "AUD"));
    }

    /// <summary>
    /// Verifies that the adapter propagates exceptions thrown by the inner provider — for example, when no rate is
    /// available under the pinned date.
    /// </summary>
    [TestMethod]
    public void GetRate_WhenInnerHasNoRate_ShouldThrowKeyNotFoundException()
    {
        FixedDatedExchangeRateTable inner = new([]);
        DatedExchangeRateProviderAdapter adapter = new(inner, s_d1, ExchangeRateLookupOptions.Exact);

        _ = Assert.ThrowsExactly<KeyNotFoundException>(() => adapter.GetRate("USD", "AUD"));
    }

    /// <summary>
    /// Verifies that the constructor rejects a <see langword="null" /> inner provider.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenInnerIsNull_ShouldThrowArgumentNullException()
    {
        ExceptionAssert.ThrowsExactlyWithParamName<ArgumentNullException>(
            () =>
            {
                _ = new DatedExchangeRateProviderAdapter(null!, s_d1, ExchangeRateLookupOptions.Exact);
            },
            "inner");
    }

    /// <summary>
    /// Verifies that the constructor rejects invalid options.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenOptionsInvalid_ShouldThrowArgumentException()
    {
        FixedDatedExchangeRateTable inner = new([]);

        _ = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            ExchangeRateLookupOptions bad = new(ExchangeRateDateResolution.Exact, 1);
            _ = new DatedExchangeRateProviderAdapter(inner, s_d1, bad);
        });
    }
}
