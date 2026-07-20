// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyBagTests.Add.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;

namespace Bodu.Financial;

public partial class MoneyBagTests
{
    /// <summary>
    /// Verifies that adding an explicit-scale amount settles it to the currency's registered minor units on entry, so
    /// the stored balance carries no sub-minor-unit digits.
    /// </summary>
    [TestMethod]
    public void Add_WhenAmountCarriesExplicitScale_ShouldSettleToRegistryPrecision()
    {
        Money unitPrice = Money.FromExplicitScale(12.345678m, CurrencyCode.USD, 6);

        MoneyBag bag = MoneyBag.Empty.Add(unitPrice);

        Assert.IsTrue(bag.TryGetBalance(CurrencyCode.USD, out Money balance));
        Assert.AreEqual(12.35m, balance.Amount);
        Assert.AreEqual(2, balance.MinorUnits);
    }

    /// <summary>
    /// Verifies that an explicit-scale amount that rounds to zero at the registered precision leaves the bag
    /// unchanged, consistent with the bag's zero-balance pruning.
    /// </summary>
    [TestMethod]
    public void Add_WhenAmountRoundsToZeroAtRegistryPrecision_ShouldLeaveBagUnchanged()
    {
        MoneyBag bag = MoneyBag.Empty.Add(Money.FromExplicitScale(0.001m, CurrencyCode.USD, 3));

        Assert.IsTrue(bag.IsEmpty);
    }

    /// <summary>
    /// Verifies that the enumerable constructor applies the same registry-precision settlement as
    /// <see cref="MoneyBag.Add(Money)" />, so both ingestion paths agree.
    /// </summary>
    [TestMethod]
    public void Add_WhenConstructedFromExplicitScaleBalances_ShouldSettleToRegistryPrecision()
    {
        var bag = new MoneyBag([Money.FromExplicitScale(7.049999m, CurrencyCode.USD, 6)]);

        Assert.IsTrue(bag.TryGetBalance(CurrencyCode.USD, out Money balance));
        Assert.AreEqual(7.05m, balance.Amount);
    }
}
