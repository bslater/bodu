// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateOfTBaseTQuoteTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;

namespace Bodu.Financial;

/// <summary>
/// Verifies the strongly-typed <see cref="ExchangeRate{TBase, TQuote}" /> companion of <see cref="ExchangeRate" />,
/// including construction, the typed <c>Inverse</c> / <c>ToRuntime</c> / <c>FromRuntime</c> bridges, and the
/// compile-time-direction-checked conversion on <see cref="Money{TCurrency}" />.
/// </summary>
[TestClass]
public class ExchangeRateOfTBaseTQuoteTests
{
    private static readonly DateOnly SampleDate = new(2026, 1, 15);
    private const string SampleProvider = "TEST";

    /// <summary>
    /// Verifies that the typed exchange rate exposes the ISO codes of <typeparamref name="TBase" /> and
    /// <typeparamref name="TQuote" /> on its <see cref="ExchangeRate{TBase, TQuote}.FromIsoCode" /> and
    /// <see cref="ExchangeRate{TBase, TQuote}.ToIsoCode" /> properties.
    /// </summary>
    [TestMethod]
    public void Properties_WhenConstructed_ShouldDeriveIsoCodesFromTypeParameters()
    {
        var rate = new ExchangeRate<USD, AUD>(1.52m, SampleDate, SampleProvider);

        Assert.AreEqual("USD", rate.FromIsoCode);
        Assert.AreEqual("AUD", rate.ToIsoCode);
        Assert.AreEqual(1.52m, rate.Rate);
        Assert.AreEqual(SampleDate, rate.Date);
        Assert.AreEqual(SampleProvider, rate.Provider);
        Assert.IsFalse(rate.IsInverted);
    }

    /// <summary>
    /// Verifies that the constructor throws <see cref="ArgumentOutOfRangeException" /> when the supplied rate is zero
    /// or negative.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenRateIsNotPositive_ShouldThrowArgumentOutOfRangeException()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new ExchangeRate<USD, AUD>(0m, SampleDate, SampleProvider);
        });
    }

    /// <summary>
    /// Verifies that the constructor throws <see cref="ArgumentException" /> when <c>provider</c> is white-space.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenProviderIsWhiteSpace_ShouldThrowArgumentException()
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = new ExchangeRate<USD, AUD>(1.52m, SampleDate, "  ");
        });
    }

    /// <summary>
    /// Verifies that <see cref="ExchangeRate{TBase, TQuote}.Inverse" /> returns the typed reciprocal rate with the
    /// inversion flag toggled.
    /// </summary>
    [TestMethod]
    public void Inverse_WhenCalled_ShouldReturnReciprocalTypedRateWithToggledInversion()
    {
        var rate = new ExchangeRate<USD, AUD>(2m, SampleDate, SampleProvider);

        ExchangeRate<AUD, USD> inverse = rate.Inverse();

        Assert.AreEqual("AUD", inverse.FromIsoCode);
        Assert.AreEqual("USD", inverse.ToIsoCode);
        Assert.AreEqual(0.5m, inverse.Rate);
        Assert.IsTrue(inverse.IsInverted);
    }

    /// <summary>
    /// Verifies that <see cref="ExchangeRate{TBase, TQuote}.ToRuntime" /> returns an equivalent runtime-tagged
    /// <see cref="ExchangeRate" />.
    /// </summary>
    [TestMethod]
    public void ToRuntime_WhenCalled_ShouldReturnEquivalentRuntimeRate()
    {
        var typed = new ExchangeRate<USD, AUD>(1.52m, SampleDate, SampleProvider);

        ExchangeRate runtime = typed.ToRuntime();

        Assert.AreEqual("USD", runtime.FromIsoCode);
        Assert.AreEqual("AUD", runtime.ToIsoCode);
        Assert.AreEqual(SampleDate, runtime.Date);
        Assert.AreEqual(1.52m, runtime.Rate);
        Assert.AreEqual(SampleProvider, runtime.Provider);
    }

    /// <summary>
    /// Verifies that <see cref="ExchangeRate{TBase, TQuote}.FromRuntime" /> adopts a runtime-tagged rate whose ISO
    /// codes match the type parameters.
    /// </summary>
    [TestMethod]
    public void FromRuntime_WhenIsoCodesMatch_ShouldReturnTypedRate()
    {
        var runtime = new ExchangeRate("USD", "AUD", SampleDate, 1.52m, SampleProvider);

        var typed = ExchangeRate<USD, AUD>.FromRuntime(runtime);

        Assert.AreEqual(1.52m, typed.Rate);
        Assert.AreEqual("USD", typed.FromIsoCode);
        Assert.AreEqual("AUD", typed.ToIsoCode);
    }

    /// <summary>
    /// Verifies that <see cref="ExchangeRate{TBase, TQuote}.FromRuntime" /> throws
    /// <see cref="InvalidOperationException" /> when the runtime rate's ISO codes differ from the type parameters.
    /// </summary>
    [TestMethod]
    public void FromRuntime_WhenIsoCodesDiffer_ShouldThrowInvalidOperationException()
    {
        var runtime = new ExchangeRate("EUR", "AUD", SampleDate, 1.52m, SampleProvider);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = ExchangeRate<USD, AUD>.FromRuntime(runtime);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ExchangeRate{TBase, TQuote}.Convert" /> multiplies the source amount by the rate and
    /// rounds to the destination currency's minor-unit precision.
    /// </summary>
    [TestMethod]
    public void Convert_WhenCalledOnTypedAmount_ShouldReturnTypedQuoteAmount()
    {
        var rate = new ExchangeRate<USD, AUD>(1.52m, SampleDate, SampleProvider);
        var amount = new Money<USD>(100m);

        Money<AUD> result = rate.Convert(amount);

        Assert.AreEqual(152.00m, result.Amount);
    }

    /// <summary>
    /// Verifies that <see cref="Money{TCurrency}.Convert{TQuote}(ExchangeRate{TCurrency, TQuote}, MidpointRounding)" />
    /// delegates to the typed rate's <c>Convert</c> and returns a value in the rate's quote currency.
    /// </summary>
    [TestMethod]
    public void MoneyOfTCurrencyConvert_WhenSuppliedTypedRate_ShouldReturnTypedQuoteAmount()
    {
        var amount = new Money<USD>(50m);
        var rate = new ExchangeRate<USD, AUD>(1.52m, SampleDate, SampleProvider);

        Money<AUD> result = amount.Convert(rate);

        Assert.AreEqual(76.00m, result.Amount);
    }
}
