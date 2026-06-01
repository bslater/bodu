// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateSeriesBufferTests.Capacity.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial;

public partial class ExchangeRateSeriesBufferTests
{
    /// <summary>
    /// Verifies that growing from zero capacity through many inserts produces a strictly ascending, complete buffer
    /// — exercising the doubling growth path inside <see cref="ExchangeRateSeriesBuffer" />.
    /// </summary>
    [TestMethod]
    public void Capacity_WhenManyInsertsForceGrowth_ShouldPreserveAllObservations()
    {
        ExchangeRateSeriesBuffer buffer = NewBuffer();
        const int Total = 100;

        for (var i = 0; i < Total; i++)
        {
            buffer.Add(1000 + i, 1m + (decimal)i / 1000m, RateParam, DateParam);
        }

        Assert.AreEqual(Total, buffer.Count);

        var observations = buffer.Enumerate().ToArray();
        for (var i = 0; i < Total; i++)
        {
            Assert.AreEqual(DateOnly.FromDayNumber(1000 + i), observations[i].Date);
            Assert.AreEqual(1m + (decimal)i / 1000m, observations[i].Rate);
        }
    }

    /// <summary>
    /// Verifies that repeatedly inserting at the front (lowest day number first each time) shifts existing entries
    /// without corrupting the buffer.
    /// </summary>
    [TestMethod]
    public void Capacity_WhenInsertingDescending_ShouldPreserveAllObservations()
    {
        ExchangeRateSeriesBuffer buffer = NewBuffer();
        const int Total = 64;

        for (var i = Total - 1; i >= 0; i--)
        {
            buffer.Add(1000 + i, 1m + (decimal)i / 1000m, RateParam, DateParam);
        }

        Assert.AreEqual(Total, buffer.Count);

        var observations = buffer.Enumerate().ToArray();
        for (var i = 0; i < Total; i++)
        {
            Assert.AreEqual(DateOnly.FromDayNumber(1000 + i), observations[i].Date);
        }
    }

    /// <summary>
    /// Verifies that an <see cref="ExchangeRateSeriesBuffer.AddRange" /> whose batch size exceeds the buffer's
    /// existing capacity completes successfully and preserves all observations.
    /// </summary>
    [TestMethod]
    public void Capacity_WhenAddRangeBatchExceedsCapacity_ShouldGrowAndPreserveAllObservations()
    {
        ExchangeRateSeriesBuffer buffer = NewBuffer();
        const int Total = 200;

        var batch = new ExchangeRateObservation[Total];
        for (var i = 0; i < Total; i++)
        {
            batch[i] = new ExchangeRateObservation(DateOnly.FromDayNumber(1000 + i), 1m + (decimal)i / 1000m);
        }

        buffer.AddRange(batch, ObservationsParam);

        Assert.AreEqual(Total, buffer.Count);
        var observations = buffer.Enumerate().ToArray();
        Assert.AreEqual(DateOnly.FromDayNumber(1000), observations[0].Date);
        Assert.AreEqual(DateOnly.FromDayNumber(1000 + Total - 1), observations[Total - 1].Date);
    }

    /// <summary>
    /// Verifies that alternating insert and remove operations preserve buffer integrity over many iterations.
    /// </summary>
    [TestMethod]
    public void Capacity_WhenAlternatingInsertAndRemove_ShouldPreserveIntegrity()
    {
        ExchangeRateSeriesBuffer buffer = NewBuffer();

        for (var i = 0; i < 50; i++)
        {
            buffer.Add(2000 + i, 1m, RateParam, DateParam);
        }

        for (var i = 0; i < 50; i += 2)
        {
            buffer.Remove(2000 + i);
        }

        Assert.AreEqual(25, buffer.Count);
        var observations = buffer.Enumerate().ToArray();
        for (var i = 0; i < 25; i++)
        {
            Assert.AreEqual(DateOnly.FromDayNumber(2001 + 2 * i), observations[i].Date);
        }
    }
}
