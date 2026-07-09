// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RateSeriesBufferTests.Upsert.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Assertions;

namespace Bodu.Financial.ExchangeRates;

public partial class RateSeriesBufferTests
{
    /// <summary>
    /// Verifies that <see cref="RateSeriesBuffer.Upsert" /> inserts a new observation when the day number
    /// is unknown, preserving sorted order.
    /// </summary>
    [TestMethod]
    public void Upsert_WhenDayMissing_ShouldInsertInSortedPosition()
    {
        RateSeriesBuffer buffer = NewBufferWith((1000, 1.4m), (1020, 1.6m));

        buffer.Upsert(1010, 1.5m, RateParam);

        Assert.AreEqual(3, buffer.Count);
        RateObservation[] observations = buffer.Enumerate().ToArray();
        Assert.AreEqual(DateOnly.FromDayNumber(1000), observations[0].Date);
        Assert.AreEqual(DateOnly.FromDayNumber(1010), observations[1].Date);
        Assert.AreEqual(DateOnly.FromDayNumber(1020), observations[2].Date);
    }

    /// <summary>
    /// Verifies that <see cref="RateSeriesBuffer.Upsert" /> replaces an existing rate in place without
    /// changing the count.
    /// </summary>
    [TestMethod]
    public void Upsert_WhenDayExists_ShouldReplaceRateInPlace()
    {
        RateSeriesBuffer buffer = NewBufferWith((1000, 1.5m));

        buffer.Upsert(1000, 1.9m, RateParam);

        Assert.AreEqual(1, buffer.Count);
        Assert.IsTrue(buffer.TryGetRate(1000, out decimal rate));
        Assert.AreEqual(1.9m, rate);
    }

    /// <summary>
    /// Verifies that <see cref="RateSeriesBuffer.Upsert" /> validates the rate using the supplied parameter
    /// name.
    /// </summary>
    [TestMethod]
    public void Upsert_WhenRateInvalid_ShouldThrowWithSuppliedRateParamName()
    {
        RateSeriesBuffer buffer = NewBuffer();

        ExceptionAssert.ThrowsExactlyWithParamName<ArgumentOutOfRangeException>(
            () =>
            {
                buffer.Upsert(1000, 0m, "customRate");
            },
            "customRate");
    }
}
