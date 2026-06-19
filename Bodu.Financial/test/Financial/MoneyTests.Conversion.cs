// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyTests.Conversion.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;

namespace Bodu.Financial;

public partial class MoneyTests
{
    /// <summary>
    /// Verifies that the implicit operator from <see cref="Money{TCurrency}" /> to <see cref="Money" /> preserves the
    /// amount and the ISO code derived from the type parameter.
    /// </summary>
    [TestMethod]
    public void ImplicitOperator_FromTypedMoney_ShouldPreserveAmountAndIsoCode()
    {
        Money<EUR> typed = new(42.50m);

        Money runtime = typed;

        Assert.AreEqual(42.50m, runtime.Amount);
        Assert.AreEqual(CurrencyCode.EUR, runtime.Code);
    }

    /// <summary>
    /// Verifies that the explicit operator from <see cref="Money" /> to <see cref="Money{TCurrency}" /> succeeds when
    /// the runtime ISO code matches <typeparamref name="TCurrency" />.
    /// </summary>
    [TestMethod]
    public void ExplicitOperator_ToTypedMoney_WhenCurrencyMatches_ShouldReturnTypedValue()
    {
        Money runtime = new(99.99m, CurrencyCode.USD);

        var typed = (Money<USD>)runtime;

        Assert.AreEqual(new Money<USD>(99.99m), typed);
    }

    /// <summary>
    /// Verifies that the explicit operator from <see cref="Money" /> to <see cref="Money{TCurrency}" /> throws
    /// <see cref="InvalidOperationException" /> when the runtime ISO code differs from <typeparamref name="TCurrency" />.
    /// </summary>
    [TestMethod]
    public void ExplicitOperator_ToTypedMoney_WhenCurrencyMismatches_ShouldThrowInvalidOperationException()
    {
        Money runtime = new(10m, CurrencyCode.EUR);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = (Money<USD>)runtime;
        });
    }

    /// <summary>
    /// Verifies that <see cref="Money.TryAs{TCurrency}(out Money{TCurrency})" /> succeeds and yields the typed value
    /// when the runtime ISO code matches the requested currency.
    /// </summary>
    [TestMethod]
    public void TryAs_WhenCurrencyMatches_ShouldReturnTrueAndTypedValue()
    {
        Money runtime = new(12.34m, CurrencyCode.USD);

        Assert.IsTrue(runtime.TryAs(out Money<USD> typed));
        Assert.AreEqual(12.34m, typed.Amount);
    }

    /// <summary>
    /// Verifies that the <see cref="Money.Of{TCurrency}(decimal, MidpointRounding)" /> factory rounds the amount to the
    /// currency's minor units using the supplied midpoint rule.
    /// </summary>
    [TestMethod]
    public void Of_WhenRoundingOverload_ShouldRoundToMinorUnits()
    {
        var value = Money.Of<USD>(1.005m, MidpointRounding.AwayFromZero);

        Assert.AreEqual(1.01m, value.Amount);
    }
}
