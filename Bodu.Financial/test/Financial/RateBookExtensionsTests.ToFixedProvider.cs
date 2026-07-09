// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RateBookExtensionsTests.ToFixedProvider.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.ExchangeRates;
using Bodu.Financial.Extensions;
using Bodu.Test.Assertions;

namespace Bodu.Financial;

public partial class RateBookExtensionsTests
{
    /// <summary>
    /// Verifies that a <see langword="null" /> book throws <see cref="ArgumentNullException" /> with the parameter
    /// name <c>book</c> on both overloads.
    /// </summary>
    [TestMethod]
    public void ToFixedProvider_WhenBookIsNull_ShouldThrowArgumentNullException()
    {
        ExceptionAssert.ThrowsExactlyWithParamName<ArgumentNullException>(
            () =>
            {
                _ = ((RateBook)null!).ToFixedProvider();
            },
            "book");

        ExceptionAssert.ThrowsExactlyWithParamName<ArgumentNullException>(
            () =>
            {
                _ = ((RateBook)null!).ToFixedProvider(["RBA"]);
            },
            "book");
    }

    /// <summary>
    /// Verifies that a single-provider book wraps into a provider that resolves its rates.
    /// </summary>
    [TestMethod]
    public void ToFixedProvider_WhenSingleProviderBook_ShouldResolveRates()
    {
        RateBook book = new([Series("RBA", 1.5m)]);

        FixedDatedRateProvider provider = book.ToFixedProvider();

        Assert.IsTrue(provider.TryGetRate("USD", "AUD", new DateOnly(2024, 1, 1), null, out RateLookupResult result));
        Assert.AreEqual(1.5m, result.Rate.Rate);
        Assert.AreSame(book, provider.Book);
    }

    /// <summary>
    /// Verifies that a book with two providers for the same pair is rejected by the priority-free overload, matching
    /// the underlying constructor's contract.
    /// </summary>
    [TestMethod]
    public void ToFixedProvider_WhenMultiProviderBookWithoutPriority_ShouldThrowArgumentException()
    {
        RateBook book = new([Series("RBA", 1.5m), Series("ECB", 1.51m)]);

        _ = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = book.ToFixedProvider();
        });
    }

    /// <summary>
    /// Verifies that the priority overload disambiguates a multi-provider book, resolving from the first listed
    /// provider with a matching series.
    /// </summary>
    [TestMethod]
    public void ToFixedProvider_WhenPrioritySupplied_ShouldResolveFromFirstMatchingProvider()
    {
        RateBook book = new([Series("RBA", 1.5m), Series("ECB", 1.51m)]);

        FixedDatedRateProvider provider = book.ToFixedProvider(["ECB", "RBA"]);

        Assert.IsTrue(provider.TryGetRate("USD", "AUD", new DateOnly(2024, 1, 1), null, out RateLookupResult result));
        Assert.AreEqual("ECB", result.Rate.Provider);
        Assert.AreEqual(1.51m, result.Rate.Rate);
    }
}
