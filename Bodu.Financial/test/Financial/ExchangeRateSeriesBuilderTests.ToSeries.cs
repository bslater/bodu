// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateSeriesBuilderTests.ToSeries.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial;

public partial class ExchangeRateSeriesBuilderTests
{
    /// <summary>
    /// Verifies that <see cref="ExchangeRateSeriesBuilder.ToSeries" /> produces a snapshot whose observations match
    /// the builder's current state.
    /// </summary>
    [TestMethod]
    public void ToSeries_WhenBuilderHasObservations_ShouldReturnSnapshot()
    {
        ExchangeRateSeriesBuilder builder = new(s_usdAud, "RBA");
        builder.AddRange(SampleObservations());

        ExchangeRateSeries snapshot = builder.ToSeries();

        Assert.AreEqual(s_usdAud, snapshot.Pair);
        Assert.AreEqual("RBA", snapshot.Provider);
        Assert.AreEqual(3, snapshot.Count);
        CollectionAssert.AreEqual(SampleObservations(), snapshot.GetObservations().ToArray());
    }

    /// <summary>
    /// Verifies that calling <see cref="ExchangeRateSeriesBuilder.ToSeries" /> on an empty builder throws
    /// <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void ToSeries_WhenBuilderIsEmpty_ShouldThrowInvalidOperationException()
    {
        ExchangeRateSeriesBuilder builder = new(s_usdAud, "RBA");

        Assert.ThrowsExactly<InvalidOperationException>(() => builder.ToSeries());
    }

    /// <summary>
    /// Verifies that calling <see cref="ExchangeRateSeriesBuilder.ToSeries" /> twice produces two separate series
    /// instances.
    /// </summary>
    [TestMethod]
    public void ToSeries_WhenCalledTwice_ShouldReturnSeparateInstances()
    {
        ExchangeRateSeriesBuilder builder = new(s_usdAud, "RBA");
        builder.AddRange(SampleObservations());

        ExchangeRateSeries first = builder.ToSeries();
        ExchangeRateSeries second = builder.ToSeries();

        Assert.AreNotSame(first, second);
        CollectionAssert.AreEqual(first.GetObservations().ToArray(), second.GetObservations().ToArray());
    }

    /// <summary>
    /// Verifies that mutating the builder after producing a snapshot does not affect the snapshot.
    /// </summary>
    [TestMethod]
    public void ToSeries_WhenBuilderMutatedAfterCall_ShouldNotAffectSnapshot()
    {
        ExchangeRateSeriesBuilder builder = new(s_usdAud, "RBA");
        builder.Add(new DateOnly(2026, 6, 1), 1.50m);

        ExchangeRateSeries snapshot = builder.ToSeries();
        builder.Upsert(new DateOnly(2026, 6, 1), 99.99m);
        builder.Add(new DateOnly(2026, 6, 2), 1.51m);

        Assert.AreEqual(1, snapshot.Count);
        Assert.IsTrue(snapshot.TryGetRate(new DateOnly(2026, 6, 1), ExchangeRateLookupOptions.Exact, out _, out var rate));
        Assert.AreEqual(1.50m, rate);
    }

    /// <summary>
    /// Verifies that producing a fresh series from <see cref="ExchangeRateSeries.WithRate" /> does not affect the
    /// underlying builder seeded by <see cref="ExchangeRateSeries.ToBuilder" />.
    /// </summary>
    [TestMethod]
    public void ToSeries_WhenSnapshotMutated_ShouldNotAffectBuilder()
    {
        ExchangeRateSeries original = new(s_usdAud, "RBA", SampleObservations());
        ExchangeRateSeriesBuilder builder = original.ToBuilder();

        ExchangeRateSeries updated = original.WithRate(new DateOnly(2026, 6, 1), 99m);

        Assert.IsTrue(builder.TryGetRate(new DateOnly(2026, 6, 1), out var rate));
        Assert.AreEqual(1.50m, rate);
        Assert.IsTrue(updated.TryGetRate(new DateOnly(2026, 6, 1), ExchangeRateLookupOptions.Exact, out _, out var updatedRate));
        Assert.AreEqual(99m, updatedRate);
    }
}
