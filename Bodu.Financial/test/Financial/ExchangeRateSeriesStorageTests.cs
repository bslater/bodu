// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateSeriesStorageTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Assertions;

namespace Bodu.Financial;

[TestClass]
public partial class ExchangeRateSeriesStorageTests
{
    private const string ObservationsParam = "observations";
    private const string RatesParam = "rates";

    private static ExchangeRateObservation Obs(int dayNumber, decimal rate) =>
        new(DateOnly.FromDayNumber(dayNumber), rate);

    /// <summary>
    /// Verifies that <see cref="ExchangeRateSeriesStorage.Create(IEnumerable{ExchangeRateObservation}, string)" />
    /// stores the supplied observations.
    /// </summary>
    [TestMethod]
    public void Create_Observations_WhenInputsValid_ShouldStoreObservations()
    {
        var storage = ExchangeRateSeriesStorage.Create(
            new[] { Obs(1000, 1.4m), Obs(1010, 1.5m) },
            ObservationsParam);

        Assert.AreEqual(2, storage.Count);
    }

    /// <summary>
    /// Verifies that an empty observation sequence raises <see cref="ArgumentException" /> with the supplied
    /// parameter name.
    /// </summary>
    [TestMethod]
    public void Create_Observations_WhenEmpty_ShouldThrowArgumentException()
    {
        ExceptionAssert.ThrowsExactlyWithParamName<ArgumentException>(
            () =>
            {
                _ = ExchangeRateSeriesStorage.Create(Array.Empty<ExchangeRateObservation>(), ObservationsParam);
            },
            ObservationsParam);
    }

    /// <summary>
    /// Verifies that a non-positive rate raises <see cref="ArgumentOutOfRangeException" /> with the supplied
    /// parameter name.
    /// </summary>
    [TestMethod]
    public void Create_Observations_WhenAnyRateInvalid_ShouldThrowArgumentOutOfRangeException()
    {
        ExceptionAssert.ThrowsExactlyWithParamName<ArgumentOutOfRangeException>(
            () =>
            {
                _ = ExchangeRateSeriesStorage.Create(
                    new[] { Obs(1000, 1.4m), Obs(1010, 0m) },
                    ObservationsParam);
            },
            ObservationsParam);
    }

    /// <summary>
    /// Verifies that a duplicate observation date raises <see cref="ArgumentException" /> with the supplied
    /// parameter name.
    /// </summary>
    [TestMethod]
    public void Create_Observations_WhenDuplicateDate_ShouldThrowArgumentException()
    {
        ExceptionAssert.ThrowsExactlyWithParamName<ArgumentException>(
            () =>
            {
                _ = ExchangeRateSeriesStorage.Create(
                    new[] { Obs(1000, 1.4m), Obs(1000, 1.5m) },
                    ObservationsParam);
            },
            ObservationsParam);
    }

    /// <summary>
    /// Verifies that unsorted observations are reordered into strictly ascending date order during creation.
    /// </summary>
    [TestMethod]
    public void Create_Observations_WhenUnsorted_ShouldStoreInAscendingOrder()
    {
        var storage = ExchangeRateSeriesStorage.Create(
            new[] { Obs(1020, 1.6m), Obs(1000, 1.4m), Obs(1010, 1.5m) },
            ObservationsParam);

        var observations = storage.Enumerate().ToArray();
        Assert.AreEqual(DateOnly.FromDayNumber(1000), observations[0].Date);
        Assert.AreEqual(DateOnly.FromDayNumber(1010), observations[1].Date);
        Assert.AreEqual(DateOnly.FromDayNumber(1020), observations[2].Date);
    }

    /// <summary>
    /// Verifies that <see cref="ExchangeRateSeriesStorage.Create(IEnumerable{ValueTuple{DateOnly, decimal}}, string)" />
    /// stores the supplied tuples.
    /// </summary>
    [TestMethod]
    public void Create_Tuples_WhenInputsValid_ShouldStoreObservations()
    {
        var storage = ExchangeRateSeriesStorage.Create(
            new (DateOnly, decimal)[]
            {
                (DateOnly.FromDayNumber(1000), 1.4m),
                (DateOnly.FromDayNumber(1010), 1.5m),
            },
            RatesParam);

        Assert.AreEqual(2, storage.Count);
    }

    /// <summary>
    /// Verifies that the tuple overload rejects an empty sequence with <see cref="ArgumentException" /> using its
    /// caller-supplied parameter name.
    /// </summary>
    [TestMethod]
    public void Create_Tuples_WhenEmpty_ShouldThrowWithRatesParamName()
    {
        ExceptionAssert.ThrowsExactlyWithParamName<ArgumentException>(
            () =>
            {
                _ = ExchangeRateSeriesStorage.Create(
                    Array.Empty<(DateOnly, decimal)>(),
                    RatesParam);
            },
            RatesParam);
    }

    /// <summary>
    /// Verifies that the tuple overload preserves the legacy <c>Rate must be greater than zero.</c> message text
    /// so existing callers' message assertions continue to hold.
    /// </summary>
    [TestMethod]
    public void Create_Tuples_WhenRateInvalid_ShouldUseLegacyMessage()
    {
        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            ExchangeRateSeriesStorage.Create(
                new (DateOnly, decimal)[] { (DateOnly.FromDayNumber(1000), 0m) },
                RatesParam));

        Assert.IsTrue(ex.Message.Contains(
            FinancialResourceStrings.Arg_OutOfRange_RateNotPositive,
            StringComparison.Ordinal));
    }
}
