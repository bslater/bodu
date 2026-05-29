// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyTests.Ctors.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Numerics.Currencies;

namespace Bodu.Numerics;

public partial class MoneyTests
{
    /// <summary>
    /// Verifies that the primary constructor rounds the amount to the currency's minor-unit precision using
    /// banker's rounding by default.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenAmountHasExcessPrecision_ShouldRoundToMinorUnits()
    {
        Money<USD> money = new Money<USD>(1.235m);

        Assert.AreEqual(1.24m, money.Amount);
    }

    /// <summary>
    /// Verifies that the constructor truncates to zero decimal places for a zero-minor-unit currency.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenZeroMinorUnitCurrency_ShouldRoundToWholeUnits()
    {
        Money<JPY> money = new Money<JPY>(99.6m);

        Assert.AreEqual(100m, money.Amount);
    }

    /// <summary>
    /// Verifies that the constructor preserves three decimal places for a three-minor-unit currency.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenThreeMinorUnitCurrency_ShouldRoundToThreeDecimalPlaces()
    {
        Money<BHD> money = new Money<BHD>(12.3456m);

        Assert.AreEqual(12.346m, money.Amount);
    }

    /// <summary>
    /// Verifies that the explicit-rounding constructor honors the supplied <see cref="MidpointRounding" /> rule.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenAwayFromZeroRequested_ShouldRoundUpAtMidpoint()
    {
        Money<USD> money = new Money<USD>(1.235m, MidpointRounding.AwayFromZero);

        Assert.AreEqual(1.24m, money.Amount);
    }

    /// <summary>
    /// Verifies that the default value represents zero.
    /// </summary>
    [TestMethod]
    public void DefaultValue_WhenInstantiated_ShouldBeZero()
    {
        Money<USD> money = default;

        Assert.AreEqual(0m, money.Amount);
        Assert.IsTrue(money.IsZero);
    }

    /// <summary>
    /// Verifies that the <see cref="Money.Of{TCurrency}(decimal)" /> helper produces the same value as the
    /// constructor.
    /// </summary>
    [TestMethod]
    public void Of_WhenCalled_ShouldMatchConstructor()
    {
        Money<USD> viaHelper = Money.Of<USD>(19.99m);
        Money<USD> viaCtor = new Money<USD>(19.99m);

        Assert.AreEqual(viaCtor, viaHelper);
    }

    /// <summary>
    /// Verifies that the <see cref="Money.Zero{TCurrency}" /> helper returns the zero value.
    /// </summary>
    [TestMethod]
    public void Zero_WhenCalled_ShouldReturnZero()
    {
        Money<USD> zero = Money.Zero<USD>();

        Assert.IsTrue(zero.IsZero);
    }
}
