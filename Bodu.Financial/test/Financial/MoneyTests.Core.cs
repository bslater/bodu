// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyTests.Core.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json;
using Bodu.Financial.Currencies;

namespace Bodu.Financial;

public partial class MoneyTests
{
    // ---------------------------------------------------------------------------------------------------------------
    // Construction
    // ---------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that the constructor rounds the amount to the currency's minor-unit precision.
    /// </summary>
    [TestMethod]
    [DataRow("USD", 1.235, 1.24)]
    [DataRow("JPY", 99.6, 100.0)]
    [DataRow("BHD", 12.3456, 12.346)]
    public void Constructor_WhenAmountHasExcessPrecision_ShouldRoundToCurrencyMinorUnits(string iso, double amount, double expected)
    {
        var money = new Money((decimal)amount, iso);

        Assert.AreEqual((decimal)expected, money.Amount);
        Assert.AreEqual(iso, money.IsoCode);
    }

    /// <summary>
    /// Verifies that the constructor with explicit rounding rule honors the supplied rule.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenAwayFromZeroRequested_ShouldRoundMidpointAwayFromZero()
    {
        var banker = new Money(1.225m, "USD");
        var awayFromZero = new Money(1.225m, "USD", MidpointRounding.AwayFromZero);

        Assert.AreEqual(1.22m, banker.Amount);
        Assert.AreEqual(1.23m, awayFromZero.Amount);
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> ISO code throws.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenIsoCodeIsNull_ShouldThrowArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new Money(10m, null!);
        });
    }

    /// <summary>
    /// Verifies that an empty/whitespace ISO code throws.
    /// </summary>
    [TestMethod]
    [DataRow("")]
    [DataRow("   ")]
    public void Constructor_WhenIsoCodeIsEmptyOrWhitespace_ShouldThrowArgumentException(string iso)
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = new Money(10m, iso);
        });
    }

    /// <summary>
    /// Verifies that the default value yields an empty ISO code and a zero amount.
    /// </summary>
    [TestMethod]
    public void DefaultValue_WhenInspected_ShouldHaveEmptyIsoCodeAndZeroAmount()
    {
        Money zero = default;

        Assert.AreEqual(string.Empty, zero.IsoCode);
        Assert.AreEqual(0m, zero.Amount);
        Assert.IsTrue(zero.IsZero);
    }

    // ---------------------------------------------------------------------------------------------------------------
    // Properties
    // ---------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="Money.MinorUnits" /> reads from the registry for known currencies.
    /// </summary>
    [TestMethod]
    [DataRow("USD", 2)]
    [DataRow("JPY", 0)]
    [DataRow("BHD", 3)]
    public void MinorUnits_WhenInspected_ShouldMatchRegistry(string iso, int expected)
    {
        var money = new Money(0m, iso);

        Assert.AreEqual(expected, money.MinorUnits);
    }

    /// <summary>
    /// Verifies that sign-bit properties classify the amount correctly.
    /// </summary>
    [TestMethod]
    [DataRow(0.0, false, false, true)]
    [DataRow(0.01, true, false, false)]
    [DataRow(-0.01, false, true, false)]
    public void SignProperties_WhenInspected_ShouldClassifyAmount(double amount, bool expectedPositive, bool expectedNegative, bool expectedZero)
    {
        var money = new Money((decimal)amount, "USD");

        Assert.AreEqual(expectedPositive, money.IsPositive);
        Assert.AreEqual(expectedNegative, money.IsNegative);
        Assert.AreEqual(expectedZero, money.IsZero);
    }

    /// <summary>
    /// Verifies that <c>Abs</c> returns the unsigned amount.
    /// </summary>
    [TestMethod]
    public void Abs_WhenNegative_ShouldReturnUnsignedAmount()
    {
        var negative = new Money(-19.99m, "USD");

        Money abs = negative.Abs;

        Assert.AreEqual(19.99m, abs.Amount);
        Assert.AreEqual("USD", abs.IsoCode);
    }

    /// <summary>
    /// Verifies that <see cref="Money.Zero(string)" /> returns a zero amount in the specified currency.
    /// </summary>
    [TestMethod]
    public void Zero_WhenCalled_ShouldReturnZeroAmountInCurrency()
    {
        var zero = Money.Zero("EUR");

        Assert.IsTrue(zero.IsZero);
        Assert.AreEqual("EUR", zero.IsoCode);
    }

    // ---------------------------------------------------------------------------------------------------------------
    // Same-currency arithmetic
    // ---------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies same-currency addition.
    /// </summary>
    [TestMethod]
    public void Addition_WhenSameCurrency_ShouldReturnSum()
    {
        var a = new Money(10.50m, "USD");
        var b = new Money(5.25m, "USD");

        Money sum = a + b;

        Assert.AreEqual(new Money(15.75m, "USD"), sum);
    }

    /// <summary>
    /// Verifies that addition across currencies throws <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void Addition_WhenDifferentCurrencies_ShouldThrowInvalidOperationException()
    {
        var usd = new Money(10m, "USD");
        var eur = new Money(10m, "EUR");

        InvalidOperationException ex = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = usd + eur;
        });

        Assert.IsTrue(ex.Message.Contains("USD", StringComparison.Ordinal));
        Assert.IsTrue(ex.Message.Contains("EUR", StringComparison.Ordinal));
    }

    /// <summary>
    /// Verifies that subtraction across currencies throws.
    /// </summary>
    [TestMethod]
    public void Subtraction_WhenDifferentCurrencies_ShouldThrowInvalidOperationException()
    {
        var usd = new Money(10m, "USD");
        var eur = new Money(10m, "EUR");

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = usd - eur;
        });
    }

    /// <summary>
    /// Verifies scalar multiplication rounds at the currency's precision.
    /// </summary>
    [TestMethod]
    public void Multiplication_WhenScalar_ShouldRoundToCurrencyMinorUnits()
    {
        var price = new Money(0.95m, "USD");

        Money result = price * 3m;

        Assert.AreEqual(new Money(2.85m, "USD"), result);
    }

    /// <summary>
    /// Verifies scalar division.
    /// </summary>
    [TestMethod]
    public void Division_WhenScalar_ShouldRoundToCurrencyMinorUnits()
    {
        var total = new Money(10m, "USD");

        Money share = total / 3m;

        Assert.AreEqual(new Money(3.33m, "USD"), share);
    }

    /// <summary>
    /// Verifies the dimensionless ratio between two same-currency amounts.
    /// </summary>
    [TestMethod]
    public void DivisionMoneyByMoney_WhenSameCurrency_ShouldReturnDecimalRatio()
    {
        var ten = new Money(10m, "USD");
        var four = new Money(4m, "USD");

        Assert.AreEqual(2.5m, ten / four);
    }

    /// <summary>
    /// Verifies that ratio across currencies throws.
    /// </summary>
    [TestMethod]
    public void DivisionMoneyByMoney_WhenDifferentCurrencies_ShouldThrowInvalidOperationException()
    {
        var usd = new Money(10m, "USD");
        var eur = new Money(10m, "EUR");

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = usd / eur;
        });
    }

    /// <summary>
    /// Verifies that comparison across currencies throws.
    /// </summary>
    [TestMethod]
    public void Comparison_WhenDifferentCurrencies_ShouldThrowInvalidOperationException()
    {
        var usd = new Money(10m, "USD");
        var eur = new Money(10m, "EUR");

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = usd < eur;
        });
    }

    // ---------------------------------------------------------------------------------------------------------------
    // Equality and comparison
    // ---------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that equality checks both currency and amount.
    /// </summary>
    [TestMethod]
    public void Equals_WhenSameCurrencyAndAmount_ShouldReturnTrue()
    {
        var a = new Money(19.99m, "USD");
        var b = new Money(19.99m, "USD");

        Assert.IsTrue(a.Equals(b));
        Assert.IsTrue(a == b);
    }

    /// <summary>
    /// Verifies that equality across currencies returns false rather than throwing.
    /// </summary>
    [TestMethod]
    public void Equals_WhenDifferentCurrencies_ShouldReturnFalse()
    {
        var usd = new Money(10m, "USD");
        var eur = new Money(10m, "EUR");

        Assert.IsFalse(usd.Equals(eur));
        Assert.IsFalse(usd == eur);
    }

    /// <summary>
    /// Verifies that hash codes differ across currencies even with identical amounts.
    /// </summary>
    [TestMethod]
    public void GetHashCode_WhenDifferentCurrencies_ShouldDiffer()
    {
        int usd = new Money(10m, "USD").GetHashCode();
        int eur = new Money(10m, "EUR").GetHashCode();

        Assert.AreNotEqual(usd, eur);
    }

    /// <summary>
    /// Verifies <see cref="IComparable{T}.CompareTo(T)" /> within a currency.
    /// </summary>
    [TestMethod]
    public void CompareTo_WhenSameCurrency_ShouldOrderByAmount()
    {
        var small = new Money(1m, "USD");
        var large = new Money(2m, "USD");

        Assert.IsTrue(small.CompareTo(large) < 0);
        Assert.IsTrue(large.CompareTo(small) > 0);
        Assert.AreEqual(0, small.CompareTo(new Money(1m, "USD")));
    }

    /// <summary>
    /// Verifies that <see cref="IComparable.CompareTo(object)" /> throws on cross-currency comparison.
    /// </summary>
    [TestMethod]
    public void CompareTo_WhenDifferentCurrency_ShouldThrowInvalidOperationException()
    {
        var usd = new Money(10m, "USD");
        var eur = new Money(10m, "EUR");

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = usd.CompareTo(eur);
        });
    }

    // ---------------------------------------------------------------------------------------------------------------
    // Conversion to/from typed Money
    // ---------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="Money.As{TCurrency}" /> succeeds when the runtime currency matches.
    /// </summary>
    [TestMethod]
    public void As_WhenCurrencyMatches_ShouldReturnTypedMoney()
    {
        var runtime = new Money(19.99m, "USD");

        Money<USD> typed = runtime.As<USD>();

        Assert.AreEqual(new Money<USD>(19.99m), typed);
    }

    /// <summary>
    /// Verifies that <see cref="Money.As{TCurrency}" /> throws when the runtime currency mismatches.
    /// </summary>
    [TestMethod]
    public void As_WhenCurrencyMismatches_ShouldThrowInvalidOperationException()
    {
        var runtime = new Money(19.99m, "EUR");

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = runtime.As<USD>();
        });
    }

    /// <summary>
    /// Verifies that <see cref="Money.TryAs{TCurrency}" /> returns false on mismatch without throwing.
    /// </summary>
    [TestMethod]
    public void TryAs_WhenCurrencyMismatches_ShouldReturnFalse()
    {
        var runtime = new Money(19.99m, "EUR");

        bool ok = runtime.TryAs(out Money<USD> result);

        Assert.IsFalse(ok);
        Assert.AreEqual(default, result);
    }

    /// <summary>
    /// Verifies that <see cref="Money{TCurrency}.ToMoney" /> round-trips a typed value to its runtime equivalent.
    /// </summary>
    [TestMethod]
    public void ToMoney_WhenCalled_ShouldYieldEquivalentRuntimeValue()
    {
        var typed = new Money<USD>(19.99m);

        var runtime = typed.ToMoney();

        Assert.AreEqual("USD", runtime.IsoCode);
        Assert.AreEqual(19.99m, runtime.Amount);
    }

    /// <summary>
    /// Verifies cross-currency conversion via runtime ISO code.
    /// </summary>
    [TestMethod]
    public void Convert_WhenRuntimeIsoTarget_ShouldRoundAtTargetCurrencyPrecision()
    {
        var usd = new Money(100m, "USD");

        Money jpy = usd.Convert("JPY", 155.5m);

        Assert.AreEqual("JPY", jpy.IsoCode);
        Assert.AreEqual(15550m, jpy.Amount);
    }

    /// <summary>
    /// Verifies cross-currency conversion to a typed target.
    /// </summary>
    [TestMethod]
    public void Convert_WhenTypedTarget_ShouldReturnTypedMoney()
    {
        var usd = new Money(100m, "USD");

        Money<JPY> jpy = usd.Convert<JPY>(155.5m);

        Assert.AreEqual(new Money<JPY>(15550m), jpy);
    }

    /// <summary>
    /// Verifies that a negative exchange rate throws.
    /// </summary>
    [TestMethod]
    public void Convert_WhenRateNegative_ShouldThrowArgumentOutOfRangeException()
    {
        var usd = new Money(100m, "USD");

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = usd.Convert("JPY", -1m);
        });
    }

    // ---------------------------------------------------------------------------------------------------------------
    // Allocation
    // ---------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies equal-parts allocation with residual distribution.
    /// </summary>
    [TestMethod]
    public void Allocate_WhenTenCentsIntoThree_ShouldDistributeResidualToFirstShare()
    {
        Money[] shares = new Money(0.10m, "USD").Allocate(3);

        Assert.AreEqual(3, shares.Length);
        Assert.AreEqual(new Money(0.04m, "USD"), shares[0]);
        Assert.AreEqual(new Money(0.03m, "USD"), shares[1]);
        Assert.AreEqual(new Money(0.03m, "USD"), shares[2]);
    }

    /// <summary>
    /// Verifies that allocation across positive ratios produces the expected weighted distribution.
    /// </summary>
    [TestMethod]
    public void Allocate_WhenRatioBased_ShouldDistributeProportionally()
    {
        decimal[] ratios = [1m, 1m, 2m];

        Money[] shares = new Money(100m, "USD").Allocate(ratios);

        Assert.AreEqual(new Money(25m, "USD"), shares[0]);
        Assert.AreEqual(new Money(25m, "USD"), shares[1]);
        Assert.AreEqual(new Money(50m, "USD"), shares[2]);
    }

    /// <summary>
    /// Verifies that allocating into zero parts throws.
    /// </summary>
    [TestMethod]
    public void Allocate_WhenPartsZero_ShouldThrowArgumentOutOfRangeException()
    {
        var money = new Money(10m, "USD");

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = money.Allocate(0);
        });
    }

    // ---------------------------------------------------------------------------------------------------------------
    // Formatting
    // ---------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies the default format renders ISO code and amount.
    /// </summary>
    [TestMethod]
    public void ToString_WhenDefault_ShouldRenderIsoCodeAndAmount()
    {
        var money = new Money(1234.56m, "USD");

        string text = money.ToString(null, System.Globalization.CultureInfo.InvariantCulture);

        Assert.AreEqual("USD 1,234.56", text);
    }

    /// <summary>
    /// Verifies that the <c>N</c> specifier strips the ISO code.
    /// </summary>
    [TestMethod]
    public void ToString_WhenNSpecifier_ShouldOmitIsoCode()
    {
        var money = new Money(1234.56m, "USD");

        string text = money.ToString("N", System.Globalization.CultureInfo.InvariantCulture);

        Assert.AreEqual("1,234.56", text);
    }

    // ---------------------------------------------------------------------------------------------------------------
    // Parsing
    // ---------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that the prefix form parses successfully.
    /// </summary>
    [TestMethod]
    public void Parse_WhenIsoPrefix_ShouldReturnAmount()
    {
        var result = Money.Parse("USD 19.99", System.Globalization.CultureInfo.InvariantCulture);

        Assert.AreEqual(new Money(19.99m, "USD"), result);
    }

    /// <summary>
    /// Verifies that the suffix form parses successfully.
    /// </summary>
    [TestMethod]
    public void Parse_WhenIsoSuffix_ShouldReturnAmount()
    {
        var result = Money.Parse("19.99 USD", System.Globalization.CultureInfo.InvariantCulture);

        Assert.AreEqual(new Money(19.99m, "USD"), result);
    }

    /// <summary>
    /// Verifies that a bare decimal without ISO code fails to parse — currency is required.
    /// </summary>
    [TestMethod]
    public void Parse_WhenBareDecimal_ShouldFail()
    {
        bool ok = Money.TryParse("19.99", System.Globalization.CultureInfo.InvariantCulture, out _);

        Assert.IsFalse(ok);
    }

    /// <summary>
    /// Verifies that round-tripping ToString → Parse yields the original value.
    /// </summary>
    [TestMethod]
    [DataRow("USD", 19.99)]
    [DataRow("JPY", 1234.0)]
    [DataRow("EUR", -100.50)]
    public void RoundTrip_WhenToStringThenParse_ShouldRecoverOriginal(string iso, double amount)
    {
        var original = new Money((decimal)amount, iso);

        string text = original.ToString(null, System.Globalization.CultureInfo.InvariantCulture);
        var recovered = Money.Parse(text, System.Globalization.CultureInfo.InvariantCulture);

        Assert.AreEqual(original, recovered);
    }

    // ---------------------------------------------------------------------------------------------------------------
    // JSON
    // ---------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies the JSON output shape.
    /// </summary>
    [TestMethod]
    public void Json_WhenSerialised_ShouldEmitAmountAndCurrencyFields()
    {
        var money = new Money(19.99m, "USD");

        string json = JsonSerializer.Serialize(money);

        Assert.AreEqual("{\"amount\":19.99,\"currency\":\"USD\"}", json);
    }

    /// <summary>
    /// Verifies a JSON round-trip preserves the value.
    /// </summary>
    [TestMethod]
    public void Json_WhenRoundTripped_ShouldPreserveValue()
    {
        var original = new Money(123.45m, "EUR");

        string json = JsonSerializer.Serialize(original);
        Money recovered = JsonSerializer.Deserialize<Money>(json);

        Assert.AreEqual(original, recovered);
    }

    /// <summary>
    /// Verifies that a JSON payload missing the currency property throws.
    /// </summary>
    [TestMethod]
    public void Json_WhenCurrencyMissing_ShouldThrowJsonException()
    {
        string json = "{\"amount\":19.99}";

        Assert.ThrowsExactly<JsonException>(() =>
        {
            _ = JsonSerializer.Deserialize<Money>(json);
        });
    }
}
