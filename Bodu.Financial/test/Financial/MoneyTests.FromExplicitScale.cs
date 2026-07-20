// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyTests.FromExplicitScale.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;

namespace Bodu.Financial;

public partial class MoneyTests
{
    /// <summary>
    /// Verifies that <see cref="Money.FromExplicitScale(decimal, CurrencyCode, int, MidpointRounding)" /> constructs a
    /// value whose <see cref="Money.MinorUnits" /> reports the supplied scale rather than the currency's registered
    /// precision.
    /// </summary>
    [TestMethod]
    public void FromExplicitScale_WhenScaleExceedsRegistryPrecision_ShouldReportSuppliedScale()
    {
        Money price = Money.FromExplicitScale(145.678912m, CurrencyCode.USD, 6);

        Assert.AreEqual(6, price.MinorUnits);
        Assert.AreEqual(145.678912m, price.Amount);
        Assert.AreEqual(CurrencyCode.USD, price.Code);
    }

    /// <summary>
    /// Verifies that <see cref="Money.FromExplicitScale(decimal, CurrencyCode, int, MidpointRounding)" /> rounds the
    /// amount to the supplied scale using banker's rounding by default.
    /// </summary>
    [TestMethod]
    public void FromExplicitScale_WhenAmountExceedsScale_ShouldRoundToScaleWithBankersRounding()
    {
        Money price = Money.FromExplicitScale(1.2345675m, CurrencyCode.USD, 6);

        Assert.AreEqual(1.234568m, price.Amount);
    }

    /// <summary>
    /// Verifies that <see cref="Money.FromExplicitScale(decimal, CurrencyCode, int, MidpointRounding)" /> honours an
    /// explicit midpoint-rounding rule when normalising to the supplied scale.
    /// </summary>
    [TestMethod]
    public void FromExplicitScale_WhenRoundingIsAwayFromZero_ShouldApplySuppliedRule()
    {
        Money banker = Money.FromExplicitScale(1.2345665m, CurrencyCode.USD, 6);
        Money away = Money.FromExplicitScale(1.2345665m, CurrencyCode.USD, 6, MidpointRounding.AwayFromZero);

        Assert.AreEqual(1.234566m, banker.Amount);
        Assert.AreEqual(1.234567m, away.Amount);
    }

    /// <summary>
    /// Verifies that a value with fewer significant fractional digits than its explicit scale formats with the full
    /// scale, padding trailing zeros.
    /// </summary>
    [TestMethod]
    public void FromExplicitScale_WhenAmountHasFewerDigitsThanScale_ShouldFormatWithTrailingZeros()
    {
        Money price = Money.FromExplicitScale(12.5m, CurrencyCode.USD, 6);

        Assert.AreEqual("USD 12.500000", price.ToString("R"));
    }

    /// <summary>
    /// Verifies that a scale below the currency's registered precision is honoured, so a coarser-grained value (for
    /// example whole-dollar pricing) reports the reduced scale.
    /// </summary>
    [TestMethod]
    public void FromExplicitScale_WhenScaleBelowRegistryPrecision_ShouldReportSuppliedScale()
    {
        Money price = Money.FromExplicitScale(19.99m, CurrencyCode.USD, 0);

        Assert.AreEqual(0, price.MinorUnits);
        Assert.AreEqual(20m, price.Amount);
    }

    /// <summary>
    /// Verifies that addition of two explicit-scale values preserves the scale, so the sum still reports and rounds at
    /// the supplied precision rather than the currency's registered minor units.
    /// </summary>
    [TestMethod]
    public void FromExplicitScale_WhenValuesAdded_ShouldPreserveExplicitScale()
    {
        Money sum = Money.FromExplicitScale(1.000001m, CurrencyCode.USD, 6)
            + Money.FromExplicitScale(2.000002m, CurrencyCode.USD, 6);

        Assert.AreEqual(6, sum.MinorUnits);
        Assert.AreEqual(3.000003m, sum.Amount);
    }

    /// <summary>
    /// Verifies that scalar multiplication of an explicit-scale value rounds the product at the value's own scale, not
    /// the currency's registered precision.
    /// </summary>
    [TestMethod]
    public void FromExplicitScale_WhenMultipliedByScalar_ShouldRoundAtExplicitScale()
    {
        Money product = Money.FromExplicitScale(0.000003m, CurrencyCode.USD, 6) * 0.5m;

        Assert.AreEqual(6, product.MinorUnits);
        Assert.AreEqual(0.000002m, product.Amount);
    }

    /// <summary>
    /// Verifies that <see cref="Money.FromExplicitScale(decimal, CurrencyCode, int, MidpointRounding)" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> when the currency is <see cref="CurrencyCode.None" />.
    /// </summary>
    [TestMethod]
    public void FromExplicitScale_WhenCurrencyCodeIsNone_ShouldThrowArgumentOutOfRangeException()
    {
        ArgumentOutOfRangeException ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = Money.FromExplicitScale(10m, CurrencyCode.None, 4);
        });

        Assert.AreEqual("code", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="Money.FromExplicitScale(decimal, CurrencyCode, int, MidpointRounding)" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> when the scale falls outside the supported 0 to 28 range.
    /// </summary>
    [TestMethod]
    [DataRow(-1, DisplayName = "Scale below range")]
    [DataRow(29, DisplayName = "Scale above range")]
    public void FromExplicitScale_WhenScaleOutOfRange_ShouldThrowArgumentOutOfRangeException(int minorUnits)
    {
        ArgumentOutOfRangeException ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = Money.FromExplicitScale(10m, CurrencyCode.USD, minorUnits);
        });

        Assert.AreEqual("minorUnits", ex.ParamName);
    }
}
