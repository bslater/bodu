// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateTableBuilderTests.ToBook.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial;

public partial class ExchangeRateTableBuilderTests
{
    /// <summary>
    /// Verifies that <c>ToBook</c> produces an immutable <see cref="ExchangeRateBook" /> preserving every
    /// pair/provider key, including cases where the same pair is published by multiple providers.
    /// </summary>
    [TestMethod]
    public void ToBook_WhenBuilderHoldsMultipleProviders_ShouldProduceImmutableBookPreservingKeys()
    {
        ExchangeRateTableBuilder table = new();
        table.Upsert(s_usdAud, "RBA", new DateOnly(2024, 1, 1), 1.5m);
        table.Upsert(s_usdAud, "ECB", new DateOnly(2024, 1, 1), 1.6m);
        table.Upsert(new ExchangeRatePair("EUR", "AUD"), "RBA", new DateOnly(2024, 1, 1), 1.7m);

        ExchangeRateBook book = table.ToBook();

        Assert.AreEqual(3, book.Count);
        Assert.IsTrue(book.TryGetSeries(s_usdAud, "RBA", out _));
        Assert.IsTrue(book.TryGetSeries(s_usdAud, "ECB", out _));
        Assert.IsTrue(book.TryGetSeries(new ExchangeRatePair("EUR", "AUD"), "RBA", out _));
    }

    /// <summary>
    /// Verifies that <c>ToBook</c> skips empty builders, because an immutable <see cref="ExchangeRateSeries" />
    /// must hold at least one observation.
    /// </summary>
    [TestMethod]
    public void ToBook_WhenBuilderHoldsEmptySeries_ShouldSkipIt()
    {
        ExchangeRateTableBuilder table = new();
        _ = table.GetOrAddSeries(s_usdAud, "RBA");
        table.Upsert(new ExchangeRatePair("EUR", "AUD"), "RBA", new DateOnly(2024, 1, 1), 1.7m);

        ExchangeRateBook book = table.ToBook();

        Assert.AreEqual(1, book.Count);
        Assert.IsFalse(book.TryGetSeries(s_usdAud, "RBA", out _));
    }

    /// <summary>
    /// Verifies that mutating the table after producing a book does not affect the book's view of the world.
    /// </summary>
    [TestMethod]
    public void ToBook_AfterFurtherMutation_ShouldNotChangeBook()
    {
        ExchangeRateTableBuilder table = new();
        table.Upsert(s_usdAud, "RBA", new DateOnly(2024, 1, 1), 1.5m);

        ExchangeRateBook book = table.ToBook();

        table.Upsert(s_usdAud, "RBA", new DateOnly(2024, 1, 2), 1.55m);

        Assert.IsTrue(book.TryGetSeries(s_usdAud, "RBA", out ExchangeRateSeries? snapshot));
        Assert.AreEqual(1, snapshot!.Count);
    }
}
