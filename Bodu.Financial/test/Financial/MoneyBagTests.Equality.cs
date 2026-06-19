// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyBagTests.Equality.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;

namespace Bodu.Financial;

public partial class MoneyBagTests
{
    /// <summary>
    /// Verifies that two bags with the same per-currency balances compare equal through every equality member,
    /// regardless of the order the balances were supplied.
    /// </summary>
    [TestMethod]
    public void Equality_WhenSameBalances_ShouldBeEqualThroughEveryMember()
    {
        var a = new MoneyBag([new Money(10m, CurrencyCode.USD), new Money(5m, CurrencyCode.EUR)]);
        var b = new MoneyBag([new Money(5m, CurrencyCode.EUR), new Money(10m, CurrencyCode.USD)]);

        Assert.IsTrue(a.Equals(b));
        Assert.IsTrue(a.Equals((object)b));
        Assert.IsTrue(a == b);
        Assert.IsFalse(a != b);
        Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
    }

    /// <summary>
    /// Verifies that bags differing in amount, in currency count, or in type are not equal.
    /// </summary>
    [TestMethod]
    public void Equality_WhenBalancesDiffer_ShouldNotBeEqual()
    {
        var a = new MoneyBag([new Money(10m, CurrencyCode.USD)]);
        var differentAmount = new MoneyBag([new Money(20m, CurrencyCode.USD)]);
        var differentCount = new MoneyBag([new Money(10m, CurrencyCode.USD), new Money(1m, CurrencyCode.EUR)]);

        Assert.IsFalse(a.Equals(differentAmount));
        Assert.IsFalse(a.Equals(differentCount));
        Assert.IsTrue(a != differentAmount);
        Assert.IsFalse(a.Equals((object)"not a bag"));
    }

    /// <summary>
    /// Verifies that the equality operators treat <see langword="null" /> operands consistently.
    /// </summary>
    [TestMethod]
    public void EqualityOperators_WhenOperandIsNull_ShouldHandleNull()
    {
        var bag = new MoneyBag([new Money(1m, CurrencyCode.USD)]);

        Assert.IsTrue((MoneyBag?)null == (MoneyBag?)null);
        Assert.IsFalse(bag == null);
        Assert.IsTrue(bag != null);
        Assert.IsFalse(null == bag);
    }
}
