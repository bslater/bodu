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
        var bag = MoneyBag.Of(new Money(10m, "USD"), new Money(5m, "USD"), new Money(2m, "EUR"));

        Assert.AreEqual(new Money(15m, "USD"), bag.GetBalance("USD"));
        Assert.AreEqual(new Money(2m, "EUR"), bag.GetBalance("EUR"));
    }

    /// <summary>
    /// Verifies that <see cref="MoneyBag.FromBalances(IEnumerable{Money})" /> builds an equivalent bag.
    /// </summary>
    [TestMethod]
    public void FromBalances_WhenGivenSequence_ShouldBuildBag()
    {
        var bag = MoneyBag.FromBalances(new[] { new Money(10m, "USD"), new Money(2m, "EUR") });

        Assert.AreEqual(2, bag.Count);
    }

    /// <summary>
    /// Verifies that <see cref="MoneyBag.TryGetBalance(string, out Money)" /> reports presence and absence without
    /// throwing.
    /// </summary>
    [TestMethod]
    public void TryGetBalance_WhenPresentAndAbsent_ShouldReflectMembership()
    {
        var bag = MoneyBag.Of(new Money(10m, "USD"));

        Assert.IsTrue(bag.TryGetBalance("USD", out Money usd));
        Assert.AreEqual(new Money(10m, "USD"), usd);

        Assert.IsFalse(bag.TryGetBalance("EUR", out Money eur));
        Assert.AreEqual(default, eur);
    }

    /// <summary>
    /// Verifies that <see cref="MoneyBag.GetBalance(CurrencyInfo)" /> resolves the balance by currency metadata.
    /// </summary>
    [TestMethod]
    public void GetBalance_WhenCurrencyInfo_ShouldResolveBalance()
    {
        var bag = MoneyBag.Of(new Money(10m, "USD"));

        Assert.AreEqual(new Money(10m, "USD"), bag.GetBalance(CurrencyRegistry.Get("USD")));
    }

    /// <summary>
    /// Verifies that <see cref="MoneyBag.GetBalance(CurrencyCode)" /> resolves the balance by ISO enum value.
    /// </summary>
    [TestMethod]
    public void GetBalance_WhenCurrencyCode_ShouldResolveBalance()
    {
        var bag = MoneyBag.Of(new Money(10m, "USD"));

        Assert.AreEqual(new Money(10m, "USD"), bag.GetBalance(CurrencyCode.USD));
        Assert.IsNull(bag.GetBalance(CurrencyCode.EUR));
    }
}
