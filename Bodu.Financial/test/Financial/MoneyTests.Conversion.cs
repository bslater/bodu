// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyTests.Conversion.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Numerics;
using Bodu.Financial.Currencies;

using Bodu.Numerics;

namespace Bodu.Financial;

public partial class MoneyTests
{
    /// <summary>
    /// Verifies that <see cref="Money{TCurrency}.Convert{TTarget}(decimal, MidpointRounding)" /> applies the
    /// exchange rate and rounds to the target currency's minor-unit precision.
    /// </summary>
    [TestMethod]
    public void Convert_WhenUsdToJpy_ShouldApplyRateAndRoundToTargetPrecision()
    {
        var usd = new Money<USD>(10m);

        Money<JPY> jpy = usd.Convert<JPY>(155.5m);

        Assert.AreEqual(new Money<JPY>(1555m), jpy);
    }

    /// <summary>
    /// Verifies that <see cref="Money{TCurrency}.Convert{TTarget}(decimal, MidpointRounding)" /> rejects a
    /// negative exchange rate.
    /// </summary>
    [TestMethod]
    public void Convert_WhenExchangeRateNegative_ShouldThrowArgumentOutOfRangeException()
    {
        var usd = new Money<USD>(10m);

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = usd.Convert<JPY>(-1m);
        });
    }

    /// <summary>
    /// Verifies that <see cref="Money{TCurrency}.Round(int, MidpointRounding)" /> coarsens the amount to the
    /// requested fractional-digit count.
    /// </summary>
    [TestMethod]
    public void Round_WhenLowerThanMinorUnits_ShouldCoarsenPrecision()
    {
        var money = new Money<USD>(19.99m);

        Money<USD> whole = money.Round(0);

        Assert.AreEqual(new Money<USD>(20m), whole);
    }

    /// <summary>
    /// Verifies that <see cref="Money{TCurrency}.Round(int, MidpointRounding)" /> rejects a fractional-digit count
    /// exceeding the currency's minor-unit precision.
    /// </summary>
    [TestMethod]
    public void Round_WhenDecimalsExceedsMinorUnits_ShouldThrowArgumentOutOfRangeException()
    {
        var money = new Money<USD>(19.99m);

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = money.Round(3);
        });
    }

    /// <summary>
    /// Verifies that <see cref="Money{TCurrency}.ToFraction" /> and
    /// <see cref="Money{TCurrency}.FromFraction(Fraction{BigInteger}, MidpointRounding)" /> round-trip exactly.
    /// </summary>
    [TestMethod]
    public void Fraction_WhenRoundTripped_ShouldPreserveValue()
    {
        var original = new Money<USD>(19.99m);

        var rational = original.ToFraction();
        var recovered = Money<USD>.FromFraction(rational);

        Assert.AreEqual(original, recovered);
    }

    /// <summary>
    /// Verifies that <see cref="Money{TCurrency}.MultiplyExact(Fraction{BigInteger}, MidpointRounding)" /> defers
    /// rounding until after the multiplication so the result is more accurate than naive scalar multiplication.
    /// </summary>
    [TestMethod]
    public void MultiplyExact_WhenAppliedToOneThird_ShouldRoundOnceAtEnd()
    {
        var dollar = new Money<USD>(1m);
        var third = Fraction<BigInteger>.Create(1, 3);

        Money<USD> result = dollar.MultiplyExact(third);

        // 1.00 * 1/3 = 0.3333... → banker's-round to 0.33
        Assert.AreEqual(new Money<USD>(0.33m), result);
    }
}
