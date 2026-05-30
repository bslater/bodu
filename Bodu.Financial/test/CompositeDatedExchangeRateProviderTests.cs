// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompositeDatedExchangeRateProviderTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test;
using Bodu.Test.Assertions;

namespace Bodu.Financial;

[TestClass]
public partial class CompositeDatedExchangeRateProviderTests
{
    private static readonly DateOnly s_d1 = new(2024, 1, 3);

    /// <summary>
    /// Verifies that a composite provider with a single inner provider resolves identically to that inner provider.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Smoke)]
    public void TryGetRate_WhenSingleInnerProvider_ShouldDelegateAndReturn()
    {
        FixedDatedExchangeRateTable inner = new(
        [
            new ExchangeRate("USD", "AUD", s_d1, 1.50m, "RBA"),
        ]);
        CompositeDatedExchangeRateProvider composite = new([(IDatedExchangeRateProvider)inner]);

        var found = composite.TryGetRate(
            "USD",
            "AUD",
            s_d1,
            ExchangeRateLookupOptions.Exact,
            out ExchangeRateLookupResult result);

        Assert.IsTrue(found);
        Assert.AreEqual(1.50m, result.Rate.Rate);
        Assert.AreEqual("RBA", result.Rate.Provider);
    }

    /// <summary>
    /// Verifies that when the first provider misses and the second has the rate, the composite returns the second
    /// provider's result.
    /// </summary>
    [TestMethod]
    public void TryGetRate_WhenFirstProviderMisses_ShouldFallThroughToSecond()
    {
        FixedDatedExchangeRateTable first = new([]);
        FixedDatedExchangeRateTable second = new(
        [
            new ExchangeRate("USD", "AUD", s_d1, 1.50m, "ECB"),
        ]);
        CompositeDatedExchangeRateProvider composite = new([first, second]);

        var found = composite.TryGetRate(
            "USD",
            "AUD",
            s_d1,
            ExchangeRateLookupOptions.Exact,
            out ExchangeRateLookupResult result);

        Assert.IsTrue(found);
        Assert.AreEqual("ECB", result.Rate.Provider);
    }

    /// <summary>
    /// Verifies that when both providers have a rate, the first-available policy returns the first provider's result.
    /// </summary>
    [TestMethod]
    public void TryGetRate_WhenBothProvidersHaveRate_ShouldReturnFirstProviderResult()
    {
        FixedDatedExchangeRateTable first = new(
        [
            new ExchangeRate("USD", "AUD", s_d1, 1.50m, "RBA"),
        ]);
        FixedDatedExchangeRateTable second = new(
        [
            new ExchangeRate("USD", "AUD", s_d1, 1.60m, "ECB"),
        ]);
        CompositeDatedExchangeRateProvider composite = new([first, second]);

        var found = composite.TryGetRate(
            "USD",
            "AUD",
            s_d1,
            ExchangeRateLookupOptions.Exact,
            out ExchangeRateLookupResult result);

        Assert.IsTrue(found);
        Assert.AreEqual(1.50m, result.Rate.Rate);
        Assert.AreEqual("RBA", result.Rate.Provider);
    }

    /// <summary>
    /// Verifies that even when the first provider would only resolve via fallback, FirstAvailable still uses it rather
    /// than reaching for a later provider with an exact match.
    /// </summary>
    [TestMethod]
    public void TryGetRate_WhenFirstProviderHasFallbackAndSecondHasExact_ShouldUseFirst()
    {
        FixedDatedExchangeRateTable first = new(
        [
            new ExchangeRate("USD", "AUD", new DateOnly(2024, 1, 1), 1.40m, "RBA"),
        ]);
        FixedDatedExchangeRateTable second = new(
        [
            new ExchangeRate("USD", "AUD", new DateOnly(2024, 1, 3), 1.50m, "ECB"),
        ]);
        CompositeDatedExchangeRateProvider composite = new([first, second]);

        var found = composite.TryGetRate(
            "USD",
            "AUD",
            new DateOnly(2024, 1, 3),
            ExchangeRateLookupOptions.PreviousWithin(10),
            out ExchangeRateLookupResult result);

        Assert.IsTrue(found);
        Assert.AreEqual("RBA", result.Rate.Provider);
    }

    /// <summary>
    /// Verifies that when all providers miss the composite returns <see langword="false" />.
    /// </summary>
    [TestMethod]
    public void TryGetRate_WhenAllProvidersMiss_ShouldReturnFalse()
    {
        FixedDatedExchangeRateTable empty = new([]);
        CompositeDatedExchangeRateProvider composite = new([empty, empty]);

        var found = composite.TryGetRate(
            "USD",
            "AUD",
            s_d1,
            ExchangeRateLookupOptions.Exact,
            out _);

        Assert.IsFalse(found);
    }

    /// <summary>
    /// Verifies that <see cref="CompositeDatedExchangeRateProvider.GetRate" /> throws <see cref="KeyNotFoundException" />
    /// when no provider can satisfy the lookup.
    /// </summary>
    [TestMethod]
    public void GetRate_WhenAllProvidersMiss_ShouldThrowKeyNotFoundException()
    {
        FixedDatedExchangeRateTable empty = new([]);
        CompositeDatedExchangeRateProvider composite = new([empty]);

        _ = Assert.ThrowsExactly<KeyNotFoundException>(() =>
            composite.GetRate("USD", "AUD", s_d1, ExchangeRateLookupOptions.Exact));
    }

    /// <summary>
    /// Verifies that the constructor throws when supplied with no providers.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenProvidersIsEmpty_ShouldThrowArgumentException()
    {
        ExceptionAssert.ThrowsExactlyWithParamName<ArgumentException>(
            () =>
            {
                _ = new CompositeDatedExchangeRateProvider([]);
            },
            "providers");
    }

    /// <summary>
    /// Verifies that the constructor throws when the supplied enumerable is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenProvidersIsNull_ShouldThrowArgumentNullException()
    {
        ExceptionAssert.ThrowsExactlyWithParamName<ArgumentNullException>(
            () =>
            {
                _ = new CompositeDatedExchangeRateProvider(null!);
            },
            "providers");
    }

    /// <summary>
    /// Verifies that the constructor throws when an element of the supplied enumerable is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenAnyProviderIsNull_ShouldThrowArgumentNullException()
    {
        ExceptionAssert.ThrowsExactlyWithParamName<ArgumentNullException>(
            () =>
            {
                _ = new CompositeDatedExchangeRateProvider([null!]);
            },
            "providers");
    }
}
