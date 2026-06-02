// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateSeriesBufferTests.Remove.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial;

public partial class ExchangeRateSeriesBufferTests
{
    /// <summary>
    /// Verifies that removing the first observation shifts subsequent entries left and reports success.
    /// </summary>
    [TestMethod]
    public void Remove_WhenRemovingFront_ShouldShiftRemainingLeft()
    {
        ExchangeRateSeriesBuffer buffer = NewBufferWith((1000, 1.4m), (1010, 1.5m), (1020, 1.6m));

        var removed = buffer.Remove(1000);

        Assert.IsTrue(removed);
        Assert.AreEqual(2, buffer.Count);
        var observations = buffer.Enumerate().ToArray();
        Assert.AreEqual(DateOnly.FromDayNumber(1010), observations[0].Date);
        Assert.AreEqual(DateOnly.FromDayNumber(1020), observations[1].Date);
    }

    /// <summary>
    /// Verifies that removing the middle observation preserves the order of the remaining entries.
    /// </summary>
    [TestMethod]
    public void Remove_WhenRemovingMiddle_ShouldPreserveSurroundingOrder()
    {
        ExchangeRateSeriesBuffer buffer = NewBufferWith((1000, 1.4m), (1010, 1.5m), (1020, 1.6m));

        var removed = buffer.Remove(1010);

        Assert.IsTrue(removed);
        Assert.AreEqual(2, buffer.Count);
        Assert.IsFalse(buffer.Contains(1010));
        Assert.IsTrue(buffer.Contains(1000));
        Assert.IsTrue(buffer.Contains(1020));
    }

    /// <summary>
    /// Verifies that removing the last observation reduces the count by one and clears the trailing slot so a
    /// subsequent insert reuses the capacity correctly.
    /// </summary>
    [TestMethod]
    public void Remove_WhenRemovingBack_ShouldReduceCountAndAllowReinsert()
    {
        ExchangeRateSeriesBuffer buffer = NewBufferWith((1000, 1.4m), (1010, 1.5m), (1020, 1.6m));

        var removed = buffer.Remove(1020);

        Assert.IsTrue(removed);
        Assert.AreEqual(2, buffer.Count);
        Assert.IsFalse(buffer.Contains(1020));

        buffer.Add(1030, 1.7m, RateParam, DateParam);
        Assert.IsTrue(buffer.Contains(1030));
        Assert.IsTrue(buffer.TryGetRate(1030, out var rate));
        Assert.AreEqual(1.7m, rate);
    }

    /// <summary>
    /// Verifies that removing every observation leaves the buffer empty and ready for fresh inserts.
    /// </summary>
    [TestMethod]
    public void Remove_WhenRemovingEvery_ShouldEmptyBuffer()
    {
        ExchangeRateSeriesBuffer buffer = NewBufferWith((1000, 1.4m), (1010, 1.5m));

        buffer.Remove(1000);
        buffer.Remove(1010);

        Assert.IsTrue(buffer.IsEmpty);
        Assert.AreEqual(0, buffer.Count);

        buffer.Add(1020, 1.6m, RateParam, DateParam);
        Assert.AreEqual(1, buffer.Count);
    }

    /// <summary>
    /// Verifies that removing an unknown day number returns <see langword="false" /> and does not mutate the
    /// buffer.
    /// </summary>
    [TestMethod]
    public void Remove_WhenDayMissing_ShouldReturnFalseAndNotMutate()
    {
        ExchangeRateSeriesBuffer buffer = NewBufferWith((1000, 1.4m));

        var removed = buffer.Remove(1010);

        Assert.IsFalse(removed);
        Assert.AreEqual(1, buffer.Count);
    }

    /// <summary>
    /// Verifies that <see cref="ExchangeRateSeriesBuffer.Contains" /> reports <see langword="true" /> for a present
    /// day number and <see langword="false" /> for a missing one.
    /// </summary>
    [TestMethod]
    public void Contains_WhenDayPresentOrMissing_ShouldReflectState()
    {
        ExchangeRateSeriesBuffer buffer = NewBufferWith((1000, 1.4m), (1010, 1.5m));

        Assert.IsTrue(buffer.Contains(1000));
        Assert.IsTrue(buffer.Contains(1010));
        Assert.IsFalse(buffer.Contains(1005));
        Assert.IsFalse(buffer.Contains(2000));
    }
}
