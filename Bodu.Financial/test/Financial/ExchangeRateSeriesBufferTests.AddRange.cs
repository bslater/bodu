// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateSeriesBufferTests.AddRange.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Assertions;

namespace Bodu.Financial;

public partial class ExchangeRateSeriesBufferTests
{
    /// <summary>
    /// Verifies that an empty incoming sequence is a no-op against an empty buffer.
    /// </summary>
    [TestMethod]
    public void AddRange_WhenIncomingEmptyOnEmptyBuffer_ShouldRemainEmpty()
    {
        ExchangeRateSeriesBuffer buffer = NewBuffer();

        buffer.AddRange(Array.Empty<ExchangeRateObservation>(), ObservationsParam);

        Assert.IsTrue(buffer.IsEmpty);
    }

    /// <summary>
    /// Verifies that an empty incoming sequence is a no-op against a populated buffer.
    /// </summary>
    [TestMethod]
    public void AddRange_WhenIncomingEmptyOnPopulatedBuffer_ShouldLeaveBufferUnchanged()
    {
        ExchangeRateSeriesBuffer buffer = NewBufferWith((1000, 1.4m), (1010, 1.5m));
        ExchangeRateObservation[] before = buffer.Enumerate().ToArray();

        buffer.AddRange(Array.Empty<ExchangeRateObservation>(), ObservationsParam);

        CollectionAssert.AreEqual(before, buffer.Enumerate().ToArray());
    }

    /// <summary>
    /// Verifies that an unsorted incoming batch is reordered into strictly ascending day order.
    /// </summary>
    [TestMethod]
    public void AddRange_WhenIncomingUnsorted_ShouldSortBeforeMerging()
    {
        ExchangeRateSeriesBuffer buffer = NewBuffer();
        ExchangeRateObservation[] batch =
        [
            new(DateOnly.FromDayNumber(1020), 1.6m),
            new(DateOnly.FromDayNumber(1000), 1.4m),
            new(DateOnly.FromDayNumber(1010), 1.5m),
        ];

        buffer.AddRange(batch, ObservationsParam);

        ExchangeRateObservation[] observations = buffer.Enumerate().ToArray();
        Assert.AreEqual(DateOnly.FromDayNumber(1000), observations[0].Date);
        Assert.AreEqual(DateOnly.FromDayNumber(1010), observations[1].Date);
        Assert.AreEqual(DateOnly.FromDayNumber(1020), observations[2].Date);
    }

    /// <summary>
    /// Verifies that an incoming batch interleaved with existing entries produces a single sorted result.
    /// </summary>
    [TestMethod]
    public void AddRange_WhenIncomingInterleavedWithExisting_ShouldMergeIntoSortedOrder()
    {
        ExchangeRateSeriesBuffer buffer = NewBufferWith((1000, 1.4m), (1020, 1.6m), (1040, 1.8m));
        ExchangeRateObservation[] batch =
        [
            new(DateOnly.FromDayNumber(1010), 1.5m),
            new(DateOnly.FromDayNumber(1030), 1.7m),
        ];

        buffer.AddRange(batch, ObservationsParam);

        Assert.AreEqual(5, buffer.Count);
        ExchangeRateObservation[] observations = buffer.Enumerate().ToArray();
        Assert.AreEqual(DateOnly.FromDayNumber(1000), observations[0].Date);
        Assert.AreEqual(DateOnly.FromDayNumber(1010), observations[1].Date);
        Assert.AreEqual(DateOnly.FromDayNumber(1020), observations[2].Date);
        Assert.AreEqual(DateOnly.FromDayNumber(1030), observations[3].Date);
        Assert.AreEqual(DateOnly.FromDayNumber(1040), observations[4].Date);
    }

    /// <summary>
    /// Verifies that an incoming batch whose dates all precede the buffer's contents merges in front while
    /// preserving the buffer's existing order.
    /// </summary>
    [TestMethod]
    public void AddRange_WhenIncomingPrecedesExisting_ShouldMergeAtFront()
    {
        ExchangeRateSeriesBuffer buffer = NewBufferWith((1020, 1.6m), (1030, 1.7m));
        ExchangeRateObservation[] batch =
        [
            new(DateOnly.FromDayNumber(1000), 1.4m),
            new(DateOnly.FromDayNumber(1010), 1.5m),
        ];

        buffer.AddRange(batch, ObservationsParam);

        Assert.AreEqual(4, buffer.Count);
        ExchangeRateObservation[] observations = buffer.Enumerate().ToArray();
        Assert.AreEqual(DateOnly.FromDayNumber(1000), observations[0].Date);
        Assert.AreEqual(DateOnly.FromDayNumber(1030), observations[3].Date);
    }

    /// <summary>
    /// Verifies that an incoming batch whose dates all follow the buffer's contents merges at the end.
    /// </summary>
    [TestMethod]
    public void AddRange_WhenIncomingFollowsExisting_ShouldAppendAfter()
    {
        ExchangeRateSeriesBuffer buffer = NewBufferWith((1000, 1.4m), (1010, 1.5m));
        ExchangeRateObservation[] batch =
        [
            new(DateOnly.FromDayNumber(1020), 1.6m),
            new(DateOnly.FromDayNumber(1030), 1.7m),
        ];

        buffer.AddRange(batch, ObservationsParam);

        Assert.AreEqual(4, buffer.Count);
        ExchangeRateObservation[] observations = buffer.Enumerate().ToArray();
        Assert.AreEqual(DateOnly.FromDayNumber(1000), observations[0].Date);
        Assert.AreEqual(DateOnly.FromDayNumber(1030), observations[3].Date);
    }

    /// <summary>
    /// Verifies that the batch is rejected as a whole when one of its entries collides with an existing date.
    /// </summary>
    [TestMethod]
    public void AddRange_WhenAnyIncomingDateCollidesWithExisting_ShouldThrowAndLeaveBufferUnchanged()
    {
        ExchangeRateSeriesBuffer buffer = NewBufferWith((1000, 1.4m), (1020, 1.6m));
        ExchangeRateObservation[] before = buffer.Enumerate().ToArray();

        ExchangeRateObservation[] batch =
        [
            new(DateOnly.FromDayNumber(1010), 1.5m),
            new(DateOnly.FromDayNumber(1020), 1.65m), // collides
            new(DateOnly.FromDayNumber(1030), 1.7m),
        ];

        ExceptionAssert.ThrowsExactlyWithParamName<ArgumentException>(
            () =>
            {
                buffer.AddRange(batch, ObservationsParam);
            },
            ObservationsParam);

        CollectionAssert.AreEqual(before, buffer.Enumerate().ToArray());
    }

    /// <summary>
    /// Verifies that an in-batch duplicate triggers an <see cref="ArgumentException" /> using the supplied
    /// parameter name and leaves the buffer unchanged.
    /// </summary>
    [TestMethod]
    public void AddRange_WhenIncomingBatchContainsDuplicate_ShouldThrowAndLeaveBufferUnchanged()
    {
        ExchangeRateSeriesBuffer buffer = NewBufferWith((1000, 1.4m));
        ExchangeRateObservation[] before = buffer.Enumerate().ToArray();

        ExchangeRateObservation[] batch =
        [
            new(DateOnly.FromDayNumber(1010), 1.5m),
            new(DateOnly.FromDayNumber(1010), 1.55m),
        ];

        ExceptionAssert.ThrowsExactlyWithParamName<ArgumentException>(
            () =>
            {
                buffer.AddRange(batch, "customParam");
            },
            "customParam");

        CollectionAssert.AreEqual(before, buffer.Enumerate().ToArray());
    }

    /// <summary>
    /// Verifies that any non-positive rate in the batch raises <see cref="ArgumentOutOfRangeException" /> and the
    /// buffer remains untouched.
    /// </summary>
    [TestMethod]
    public void AddRange_WhenAnyRateInvalid_ShouldThrowAndLeaveBufferUnchanged()
    {
        ExchangeRateSeriesBuffer buffer = NewBufferWith((1000, 1.4m));
        ExchangeRateObservation[] before = buffer.Enumerate().ToArray();

        ExchangeRateObservation[] batch =
        [
            new(DateOnly.FromDayNumber(1010), 1.5m),
            new(DateOnly.FromDayNumber(1020), 0m),
        ];

        ExceptionAssert.ThrowsExactlyWithParamName<ArgumentOutOfRangeException>(
            () =>
            {
                buffer.AddRange(batch, ObservationsParam);
            },
            ObservationsParam);

        CollectionAssert.AreEqual(before, buffer.Enumerate().ToArray());
    }
}
