// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AggregatingExchangeRateProviderTests.HistoryAvailability.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Caching;

public partial class AggregatingExchangeRateProviderTests
{
    /// <summary>
    /// The fixed instant the composed-availability tests evaluate rolling windows against.
    /// </summary>
    private static readonly DateTimeOffset AvailabilityNow = new(2024, 6, 1, 0, 0, 0, TimeSpan.Zero);

    /// <summary>
    /// Verifies that a group whose only child advertises a bounded availability advertises that same availability.
    /// </summary>
    [TestMethod]
    public void HistoryAvailability_WhenSingleBoundedChild_ShouldReturnChildValue()
    {
        ExchangeRateHistoryAvailability declared = ExchangeRateHistoryAvailability.Since(new DateOnly(2020, 1, 1));
        AggregatingExchangeRateProvider agg = CreateAvailabilityGroup(NamedAware("A", declared));

        Assert.AreEqual(declared, agg.HistoryAvailability);
    }

    /// <summary>
    /// Verifies that the group advertises the most generous availability across bounded children — the one whose
    /// earliest available date reaches furthest back — because a date any single child can serve is a date the group
    /// can serve.
    /// </summary>
    [TestMethod]
    public void HistoryAvailability_WhenAllChildrenBounded_ShouldReturnMostGenerous()
    {
        AggregatingExchangeRateProvider agg = CreateAvailabilityGroup(
            NamedAware("Shallow", ExchangeRateHistoryAvailability.RollingDays(30)),
            NamedAware("Deep", ExchangeRateHistoryAvailability.Since(new DateOnly(2015, 1, 1))),
            NamedAware("Mid", ExchangeRateHistoryAvailability.Since(new DateOnly(2022, 1, 1))));

        Assert.AreEqual(ExchangeRateHistoryAvailability.Since(new DateOnly(2015, 1, 1)), agg.HistoryAvailability);
    }

    /// <summary>
    /// Verifies that a single <see cref="ExchangeRateHistoryAvailability.Unbounded" /> child makes the whole group
    /// unbounded, regardless of how shallow the other children are.
    /// </summary>
    [TestMethod]
    public void HistoryAvailability_WhenAnyChildUnbounded_ShouldReturnUnbounded()
    {
        AggregatingExchangeRateProvider agg = CreateAvailabilityGroup(
            NamedAware("Shallow", ExchangeRateHistoryAvailability.RollingDays(30)),
            NamedAware("Deep", ExchangeRateHistoryAvailability.Unbounded));

        Assert.AreEqual(ExchangeRateHistoryAvailability.Unbounded, agg.HistoryAvailability);
    }

    /// <summary>
    /// Verifies that a child that does not implement <see cref="IHistoryAwareExchangeRateProvider" /> makes the whole
    /// group unbounded: a non-aware child declares no floor, so the group cannot declare one either.
    /// </summary>
    [TestMethod]
    public void HistoryAvailability_WhenAnyChildNotHistoryAware_ShouldReturnUnbounded()
    {
        AggregatingExchangeRateProvider agg = CreateAvailabilityGroup(
            NamedAware("Shallow", ExchangeRateHistoryAvailability.RollingDays(30)),
            new NamedDatedExchangeRateProvider("Legacy", new CountingDatedExchangeRateProvider([])));

        Assert.AreEqual(ExchangeRateHistoryAvailability.Unbounded, agg.HistoryAvailability);
    }

    /// <summary>
    /// Verifies that rolling and since declarations are compared through their earliest available date as of the
    /// injected clock, so a deep rolling window beats a shallow fixed inception.
    /// </summary>
    [TestMethod]
    public void HistoryAvailability_WhenRollingReachesFurtherThanSince_ShouldReturnRolling()
    {
        // As of 2024-06-01 a 3650-day rolling window reaches back to 2014, further than the 2023 inception.
        ExchangeRateHistoryAvailability rolling = ExchangeRateHistoryAvailability.RollingDays(3650);
        AggregatingExchangeRateProvider agg = CreateAvailabilityGroup(
            NamedAware("Rolling", rolling),
            NamedAware("Since", ExchangeRateHistoryAvailability.Since(new DateOnly(2023, 1, 1))));

        Assert.AreEqual(rolling, agg.HistoryAvailability);
    }

    /// <summary>
    /// Builds an aggregator over the supplied children with the clock pinned to <see cref="AvailabilityNow" />, so
    /// rolling-window comparisons are deterministic.
    /// </summary>
    /// <param name="children">The named children to group.</param>
    /// <returns>The aggregator under test.</returns>
    private static AggregatingExchangeRateProvider CreateAvailabilityGroup(params NamedDatedExchangeRateProvider[] children) =>
        new(children, options: null, timeProvider: new MutableTimeProvider(AvailabilityNow));

    /// <summary>
    /// Builds a named history-aware child with an empty book that advertises the supplied availability.
    /// </summary>
    /// <param name="name">The child name.</param>
    /// <param name="availability">The history depth the child advertises.</param>
    /// <returns>The named child.</returns>
    private static NamedDatedExchangeRateProvider NamedAware(string name, ExchangeRateHistoryAvailability availability) =>
        new(name, new HistoryAwareCountingProvider(availability, []));
}
