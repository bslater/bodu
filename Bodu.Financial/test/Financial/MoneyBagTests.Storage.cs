// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyBagTests.Storage.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;

namespace Bodu.Financial;

public partial class MoneyBagTests
{
    /// <summary>
    /// Verifies that <see cref="MoneyBag.Balances" /> returns the same immutable instance on repeated access rather than
    /// allocating a new wrapper each call.
    /// </summary>
    [TestMethod]
    public void Balances_WhenAccessedRepeatedly_ShouldReturnSameImmutableInstance()
    {
        var bag = MoneyBag.Of(new Money(10m, CurrencyCode.USD), new Money(5m, CurrencyCode.EUR));

        Assert.AreSame(bag.Balances, bag.Balances);
    }

    /// <summary>
    /// Verifies that enumeration yields balances in stable ISO-code lexicographic order regardless of insertion order.
    /// </summary>
    [TestMethod]
    public void Enumeration_WhenInsertedOutOfOrder_ShouldYieldIsoCodeOrder()
    {
        MoneyBag bag = MoneyBag.Empty
            .Add(new Money(1m, CurrencyCode.USD))
            .Add(new Money(2m, CurrencyCode.AUD))
            .Add(new Money(3m, CurrencyCode.EUR));

        CollectionAssert.AreEqual(
            new[] { "AUD", "EUR", "USD" },
            bag.Select(m => m.Code.ToString()).ToArray());
    }

    /// <summary>
    /// Verifies that two bags with the same balances are equal regardless of the order their balances were added.
    /// </summary>
    [TestMethod]
    public void Equality_WhenSameBalancesDifferentInsertionOrder_ShouldBeEqual()
    {
        MoneyBag a = MoneyBag.Empty.Add(new Money(1m, CurrencyCode.USD)).Add(new Money(2m, CurrencyCode.EUR));
        MoneyBag b = MoneyBag.Empty.Add(new Money(2m, CurrencyCode.EUR)).Add(new Money(1m, CurrencyCode.USD));

        Assert.AreEqual(a, b);
        Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
    }

    /// <summary>
    /// Verifies that a balance reduced to zero is pruned from the bag.
    /// </summary>
    [TestMethod]
    public void Add_WhenBalanceCancelsToZero_ShouldPruneCurrency()
    {
        MoneyBag bag = MoneyBag.Empty.Add(new Money(10m, CurrencyCode.USD)).Add(new Money(-10m, CurrencyCode.USD));

        Assert.IsTrue(bag.IsEmpty);
    }
}
