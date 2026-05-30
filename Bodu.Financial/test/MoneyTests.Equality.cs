// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyTests.Equality.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;

namespace Bodu.Financial;

public partial class MoneyTests
{
    /// <summary>
    /// Verifies that two amounts with the same currency and value compare equal.
    /// </summary>
    [TestMethod]
    public void Equals_WhenSameAmount_ShouldReturnTrue()
    {
        Money<USD> a = new Money<USD>(19.99m);
        Money<USD> b = new Money<USD>(19.99m);

        Assert.IsTrue(a.Equals(b));
        Assert.IsTrue(a == b);
    }

    /// <summary>
    /// Verifies that two amounts differing only in input scale compare equal after construction normalization.
    /// </summary>
    [TestMethod]
    public void Equals_WhenAmountDiffersBeforeNormalization_ShouldNormalizeAndReturnTrue()
    {
        Money<USD> a = new Money<USD>(19.99m);
        Money<USD> b = new Money<USD>(19.9900m);

        Assert.AreEqual(a, b);
    }

    /// <summary>
    /// Verifies that boxed comparison against a same-currency <see cref="Money{TCurrency}" /> succeeds.
    /// </summary>
    [TestMethod]
    public void Equals_WhenBoxedSameCurrency_ShouldReturnTrue()
    {
        object boxed = new Money<USD>(19.99m);

        Assert.IsTrue(new Money<USD>(19.99m).Equals(boxed));
    }

    /// <summary>
    /// Verifies that boxed comparison against a different-currency instance returns <see langword="false" /> without throwing.
    /// </summary>
    [TestMethod]
    public void Equals_WhenBoxedDifferentCurrency_ShouldReturnFalse()
    {
        object boxedJpy = new Money<JPY>(5m);

        Assert.IsFalse(new Money<USD>(5m).Equals(boxedJpy));
    }

    /// <summary>
    /// Verifies that hash codes for amounts of different currencies but the same numeric value differ.
    /// </summary>
    [TestMethod]
    public void GetHashCode_WhenSameAmountDifferentCurrency_ShouldDifferInMostCases()
    {
        int hashUsd = new Money<USD>(5m).GetHashCode();
        int hashJpy = new Money<JPY>(5m).GetHashCode();

        Assert.AreNotEqual(hashUsd, hashJpy);
    }

    /// <summary>
    /// Verifies that <see cref="IComparable{T}.CompareTo(T)" /> orders amounts within a single currency.
    /// </summary>
    [TestMethod]
    public void CompareTo_WhenSameCurrency_ShouldOrderByAmount()
    {
        Money<USD> small = new Money<USD>(1m);
        Money<USD> large = new Money<USD>(2m);

        Assert.IsTrue(small.CompareTo(large) < 0);
        Assert.IsTrue(large.CompareTo(small) > 0);
        Assert.AreEqual(0, small.CompareTo(new Money<USD>(1m)));
    }

    /// <summary>
    /// Verifies that the non-generic <see cref="IComparable" /> entry-point returns 1 for a <see langword="null" />
    /// comparand, matching the BCL convention.
    /// </summary>
    [TestMethod]
    public void NonGenericCompareTo_WhenComparandIsNull_ShouldReturnOne()
    {
        Money<USD> money = new Money<USD>(1m);

        Assert.AreEqual(1, ((IComparable)money).CompareTo(null));
    }

    /// <summary>
    /// Verifies that the non-generic <see cref="IComparable" /> entry-point throws when the comparand is a
    /// different-currency instance.
    /// </summary>
    [TestMethod]
    public void NonGenericCompareTo_WhenComparandDiffersInCurrency_ShouldThrowArgumentException()
    {
        Money<USD> usd = new Money<USD>(1m);
        object jpy = new Money<JPY>(1m);

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = ((IComparable)usd).CompareTo(jpy);
        });
    }
}
