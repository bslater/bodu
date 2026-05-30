// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FixedExchangeRateTableTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test;
using Bodu.Test.Assertions;

namespace Bodu.Numerics;

[TestClass]
public partial class FixedExchangeRateTableTests
{
    private static FixedExchangeRateTable BuildTable() => new(new[]
    {
        new KeyValuePair<ExchangeRatePair, decimal>(new ExchangeRatePair("USD", "AUD"), 1.50m),
        new KeyValuePair<ExchangeRatePair, decimal>(new ExchangeRatePair("EUR", "AUD"), 1.60m),
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
    /// Verifies that the constructor rejects duplicate pair keys.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenDuplicatePair_ShouldThrowArgumentException()
    {
        ExceptionAssert.ThrowsExactlyWithParamName<ArgumentException>(
            () =>
            {
                _ = new FixedExchangeRateTable(new[]
                {
                    new KeyValuePair<ExchangeRatePair, decimal>(new ExchangeRatePair("USD", "AUD"), 1.50m),
                    new KeyValuePair<ExchangeRatePair, decimal>(new ExchangeRatePair("USD", "AUD"), 1.55m),
                });
            },
            "rates");
    }

    /// <summary>
    /// Verifies that the constructor rejects zero or negative rates.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenRateIsNotPositive_ShouldThrowArgumentOutOfRangeException()
    {
        ExceptionAssert.ThrowsExactlyWithParamName<ArgumentOutOfRangeException>(
            () =>
            {
                _ = new FixedExchangeRateTable(new[]
                {
                    new KeyValuePair<ExchangeRatePair, decimal>(new ExchangeRatePair("USD", "AUD"), 0m),
                });
            },
            "rates");
    }

    /// <summary>
    /// Verifies that the constructor rejects a <see langword="null" /> input enumerable.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenRatesIsNull_ShouldThrowArgumentNullException()
    {
        ExceptionAssert.ThrowsExactlyWithParamName<ArgumentNullException>(
            () =>
            {
                _ = new FixedExchangeRateTable(null!);
            },
            "rates");
    }

    /// <summary>
    /// Verifies that invalid ISO codes throw <see cref="ArgumentException" />.
    /// </summary>
    [TestMethod]
    public void GetRate_WhenFromIsoCodeMalformed_ShouldThrowArgumentException()
    {
        FixedExchangeRateTable table = BuildTable();

        ExceptionAssert.ThrowsExactlyWithParamName<ArgumentException>(
            () => table.GetRate("usd", "AUD"),
            "fromIsoCode");
    }
}
