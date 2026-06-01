// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateSeriesBuilderTests.AddRange.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Assertions;

namespace Bodu.Financial;

public partial class ExchangeRateSeriesBuilderTests
{
    /// <summary>
    /// Verifies that <see cref="ExchangeRateSeriesBuilder.AddRange" /> throws on a null sequence.
    /// </summary>
    [TestMethod]
    public void AddRange_WhenObservationsNull_ShouldThrowArgumentNullException()
    {
        ExchangeRateSeriesBuilder builder = new(s_usdAud, "RBA");

        ExceptionAssert.ThrowsExactlyWithParamName<ArgumentNullException>(
            () =>
            {
                builder.AddRange(null!);
            },
            "observations");
    }

    /// <summary>
    /// Verifies that pre-sorted observations are inserted in order.
    /// </summary>
    [TestMethod]
    public void AddRange_WhenAllNew_ShouldInsertSortedAndPreserveOrder()
    {
        ExchangeRateSeriesBuilder builder = new(s_usdAud, "RBA");
        builder.AddRange(SampleObservations());

        CollectionAssert.AreEqual(SampleObservations(), builder.GetObservations().ToArray());
    }

    /// <summary>
    /// Verifies that an unsorted incoming batch is reordered into strictly ascending enumeration order.
    /// </summary>
    [TestMethod]
    public void AddRange_WhenInputUnsorted_ShouldInsertSorted()
    {
        ExchangeRateSeriesBuilder builder = new(s_usdAud, "RBA");
        ExchangeRateObservation[] unsorted =
        [
            new(new DateOnly(2026, 6, 5), 1.54m),
            new(new DateOnly(2026, 6, 1), 1.50m),
            new(new DateOnly(2026, 6, 3), 1.52m),
        ];

        builder.AddRange(unsorted);

        CollectionAssert.AreEqual(SampleObservations(), builder.GetObservations().ToArray());
    }

    /// <summary>
    /// Verifies that duplicate dates inside the incoming batch throw <see cref="ArgumentException" /> with
    /// parameter name <c>observations</c>.
    /// </summary>
    [TestMethod]
    public void AddRange_WhenIncomingBatchContainsDuplicateDate_ShouldThrowArgumentException()
    {
        ExchangeRateSeriesBuilder builder = new(s_usdAud, "RBA");
        ExchangeRateObservation[] batch =
        [
            new(new DateOnly(2026, 6, 1), 1.50m),
            new(new DateOnly(2026, 6, 1), 1.55m),
        ];

        ExceptionAssert.ThrowsExactlyWithParamName<ArgumentException>(
            () =>
            {
                builder.AddRange(batch);
            },
            "observations");
    }

    /// <summary>
    /// Verifies that incoming dates that collide with existing observations throw
    /// <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    public void AddRange_WhenIncomingDateAlreadyPresent_ShouldThrowArgumentException()
    {
        ExchangeRateSeriesBuilder builder = new(s_usdAud, "RBA");
        builder.Add(new DateOnly(2026, 6, 1), 1.50m);

        ExchangeRateObservation[] batch =
        [
            new(new DateOnly(2026, 6, 1), 1.55m),
        ];

        ExceptionAssert.ThrowsExactlyWithParamName<ArgumentException>(
            () =>
            {
                builder.AddRange(batch);
            },
            "observations");
    }

    /// <summary>
    /// Verifies that an invalid (non-positive) rate inside the batch throws
    /// <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void AddRange_WhenRateIsZero_ShouldThrowArgumentOutOfRangeException()
    {
        ExchangeRateSeriesBuilder builder = new(s_usdAud, "RBA");
        ExchangeRateObservation[] batch =
        [
            new(new DateOnly(2026, 6, 1), 0m),
        ];

        ExceptionAssert.ThrowsExactlyWithParamName<ArgumentOutOfRangeException>(
            () =>
            {
                builder.AddRange(batch);
            },
            "observations");
    }

    /// <summary>
    /// Verifies that a validation failure during <see cref="ExchangeRateSeriesBuilder.AddRange" /> leaves the
    /// builder in its prior state (the merge is atomic).
    /// </summary>
    [TestMethod]
    public void AddRange_WhenThrows_ShouldLeaveBuilderUnchanged()
    {
        ExchangeRateSeriesBuilder builder = new(s_usdAud, "RBA");
        builder.Add(new DateOnly(2026, 6, 1), 1.50m);
        builder.Add(new DateOnly(2026, 6, 3), 1.52m);

        var snapshotBefore = builder.GetObservations().ToArray();
        ExchangeRateObservation[] conflicting =
        [
            new(new DateOnly(2026, 6, 1), 1.55m), // collides with existing
            new(new DateOnly(2026, 6, 7), 1.60m), // would otherwise be inserted
        ];

        Assert.ThrowsExactly<ArgumentException>(() => builder.AddRange(conflicting));

        CollectionAssert.AreEqual(snapshotBefore, builder.GetObservations().ToArray());
    }

    /// <summary>
    /// Verifies that a batch of new dates merged with existing observations produces a single sorted result.
    /// </summary>
    [TestMethod]
    public void AddRange_WhenAllNewBatchMergesWithExisting_ShouldPreserveSortedOrder()
    {
        ExchangeRateSeriesBuilder builder = new(s_usdAud, "RBA");
        builder.Add(new DateOnly(2026, 6, 1), 1.50m);
        builder.Add(new DateOnly(2026, 6, 5), 1.54m);

        ExchangeRateObservation[] batch =
        [
            new(new DateOnly(2026, 6, 2), 1.51m),
            new(new DateOnly(2026, 6, 4), 1.53m),
        ];

        builder.AddRange(batch);

        ExchangeRateObservation[] expected =
        [
            new(new DateOnly(2026, 6, 1), 1.50m),
            new(new DateOnly(2026, 6, 2), 1.51m),
            new(new DateOnly(2026, 6, 4), 1.53m),
            new(new DateOnly(2026, 6, 5), 1.54m),
        ];

        CollectionAssert.AreEqual(expected, builder.GetObservations().ToArray());
    }
}
