// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyBagTests.Accessors.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;

namespace Bodu.Financial;

public partial class MoneyBagTests
{
    /// <summary>
    /// Verifies that <see cref="MoneyBag.Of(Money[])" /> sums duplicate currencies.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void Of_WhenDuplicateCurrencies_ShouldSumBalances()
    {
        var bag = MoneyBag.Of(new Money(10m, CurrencyCode.USD), new Money(5m, CurrencyCode.USD), new Money(2m, CurrencyCode.EUR));

        Assert.AreEqual(new Money(15m, CurrencyCode.USD), bag.GetBalance(CurrencyCode.USD));
        Assert.AreEqual(new Money(2m, CurrencyCode.EUR), bag.GetBalance(CurrencyCode.EUR));
    }

    /// <summary>
    /// Verifies that <see cref="MoneyBag.FromBalances(IEnumerable{Money})" /> builds an equivalent bag.
    /// </summary>
    [TestMethod]
    public void FromBalances_WhenGivenSequence_ShouldBuildBag()
    {
        var bag = MoneyBag.FromBalances([new Money(10m, CurrencyCode.USD), new Money(2m, CurrencyCode.EUR)]);

        Assert.AreEqual(2, bag.Count);
    }

    /// <summary>
    /// Verifies that <see cref="MoneyBag.TryGetBalance(CurrencyCode, out Money)" /> reports presence and absence without
    /// throwing.
    /// </summary>
    [TestMethod]
    public void TryGetBalance_WhenPresentAndAbsent_ShouldReflectMembership()
    {
        var bag = MoneyBag.Of(new Money(10m, CurrencyCode.USD));

        Assert.IsTrue(bag.TryGetBalance(CurrencyCode.USD, out Money usd));
        Assert.AreEqual(new Money(10m, CurrencyCode.USD), usd);

        Assert.IsFalse(bag.TryGetBalance(CurrencyCode.EUR, out Money eur));
        Assert.AreEqual(default, eur);
    }

    /// <summary>
    /// Verifies that <see cref="MoneyBag.GetBalance(CurrencyInfo)" /> resolves the balance by currency metadata.
    /// </summary>
    [TestMethod]
    public void GetBalance_WhenCurrencyInfo_ShouldResolveBalance()
    {
        var bag = MoneyBag.Of(new Money(10m, CurrencyCode.USD));

        Assert.AreEqual(new Money(10m, CurrencyCode.USD), bag.GetBalance(CurrencyRegistry.Get("USD")));
    }

    /// <summary>
    /// Verifies that <see cref="MoneyBag.GetBalance(CurrencyCode)" /> resolves the balance by ISO enum value.
    /// </summary>
    [TestMethod]
    public void GetBalance_WhenCurrencyCode_ShouldResolveBalance()
    {
        var bag = MoneyBag.Of(new Money(10m, CurrencyCode.USD));

        Assert.AreEqual(new Money(10m, CurrencyCode.USD), bag.GetBalance(CurrencyCode.USD));
        Assert.IsNull(bag.GetBalance(CurrencyCode.EUR));
    }
}
