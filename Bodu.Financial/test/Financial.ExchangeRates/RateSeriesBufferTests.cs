// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RateSeriesBufferTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

[TestClass]
public partial class RateSeriesBufferTests
{
    private const string RateParam = "rate";
    private const string DateParam = "date";
    private const string ObservationsParam = "observations";

    private static RateSeriesBuffer NewBuffer() => new();

    private static RateSeriesBuffer NewBufferWith(params (int Day, decimal Rate)[] entries)
    {
        var buffer = new RateSeriesBuffer();
        foreach ((int day, decimal rate) in entries)
        {
            buffer.Add(day, rate, RateParam, DateParam);
        }

        return buffer;
    }

    /// <summary>
    /// Verifies that a default-constructed buffer reports zero count and empty state.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenDefaultConstructed_ShouldStartEmpty()
    {
        RateSeriesBuffer buffer = NewBuffer();

        Assert.AreEqual(0, buffer.Count);
        Assert.IsTrue(buffer.IsEmpty);
    }

    /// <summary>
    /// Verifies that a non-positive capacity argument is clamped to zero and produces an empty buffer that still
    /// accepts subsequent inserts.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenCapacityIsZeroOrNegative_ShouldStartEmptyAndAcceptInserts()
    {
        RateSeriesBuffer zero = new(0);
        RateSeriesBuffer negative = new(-5);

        Assert.AreEqual(0, zero.Count);
        Assert.AreEqual(0, negative.Count);

        zero.Add(1000, 1.5m, RateParam, DateParam);
        negative.Add(1000, 1.5m, RateParam, DateParam);

        Assert.AreEqual(1, zero.Count);
        Assert.AreEqual(1, negative.Count);
    }

    /// <summary>
    /// Verifies that a positive initial capacity is allocated up front while live count starts at zero.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenPositiveCapacitySupplied_ShouldStartEmpty()
    {
        RateSeriesBuffer buffer = new(32);

        Assert.AreEqual(0, buffer.Count);
        Assert.IsTrue(buffer.IsEmpty);
    }

    /// <summary>
    /// Verifies that <see cref="RateSeriesBuffer.IsEmpty" /> flips from <see langword="true" /> to
    /// <see langword="false" /> as observations are added and back to <see langword="true" /> as they are removed.
    /// </summary>
    [TestMethod]
    public void IsEmpty_WhenObservationsAddedAndRemoved_ShouldReflectLiveCount()
    {
        RateSeriesBuffer buffer = NewBuffer();

        Assert.IsTrue(buffer.IsEmpty);

        buffer.Add(1000, 1.5m, RateParam, DateParam);
        Assert.IsFalse(buffer.IsEmpty);

        buffer.Remove(1000);
        Assert.IsTrue(buffer.IsEmpty);
    }
}
