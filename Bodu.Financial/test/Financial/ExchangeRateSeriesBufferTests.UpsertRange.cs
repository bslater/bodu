// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateSeriesBufferTests.UpsertRange.cs" company="Bodu Pty. Ltd.">
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
    public void UpsertRange_WhenIncomingEmptyOnEmptyBuffer_ShouldRemainEmpty()
    {
        ExchangeRateSeriesBuffer buffer = NewBuffer();

        buffer.UpsertRange(Array.Empty<ExchangeRateObservation>(), ObservationsParam);

        Assert.IsTrue(buffer.IsEmpty);
    }

    /// <summary>
    /// Verifies that batch entries that collide with existing dates are replaced and untouched entries remain.
    /// </summary>
    [TestMethod]
    public void UpsertRange_WhenIncomingCollidesWithExisting_ShouldReplaceMatchingAndPreserveOthers()
    {
        ExchangeRateSeriesBuffer buffer = NewBufferWith((1000, 1.4m), (1010, 1.5m), (1020, 1.6m));
        ExchangeRateObservation[] batch =
        [
            new(DateOnly.FromDayNumber(1010), 2.5m),
            new(DateOnly.FromDayNumber(1020), 2.6m),
        ];

        buffer.UpsertRange(batch, ObservationsParam);

        Assert.AreEqual(3, buffer.Count);
        Assert.IsTrue(buffer.TryGetRate(1000, out decimal r1));
        Assert.IsTrue(buffer.TryGetRate(1010, out decimal r2));
        Assert.IsTrue(buffer.TryGetRate(1020, out decimal r3));
        Assert.AreEqual(1.4m, r1);
        Assert.AreEqual(2.5m, r2);
        Assert.AreEqual(2.6m, r3);
    }

    /// <summary>
    /// Verifies that a batch containing both new and existing dates simultaneously inserts the new entries and
    /// replaces the existing ones.
    /// </summary>
    [TestMethod]
    public void UpsertRange_WhenIncomingMixesNewAndExisting_ShouldInsertAndReplaceCorrectly()
    {
        ExchangeRateSeriesBuffer buffer = NewBufferWith((1000, 1.4m), (1020, 1.6m));
        ExchangeRateObservation[] batch =
        [
            new(DateOnly.FromDayNumber(1010), 1.5m), // new
            new(DateOnly.FromDayNumber(1020), 2.6m), // replace
            new(DateOnly.FromDayNumber(1030), 1.7m), // new
        ];

        buffer.UpsertRange(batch, ObservationsParam);

        Assert.AreEqual(4, buffer.Count);
        ExchangeRateObservation[] observations = buffer.Enumerate().ToArray();
        Assert.AreEqual(DateOnly.FromDayNumber(1000), observations[0].Date);
        Assert.AreEqual(1.4m, observations[0].Rate);
        Assert.AreEqual(DateOnly.FromDayNumber(1010), observations[1].Date);
        Assert.AreEqual(DateOnly.FromDayNumber(1020), observations[2].Date);
        Assert.AreEqual(2.6m, observations[2].Rate);
        Assert.AreEqual(DateOnly.FromDayNumber(1030), observations[3].Date);
    }

    /// <summary>
    /// Verifies that an in-batch duplicate raises <see cref="ArgumentException" /> and leaves the buffer unchanged.
    /// </summary>
    [TestMethod]
    public void UpsertRange_WhenIncomingBatchContainsDuplicate_ShouldThrowAndLeaveBufferUnchanged()
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
                buffer.UpsertRange(batch, ObservationsParam);
            },
            ObservationsParam);

        CollectionAssert.AreEqual(before, buffer.Enumerate().ToArray());
    }

    /// <summary>
    /// Verifies that a non-positive rate in the batch raises <see cref="ArgumentOutOfRangeException" /> and the
    /// buffer remains untouched.
    /// </summary>
    [TestMethod]
    public void UpsertRange_WhenAnyRateInvalid_ShouldThrowAndLeaveBufferUnchanged()
    {
        ExchangeRateSeriesBuffer buffer = NewBufferWith((1000, 1.4m));
        ExchangeRateObservation[] before = buffer.Enumerate().ToArray();

        ExchangeRateObservation[] batch =
        [
            new(DateOnly.FromDayNumber(1010), 1.5m),
            new(DateOnly.FromDayNumber(1020), -1m),
        ];

        ExceptionAssert.ThrowsExactlyWithParamName<ArgumentOutOfRangeException>(
            () =>
            {
                buffer.UpsertRange(batch, ObservationsParam);
            },
            ObservationsParam);

        CollectionAssert.AreEqual(before, buffer.Enumerate().ToArray());
    }
}
