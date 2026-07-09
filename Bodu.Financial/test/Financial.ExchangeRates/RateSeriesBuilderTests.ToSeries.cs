// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RateSeriesBuilderTests.ToSeries.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

public partial class RateSeriesBuilderTests
{
    /// <summary>
    /// Verifies that <see cref="RateSeriesBuilder.ToSeries" /> produces a snapshot whose observations match
    /// the builder's current state.
    /// </summary>
    [TestMethod]
    public void ToSeries_WhenBuilderHasObservations_ShouldReturnSnapshot()
    {
        RateSeriesBuilder builder = new(s_usdAud, "RBA");
        builder.AddRange(SampleObservations());

        RateSeries snapshot = builder.ToSeries();

        Assert.AreEqual(s_usdAud, snapshot.Pair);
        Assert.AreEqual("RBA", snapshot.Provider);
        Assert.AreEqual(3, snapshot.Count);
        CollectionAssert.AreEqual(SampleObservations(), snapshot.GetObservations().ToArray());
    }

    /// <summary>
    /// Verifies that calling <see cref="RateSeriesBuilder.ToSeries" /> on an empty builder throws
    /// <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void ToSeries_WhenBuilderIsEmpty_ShouldThrowInvalidOperationException()
    {
        RateSeriesBuilder builder = new(s_usdAud, "RBA");

        Assert.ThrowsExactly<InvalidOperationException>(builder.ToSeries);
    }

    /// <summary>
    /// Verifies that calling <see cref="RateSeriesBuilder.ToSeries" /> twice produces two separate series
    /// instances.
    /// </summary>
    [TestMethod]
    public void ToSeries_WhenCalledTwice_ShouldReturnSeparateInstances()
    {
        RateSeriesBuilder builder = new(s_usdAud, "RBA");
        builder.AddRange(SampleObservations());

        RateSeries first = builder.ToSeries();
        RateSeries second = builder.ToSeries();

        Assert.AreNotSame(first, second);
        CollectionAssert.AreEqual(first.GetObservations().ToArray(), second.GetObservations().ToArray());
    }

    /// <summary>
    /// Verifies that mutating the builder after producing a snapshot does not affect the snapshot.
    /// </summary>
    [TestMethod]
    public void ToSeries_WhenBuilderMutatedAfterCall_ShouldNotAffectSnapshot()
    {
        RateSeriesBuilder builder = new(s_usdAud, "RBA");
        builder.Add(new DateOnly(2026, 6, 1), 1.50m);

        RateSeries snapshot = builder.ToSeries();
        builder.Upsert(new DateOnly(2026, 6, 1), 99.99m);
        builder.Add(new DateOnly(2026, 6, 2), 1.51m);

        Assert.AreEqual(1, snapshot.Count);
        Assert.IsTrue(snapshot.TryGetRate(new DateOnly(2026, 6, 1), RateLookupOptions.Exact, out _, out decimal rate));
        Assert.AreEqual(1.50m, rate);
    }

    /// <summary>
    /// Verifies that producing a fresh series from <see cref="RateSeries.WithRate" /> does not affect the
    /// underlying builder seeded by <see cref="RateSeries.ToBuilder" />.
    /// </summary>
    [TestMethod]
    public void ToSeries_WhenSnapshotMutated_ShouldNotAffectBuilder()
    {
        RateSeries original = new(s_usdAud, "RBA", SampleObservations());
        RateSeriesBuilder builder = original.ToBuilder();

        RateSeries updated = original.WithRate(new DateOnly(2026, 6, 1), 99m);

        Assert.IsTrue(builder.TryGetRate(new DateOnly(2026, 6, 1), out decimal rate));
        Assert.AreEqual(1.50m, rate);
        Assert.IsTrue(updated.TryGetRate(new DateOnly(2026, 6, 1), RateLookupOptions.Exact, out _, out decimal updatedRate));
        Assert.AreEqual(99m, updatedRate);
    }
}
