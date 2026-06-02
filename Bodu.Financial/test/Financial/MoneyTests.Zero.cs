// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyTests.Zero.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial;

public partial class MoneyTests
{
    /// <summary>
    /// Verifies that <see cref="Money.Zero(string)" /> returns a zero amount carrying the supplied registered currency.
    /// </summary>
    [TestMethod]
    public void Zero_WhenIsoCodeValid_ShouldReturnZeroForCurrency()
    {
        Money money = Money.Zero("USD");

        Assert.AreEqual(0m, money.Amount);
        Assert.AreEqual("USD", money.IsoCode);
        Assert.IsTrue(money.HasCurrency);
    }

    /// <summary>
    /// Verifies that <see cref="Money.Zero(string)" /> throws <see cref="ArgumentNullException" /> when the ISO code is
    /// <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Zero_WhenIsoCodeNull_ShouldThrowArgumentNullException()
    {
        _ = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = Money.Zero(null!);
        });
    }

    /// <summary>
    /// Verifies that <see cref="Money.Zero(string)" /> throws <see cref="ArgumentException" /> when the ISO code is not
    /// exactly three uppercase ASCII letters, rather than only rejecting empty or whitespace input.
    /// </summary>
    /// <param name="isoCode">The malformed ISO code under test.</param>
    [TestMethod]
    [DataRow("")]
    [DataRow("US")]
    [DataRow("usd")]
    [DataRow("US1")]
    [DataRow("USDD")]
    public void Zero_WhenIsoCodeWrongShape_ShouldThrowArgumentException(string isoCode)
    {
        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = Money.Zero(isoCode);
        });

        Assert.AreEqual("isoCode", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="Money.Zero(string)" /> throws <see cref="ArgumentException" /> when the ISO code is
    /// structurally valid but is not registered in <see cref="CurrencyRegistry" />.
    /// </summary>
    [TestMethod]
    public void Zero_WhenIsoCodeUnregistered_ShouldThrowArgumentException()
    {
        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = Money.Zero("QQQ");
        });

        Assert.AreEqual("isoCode", ex.ParamName);
    }
}
