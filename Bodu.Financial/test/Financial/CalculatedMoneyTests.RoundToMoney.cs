// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CalculatedMoneyTests.RoundToMoney.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;

namespace Bodu.Financial;

public partial class CalculatedMoneyTests
{

    /// <summary>
    /// Verifies that <see cref="CalculatedMoney.RoundToMoney(MonetaryContext?)" /> rounds to the registered currency's
    /// minor units under the default context.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void RoundToMoney_WhenDefaultContext_ShouldRoundToRegistryMinorUnits()
    {
        CalculatedMoney calc = new(1.235m, CurrencyCode.USD);

        Money settled = calc.RoundToMoney();

        Assert.AreEqual(new Money(1.24m, CurrencyCode.USD), settled);
    }

    /// <summary>
    /// Verifies that deferring rounding through a calculation chain yields a more accurate settlement than rounding the
    /// equivalent <see cref="Money" /> operations at each step.
    /// </summary>
    [TestMethod]
    public void RoundToMoney_WhenChainDefersRounding_ShouldBeMoreAccurateThanEagerRounding()
    {
        // 1/3 * 3 = 1.00 when rounding is deferred; eager per-step Money rounding yields 0.99.
        Money deferred = (new Money(1m, CurrencyCode.USD).ToCalculated() / 3m * 3m).RoundToMoney();
        Money eager = new Money(1m, CurrencyCode.USD) / 3m * 3m;

        Assert.AreEqual(new Money(1.00m, CurrencyCode.USD), deferred);
        Assert.AreEqual(new Money(0.99m, CurrencyCode.USD), eager);
    }

    /// <summary>
    /// Verifies that <see cref="CalculatedMoney.RoundToMoney(MidpointRounding)" /> honours the supplied midpoint rule.
    /// </summary>
    [TestMethod]
    public void RoundToMoney_WhenAwayFromZero_ShouldRoundMidpointAway()
    {
        CalculatedMoney calc = new(1.225m, CurrencyCode.USD);

        Assert.AreEqual(new Money(1.23m, CurrencyCode.USD), calc.RoundToMoney(MidpointRounding.AwayFromZero));
        Assert.AreEqual(new Money(1.22m, CurrencyCode.USD), calc.RoundToMoney());
    }

    /// <summary>
    /// Verifies that a context custom scale wider than the currency's minor units settles through the explicit-scale
    /// path, so the resulting value reports the requested precision.
    /// </summary>
    [TestMethod]
    public void RoundToMoney_WhenCustomScaleWiderThanMinorUnits_ShouldSettleWithExplicitScale()
    {
        CalculatedMoney calc = new(1.23456m, CurrencyCode.USD);

        Money settled = calc.RoundToMoney(MonetaryContext.Default with { ScalePolicy = ScalePolicy.Custom, CustomScale = 4 });

        Assert.AreEqual(1.2346m, settled.Amount);
        Assert.AreEqual(4, settled.MinorUnits);
        Assert.AreEqual(CurrencyCode.USD, settled.Code);
    }

    /// <summary>
    /// Verifies the documented settlement-versus-calculation distinction: a per-step <see cref="Money" /> chain rounds
    /// at every operation and accumulates error, whereas the equivalent <see cref="CalculatedMoney" /> chain defers
    /// rounding to a single settlement boundary and recovers the exact result. Splitting 10.00 USD into a third and
    /// recombining yields 9.99 through <see cref="Money" /> but 10.00 through <see cref="CalculatedMoney" />.
    /// </summary>
    [TestMethod]
    public void RoundToMoney_WhenChainDefersRounding_ShouldAvoidPerStepRoundingError()
    {
        Money perStep = Money.From(10.00m, CurrencyCode.USD) * 0.3333m * 3m;

        CalculatedMoney deferred = new Money(10.00m, CurrencyCode.USD).ToCalculated() * 0.3333m * 3m;
        Money settled = deferred.RoundToMoney();

        Assert.AreEqual(9.99m, perStep.Amount);
        Assert.AreEqual(10.00m, settled.Amount);
    }

    /// <summary>
    /// Verifies that settling a currency-less <see cref="CalculatedMoney" /> throws
    /// <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void RoundToMoney_WhenCurrencyless_ShouldThrowInvalidOperationException()
    {
        Assert.ThrowsExactly<InvalidOperationException>(() => _ = default(CalculatedMoney).RoundToMoney());
    }
}
