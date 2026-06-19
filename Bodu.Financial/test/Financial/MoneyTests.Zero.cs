// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyTests.Zero.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;
using Bodu.Test.Assertions;

namespace Bodu.Financial;

public partial class MoneyTests
{
    /// <summary>
    /// Verifies that <see cref="Money.Zero(CurrencyCode)" /> returns a zero amount carrying the supplied currency.
    /// </summary>
    [TestMethod]
    public void Zero_WhenCurrencyCodeValid_ShouldReturnZeroForCurrency()
    {
        var money = Money.Zero(CurrencyCode.USD);

        Assert.AreEqual(0m, money.Amount);
        Assert.AreEqual(CurrencyCode.USD, money.Code);
        Assert.IsTrue(money.HasCurrency);
    }

    /// <summary>
    /// Verifies that <see cref="Money.Zero(CurrencyCode)" /> throws <see cref="ArgumentOutOfRangeException" /> when the
    /// currency is <see cref="CurrencyCode.None" />.
    /// </summary>
    [TestMethod]
    public void Zero_WhenCodeIsNone_ShouldThrowArgumentOutOfRangeException()
    {
        ExceptionAssert.ThrowsExactlyWithParamName<ArgumentOutOfRangeException>(
            () =>
            {
                _ = Money.Zero(CurrencyCode.None);
            },
            "code");
    }

    /// <summary>
    /// Verifies that <see cref="Money.Zero(CurrencyCode)" /> throws <see cref="ArgumentOutOfRangeException" /> when the
    /// currency is not a defined <see cref="CurrencyCode" /> member.
    /// </summary>
    [TestMethod]
    public void Zero_WhenCodeUndefined_ShouldThrowArgumentOutOfRangeException()
    {
        ExceptionAssert.ThrowsExactlyWithParamName<ArgumentOutOfRangeException>(
            () =>
            {
                _ = Money.Zero((CurrencyCode)9999);
            },
            "code");
    }
}
