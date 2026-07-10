// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyOfTCurrencyTests.ReassessmentDefects.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Numerics;
using Bodu.Financial.Currencies;

using Bodu.Financial.ExchangeRates;
using Bodu.Numerics;

namespace Bodu.Financial;

/// <summary>
/// Failing-first tests for the defects called out in the reassessment review. Each test reproduces the
/// reported bug before the production fix is applied so the regression is locked in.
/// </summary>
public partial class MoneyOfTCurrencyTests
{
    // ---------------------------------------------------------------------------------------------------------------
    // P0.1 — DivideRound applies directed modes only at midpoint; must apply at every non-zero remainder.
    // ---------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="MidpointRounding.ToZero" /> truncates toward zero at <i>any</i> non-zero
    /// remainder, not only at midpoints. <c>1.209 USD → 1.20</c>, not <c>1.21</c>.
    /// </summary>
    [TestMethod]
    public void FromFraction_WhenToZeroOnNonMidpointPositive_ShouldTruncateToZero()
    {
        var value = Fraction<BigInteger>.Create(1209, 1000);

        var result = Money<USD>.FromFraction(value, MidpointRounding.ToZero);

        Assert.AreEqual(new Money<USD>(1.20m), result);
    }

    /// <summary>
    /// Verifies that <see cref="MidpointRounding.ToZero" /> truncates toward zero for negative values too —
    /// <c>-1.209 USD → -1.20</c> (the integer nearer zero), not <c>-1.21</c>.
    /// </summary>
    [TestMethod]
    public void FromFraction_WhenToZeroOnNonMidpointNegative_ShouldTruncateToZero()
    {
        var value = Fraction<BigInteger>.Create(-1209, 1000);

        var result = Money<USD>.FromFraction(value, MidpointRounding.ToZero);

        Assert.AreEqual(new Money<USD>(-1.20m), result);
    }

    /// <summary>
    /// Verifies that <see cref="MidpointRounding.ToPositiveInfinity" /> rounds up at any positive remainder.
    /// <c>1.201 USD → 1.21</c>, not <c>1.20</c>.
    /// </summary>
    [TestMethod]
    public void FromFraction_WhenToPositiveInfinityOnNonMidpointPositive_ShouldRoundUp()
    {
        var value = Fraction<BigInteger>.Create(1201, 1000);

        var result = Money<USD>.FromFraction(value, MidpointRounding.ToPositiveInfinity);

        Assert.AreEqual(new Money<USD>(1.21m), result);
    }

    /// <summary>
    /// Verifies that <see cref="MidpointRounding.ToPositiveInfinity" /> rounds toward positive infinity for
    /// negative values — i.e. truncates toward zero. <c>-1.209 USD → -1.20</c>, not <c>-1.21</c>.
    /// </summary>
    [TestMethod]
    public void FromFraction_WhenToPositiveInfinityOnNegative_ShouldRoundTowardZero()
    {
        var value = Fraction<BigInteger>.Create(-1209, 1000);

        var result = Money<USD>.FromFraction(value, MidpointRounding.ToPositiveInfinity);

        Assert.AreEqual(new Money<USD>(-1.20m), result);
    }

    /// <summary>
    /// Verifies that <see cref="MidpointRounding.ToNegativeInfinity" /> rounds toward minus infinity at any
    /// non-zero positive remainder — i.e. truncates toward zero for positives. <c>1.209 USD → 1.20</c>.
    /// </summary>
    [TestMethod]
    public void FromFraction_WhenToNegativeInfinityOnPositive_ShouldRoundTowardZero()
    {
        var value = Fraction<BigInteger>.Create(1209, 1000);

        var result = Money<USD>.FromFraction(value, MidpointRounding.ToNegativeInfinity);

        Assert.AreEqual(new Money<USD>(1.20m), result);
    }

    /// <summary>
    /// Verifies that <see cref="MidpointRounding.ToNegativeInfinity" /> rounds toward more-negative at any
    /// non-zero negative remainder. <c>-1.201 USD → -1.21</c>, not <c>-1.20</c>.
    /// </summary>
    [TestMethod]
    public void FromFraction_WhenToNegativeInfinityOnNegative_ShouldRoundAwayToMoreNegative()
    {
        var value = Fraction<BigInteger>.Create(-1201, 1000);

        var result = Money<USD>.FromFraction(value, MidpointRounding.ToNegativeInfinity);

        Assert.AreEqual(new Money<USD>(-1.21m), result);
    }

    // ---------------------------------------------------------------------------------------------------------------
    // P1.3 — Invalid MidpointRounding enum values must throw, not fall back to ToEven.
    // ---------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that an invalid <see cref="MidpointRounding" /> value passed to
    /// <see cref="Money{TCurrency}.FromFraction" /> throws <see cref="ArgumentOutOfRangeException" /> rather
    /// than silently falling back to banker's rounding.
    /// </summary>
    [TestMethod]
    public void FromFraction_WhenMidpointRoundingValueIsUndefined_ShouldThrowArgumentOutOfRangeException()
    {
        var value = Fraction<BigInteger>.Create(1225, 1000);

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = Money<USD>.FromFraction(value, (MidpointRounding)999);
        });
    }

    // ---------------------------------------------------------------------------------------------------------------
    // P0.2 — Ratio allocation should use largest-remainder method, not first-positive-slot.
    // ---------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that ratio-based allocation assigns the residual to the slot with the largest fractional
    /// remainder, not to the first positive-ratio slot. The canonical example: <c>0.05</c> across
    /// <c>[1, 1, 100]</c> exact-shares to <c>[0.049, 0.049, 4.902]</c> cents → residual should go to the
    /// third slot (largest remainder), producing <c>[0.00, 0.00, 0.05]</c>, not <c>[0.01, 0.00, 0.04]</c>.
    /// </summary>
    [TestMethod]
    public void Allocate_WhenSmallAmountAcrossUnequalRatios_ShouldGiveResidualToLargestRemainder()
    {
        var revenue = new Money<USD>(0.05m);
        decimal[] ratios = [1m, 1m, 100m];

        Money<USD>[] shares = revenue.Allocate(ratios);

        Assert.AreEqual(new Money<USD>(0.00m), shares[0]);
        Assert.AreEqual(new Money<USD>(0.00m), shares[1]);
        Assert.AreEqual(new Money<USD>(0.05m), shares[2]);
    }

    /// <summary>
    /// Verifies the mirror case: when the largest ratio is at index zero, the residual goes there.
    /// </summary>
    [TestMethod]
    public void Allocate_WhenLargestRatioFirst_ShouldGiveResidualToFirstSlot()
    {
        var revenue = new Money<USD>(0.05m);
        decimal[] ratios = [100m, 1m, 1m];

        Money<USD>[] shares = revenue.Allocate(ratios);

        Assert.AreEqual(new Money<USD>(0.05m), shares[0]);
        Assert.AreEqual(new Money<USD>(0.00m), shares[1]);
        Assert.AreEqual(new Money<USD>(0.00m), shares[2]);
    }

    /// <summary>
    /// Verifies that ties in fractional remainder fall back to stable input order — the first slot among
    /// ties receives the residual.
    /// </summary>
    [TestMethod]
    public void Allocate_WhenFractionalRemaindersTie_ShouldUseStableInputOrder()
    {
        var revenue = new Money<USD>(0.02m);
        decimal[] ratios = [1m, 1m, 1m];

        Money<USD>[] shares = revenue.Allocate(ratios);

        Money<USD> sum = Money<USD>.Zero;
        foreach (Money<USD> share in shares)
            sum += share;

        Assert.AreEqual(revenue, sum);
        // All three exact shares are 2/3 cent → tied. Distribute the two residual cents to the first two
        // slots in stable order.
        Assert.AreEqual(new Money<USD>(0.01m), shares[0]);
        Assert.AreEqual(new Money<USD>(0.01m), shares[1]);
        Assert.AreEqual(new Money<USD>(0.00m), shares[2]);
    }

    /// <summary>
    /// Verifies that negative-amount ratio allocation mirrors the positive case via sign reapplication.
    /// </summary>
    [TestMethod]
    public void Allocate_WhenNegativeAmountAcrossUnequalRatios_ShouldMirrorPositiveLargestRemainder()
    {
        var loss = new Money<USD>(-0.05m);
        decimal[] ratios = [1m, 1m, 100m];

        Money<USD>[] shares = loss.Allocate(ratios);

        Assert.AreEqual(new Money<USD>(0m), shares[0]);
        Assert.AreEqual(new Money<USD>(0m), shares[1]);
        Assert.AreEqual(new Money<USD>(-0.05m), shares[2]);
    }

    // ---------------------------------------------------------------------------------------------------------------
    // P0.3 — MoneyBag.Balances must not expose the internal mutable dictionary.
    // ---------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that mutating the returned <see cref="MoneyBag.Balances" /> does not affect the bag's state —
    /// the property returns a snapshot or read-only wrapper, not the underlying mutable dictionary.
    /// </summary>
    [TestMethod]
    public void Balances_WhenCastToMutableAndMutated_ShouldNotAffectBagState()
    {
        var bag = new MoneyBag([new Money(100m, CurrencyCode.USD)]);

        IReadOnlyDictionary<CurrencyCode, decimal> balances = bag.Balances;

        // The reviewer's exact attack: cast to Dictionary<CurrencyCode, decimal> and mutate. Either the cast must
        // fail (an ICollection wrapper) or the mutation must not propagate (a snapshot copy).
        if (balances is Dictionary<CurrencyCode, decimal> exposed)
        {
            decimal originalUsd = bag.Balances[CurrencyCode.USD];
            try
            {
                exposed[CurrencyCode.USD] = -999_999_999m;
                exposed[CurrencyCode.AUD] = 123.456789m;
            }
            catch (NotSupportedException)
            {
                return;     // wrapper threw on mutation — acceptable.
            }

            Assert.AreEqual(originalUsd, bag.Balances[CurrencyCode.USD], "Bag state must not be mutable through Balances cast.");
            Assert.IsFalse(bag.Balances.ContainsKey(CurrencyCode.AUD), "Bag must not pick up keys mutated through Balances cast.");
        }
    }

    // ---------------------------------------------------------------------------------------------------------------
    // P0.5 — MoneyBag.ConvertTo must reject zero/negative rates from providers and delegates.
    // ---------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that a provider returning a zero exchange rate causes
    /// <see cref="MoneyBag.ConvertTo{TTarget}(IRateProvider)" /> to fail loudly rather than silently
    /// producing a wrong total.
    /// </summary>
    [TestMethod]
    public void ConvertTo_WhenProviderReturnsZeroRate_ShouldThrowInvalidOperationException()
    {
        MoneyBag bag = MoneyBag.Empty.Add(new Money<EUR>(100m));
        IRateProvider rates = new ConstantRateProvider(0m);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = bag.ConvertTo<USD>(rates);
        });
    }

    /// <summary>
    /// Verifies that a provider returning a negative exchange rate also throws.
    /// </summary>
    [TestMethod]
    public void ConvertTo_WhenProviderReturnsNegativeRate_ShouldThrowInvalidOperationException()
    {
        MoneyBag bag = MoneyBag.Empty.Add(new Money<EUR>(100m));
        IRateProvider rates = new ConstantRateProvider(-1.10m);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = bag.ConvertTo<USD>(rates);
        });
    }

    /// <summary>
    /// A minimal <see cref="IRateProvider" /> that returns a fixed rate for every pair, used to drive the
    /// bag's defensive zero/negative-rate check independently of <see cref="FixedRateTable" />.
    /// </summary>
    private sealed class ConstantRateProvider
        : IRateProvider
    {
        /// <summary>
        /// The fixed rate this provider returns from every lookup.
        /// </summary>
        private readonly decimal _rate;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConstantRateProvider" /> class.
        /// </summary>
        /// <param name="rate">The fixed rate to return.</param>
        public ConstantRateProvider(decimal rate)
        {
            _rate = rate;
        }

        /// <inheritdoc />
        public decimal GetRate(string fromIsoCode, string toIsoCode) => _rate;
    }

    /// <summary>
    /// Verifies that the delegate-based overload validates rates the same way.
    /// </summary>
    [TestMethod]
    public void ConvertTo_WhenDelegateReturnsZeroRate_ShouldThrowInvalidOperationException()
    {
        MoneyBag bag = MoneyBag.Empty.Add(new Money<EUR>(100m));

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = bag.ConvertTo<USD>((from, to) => 0m);
        });
    }

    /// <summary>
    /// Verifies the negative case for the delegate overload.
    /// </summary>
    [TestMethod]
    public void ConvertTo_WhenDelegateReturnsNegativeRate_ShouldThrowInvalidOperationException()
    {
        MoneyBag bag = MoneyBag.Empty.Add(new Money<EUR>(100m));

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = bag.ConvertTo<USD>((from, to) => -0.5m);
        });
    }

    // ---------------------------------------------------------------------------------------------------------------
    // P0.6 — Money must reject the currency-less and undefined CurrencyCode values.
    // ---------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="Money" />'s constructor rejects the currency-less <see cref="CurrencyCode.None" />
    /// with <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void MoneyConstructor_WhenCodeIsNone_ShouldThrowArgumentOutOfRangeException()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new Money(10m, CurrencyCode.None);
        });
    }

    /// <summary>
    /// Verifies that <see cref="Money" />'s constructor rejects an undefined <see cref="CurrencyCode" /> with
    /// <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void MoneyConstructor_WhenCodeUndefined_ShouldThrowArgumentOutOfRangeException()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new Money(10m, (CurrencyCode)9999);
        });
    }

    // ---------------------------------------------------------------------------------------------------------------
    // P1.5 — MoneyBag constructor must reject (not silently drop) Money entries with an empty ISO.
    // ---------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that a default <see cref="Money" /> (empty ISO) supplied to <see cref="MoneyBag" />'s
    /// constructor causes <see cref="ArgumentException" /> rather than being silently dropped.
    /// </summary>
    [TestMethod]
    public void MoneyBagConstructor_WhenBalanceCarriesEmptyIsoCode_ShouldThrowArgumentException()
    {
        Money[] entries = [new Money(100m, CurrencyCode.USD), default];

        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = new MoneyBag(entries);
        });
    }

    // ---------------------------------------------------------------------------------------------------------------
    // P1.6 — Default Money arithmetic must reject (both operands have empty ISO).
    // ---------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that arithmetic between two default <see cref="Money" /> instances throws — empty-ISO
    /// values must not produce more empty-ISO values.
    /// </summary>
    [TestMethod]
    public void MoneyArithmetic_WhenBothOperandsAreDefault_ShouldThrowInvalidOperationException()
    {
        Money a = default;
        Money b = default;

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = a + b;
        });
    }

    /// <summary>
    /// Verifies that comparison between two default <see cref="Money" /> instances also throws.
    /// </summary>
    [TestMethod]
    public void MoneyComparison_WhenBothOperandsAreDefault_ShouldThrowInvalidOperationException()
    {
        Money a = default;
        Money b = default;

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = a < b;
        });
    }

    /// <summary>
    /// Verifies that a default <see cref="Money" /> combined with a real one throws.
    /// </summary>
    [TestMethod]
    public void MoneyArithmetic_WhenOneOperandIsDefault_ShouldThrowInvalidOperationException()
    {
        var real = new Money(100m, CurrencyCode.USD);
        Money empty = default;

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = real + empty;
        });
    }

    // ---------------------------------------------------------------------------------------------------------------
    // P0.4 — MoneyBag.ConvertTo rounding-policy explicit overload.
    // ---------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that the documented default (round once at end after raw aggregation) and the per-line policy
    /// produce different results on the canonical case where two converted balances individually round to zero
    /// but their sum rounds to one cent.
    /// </summary>
    [TestMethod]
    public void ConvertTo_WhenTwoSubMinorUnitLinesAggregate_ShouldDifferByRoundingPolicy()
    {
        // Use two three-minor-unit currencies (BHD, KWD) so Money natively carries the sub-cent source precision
        // instead of rounding it away.
        MoneyBag bag = MoneyBag.Empty
            .Add(new Money(0.005m, CurrencyCode.BHD))
            .Add(new Money(0.005m, CurrencyCode.KWD));

        FixedRateTable rates = new(new Dictionary<(string From, string To), decimal>
        {
            { ("BHD", "USD"), 1m },
            { ("KWD", "USD"), 1m },
        });

        Money<USD> defaultPolicy = bag.ConvertTo<USD>(rates);
        Money<USD> perLine = bag.ConvertTo<USD>(rates, MoneyBagConversionRoundingPolicy.RoundEachCurrencyThenSum);

        // 0.005 + 0.005 = 0.010 → 0.01 USD under sum-raw-then-round (current default).
        // 0.005 → 0.00, 0.005 → 0.00 → 0.00 + 0.00 = 0.00 USD under round-each-then-sum.
        Assert.AreEqual(new Money<USD>(0.01m), defaultPolicy);
        Assert.AreEqual(new Money<USD>(0m), perLine);
    }
}
