// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateSeriesBuilderTests.UpsertRange.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Assertions;

namespace Bodu.Financial;

public partial class ExchangeRateSeriesBuilderTests
{
    /// <summary>
    /// Verifies that <see cref="ExchangeRateSeriesBuilder.UpsertRange" /> throws on a null sequence.
    /// </summary>
    [TestMethod]
    public void UpsertRange_WhenObservationsNull_ShouldThrowArgumentNullException()
    {
        ExchangeRateSeriesBuilder builder = new(s_usdAud, "RBA");

        ExceptionAssert.ThrowsExactlyWithParamName<ArgumentNullException>(
            () =>
            {
                builder.UpsertRange((IEnumerable<ExchangeRateObservation>)null!);
            },
            "observations");
    }

    /// <summary>
    /// Verifies that a batch of entirely new observations is inserted in sorted order.
    /// </summary>
    [TestMethod]
    public void UpsertRange_WhenAllNew_ShouldInsertSorted()
    {
        ExchangeRateSeriesBuilder builder = new(s_usdAud, "RBA");

        builder.UpsertRange(SampleObservations());

        CollectionAssert.AreEqual(SampleObservations(), builder.GetObservations().ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="ExchangeRateSeriesBuilder.UpsertRange" /> replaces existing observations in place.
    /// </summary>
    [TestMethod]
    public void UpsertRange_WhenAllExisting_ShouldReplaceRates()
    {
        ExchangeRateSeriesBuilder builder = new(s_usdAud, "RBA");
        builder.AddRange(SampleObservations());

        ExchangeRateObservation[] replacements =
        [
            new(new DateOnly(2026, 6, 1), 2.00m),
            new(new DateOnly(2026, 6, 3), 2.10m),
        ];

        builder.UpsertRange(replacements);

        Assert.AreEqual(3, builder.Count);
        Assert.IsTrue(builder.TryGetRate(new DateOnly(2026, 6, 1), out var rate1));
        Assert.IsTrue(builder.TryGetRate(new DateOnly(2026, 6, 3), out var rate3));
        Assert.IsTrue(builder.TryGetRate(new DateOnly(2026, 6, 5), out var rate5));
        Assert.AreEqual(2.00m, rate1);
        Assert.AreEqual(2.10m, rate3);
        Assert.AreEqual(1.54m, rate5);
    }

    /// <summary>
    /// Verifies that a batch combining new and existing dates inserts and replaces respectively.
    /// </summary>
    [TestMethod]
    public void UpsertRange_WhenMixed_ShouldInsertAndReplace()
    {
        ExchangeRateSeriesBuilder builder = new(s_usdAud, "RBA");
        builder.Add(new DateOnly(2026, 6, 1), 1.50m);

        ExchangeRateObservation[] batch =
        [
            new(new DateOnly(2026, 6, 1), 1.55m), // replace
            new(new DateOnly(2026, 6, 2), 1.51m), // insert
        ];

        builder.UpsertRange(batch);

        ExchangeRateObservation[] expected =
        [
            new(new DateOnly(2026, 6, 1), 1.55m),
            new(new DateOnly(2026, 6, 2), 1.51m),
        ];

        CollectionAssert.AreEqual(expected, builder.GetObservations().ToArray());
    }

    /// <summary>
    /// Verifies that duplicate dates inside the incoming batch throw <see cref="ArgumentException" /> with
    /// parameter name <c>observations</c>.
    /// </summary>
    [TestMethod]
    public void UpsertRange_WhenIncomingBatchContainsDuplicateDate_ShouldThrowArgumentException()
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
                builder.UpsertRange(batch);
            },
            "observations");
    }

    /// <summary>
    /// Verifies that an invalid rate inside the batch throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void UpsertRange_WhenRateIsZero_ShouldThrowArgumentOutOfRangeException()
    {
        ExchangeRateSeriesBuilder builder = new(s_usdAud, "RBA");
        ExchangeRateObservation[] batch =
        [
            new(new DateOnly(2026, 6, 1), 0m),
        ];

        ExceptionAssert.ThrowsExactlyWithParamName<ArgumentOutOfRangeException>(
            () =>
            {
                builder.UpsertRange(batch);
            },
            "observations");
    }

    /// <summary>
    /// Verifies that a validation failure during <see cref="ExchangeRateSeriesBuilder.UpsertRange" /> leaves the
    /// builder in its prior state.
    /// </summary>
    [TestMethod]
    public void UpsertRange_WhenThrows_ShouldLeaveBuilderUnchanged()
    {
        ExchangeRateSeriesBuilder builder = new(s_usdAud, "RBA");
        builder.Add(new DateOnly(2026, 6, 1), 1.50m);

        ExchangeRateObservation[] snapshotBefore = builder.GetObservations().ToArray();
        ExchangeRateObservation[] invalidBatch =
        [
            new(new DateOnly(2026, 6, 2), 1.55m),
            new(new DateOnly(2026, 6, 2), 1.60m), // duplicate within batch
        ];

        Assert.ThrowsExactly<ArgumentException>(() => builder.UpsertRange(invalidBatch));

        CollectionAssert.AreEqual(snapshotBefore, builder.GetObservations().ToArray());
    }
}
