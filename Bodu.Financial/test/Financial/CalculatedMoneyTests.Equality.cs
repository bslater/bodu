// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CalculatedMoneyTests.Equality.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial;

public partial class CalculatedMoneyTests
{

    /// <summary>
    /// Verifies value equality across amount and currency.
    /// </summary>
    [TestMethod]
    public void Equality_WhenSameAmountAndCurrency_ShouldBeEqual()
    {
        Assert.AreEqual(new CalculatedMoney(1.5m, "USD"), new CalculatedMoney(1.5m, "USD"));
        Assert.AreNotEqual(new CalculatedMoney(1.5m, "USD"), new CalculatedMoney(1.5m, "EUR"));
    }

    /// <summary>
    /// Verifies that the equality members agree: equal values compare equal through the operators, the boxed override,
    /// and the hash code, while a non-<see cref="CalculatedMoney" /> object is never equal.
    /// </summary>
    [TestMethod]
    public void EqualityMembers_WhenComparingValues_ShouldBeConsistent()
    {
        CalculatedMoney a = new(1.5m, "USD");
        CalculatedMoney b = new(1.5m, "USD");
        CalculatedMoney c = new(2.5m, "USD");

        Assert.IsTrue(a == b);
        Assert.IsFalse(a != b);
        Assert.IsTrue(a != c);
        Assert.IsTrue(a.Equals((object)b));
        Assert.IsFalse(a.Equals("not money"));
        Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
    }
}
