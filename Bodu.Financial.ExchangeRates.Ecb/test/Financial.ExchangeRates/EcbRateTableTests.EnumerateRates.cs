// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EcbRateTableTests.EnumerateRates.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

namespace Bodu.Financial.ExchangeRates;

public sealed partial class EcbRateTableTests
{
    /// <summary>
    /// Verifies that a feed the ECB lists newest day first yields each pair's rates oldest day first, so a load appends
    /// every rate to its series: no rate is inserted ahead of the days its series already holds, which made loading the
    /// full history quadratic.
    /// </summary>
    [TestMethod]
    public void EnumerateRates_WhenFeedListsNewestDayFirst_ShouldYieldEachPairInDateOrder()
    {
        using MemoryStream stream = new(Encoding.UTF8.GetBytes(NewestFirstFeed(500)), writable: false);
        EcbRateTable table = EcbRateXmlParser.Parse(stream, new EcbRateProviderOptions());

        List<ExchangeRate> rates = table.EnumerateRates().ToList();

        Assert.HasCount(1000, rates);
        Assert.AreEqual(0, CountOutOfOrder(rates));
    }

    /// <summary>
    /// Verifies that the sample fixture, listed newest day first, yields each pair's rates oldest day first.
    /// </summary>
    [TestMethod]
    public void EnumerateRates_WhenSampleFeedIsParsed_ShouldYieldEachPairInDateOrder()
    {
        using MemoryStream stream = EcbFixtures.OpenStream(EcbFixtures.Sample);
        EcbRateTable table = EcbRateXmlParser.Parse(stream, new EcbRateProviderOptions());

        Assert.AreEqual(0, CountOutOfOrder(table.EnumerateRates()));
    }

    /// <summary>
    /// Verifies that a table already listed oldest day first is yielded in its own order, since reversing it would
    /// turn every append into an insertion.
    /// </summary>
    [TestMethod]
    public void EnumerateRates_WhenTableListsOldestDayFirst_ShouldYieldInListedOrder()
    {
        EcbRateTable table = new(
        [
            new EcbRateObservation(new DateOnly(2023, 1, 3), "USD", 1.0545m),
            new EcbRateObservation(new DateOnly(2023, 1, 4), "USD", 1.0600m),
        ]);

        List<ExchangeRate> rates = table.EnumerateRates().ToList();

        Assert.HasCount(2, rates);
        Assert.AreEqual(new DateOnly(2023, 1, 3), rates[0].Date);
        Assert.AreEqual(new DateOnly(2023, 1, 4), rates[1].Date);
    }

    /// <summary>
    /// Verifies that a table holding a single observation yields that one rate, with no order to settle.
    /// </summary>
    [TestMethod]
    public void EnumerateRates_WhenTableHoldsOneObservation_ShouldYieldIt()
    {
        EcbRateTable table = new([new EcbRateObservation(new DateOnly(2023, 1, 3), "USD", 1.0545m)]);

        List<ExchangeRate> rates = table.EnumerateRates().ToList();

        Assert.HasCount(1, rates);
        Assert.AreEqual(1.0545m, rates[0].Rate);
    }
}
