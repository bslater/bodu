// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CalculatedMoneyTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial;

/// <summary>
/// Verifies the runtime-tagged <see cref="CalculatedMoney" /> high-precision deferred-rounding value type.
/// </summary>
[TestClass]
public class CalculatedMoneyTests
{
    /// <summary>
    /// Verifies that the constructor rejects a malformed ISO code.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenIsoCodeShapeInvalid_ShouldThrowArgumentException()
    {
        Assert.ThrowsExactly<ArgumentException>(() => _ = new CalculatedMoney(1m, "us"));
    }

    /// <summary>
    /// Verifies that <see cref="Money.ToCalculated" /> preserves the amount and ISO code.
    /// </summary>
    [TestMethod]
    public void ToCalculated_FromMoney_ShouldPreserveAmountAndIsoCode()
    {
        CalculatedMoney calc = new Money(12.34m, "USD").ToCalculated();

        Assert.AreEqual(12.34m, calc.Amount);
        Assert.AreEqual("USD", calc.IsoCode);
    }

    /// <summary>
    /// Verifies that <see cref="Money.ToCalculated" /> throws when the source carries no currency.
    /// </summary>
    [TestMethod]
    public void ToCalculated_FromDefaultMoney_ShouldThrowInvalidOperationException()
    {
        Assert.ThrowsExactly<InvalidOperationException>(() => _ = default(Money).ToCalculated());
    }

    /// <summary>
    /// Verifies that arithmetic preserves full precision rather than rounding to minor units.
    /// </summary>
    [TestMethod]
    public void Operators_WhenChained_ShouldPreserveFullPrecision()
    {
        CalculatedMoney value = new(1m, "USD");

        CalculatedMoney result = value / 3m;

        Assert.AreEqual(1m / 3m, result.Amount);
    }

    /// <summary>
    /// Verifies that addition across different currencies throws <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void Addition_WhenDifferentCurrencies_ShouldThrowInvalidOperationException()
    {
        CalculatedMoney usd = new(1m, "USD");
        CalculatedMoney eur = new(1m, "EUR");

        Assert.ThrowsExactly<InvalidOperationException>(() => _ = usd + eur);
    }

    /// <summary>
    /// Verifies that <see cref="CalculatedMoney.RoundToMoney(MonetaryContext?)" /> rounds to the registered currency's
    /// minor units under the default context.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void RoundToMoney_WhenDefaultContext_ShouldRoundToRegistryMinorUnits()
    {
        CalculatedMoney calc = new(1.235m, "USD");

        Money settled = calc.RoundToMoney();

        Assert.AreEqual(new Money(1.24m, "USD"), settled);
    }

    /// <summary>
    /// Verifies that deferring rounding through a calculation chain yields a more accurate settlement than rounding the
    /// equivalent <see cref="Money" /> operations at each step.
    /// </summary>
    [TestMethod]
    public void RoundToMoney_WhenChainDefersRounding_ShouldBeMoreAccurateThanEagerRounding()
    {
        // 1/3 * 3 = 1.00 when rounding is deferred; eager per-step Money rounding yields 0.99.
        Money deferred = (new Money(1m, "USD").ToCalculated() / 3m * 3m).RoundToMoney();
        Money eager = new Money(1m, "USD") / 3m * 3m;

        Assert.AreEqual(new Money(1.00m, "USD"), deferred);
        Assert.AreEqual(new Money(0.99m, "USD"), eager);
    }

    /// <summary>
    /// Verifies that <see cref="CalculatedMoney.RoundToMoney(MidpointRounding)" /> honours the supplied midpoint rule.
    /// </summary>
    [TestMethod]
    public void RoundToMoney_WhenAwayFromZero_ShouldRoundMidpointAway()
    {
        CalculatedMoney calc = new(1.225m, "USD");

        Assert.AreEqual(new Money(1.23m, "USD"), calc.RoundToMoney(MidpointRounding.AwayFromZero));
        Assert.AreEqual(new Money(1.22m, "USD"), calc.RoundToMoney());
    }

    /// <summary>
    /// Verifies that an unregistered currency settles to the context's custom scale via the explicit-scale path.
    /// </summary>
    [TestMethod]
    public void RoundToMoney_WhenUnregisteredAndCustomScale_ShouldSettleWithExplicitScale()
    {
        CalculatedMoney calc = new(1.23456m, "XYZ");

        Money settled = calc.RoundToMoney(MonetaryContext.Default with { ScalePolicy = ScalePolicy.Custom, CustomScale = 4 });

        Assert.AreEqual(1.2346m, settled.Amount);
        Assert.AreEqual(4, settled.MinorUnits);
        Assert.AreEqual("XYZ", settled.IsoCode);
    }

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
    /// Verifies the documented settlement-versus-calculation distinction: a per-step <see cref="Money" /> chain rounds
    /// at every operation and accumulates error, whereas the equivalent <see cref="CalculatedMoney" /> chain defers
    /// rounding to a single settlement boundary and recovers the exact result. Splitting 10.00 USD into a third and
    /// recombining yields 9.99 through <see cref="Money" /> but 10.00 through <see cref="CalculatedMoney" />.
    /// </summary>
    [TestMethod]
    public void RoundToMoney_WhenChainDefersRounding_ShouldAvoidPerStepRoundingError()
    {
        Money perStep = Money.From(10.00m, "USD") * 0.3333m * 3m;

        CalculatedMoney deferred = new Money(10.00m, "USD").ToCalculated() * 0.3333m * 3m;
        Money settled = deferred.RoundToMoney();

        Assert.AreEqual(9.99m, perStep.Amount);
        Assert.AreEqual(10.00m, settled.Amount);
    }

    /// <summary>
    /// Verifies that <see cref="CalculatedMoney" /> carries high-precision <see cref="decimal" /> rather than an exact
    /// rational: one third stored and read back equals the 28-29 digit <see cref="decimal" /> quotient, not the exact
    /// mathematical value (which would require the <c>Fraction</c>-based exact APIs).
    /// </summary>
    [TestMethod]
    public void Amount_WhenStoringOneThird_ShouldBeDecimalPrecisionNotExactRational()
    {
        CalculatedMoney third = new CalculatedMoney(1m, "USD") / 3m;

        Assert.AreEqual(1m / 3m, third.Amount);
        Assert.AreNotEqual(1m, third.Amount * 3m);
    }
}
