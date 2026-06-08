// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyTests.From.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;

namespace Bodu.Financial;

public partial class MoneyTests
{
    /// <summary>
    /// Verifies that <see cref="Money.From(decimal, string, MidpointRounding)" /> constructs a runtime-tagged
    /// <see cref="Money" /> with the supplied ISO code and rounded amount.
    /// </summary>
    [TestMethod]
    public void From_WhenIsoCodeIsValid_ShouldCreateMoneyMatchingArguments()
    {
        var money = Money.From(19.995m, "USD");

        Assert.AreEqual(20.00m, money.Amount);
        Assert.AreEqual("USD", money.IsoCode);
    }

    /// <summary>
    /// Verifies that <see cref="Money.From(decimal, string, MidpointRounding)" /> throws
    /// <see cref="ArgumentNullException" /> when the ISO code is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void From_WhenIsoCodeIsNull_ShouldThrowArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = Money.From(10m, (string)null!);
        });
    }

    /// <summary>
    /// Verifies that <see cref="Money.From(decimal, CurrencyInfo, MidpointRounding)" /> constructs a money value with
    /// the supplied currency metadata's ISO code.
    /// </summary>
    [TestMethod]
    public void From_WhenCurrencyInfoIsValid_ShouldCreateMoneyMatchingInfo()
    {
        CurrencyInfo info = CurrencyRegistry.Get("EUR");

        var money = Money.From(123.45m, info);

        Assert.AreEqual(123.45m, money.Amount);
        Assert.AreEqual("EUR", money.IsoCode);
    }

    /// <summary>
    /// Verifies that <see cref="Money.From(decimal, CurrencyInfo, MidpointRounding)" /> throws
    /// <see cref="ArgumentNullException" /> when the currency metadata is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void From_WhenCurrencyInfoIsNull_ShouldThrowArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = Money.From(10m, (CurrencyInfo)null!);
        });
    }

    /// <summary>
    /// Verifies that <see cref="Money.From(decimal, CurrencyCode, MidpointRounding)" /> constructs a money value with
    /// the ISO code corresponding to the supplied enum member.
    /// </summary>
    [TestMethod]
    public void From_WhenCurrencyCodeIsDefined_ShouldCreateMoneyMatchingCode()
    {
        var money = Money.From(50.25m, CurrencyCode.JPY);

        Assert.AreEqual(50m, money.Amount);
        Assert.AreEqual("JPY", money.IsoCode);
    }

    /// <summary>
    /// Verifies that <see cref="Money.From(decimal, CurrencyCode, MidpointRounding)" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> when the enum value is not a defined member.
    /// </summary>
    [TestMethod]
    public void From_WhenCurrencyCodeIsUndefined_ShouldThrowArgumentOutOfRangeException()
    {
        ArgumentOutOfRangeException ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = Money.From(10m, (CurrencyCode)9999);
        });

        Assert.AreEqual("code", ex.ParamName);
    }
}
