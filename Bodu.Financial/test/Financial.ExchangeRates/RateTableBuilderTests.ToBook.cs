// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RateTableBuilderTests.ToBook.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;

namespace Bodu.Financial.ExchangeRates;

public partial class RateTableBuilderTests
{
    /// <summary>
    /// Verifies that <c>ToBook</c> produces an immutable <see cref="RateBook" /> preserving every
    /// pair/provider key, including cases where the same pair is published by multiple providers.
    /// </summary>
    [TestMethod]
    public void ToBook_WhenBuilderHoldsMultipleProviders_ShouldProduceImmutableBookPreservingKeys()
    {
        RateTableBuilder table = new();
        table.Upsert(s_usdAud, "RBA", new DateOnly(2024, 1, 1), 1.5m);
        table.Upsert(s_usdAud, "ECB", new DateOnly(2024, 1, 1), 1.6m);
        table.Upsert(new CurrencyPair(CurrencyCode.EUR, CurrencyCode.AUD), "RBA", new DateOnly(2024, 1, 1), 1.7m);

        RateBook book = table.ToBook();

        Assert.AreEqual(3, book.Count);
        Assert.IsTrue(book.TryGetSeries(s_usdAud, "RBA", out _));
        Assert.IsTrue(book.TryGetSeries(s_usdAud, "ECB", out _));
        Assert.IsTrue(book.TryGetSeries(new CurrencyPair(CurrencyCode.EUR, CurrencyCode.AUD), "RBA", out _));
    }

    /// <summary>
    /// Verifies that <c>ToBook</c> skips empty builders, because an immutable <see cref="RateSeries" />
    /// must hold at least one observation.
    /// </summary>
    [TestMethod]
    public void ToBook_WhenBuilderHoldsEmptySeries_ShouldSkipIt()
    {
        RateTableBuilder table = new();
        _ = table.GetOrAddSeries(s_usdAud, "RBA");
        table.Upsert(new CurrencyPair(CurrencyCode.EUR, CurrencyCode.AUD), "RBA", new DateOnly(2024, 1, 1), 1.7m);

        RateBook book = table.ToBook();

        Assert.AreEqual(1, book.Count);
        Assert.IsFalse(book.TryGetSeries(s_usdAud, "RBA", out _));
    }

    /// <summary>
    /// Verifies that mutating the table after producing a book does not affect the book's view of the world.
    /// </summary>
    [TestMethod]
    public void ToBook_AfterFurtherMutation_ShouldNotChangeBook()
    {
        RateTableBuilder table = new();
        table.Upsert(s_usdAud, "RBA", new DateOnly(2024, 1, 1), 1.5m);

        RateBook book = table.ToBook();

        table.Upsert(s_usdAud, "RBA", new DateOnly(2024, 1, 2), 1.55m);

        Assert.IsTrue(book.TryGetSeries(s_usdAud, "RBA", out RateSeries? snapshot));
        Assert.AreEqual(1, snapshot!.Count);
    }
}
