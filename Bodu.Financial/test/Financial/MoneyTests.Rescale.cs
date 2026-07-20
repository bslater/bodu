// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyTests.Rescale.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;

namespace Bodu.Financial;

public partial class MoneyTests
{
    /// <summary>
    /// Verifies that <see cref="Money.Rescale(int, MidpointRounding)" /> to a finer scale is lossless: the amount is
    /// unchanged and the reported precision widens, padding trailing zeros in formatting.
    /// </summary>
    [TestMethod]
    public void Rescale_WhenTargetScaleIsFiner_ShouldWidenPrecisionWithoutChangingAmount()
    {
        Money rescaled = new Money(19.99m, CurrencyCode.USD).Rescale(6);

        Assert.AreEqual(6, rescaled.MinorUnits);
        Assert.AreEqual(19.99m, rescaled.Amount);
        Assert.AreEqual("USD 19.990000", rescaled.ToString("R"));
    }

    /// <summary>
    /// Verifies that <see cref="Money.Rescale(int, MidpointRounding)" /> to a coarser scale rounds the amount at the
    /// new scale using banker's rounding by default.
    /// </summary>
    [TestMethod]
    public void Rescale_WhenTargetScaleIsCoarser_ShouldRoundWithBankersRounding()
    {
        Money price = Money.FromExplicitScale(145.678912m, CurrencyCode.USD, 6);

        Money settled = price.Rescale(2);

        Assert.AreEqual(2, settled.MinorUnits);
        Assert.AreEqual(145.68m, settled.Amount);
    }

    /// <summary>
    /// Verifies that <see cref="Money.Rescale(int, MidpointRounding)" /> honours an explicit midpoint rule when
    /// rounding to the coarser scale, diverging from banker's rounding on a midpoint digit.
    /// </summary>
    [TestMethod]
    public void Rescale_WhenRoundingIsAwayFromZero_ShouldApplySuppliedRule()
    {
        Money midpoint = Money.FromExplicitScale(1.225m, CurrencyCode.USD, 3);

        Assert.AreEqual(1.22m, midpoint.Rescale(2).Amount);
        Assert.AreEqual(1.23m, midpoint.Rescale(2, MidpointRounding.AwayFromZero).Amount);
    }

    /// <summary>
    /// Verifies that rescaling to the currency's registered precision produces a value equal to ordinary settled
    /// money at that amount.
    /// </summary>
    [TestMethod]
    public void Rescale_WhenTargetIsRegistryPrecision_ShouldEqualOrdinaryMoney()
    {
        Money settled = Money.FromExplicitScale(19.994m, CurrencyCode.USD, 3).Rescale(2);

        Assert.AreEqual(new Money(19.99m, CurrencyCode.USD), settled);
        Assert.AreEqual(2, settled.MinorUnits);
    }

    /// <summary>
    /// Verifies that <see cref="Money.Rescale(int, MidpointRounding)" /> on a default-initialised, currency-less value
    /// throws <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void Rescale_WhenValueIsDefault_ShouldThrowInvalidOperationException() =>
        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = default(Money).Rescale(2);
        });

    /// <summary>
    /// Verifies that <see cref="Money.Rescale(int, MidpointRounding)" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> when the target scale falls outside the supported 0 to 28 range.
    /// </summary>
    [TestMethod]
    [DataRow(-1, DisplayName = "Scale below range")]
    [DataRow(29, DisplayName = "Scale above range")]
    public void Rescale_WhenScaleOutOfRange_ShouldThrowArgumentOutOfRangeException(int minorUnits)
    {
        ArgumentOutOfRangeException ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new Money(10m, CurrencyCode.USD).Rescale(minorUnits);
        });

        Assert.AreEqual("minorUnits", ex.ParamName);
    }
}
