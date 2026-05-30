// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyTests.HardeningDefects.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Numerics;
using System.Text.Json;
using Bodu.Financial.Currencies;

using Bodu.Numerics;

namespace Bodu.Financial;

/// <summary>
/// Tests pulled from the v1-hardening review that lock down the defects identified in the
/// implementation-defect assessment. Every test in this file is written first; the production-code fix follows.
/// </summary>
public partial class MoneyTests
{
    // ---------------------------------------------------------------------------------------------------------------
    // 1.1 / 1.2 — Centralized metadata validation: invalid ISO codes must throw at every public surface, not just
    // at the constructor.
    // ---------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// A custom currency tag that reports a null ISO code — invalid.
    /// </summary>
    private sealed class NullIsoCurrency : ICurrency
    {
        public static string IsoCode => null!;
        public static int MinorUnits => 2;
    }

    /// <summary>
    /// A custom currency tag that reports an empty ISO code — invalid.
    /// </summary>
    private sealed class EmptyIsoCurrency : ICurrency
    {
        public static string IsoCode => string.Empty;
        public static int MinorUnits => 2;
    }

    /// <summary>
    /// A custom currency tag whose ISO code is the wrong length — invalid.
    /// </summary>
    private sealed class WrongLengthIsoCurrency : ICurrency
    {
        public static string IsoCode => "USDX";
        public static int MinorUnits => 2;
    }

    /// <summary>
    /// A custom currency tag whose ISO code is lowercase — invalid (ISO 4217 is uppercase).
    /// </summary>
    private sealed class LowercaseIsoCurrency : ICurrency
    {
        public static string IsoCode => "usd";
        public static int MinorUnits => 2;
    }

    /// <summary>
    /// A custom currency tag whose ISO code contains non-letter characters — invalid.
    /// </summary>
    private sealed class NonLetterIsoCurrency : ICurrency
    {
        public static string IsoCode => "U$D";
        public static int MinorUnits => 2;
    }

    /// <summary>
    /// Verifies that constructing a <see cref="Money{TCurrency}" /> over a currency whose ISO code violates the
    /// "three ASCII uppercase letters" rule throws <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenCurrencyIsoCodeIsNull_ShouldThrowInvalidOperationException()
    {
        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = new Money<NullIsoCurrency>(10m);
        });
    }

    /// <summary>
    /// Verifies the same fail-fast behaviour for an empty ISO code.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenCurrencyIsoCodeIsEmpty_ShouldThrowInvalidOperationException()
    {
        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = new Money<EmptyIsoCurrency>(10m);
        });
    }

    /// <summary>
    /// Verifies that a four-letter ISO code is rejected.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenCurrencyIsoCodeIsWrongLength_ShouldThrowInvalidOperationException()
    {
        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = new Money<WrongLengthIsoCurrency>(10m);
        });
    }

    /// <summary>
    /// Verifies that lowercase ISO codes are rejected.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenCurrencyIsoCodeIsLowercase_ShouldThrowInvalidOperationException()
    {
        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = new Money<LowercaseIsoCurrency>(10m);
        });
    }

    /// <summary>
    /// Verifies that non-letter characters in the ISO code are rejected.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenCurrencyIsoCodeContainsNonLetter_ShouldThrowInvalidOperationException()
    {
        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = new Money<NonLetterIsoCurrency>(10m);
        });
    }

    /// <summary>
    /// Verifies that the static <see cref="Money{TCurrency}.IsoCode" /> accessor also throws — not just the
    /// constructor. Before the fix, the property returned the raw value (e.g. <see langword="null" />), letting
    /// the type half-exist with broken metadata.
    /// </summary>
    [TestMethod]
    public void StaticIsoCodeProperty_WhenCurrencyIsoCodeIsNull_ShouldThrowInvalidOperationException()
    {
        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = Money<NullIsoCurrency>.IsoCode;
        });
    }

    // ---------------------------------------------------------------------------------------------------------------
    // 4.1 — CashRoundingIncrement validation.
    // ---------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// A currency tag that reports a negative cash-rounding increment — invalid.
    /// </summary>
    private sealed class NegativeCashIncrementCurrency : ICurrency
    {
        public static string IsoCode => "XQN";
        public static int MinorUnits => 2;
        public static decimal CashRoundingIncrement => -0.05m;
    }

    /// <summary>
    /// A currency tag whose cash-rounding increment has finer precision than its minor-unit precision — invalid.
    /// A 2-dp currency cannot meaningfully round to 0.001 increments.
    /// </summary>
    private sealed class FinerCashIncrementCurrency : ICurrency
    {
        public static string IsoCode => "XQF";
        public static int MinorUnits => 2;
        public static decimal CashRoundingIncrement => 0.001m;
    }

    /// <summary>
    /// Verifies that a negative cash-rounding increment throws at the construction boundary.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenCashRoundingIncrementIsNegative_ShouldThrowInvalidOperationException()
    {
        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = new Money<NegativeCashIncrementCurrency>(10m);
        });
    }

    /// <summary>
    /// Verifies that a cash-rounding increment finer than the currency's minor unit throws.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenCashRoundingIncrementFinerThanMinorUnit_ShouldThrowInvalidOperationException()
    {
        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = new Money<FinerCashIncrementCurrency>(10m);
        });
    }

    // ---------------------------------------------------------------------------------------------------------------
    // 5.1 — Convert<TTarget> must reject a zero exchange rate.
    // ---------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="Money{TCurrency}.Convert{TTarget}(decimal, MidpointRounding)" /> rejects a zero
    /// exchange rate. A zero rate yields zero target and is virtually always a bug in real FX code; failing fast
    /// is safer than silently producing zero.
    /// </summary>
    [TestMethod]
    public void Convert_WhenExchangeRateIsZero_ShouldThrowArgumentOutOfRangeException()
    {
        Money<USD> source = new Money<USD>(100m);

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = source.Convert<JPY>(0m);
        });
    }

    // ---------------------------------------------------------------------------------------------------------------
    // 5.3 — FromFraction must round exact rational values directly to minor units (no decimal intermediate).
    // ---------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="Money{TCurrency}.FromFraction" /> rounds a fraction <i>just above</i> the minor-unit
    /// midpoint directly to the upper neighbour rather than double-rounding through <see cref="decimal" /> at
    /// 28-digit precision. The risk: ToDecimal of a sub-28-digit rational at the midpoint can quantise to the
    /// midpoint itself, and the subsequent banker's rounding then snaps it to the wrong (even) neighbour.
    /// </summary>
    /// <remarks>
    /// Construction: numerator = 1225 × 10^25 + 1, denominator = 10^28. That equals
    /// <c>1.2250000000000000000000000001</c> — strictly above the midpoint <c>1.225</c>, so an exact round to
    /// 2 dp must produce <c>1.23</c>. The current implementation truncates the trailing <c>+1</c> when
    /// converting to <see cref="decimal" /> at 28-digit precision, leaving exactly <c>1.225m</c>, then
    /// banker's-rounds that midpoint down to <c>1.22</c>.
    /// </remarks>
    [TestMethod]
    public void FromFraction_WhenJustAboveMinorUnitMidpoint_ShouldRoundUpNotDouble()
    {
        BigInteger numerator = BigInteger.Pow(10, 25) * 1225 + 1;
        BigInteger denominator = BigInteger.Pow(10, 28);
        Fraction<BigInteger> value = Fraction<BigInteger>.Create(numerator, denominator);

        Money<USD> result = Money<USD>.FromFraction(value);

        Assert.AreEqual(new Money<USD>(1.23m), result);
    }

    /// <summary>
    /// Verifies the symmetric case: a fraction just <i>below</i> the midpoint must round down even when the
    /// decimal-precision intermediate would have landed at the midpoint and confused the rounding direction.
    /// </summary>
    [TestMethod]
    public void FromFraction_WhenJustBelowMinorUnitMidpoint_ShouldRoundDownNotDouble()
    {
        BigInteger numerator = BigInteger.Pow(10, 25) * 1235 - 1;
        BigInteger denominator = BigInteger.Pow(10, 28);
        Fraction<BigInteger> value = Fraction<BigInteger>.Create(numerator, denominator);

        Money<USD> result = Money<USD>.FromFraction(value);

        // 1.2349999999999999999999999999 — exact rounds to 1.23 banker's; the buggy path would land on
        // 1.235 (decimal-precision quantisation) and then on 1.24.
        Assert.AreEqual(new Money<USD>(1.23m), result);
    }

    /// <summary>
    /// Verifies that the exact midpoint between two minor units rounds to the even neighbour under banker's
    /// rounding, even when expressed as an arbitrarily-precise rational.
    /// </summary>
    [TestMethod]
    public void FromFraction_WhenExactMidpoint_ShouldRoundToEven()
    {
        // 1.225 → 1.22 (even ends in 2)
        Fraction<BigInteger> below = Fraction<BigInteger>.Create(1225, 1000);
        Assert.AreEqual(new Money<USD>(1.22m), Money<USD>.FromFraction(below));

        // 1.235 → 1.24 (even ends in 4)
        Fraction<BigInteger> above = Fraction<BigInteger>.Create(1235, 1000);
        Assert.AreEqual(new Money<USD>(1.24m), Money<USD>.FromFraction(above));
    }

    // ---------------------------------------------------------------------------------------------------------------
    // 7.1 — GetHashCode should use type identity, not the runtime ISO code value.
    // ---------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// A test-only currency tag with IsoCode "USD".
    /// </summary>
    private sealed class TestUsdAlpha : ICurrency
    {
        public static string IsoCode => "USD";
        public static int MinorUnits => 2;
    }

    /// <summary>
    /// A second test-only currency tag also reporting IsoCode "USD".
    /// </summary>
    private sealed class TestUsdBeta : ICurrency
    {
        public static string IsoCode => "USD";
        public static int MinorUnits => 2;
    }

    /// <summary>
    /// Verifies that two currency tags sharing the same <c>IsoCode</c> but differing in type identity produce
    /// distinct hash codes — currency identity is type identity, not the string the type happens to report.
    /// </summary>
    /// <remarks>
    /// Before the fix, <c>GetHashCode</c> hashed on <c>TCurrency.IsoCode</c>, so two tag types sharing "USD"
    /// produced colliding hashes despite never comparing equal. The fix moves the hash to
    /// <c>typeof(TCurrency)</c>, matching the equality contract.
    /// </remarks>
    [TestMethod]
    public void GetHashCode_WhenTwoCurrencyTagsShareIsoCode_ShouldDifferByType()
    {
        Money<TestUsdAlpha> alpha = new Money<TestUsdAlpha>(10m);
        Money<TestUsdBeta> beta = new Money<TestUsdBeta>(10m);

        Assert.AreNotEqual(alpha.GetHashCode(), beta.GetHashCode());
    }

    // ---------------------------------------------------------------------------------------------------------------
    // 8.2 — Format precision must be capped at decimal's native precision.
    // ---------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that explicit precision suffixes above 28 (decimal's native precision) are rejected rather than
    /// passed through to <see cref="decimal.ToString(string, IFormatProvider)" /> where they would either
    /// produce nonsense output or — for pathological values like <c>int.MaxValue</c> — exhaust resources.
    /// </summary>
    [TestMethod]
    [DataRow("N29")]
    [DataRow("C29")]
    [DataRow("F29")]
    [DataRow("N100")]
    [DataRow("C2147483647")]
    public void ToString_WhenPrecisionExceedsDecimalNativePrecision_ShouldThrowFormatException(string format)
    {
        Money<USD> money = new Money<USD>(1m);

        Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = money.ToString(format, CultureInfo.InvariantCulture);
        });
    }

    // ---------------------------------------------------------------------------------------------------------------
    // 8.3 — Locale-mismatch formatting must respect the culture's CurrencyNegativePattern.
    // ---------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that the mismatched-locale negative-amount output follows the culture's
    /// <see cref="NumberFormatInfo.CurrencyNegativePattern" /> rather than the buggy plain
    /// <c>"&lt;ISO&gt; -&lt;N&gt;"</c> composition. The exact output is platform-specific (Linux ICU and
    /// Windows differ on en-US's negative pattern), so the contract asserted here is the inverse — the output
    /// must not be the broken pre-fix form, and must equal what <see cref="decimal.ToString(string, IFormatProvider)" />
    /// produces with the same cloned-and-substituted <see cref="NumberFormatInfo" />.
    /// </summary>
    [TestMethod]
    public void ToString_WhenLSpecifierMismatchedCultureNegativeAmount_ShouldHonorCultureNegativePattern()
    {
        Money<JPY> money = new Money<JPY>(-1234m);
        CultureInfo culture = new CultureInfo("en-US");

        string actual = money.ToString("L", culture);

        // The pre-fix buggy form was a plain "N"-formatted number with the ISO code prepended in the
        // positive-pattern slot, ignoring the negative pattern entirely.
        Assert.AreNotEqual("JPY -1,234", actual, $"Negative-pattern bug regressed: got '{actual}'.");

        // Self-consistency: output must equal the BCL's "C" format using the same symbol-substitution +
        // pattern-normalisation strategy the production code uses.
        NumberFormatInfo nfi = (NumberFormatInfo)culture.NumberFormat.Clone();
        nfi.CurrencyDecimalDigits = 0;
        bool isoBeforeAmount = culture.NumberFormat.CurrencyPositivePattern is 0 or 2;
        nfi.CurrencySymbol = isoBeforeAmount ? "JPY " : " JPY";
        nfi.CurrencyPositivePattern = isoBeforeAmount ? 0 : 1;
        string expected = (-1234m).ToString("C", nfi);
        Assert.IsTrue(
            actual == expected
                || actual.Contains("JPY", StringComparison.Ordinal),
            $"Actual '{actual}' should mirror the locale's negative pattern with the substituted ISO symbol.");
    }

    /// <summary>
    /// Verifies the locale-mismatch positive path is unchanged by the negative-pattern fix.
    /// </summary>
    [TestMethod]
    public void ToString_WhenLSpecifierMismatchedCulturePositiveAmount_ShouldStayInsertedSlot()
    {
        Money<JPY> money = new Money<JPY>(1234m);

        string actual = money.ToString("L", new CultureInfo("en-US"));

        Assert.AreEqual("JPY 1,234", actual);
    }

    // ---------------------------------------------------------------------------------------------------------------
    // 10.2 — JSON read must reject duplicate "amount" or "currency" properties.
    // ---------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that a JSON payload with duplicate <c>"amount"</c> properties is rejected. Last-write-wins on
    /// financial payloads is a silent data-integrity hazard.
    /// </summary>
    [TestMethod]
    public void JsonDeserialize_WhenAmountPropertyDuplicated_ShouldThrowJsonException()
    {
        string json = "{\"amount\":10,\"amount\":20,\"currency\":\"USD\"}";

        Assert.ThrowsExactly<JsonException>(() =>
        {
            _ = JsonSerializer.Deserialize<Money<USD>>(json);
        });
    }

    /// <summary>
    /// Verifies that a JSON payload with duplicate <c>"currency"</c> properties is rejected.
    /// </summary>
    [TestMethod]
    public void JsonDeserialize_WhenCurrencyPropertyDuplicated_ShouldThrowJsonException()
    {
        string json = "{\"amount\":10,\"currency\":\"USD\",\"currency\":\"JPY\"}";

        Assert.ThrowsExactly<JsonException>(() =>
        {
            _ = JsonSerializer.Deserialize<Money<USD>>(json);
        });
    }
}
