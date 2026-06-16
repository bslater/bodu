// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateSeriesBufferTests.Set.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Test.Assertions;

namespace Bodu.Financial;

public partial class ExchangeRateSeriesBufferTests
{
    /// <summary>
    /// Verifies that <see cref="ExchangeRateSeriesBuffer.Set" /> overwrites the rate for a present day number.
    /// </summary>
    [TestMethod]
    public void Set_WhenDayExists_ShouldReplaceRate()
    {
        ExchangeRateSeriesBuffer buffer = NewBufferWith((1000, 1.5m), (1010, 1.6m));

        buffer.Set(1010, 1.75m, RateParam);

        Assert.AreEqual(2, buffer.Count);
        Assert.IsTrue(buffer.TryGetRate(1010, out decimal rate));
        Assert.AreEqual(1.75m, rate);
    }

    /// <summary>
    /// Verifies that <see cref="ExchangeRateSeriesBuffer.Set" /> on a missing day number throws
    /// <see cref="KeyNotFoundException" />.
    /// </summary>
    [TestMethod]
    public void Set_WhenDayMissing_ShouldThrowKeyNotFoundException()
    {
        ExchangeRateSeriesBuffer buffer = NewBufferWith((1000, 1.5m));

        KeyNotFoundException ex = Assert.ThrowsExactly<KeyNotFoundException>(() =>
            buffer.Set(1010, 1.6m, RateParam));

        Assert.IsTrue(ex.Message.Contains(
            DateOnly.FromDayNumber(1010).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            StringComparison.Ordinal));
    }

    /// <summary>
    /// Verifies that an invalid rate is rejected before the buffer is consulted for the day number.
    /// </summary>
    [TestMethod]
    public void Set_WhenRateInvalid_ShouldThrowEvenIfDayMissing()
    {
        ExchangeRateSeriesBuffer buffer = NewBuffer();

        ExceptionAssert.ThrowsExactlyWithParamName<ArgumentOutOfRangeException>(
            () =>
            {
                buffer.Set(1000, 0m, "customRate");
            },
            "customRate");
    }

    /// <summary>
    /// Verifies that <see cref="ExchangeRateSeriesBuffer.TrySet" /> updates and returns <see langword="true" /> for
    /// a present day number.
    /// </summary>
    [TestMethod]
    public void TrySet_WhenDayExists_ShouldUpdateAndReturnTrue()
    {
        ExchangeRateSeriesBuffer buffer = NewBufferWith((1000, 1.5m));

        bool updated = buffer.TrySet(1000, 1.9m, RateParam);

        Assert.IsTrue(updated);
        Assert.IsTrue(buffer.TryGetRate(1000, out decimal rate));
        Assert.AreEqual(1.9m, rate);
    }

    /// <summary>
    /// Verifies that <see cref="ExchangeRateSeriesBuffer.TrySet" /> returns <see langword="false" /> for a missing
    /// day number and does not mutate the buffer.
    /// </summary>
    [TestMethod]
    public void TrySet_WhenDayMissing_ShouldReturnFalseAndNotMutate()
    {
        ExchangeRateSeriesBuffer buffer = NewBufferWith((1000, 1.5m));

        bool updated = buffer.TrySet(1010, 1.6m, RateParam);

        Assert.IsFalse(updated);
        Assert.AreEqual(1, buffer.Count);
    }

    /// <summary>
    /// Verifies that <see cref="ExchangeRateSeriesBuffer.TrySet" /> validates the rate before checking the day
    /// number.
    /// </summary>
    [TestMethod]
    public void TrySet_WhenRateInvalid_ShouldThrowEvenIfDayMissing()
    {
        ExchangeRateSeriesBuffer buffer = NewBuffer();

        ExceptionAssert.ThrowsExactlyWithParamName<ArgumentOutOfRangeException>(
            () =>
            {
                buffer.TrySet(1000, -1m, "customRate");
            },
            "customRate");
    }
}
