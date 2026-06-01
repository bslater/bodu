// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FixedExchangeRateTableTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test;

namespace Bodu.Financial;

[TestClass]
public partial class FixedExchangeRateTableTests
{
    private static FixedExchangeRateTable BuildTable() => new(new Dictionary<(string From, string To), decimal>
    {
        [("USD", "AUD")] = 1.50m,
        [("EUR", "AUD")] = 1.60m,
    });

    /// <summary>
    /// Verifies that <see cref="FixedExchangeRateTable.GetRate" /> returns the stored rate for a direct pair.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Smoke)]
    public void GetRate_WhenDirectPairExists_ShouldReturnStoredRate()
    {
        FixedExchangeRateTable table = BuildTable();

        Assert.AreEqual(1.50m, table.GetRate("USD", "AUD"));
    }

    /// <summary>
    /// Verifies that <see cref="FixedExchangeRateTable.GetRate" /> returns <c>1</c> for a same-currency lookup.
    /// </summary>
    [TestMethod]
    public void GetRate_WhenSameCurrency_ShouldReturnOne()
    {
        FixedExchangeRateTable table = BuildTable();

        Assert.AreEqual(1m, table.GetRate("USD", "USD"));
    }

    /// <summary>
    /// Verifies that <see cref="FixedExchangeRateTable.GetRate" /> falls back to the reciprocal of the inverse pair
    /// when the direct pair is absent.
    /// </summary>
    [TestMethod]
    public void GetRate_WhenOnlyInverseExists_ShouldReturnReciprocal()
    {
        FixedExchangeRateTable table = BuildTable();

        Assert.AreEqual(1m / 1.50m, table.GetRate("AUD", "USD"));
    }

    /// <summary>
    /// Verifies that <see cref="FixedExchangeRateTable.GetRate" /> throws when no rate is available.
    /// </summary>
    [TestMethod]
    public void GetRate_WhenPairMissing_ShouldThrowKeyNotFoundException()
    {
        FixedExchangeRateTable table = BuildTable();

        _ = Assert.ThrowsExactly<KeyNotFoundException>(() => table.GetRate("JPY", "EUR"));
    }

    /// <summary>
    /// Verifies that the constructor rejects a <see langword="null" /> rates dictionary.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenRatesIsNull_ShouldThrowArgumentNullException()
    {
        _ = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new FixedExchangeRateTable((IReadOnlyDictionary<(string From, string To), decimal>)null!);
        });
    }

    /// <summary>
    /// Verifies that <see cref="FixedExchangeRateTable.GetRate" /> throws when either ISO code argument is
    /// <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void GetRate_WhenAnyIsoCodeIsNull_ShouldThrowArgumentNullException()
    {
        FixedExchangeRateTable table = BuildTable();

        _ = Assert.ThrowsExactly<ArgumentNullException>(() => table.GetRate(null!, "AUD"));
        _ = Assert.ThrowsExactly<ArgumentNullException>(() => table.GetRate("USD", null!));
    }
}
