// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FixedExchangeRateTableTests.Hardening.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial;

public partial class FixedExchangeRateTableTests
{
    /// <summary>
    /// Verifies that the constructor takes an independent copy of the supplied rates, so that mutating the source
    /// dictionary after construction cannot tamper with the table's view of the world.
    /// </summary>
    [TestMethod]
    public void Mutation_WhenSourceDictionaryIsMutatedAfterConstruction_ShouldNotAffectTable()
    {
        Dictionary<(string From, string To), decimal> source = new()
        {
            [("USD", "AUD")] = 1.50m,
        };

        FixedExchangeRateTable table = new(source);

        source[("USD", "AUD")] = 999m;
        source[("JPY", "USD")] = 0.0067m;

        Assert.AreEqual(1.50m, table.GetRate("USD", "AUD"));
        _ = Assert.ThrowsExactly<KeyNotFoundException>(() => table.GetRate("JPY", "USD"));
    }

    /// <summary>
    /// Verifies that the constructor rejects an ISO code that fails the standard three-letter uppercase shape.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenKeyContainsInvalidIsoCode_ShouldThrowArgumentException()
    {
        Dictionary<(string From, string To), decimal> source = new()
        {
            [("usd", "AUD")] = 1.50m,
        };

        _ = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = new FixedExchangeRateTable(source);
        });
    }

    /// <summary>
    /// Verifies that the constructor rejects a zero rate.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenRateIsZero_ShouldThrowArgumentOutOfRangeException()
    {
        Dictionary<(string From, string To), decimal> source = new()
        {
            [("USD", "AUD")] = 0m,
        };

        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new FixedExchangeRateTable(source);
        });
    }

    /// <summary>
    /// Verifies that the constructor rejects a negative rate.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenRateIsNegative_ShouldThrowArgumentOutOfRangeException()
    {
        Dictionary<(string From, string To), decimal> source = new()
        {
            [("USD", "AUD")] = -1m,
        };

        _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new FixedExchangeRateTable(source);
        });
    }
}
