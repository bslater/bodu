// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CurrencyInfoTests.CashRoundingIncrement.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Financial.Currencies;

public partial class CurrencyInfoTests
{
    /// <summary>
    /// Verifies that a <c>with</c> expression that sets <see cref="CurrencyInfo.CashRoundingIncrement" /> to a negative
    /// value, or to one that is not a whole number of the record's minor units, throws
    /// <see cref="ArgumentOutOfRangeException" /> naming the property, as the constructor does.
    /// </summary>
    /// <param name="increment">The invalid cash-rounding increment, as invariant text.</param>
    [TestMethod]
    [DataRow("-0.05")]
    [DataRow("0.001")]
    public void CashRoundingIncrement_WhenSetInvalidInWithExpression_ShouldThrowArgumentOutOfRangeException(string increment)
    {
        decimal value = decimal.Parse(increment, CultureInfo.InvariantCulture);
        var info = new CurrencyInfo("XQQ", 2, 0m, false, null, null);

        ArgumentOutOfRangeException ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = info with { CashRoundingIncrement = value };
        });

        Assert.AreEqual(nameof(CurrencyInfo.CashRoundingIncrement), ex.ParamName);
    }

    /// <summary>
    /// Verifies that an object initializer that sets <see cref="CurrencyInfo.CashRoundingIncrement" /> to a negative
    /// value, or to one that is not a whole number of the record's minor units, throws
    /// <see cref="ArgumentOutOfRangeException" /> naming the property, as the constructor does.
    /// </summary>
    /// <param name="increment">The invalid cash-rounding increment, as invariant text.</param>
    [TestMethod]
    [DataRow("-0.05")]
    [DataRow("0.001")]
    public void CashRoundingIncrement_WhenSetInvalidInObjectInitializer_ShouldThrowArgumentOutOfRangeException(string increment)
    {
        decimal value = decimal.Parse(increment, CultureInfo.InvariantCulture);

        ArgumentOutOfRangeException ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new CurrencyInfo("XQQ", 2, 0m, false, null, null) { CashRoundingIncrement = value };
        });

        Assert.AreEqual(nameof(CurrencyInfo.CashRoundingIncrement), ex.ParamName);
    }

    /// <summary>
    /// Verifies that a <c>with</c> expression that sets a valid <see cref="CurrencyInfo.CashRoundingIncrement" />
    /// produces a record carrying it and leaves the source record unchanged.
    /// </summary>
    [TestMethod]
    public void CashRoundingIncrement_WhenSetValidInWithExpression_ShouldProduceThatIncrement()
    {
        var info = new CurrencyInfo("XQQ", 2, 0m, false, null, null);

        CurrencyInfo changed = info with { CashRoundingIncrement = 0.05m };

        Assert.AreEqual(0.05m, changed.CashRoundingIncrement);
        Assert.AreEqual(0m, info.CashRoundingIncrement);
    }
}
