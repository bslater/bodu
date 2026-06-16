// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateSeriesBuilderTests.Add.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Assertions;

namespace Bodu.Financial;

public partial class ExchangeRateSeriesBuilderTests
{
    /// <summary>
    /// Verifies that adding observations in arbitrary order results in strictly ascending enumeration order.
    /// </summary>
    [TestMethod]
    public void Add_WhenInsertedOutOfOrder_ShouldMaintainSortedEnumeration()
    {
        ExchangeRateSeriesBuilder builder = new(s_usdAud, "RBA");
        builder.Add(new DateOnly(2026, 6, 5), 1.54m);
        builder.Add(new DateOnly(2026, 6, 1), 1.50m);
        builder.Add(new DateOnly(2026, 6, 3), 1.52m);

        ExchangeRateObservation[] expected =
        [
            new(new DateOnly(2026, 6, 1), 1.50m),
            new(new DateOnly(2026, 6, 3), 1.52m),
            new(new DateOnly(2026, 6, 5), 1.54m),
        ];

        CollectionAssert.AreEqual(expected, builder.GetObservations().ToArray());
    }

    /// <summary>
    /// Verifies that adding an observation for an already-present date throws
    /// <see cref="ArgumentException" /> with parameter name <c>date</c>.
    /// </summary>
    [TestMethod]
    public void Add_WhenDateExists_ShouldThrowArgumentException()
    {
        ExchangeRateSeriesBuilder builder = new(s_usdAud, "RBA");
        builder.Add(new DateOnly(2026, 6, 1), 1.50m);

        ExceptionAssert.ThrowsExactlyWithParamName<ArgumentException>(
            () =>
            {
                builder.Add(new DateOnly(2026, 6, 1), 1.60m);
            },
            "date");
    }

    /// <summary>
    /// Verifies that a zero rate throws <see cref="ArgumentOutOfRangeException" /> with parameter name <c>rate</c>.
    /// </summary>
    [TestMethod]
    public void Add_WhenRateIsZero_ShouldThrowArgumentOutOfRangeException()
    {
        ExchangeRateSeriesBuilder builder = new(s_usdAud, "RBA");

        ExceptionAssert.ThrowsExactlyWithParamName<ArgumentOutOfRangeException>(
            () =>
            {
                builder.Add(new DateOnly(2026, 6, 1), 0m);
            },
            "rate");
    }

    /// <summary>
    /// Verifies that a negative rate throws <see cref="ArgumentOutOfRangeException" /> with parameter name
    /// <c>rate</c>.
    /// </summary>
    [TestMethod]
    public void Add_WhenRateIsNegative_ShouldThrowArgumentOutOfRangeException()
    {
        ExchangeRateSeriesBuilder builder = new(s_usdAud, "RBA");

        ExceptionAssert.ThrowsExactlyWithParamName<ArgumentOutOfRangeException>(
            () =>
            {
                builder.Add(new DateOnly(2026, 6, 1), -1m);
            },
            "rate");
    }

    /// <summary>
    /// Verifies that <see cref="ExchangeRateSeriesBuilder.TryAdd" /> reports success and inserts when the date is new.
    /// </summary>
    [TestMethod]
    public void TryAdd_WhenDateIsNew_ShouldReturnTrueAndInsert()
    {
        ExchangeRateSeriesBuilder builder = new(s_usdAud, "RBA");

        bool inserted = builder.TryAdd(new DateOnly(2026, 6, 1), 1.50m);

        Assert.IsTrue(inserted);
        Assert.AreEqual(1, builder.Count);
    }

    /// <summary>
    /// Verifies that <see cref="ExchangeRateSeriesBuilder.TryAdd" /> reports failure and leaves the existing rate
    /// untouched when the date already exists.
    /// </summary>
    [TestMethod]
    public void TryAdd_WhenDateExists_ShouldReturnFalseAndNotMutate()
    {
        ExchangeRateSeriesBuilder builder = new(s_usdAud, "RBA");
        builder.Add(new DateOnly(2026, 6, 1), 1.50m);

        bool inserted = builder.TryAdd(new DateOnly(2026, 6, 1), 1.60m);

        Assert.IsFalse(inserted);
        Assert.IsTrue(builder.TryGetRate(new DateOnly(2026, 6, 1), out decimal rate));
        Assert.AreEqual(1.50m, rate);
    }

    /// <summary>
    /// Verifies that <see cref="ExchangeRateSeriesBuilder.TryAdd" /> validates the rate before checking the date.
    /// </summary>
    [TestMethod]
    public void TryAdd_WhenRateInvalid_ShouldThrowArgumentOutOfRangeException()
    {
        ExchangeRateSeriesBuilder builder = new(s_usdAud, "RBA");

        ExceptionAssert.ThrowsExactlyWithParamName<ArgumentOutOfRangeException>(
            () =>
            {
                builder.TryAdd(new DateOnly(2026, 6, 1), 0m);
            },
            "rate");
    }
}
