// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Numerics;

[TestClass]
public partial class MoneyTests
{
    /// <summary>
    /// Verifies that <see cref="Money{TCurrency}.IsoCode" /> and <see cref="Money{TCurrency}.DecimalPlaces" /> return
    /// the values defined by the marker currency type.
    /// </summary>
    [TestMethod]
    public void StaticProperties_WhenAccessed_ShouldReturnCurrencyMetadata()
    {
        Assert.AreEqual("USD", Money<TestCurrencies.Usd>.IsoCode);
        Assert.AreEqual(2, Money<TestCurrencies.Usd>.DecimalPlaces);
        Assert.AreEqual("JPY", Money<TestCurrencies.Jpy>.IsoCode);
        Assert.AreEqual(0, Money<TestCurrencies.Jpy>.DecimalPlaces);
    }

    /// <summary>
    /// Verifies that <see cref="Money{TCurrency}.Convert{TTarget}(decimal, MidpointRounding)" /> applies the supplied
    /// rate and rounds to the target currency's natural decimal-place count.
    /// </summary>
    [TestMethod]
    public void Convert_WhenCalled_ShouldMultiplyAndRoundToTargetDecimalPlaces()
    {
        Money<TestCurrencies.Usd> amount = new(100m);

        Money<TestCurrencies.Aud> converted = amount.Convert<TestCurrencies.Aud>(1.515m);

        Assert.AreEqual(151.50m, converted.Amount);
    }

    /// <summary>
    /// Verifies that converting to JPY (zero decimal places) produces an integer-valued amount.
    /// </summary>
    [TestMethod]
    public void Convert_WhenTargetHasZeroDecimals_ShouldRoundToInteger()
    {
        Money<TestCurrencies.Usd> amount = new(100m);

        Money<TestCurrencies.Jpy> converted = amount.Convert<TestCurrencies.Jpy>(155.36m, MidpointRounding.ToEven);

        Assert.AreEqual(15536m, converted.Amount);
    }

    /// <summary>
    /// Verifies that the <see cref="MidpointRounding.AwayFromZero" /> mode is honoured.
    /// </summary>
    [TestMethod]
    public void Convert_WhenAwayFromZeroSelected_ShouldRoundUpOnTie()
    {
        Money<TestCurrencies.Usd> amount = new(1m);

        Money<TestCurrencies.Usd> converted = amount.Convert<TestCurrencies.Usd>(0.125m, MidpointRounding.AwayFromZero);

        Assert.AreEqual(0.13m, converted.Amount);
    }

    /// <summary>
    /// Verifies that the <see cref="MidpointRounding.ToEven" /> mode rounds to nearest even on ties.
    /// </summary>
    [TestMethod]
    public void Convert_WhenToEvenSelected_ShouldRoundToNearestEvenOnTie()
    {
        Money<TestCurrencies.Usd> amount = new(1m);

        Money<TestCurrencies.Usd> converted = amount.Convert<TestCurrencies.Usd>(0.125m, MidpointRounding.ToEven);

        Assert.AreEqual(0.12m, converted.Amount);
    }
}
