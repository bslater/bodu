// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateSeriesBuilderTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test;
using Bodu.Test.Assertions;

namespace Bodu.Financial;

[TestClass]
public partial class ExchangeRateSeriesBuilderTests
{
    private static readonly ExchangeRatePair s_usdAud = new("USD", "AUD");

    private static ExchangeRateObservation[] SampleObservations() =>
    [
        new(new DateOnly(2026, 6, 1), 1.50m),
        new(new DateOnly(2026, 6, 3), 1.52m),
        new(new DateOnly(2026, 6, 5), 1.54m),
    ];

    /// <summary>
    /// Verifies that the smoke-tier happy path builds an empty builder, accepts a single observation, and
    /// produces an immutable snapshot exposing the inserted observation.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Smoke)]
    public void Builder_WhenAddingSingleObservationAndSnapshotting_ShouldExposeObservationInSeries()
    {
        ExchangeRateSeriesBuilder builder = new(s_usdAud, "RBA");
        builder.Add(new DateOnly(2026, 6, 1), 1.50m);

        ExchangeRateSeries snapshot = builder.ToSeries();

        Assert.AreEqual(s_usdAud, snapshot.Pair);
        Assert.AreEqual("RBA", snapshot.Provider);
        Assert.AreEqual(1, snapshot.Count);
        Assert.IsTrue(snapshot.TryGetRate(new DateOnly(2026, 6, 1), ExchangeRateLookupOptions.Exact, out _, out var rate));
        Assert.AreEqual(1.50m, rate);
    }

    /// <summary>
    /// Verifies that a freshly constructed builder stores the supplied pair and provider and reports empty state.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenInputsValid_ShouldStorePairProviderAndEmpty()
    {
        ExchangeRateSeriesBuilder builder = new(s_usdAud, "RBA");

        Assert.AreEqual(s_usdAud, builder.Pair);
        Assert.AreEqual("RBA", builder.Provider);
        Assert.AreEqual(0, builder.Count);
        Assert.IsTrue(builder.IsEmpty);
    }

    /// <summary>
    /// Verifies that a null provider on the empty constructor throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenProviderIsNull_ShouldThrowArgumentNullException()
    {
        ExceptionAssert.ThrowsExactlyWithParamName<ArgumentNullException>(
            () =>
            {
                _ = new ExchangeRateSeriesBuilder(s_usdAud, null!);
            },
            "provider");
    }

    /// <summary>
    /// Verifies that a white-space provider on the empty constructor throws <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenProviderIsEmpty_ShouldThrowArgumentException()
    {
        ExceptionAssert.ThrowsExactlyWithParamName<ArgumentException>(
            () =>
            {
                _ = new ExchangeRateSeriesBuilder(s_usdAud, "   ");
            },
            "provider");
    }

    /// <summary>
    /// Verifies that a null series on the copy constructor throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenSeriesIsNull_ShouldThrowArgumentNullException()
    {
        ExceptionAssert.ThrowsExactlyWithParamName<ArgumentNullException>(
            () =>
            {
                _ = new ExchangeRateSeriesBuilder((ExchangeRateSeries)null!);
            },
            "series");
    }

    /// <summary>
    /// Verifies that the copy constructor seeds the builder with the same observations as the source series.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenSeriesSupplied_ShouldCopyObservations()
    {
        ExchangeRateSeries series = new(s_usdAud, "RBA", SampleObservations());
        ExchangeRateSeriesBuilder builder = new(series);

        Assert.AreEqual(s_usdAud, builder.Pair);
        Assert.AreEqual("RBA", builder.Provider);
        Assert.AreEqual(3, builder.Count);
        CollectionAssert.AreEqual(series.GetObservations().ToArray(), builder.GetObservations().ToArray());
    }
}
