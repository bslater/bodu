// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateSeriesBufferTests.Add.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Assertions;

namespace Bodu.Financial;

public partial class ExchangeRateSeriesBufferTests
{
    /// <summary>
    /// Verifies that <see cref="ExchangeRateSeriesBuffer.Add" /> appends a single observation to an empty buffer.
    /// </summary>
    [TestMethod]
    public void Add_WhenBufferEmpty_ShouldInsertFirstObservation()
    {
        ExchangeRateSeriesBuffer buffer = NewBuffer();

        buffer.Add(1000, 1.5m, RateParam, DateParam);

        Assert.AreEqual(1, buffer.Count);
        Assert.IsTrue(buffer.TryGetRate(1000, out decimal rate));
        Assert.AreEqual(1.5m, rate);
    }

    /// <summary>
    /// Verifies that <see cref="ExchangeRateSeriesBuffer.Add" /> inserts before all existing entries when the new
    /// day number is smaller than every present day number.
    /// </summary>
    [TestMethod]
    public void Add_WhenDayNumberLessThanAllExisting_ShouldInsertAtFront()
    {
        ExchangeRateSeriesBuffer buffer = NewBufferWith((1010, 1.5m), (1020, 1.6m));

        buffer.Add(1000, 1.4m, RateParam, DateParam);

        AssertContents(buffer, (1000, 1.4m), (1010, 1.5m), (1020, 1.6m));
    }

    /// <summary>
    /// Verifies that <see cref="ExchangeRateSeriesBuffer.Add" /> inserts between existing entries to preserve
    /// strictly ascending order.
    /// </summary>
    [TestMethod]
    public void Add_WhenDayNumberFallsBetweenExisting_ShouldInsertInMiddle()
    {
        ExchangeRateSeriesBuffer buffer = NewBufferWith((1000, 1.4m), (1020, 1.6m));

        buffer.Add(1010, 1.5m, RateParam, DateParam);

        AssertContents(buffer, (1000, 1.4m), (1010, 1.5m), (1020, 1.6m));
    }

    /// <summary>
    /// Verifies that <see cref="ExchangeRateSeriesBuffer.Add" /> appends to the end when the new day number exceeds
    /// every existing day number.
    /// </summary>
    [TestMethod]
    public void Add_WhenDayNumberGreaterThanAllExisting_ShouldInsertAtEnd()
    {
        ExchangeRateSeriesBuffer buffer = NewBufferWith((1000, 1.4m), (1010, 1.5m));

        buffer.Add(1020, 1.6m, RateParam, DateParam);

        AssertContents(buffer, (1000, 1.4m), (1010, 1.5m), (1020, 1.6m));
    }

    /// <summary>
    /// Verifies that <see cref="ExchangeRateSeriesBuffer.Add" /> rejects a duplicate day with
    /// <see cref="ArgumentException" /> and the supplied <paramref name="dateParamName" />.
    /// </summary>
    [TestMethod]
    public void Add_WhenDayAlreadyExists_ShouldThrowWithSuppliedDateParamName()
    {
        ExchangeRateSeriesBuffer buffer = NewBufferWith((1000, 1.4m));

        ExceptionAssert.ThrowsExactlyWithParamName<ArgumentException>(
            () =>
            {
                buffer.Add(1000, 1.5m, RateParam, "customDate");
            },
            "customDate");
    }

    /// <summary>
    /// Verifies that a non-positive rate raises <see cref="ArgumentOutOfRangeException" /> using the supplied
    /// <paramref name="rateParamName" />.
    /// </summary>
    [TestMethod]
    public void Add_WhenRateIsZeroOrNegative_ShouldThrowWithSuppliedRateParamName()
    {
        ExchangeRateSeriesBuffer buffer = NewBuffer();

        ExceptionAssert.ThrowsExactlyWithParamName<ArgumentOutOfRangeException>(
            () =>
            {
                buffer.Add(1000, 0m, "customRate", DateParam);
            },
            "customRate");

        ExceptionAssert.ThrowsExactlyWithParamName<ArgumentOutOfRangeException>(
            () =>
            {
                buffer.Add(1000, -1m, "customRate", DateParam);
            },
            "customRate");
    }

    /// <summary>
    /// Verifies that <see cref="ExchangeRateSeriesBuffer.Add" /> rejects an invalid rate without mutating the
    /// buffer.
    /// </summary>
    [TestMethod]
    public void Add_WhenRateInvalid_ShouldNotMutateBuffer()
    {
        ExchangeRateSeriesBuffer buffer = NewBufferWith((1000, 1.4m));

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            buffer.Add(1010, 0m, RateParam, DateParam));

        Assert.AreEqual(1, buffer.Count);
    }

    /// <summary>
    /// Verifies that <see cref="ExchangeRateSeriesBuffer.TryAdd" /> inserts and returns <see langword="true" />
    /// when the day number is new.
    /// </summary>
    [TestMethod]
    public void TryAdd_WhenDayNumberIsNew_ShouldInsertAndReturnTrue()
    {
        ExchangeRateSeriesBuffer buffer = NewBuffer();

        bool inserted = buffer.TryAdd(1000, 1.5m, RateParam);

        Assert.IsTrue(inserted);
        Assert.AreEqual(1, buffer.Count);
    }

    /// <summary>
    /// Verifies that <see cref="ExchangeRateSeriesBuffer.TryAdd" /> returns <see langword="false" /> and preserves
    /// the existing rate when the day number is already present.
    /// </summary>
    [TestMethod]
    public void TryAdd_WhenDayNumberExists_ShouldReturnFalseAndNotMutate()
    {
        ExchangeRateSeriesBuffer buffer = NewBufferWith((1000, 1.5m));

        bool inserted = buffer.TryAdd(1000, 1.9m, RateParam);

        Assert.IsFalse(inserted);
        Assert.IsTrue(buffer.TryGetRate(1000, out decimal rate));
        Assert.AreEqual(1.5m, rate);
    }

    /// <summary>
    /// Verifies that <see cref="ExchangeRateSeriesBuffer.TryAdd" /> validates the rate before checking for an
    /// existing day number.
    /// </summary>
    [TestMethod]
    public void TryAdd_WhenRateInvalid_ShouldThrowEvenIfDayAlreadyExists()
    {
        ExchangeRateSeriesBuffer buffer = NewBufferWith((1000, 1.5m));

        ExceptionAssert.ThrowsExactlyWithParamName<ArgumentOutOfRangeException>(
            () =>
            {
                buffer.TryAdd(1000, 0m, "customRate");
            },
            "customRate");
    }

    private static void AssertContents(ExchangeRateSeriesBuffer buffer, params (int Day, decimal Rate)[] expected)
    {
        Assert.AreEqual(expected.Length, buffer.Count);

        ExchangeRateObservation[] observations = buffer.Enumerate().ToArray();
        for (int i = 0; i < expected.Length; i++)
        {
            Assert.AreEqual(DateOnly.FromDayNumber(expected[i].Day), observations[i].Date);
            Assert.AreEqual(expected[i].Rate, observations[i].Rate);
        }
    }
}
