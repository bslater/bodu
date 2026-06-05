// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateSeriesBufferTests.ToStorage.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial;

public partial class ExchangeRateSeriesBufferTests
{
    /// <summary>
    /// Verifies that <see cref="ExchangeRateSeriesBuffer.ToStorage" /> on an empty buffer throws
    /// <see cref="InvalidOperationException" /> with the empty-builder message.
    /// </summary>
    [TestMethod]
    public void ToStorage_WhenBufferEmpty_ShouldThrowInvalidOperationException()
    {
        ExchangeRateSeriesBuffer buffer = NewBuffer();

        var ex = Assert.ThrowsExactly<InvalidOperationException>(() => buffer.ToStorage());
        Assert.AreEqual(FinancialResourceStrings.Op_Invalid_RateSeriesBuilderEmpty, ex.Message);
    }

    /// <summary>
    /// Verifies that <see cref="ExchangeRateSeriesBuffer.ToStorage" /> trims to the live segment so that excess
    /// buffer capacity does not leak into the snapshot.
    /// </summary>
    [TestMethod]
    public void ToStorage_WhenBufferOverCapacity_ShouldTrimToLiveCount()
    {
        ExchangeRateSeriesBuffer buffer = new(64);
        buffer.Add(1000, 1.4m, RateParam, DateParam);
        buffer.Add(1010, 1.5m, RateParam, DateParam);

        ExchangeRateSeriesStorage storage = buffer.ToStorage();

        Assert.AreEqual(2, storage.Count);
        Assert.AreEqual(DateOnly.FromDayNumber(1000), storage.FirstDate);
        Assert.AreEqual(DateOnly.FromDayNumber(1010), storage.LastDate);
    }

    /// <summary>
    /// Verifies that further mutations on the buffer do not bleed into a previously produced storage snapshot.
    /// </summary>
    [TestMethod]
    public void ToStorage_WhenBufferMutatedAfterCall_ShouldNotAffectSnapshot()
    {
        ExchangeRateSeriesBuffer buffer = NewBufferWith((1000, 1.4m));
        ExchangeRateSeriesStorage snapshot = buffer.ToStorage();

        buffer.Upsert(1000, 99m, RateParam);
        buffer.Add(1010, 1.5m, RateParam, DateParam);

        var observations = snapshot.Enumerate().ToArray();
        Assert.AreEqual(1, observations.Length);
        Assert.AreEqual(1.4m, observations[0].Rate);
    }

    /// <summary>
    /// Verifies that <see cref="ExchangeRateSeriesBuffer.FromStorage" /> copies every observation from the source
    /// storage into a new buffer.
    /// </summary>
    [TestMethod]
    public void FromStorage_WhenSeeded_ShouldCopyAllObservations()
    {
        var source = ExchangeRateSeriesStorage.Create(
            new[]
            {
                new ExchangeRateObservation(DateOnly.FromDayNumber(1000), 1.4m),
                new ExchangeRateObservation(DateOnly.FromDayNumber(1010), 1.5m),
                new ExchangeRateObservation(DateOnly.FromDayNumber(1020), 1.6m),
            },
            ObservationsParam);

        var buffer = ExchangeRateSeriesBuffer.FromStorage(source);

        Assert.AreEqual(3, buffer.Count);
        Assert.IsTrue(buffer.Contains(1000));
        Assert.IsTrue(buffer.Contains(1010));
        Assert.IsTrue(buffer.Contains(1020));
    }

    /// <summary>
    /// Verifies that a buffer seeded by <see cref="ExchangeRateSeriesBuffer.FromStorage" /> can be mutated without
    /// affecting the source storage.
    /// </summary>
    [TestMethod]
    public void FromStorage_WhenBufferMutated_ShouldNotAffectSourceStorage()
    {
        var source = ExchangeRateSeriesStorage.Create(
            new[] { new ExchangeRateObservation(DateOnly.FromDayNumber(1000), 1.4m) },
            ObservationsParam);

        var buffer = ExchangeRateSeriesBuffer.FromStorage(source);
        buffer.Upsert(1000, 99m, RateParam);
        buffer.Add(1010, 1.5m, RateParam, DateParam);

        Assert.AreEqual(1, source.Count);
        var sourceObservations = source.Enumerate().ToArray();
        Assert.AreEqual(1.4m, sourceObservations[0].Rate);
    }

    /// <summary>
    /// Verifies that <see cref="ExchangeRateSeriesBuffer.Enumerate" /> emits observations in strictly ascending
    /// date order.
    /// </summary>
    [TestMethod]
    public void Enumerate_WhenInvoked_ShouldYieldInAscendingOrder()
    {
        ExchangeRateSeriesBuffer buffer = NewBuffer();
        buffer.Add(1020, 1.6m, RateParam, DateParam);
        buffer.Add(1000, 1.4m, RateParam, DateParam);
        buffer.Add(1010, 1.5m, RateParam, DateParam);

        var observations = buffer.Enumerate().ToArray();

        Assert.AreEqual(3, observations.Length);
        Assert.IsTrue(observations[0].Date < observations[1].Date);
        Assert.IsTrue(observations[1].Date < observations[2].Date);
    }

    /// <summary>
    /// Verifies that two enumeration passes over the same buffer yield equivalent observation sequences.
    /// </summary>
    [TestMethod]
    public void Enumerate_WhenInvokedTwice_ShouldYieldEquivalentSequences()
    {
        ExchangeRateSeriesBuffer buffer = NewBufferWith((1000, 1.4m), (1010, 1.5m), (1020, 1.6m));

        var first = buffer.Enumerate().ToArray();
        var second = buffer.Enumerate().ToArray();

        CollectionAssert.AreEqual(first, second);
    }
}
