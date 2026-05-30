// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyValueTests.Core.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json;
using Bodu.Numerics.Currencies;

namespace Bodu.Numerics;

public partial class MoneyValueTests
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
    [DataRow("XQX", 1.234567, 1.234567)]   // unknown currency — no rounding
    public void Constructor_WhenAmountHasExcessPrecision_ShouldRoundToCurrencyMinorUnits(string iso, double amount, double expected)
    {
        MoneyValue money = new MoneyValue((decimal)amount, iso);

        Assert.AreEqual((decimal)expected, money.Amount);
        Assert.AreEqual(iso, money.IsoCode);
    }

    /// <summary>
    /// Verifies that the constructor with explicit rounding rule honors the supplied rule.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenAwayFromZeroRequested_ShouldRoundMidpointAwayFromZero()
    {
        MoneyValue banker = new MoneyValue(1.225m, "USD");
        MoneyValue awayFromZero = new MoneyValue(1.225m, "USD", MidpointRounding.AwayFromZero);

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
            _ = new MoneyValue(10m, null!);
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
            _ = new MoneyValue(10m, iso);
        });
    }

    /// <summary>
    /// Verifies that the default value yields an empty ISO code and a zero amount.
    /// </summary>
    [TestMethod]
    public void DefaultValue_WhenInspected_ShouldHaveEmptyIsoCodeAndZeroAmount()
    {
        MoneyValue zero = default;

        Assert.AreEqual(string.Empty, zero.IsoCode);
        Assert.AreEqual(0m, zero.Amount);
        Assert.IsTrue(zero.IsZero);
    }

    // ---------------------------------------------------------------------------------------------------------------
    // Properties
    // ---------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="MoneyValue.MinorUnits" /> reads from the registry for known currencies.
    /// </summary>
    [TestMethod]
    [DataRow("USD", 2)]
    [DataRow("JPY", 0)]
    [DataRow("BHD", 3)]
    [DataRow("XQR", 0)]    // unknown — registry returns 0
    public void MinorUnits_WhenInspected_ShouldMatchRegistry(string iso, int expected)
    {
        MoneyValue money = new MoneyValue(0m, iso);

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
        MoneyValue money = new MoneyValue((decimal)amount, "USD");

        Assert.AreEqual(expectedPositive, money.IsPositive);
        Assert.AreEqual(expectedNegative, money.IsNegative);
        Assert.AreEqual(expectedZero, money.IsZero);
    }

    /// <summary>
    /// Verifies that <see cref="MoneyValue.Abs" /> returns the unsigned amount.
    /// </summary>
    [TestMethod]
    public void Abs_WhenNegative_ShouldReturnUnsignedAmount()
    {
        MoneyValue negative = new MoneyValue(-19.99m, "USD");

        MoneyValue abs = negative.Abs;

        Assert.AreEqual(19.99m, abs.Amount);
        Assert.AreEqual("USD", abs.IsoCode);
    }

    /// <summary>
    /// Verifies that <see cref="MoneyValue.Zero(string)" /> returns a zero amount in the specified currency.
    /// </summary>
    [TestMethod]
    public void Zero_WhenCalled_ShouldReturnZeroAmountInCurrency()
    {
        MoneyValue zero = MoneyValue.Zero("EUR");

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
        MoneyValue a = new MoneyValue(10.50m, "USD");
        MoneyValue b = new MoneyValue(5.25m, "USD");

        MoneyValue sum = a + b;

        Assert.AreEqual(new MoneyValue(15.75m, "USD"), sum);
    }

    /// <summary>
    /// Verifies that addition across currencies throws <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void Addition_WhenDifferentCurrencies_ShouldThrowInvalidOperationException()
    {
        MoneyValue usd = new MoneyValue(10m, "USD");
        MoneyValue eur = new MoneyValue(10m, "EUR");

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
        MoneyValue usd = new MoneyValue(10m, "USD");
        MoneyValue eur = new MoneyValue(10m, "EUR");

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
        MoneyValue price = new MoneyValue(0.95m, "USD");

        MoneyValue result = price * 3m;

        Assert.AreEqual(new MoneyValue(2.85m, "USD"), result);
    }

    /// <summary>
    /// Verifies scalar division.
    /// </summary>
    [TestMethod]
    public void Division_WhenScalar_ShouldRoundToCurrencyMinorUnits()
    {
        MoneyValue total = new MoneyValue(10m, "USD");

        MoneyValue share = total / 3m;

        Assert.AreEqual(new MoneyValue(3.33m, "USD"), share);
    }

    /// <summary>
    /// Verifies the dimensionless ratio between two same-currency amounts.
    /// </summary>
    [TestMethod]
    public void DivisionMoneyValueByMoneyValue_WhenSameCurrency_ShouldReturnDecimalRatio()
    {
        MoneyValue ten = new MoneyValue(10m, "USD");
        MoneyValue four = new MoneyValue(4m, "USD");

        Assert.AreEqual(2.5m, ten / four);
    }

    /// <summary>
    /// Verifies that ratio across currencies throws.
    /// </summary>
    [TestMethod]
    public void DivisionMoneyValueByMoneyValue_WhenDifferentCurrencies_ShouldThrowInvalidOperationException()
    {
        MoneyValue usd = new MoneyValue(10m, "USD");
        MoneyValue eur = new MoneyValue(10m, "EUR");

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
        MoneyValue usd = new MoneyValue(10m, "USD");
        MoneyValue eur = new MoneyValue(10m, "EUR");

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
        MoneyValue a = new MoneyValue(19.99m, "USD");
        MoneyValue b = new MoneyValue(19.99m, "USD");

        Assert.IsTrue(a.Equals(b));
        Assert.IsTrue(a == b);
    }

    /// <summary>
    /// Verifies that equality across currencies returns false rather than throwing.
    /// </summary>
    [TestMethod]
    public void Equals_WhenDifferentCurrencies_ShouldReturnFalse()
    {
        MoneyValue usd = new MoneyValue(10m, "USD");
        MoneyValue eur = new MoneyValue(10m, "EUR");

        Assert.IsFalse(usd.Equals(eur));
        Assert.IsFalse(usd == eur);
    }

    /// <summary>
    /// Verifies that hash codes differ across currencies even with identical amounts.
    /// </summary>
    [TestMethod]
    public void GetHashCode_WhenDifferentCurrencies_ShouldDiffer()
    {
        int usd = new MoneyValue(10m, "USD").GetHashCode();
        int eur = new MoneyValue(10m, "EUR").GetHashCode();

        Assert.AreNotEqual(usd, eur);
    }

    /// <summary>
    /// Verifies <see cref="IComparable{T}.CompareTo(T)" /> within a currency.
    /// </summary>
    [TestMethod]
    public void CompareTo_WhenSameCurrency_ShouldOrderByAmount()
    {
        MoneyValue small = new MoneyValue(1m, "USD");
        MoneyValue large = new MoneyValue(2m, "USD");

        Assert.IsTrue(small.CompareTo(large) < 0);
        Assert.IsTrue(large.CompareTo(small) > 0);
        Assert.AreEqual(0, small.CompareTo(new MoneyValue(1m, "USD")));
    }

    /// <summary>
    /// Verifies that <see cref="IComparable.CompareTo(object)" /> throws on cross-currency comparison.
    /// </summary>
    [TestMethod]
    public void CompareTo_WhenDifferentCurrency_ShouldThrowInvalidOperationException()
    {
        MoneyValue usd = new MoneyValue(10m, "USD");
        MoneyValue eur = new MoneyValue(10m, "EUR");

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = usd.CompareTo(eur);
        });
    }

    // ---------------------------------------------------------------------------------------------------------------
    // Conversion to/from typed Money
    // ---------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="MoneyValue.ToTyped{TCurrency}" /> succeeds when the runtime currency matches.
    /// </summary>
    [TestMethod]
    public void ToTyped_WhenCurrencyMatches_ShouldReturnTypedMoney()
    {
        MoneyValue runtime = new MoneyValue(19.99m, "USD");

        Money<USD> typed = runtime.ToTyped<USD>();

        Assert.AreEqual(new Money<USD>(19.99m), typed);
    }

    /// <summary>
    /// Verifies that <see cref="MoneyValue.ToTyped{TCurrency}" /> throws when the runtime currency mismatches.
    /// </summary>
    [TestMethod]
    public void ToTyped_WhenCurrencyMismatches_ShouldThrowInvalidOperationException()
    {
        MoneyValue runtime = new MoneyValue(19.99m, "EUR");

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = runtime.ToTyped<USD>();
        });
    }

    /// <summary>
    /// Verifies that <see cref="MoneyValue.TryToTyped{TCurrency}" /> returns false on mismatch without throwing.
    /// </summary>
    [TestMethod]
    public void TryToTyped_WhenCurrencyMismatches_ShouldReturnFalse()
    {
        MoneyValue runtime = new MoneyValue(19.99m, "EUR");

        bool ok = runtime.TryToTyped(out Money<USD> result);

        Assert.IsFalse(ok);
        Assert.AreEqual(default(Money<USD>), result);
    }

    /// <summary>
    /// Verifies that <see cref="MoneyValue.FromTyped{TCurrency}" /> round-trips a typed value.
    /// </summary>
    [TestMethod]
    public void FromTyped_WhenCalled_ShouldYieldEquivalentRuntimeValue()
    {
        Money<USD> typed = new Money<USD>(19.99m);

        MoneyValue runtime = MoneyValue.FromTyped(typed);

        Assert.AreEqual("USD", runtime.IsoCode);
        Assert.AreEqual(19.99m, runtime.Amount);
    }

    /// <summary>
    /// Verifies cross-currency conversion via runtime ISO code.
    /// </summary>
    [TestMethod]
    public void Convert_WhenRuntimeIsoTarget_ShouldRoundAtTargetCurrencyPrecision()
    {
        MoneyValue usd = new MoneyValue(100m, "USD");

        MoneyValue jpy = usd.Convert("JPY", 155.5m);

        Assert.AreEqual("JPY", jpy.IsoCode);
        Assert.AreEqual(15550m, jpy.Amount);
    }

    /// <summary>
    /// Verifies cross-currency conversion to a typed target.
    /// </summary>
    [TestMethod]
    public void Convert_WhenTypedTarget_ShouldReturnTypedMoney()
    {
        MoneyValue usd = new MoneyValue(100m, "USD");

        Money<JPY> jpy = usd.Convert<JPY>(155.5m);

        Assert.AreEqual(new Money<JPY>(15550m), jpy);
    }

    /// <summary>
    /// Verifies that a negative exchange rate throws.
    /// </summary>
    [TestMethod]
    public void Convert_WhenRateNegative_ShouldThrowArgumentOutOfRangeException()
    {
        MoneyValue usd = new MoneyValue(100m, "USD");

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
        MoneyValue[] shares = new MoneyValue(0.10m, "USD").Allocate(3);

        Assert.AreEqual(3, shares.Length);
        Assert.AreEqual(new MoneyValue(0.04m, "USD"), shares[0]);
        Assert.AreEqual(new MoneyValue(0.03m, "USD"), shares[1]);
        Assert.AreEqual(new MoneyValue(0.03m, "USD"), shares[2]);
    }

    /// <summary>
    /// Verifies that allocation across positive ratios produces the expected weighted distribution.
    /// </summary>
    [TestMethod]
    public void Allocate_WhenRatioBased_ShouldDistributeProportionally()
    {
        decimal[] ratios = { 1m, 1m, 2m };

        MoneyValue[] shares = new MoneyValue(100m, "USD").Allocate(ratios);

        Assert.AreEqual(new MoneyValue(25m, "USD"), shares[0]);
        Assert.AreEqual(new MoneyValue(25m, "USD"), shares[1]);
        Assert.AreEqual(new MoneyValue(50m, "USD"), shares[2]);
    }

    /// <summary>
    /// Verifies that allocating into zero parts throws.
    /// </summary>
    [TestMethod]
    public void Allocate_WhenPartsZero_ShouldThrowArgumentOutOfRangeException()
    {
        MoneyValue money = new MoneyValue(10m, "USD");

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
        MoneyValue money = new MoneyValue(1234.56m, "USD");

        string text = money.ToString(null, System.Globalization.CultureInfo.InvariantCulture);

        Assert.AreEqual("USD 1,234.56", text);
    }

    /// <summary>
    /// Verifies that the <c>N</c> specifier strips the ISO code.
    /// </summary>
    [TestMethod]
    public void ToString_WhenNSpecifier_ShouldOmitIsoCode()
    {
        MoneyValue money = new MoneyValue(1234.56m, "USD");

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
        MoneyValue result = MoneyValue.Parse("USD 19.99", System.Globalization.CultureInfo.InvariantCulture);

        Assert.AreEqual(new MoneyValue(19.99m, "USD"), result);
    }

    /// <summary>
    /// Verifies that the suffix form parses successfully.
    /// </summary>
    [TestMethod]
    public void Parse_WhenIsoSuffix_ShouldReturnAmount()
    {
        MoneyValue result = MoneyValue.Parse("19.99 USD", System.Globalization.CultureInfo.InvariantCulture);

        Assert.AreEqual(new MoneyValue(19.99m, "USD"), result);
    }

    /// <summary>
    /// Verifies that a bare decimal without ISO code fails to parse — currency is required.
    /// </summary>
    [TestMethod]
    public void Parse_WhenBareDecimal_ShouldFail()
    {
        bool ok = MoneyValue.TryParse("19.99", System.Globalization.CultureInfo.InvariantCulture, out _);

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
        MoneyValue original = new MoneyValue((decimal)amount, iso);

        string text = original.ToString(null, System.Globalization.CultureInfo.InvariantCulture);
        MoneyValue recovered = MoneyValue.Parse(text, System.Globalization.CultureInfo.InvariantCulture);

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
        MoneyValue money = new MoneyValue(19.99m, "USD");

        string json = JsonSerializer.Serialize(money);

        Assert.AreEqual("{\"amount\":19.99,\"currency\":\"USD\"}", json);
    }

    /// <summary>
    /// Verifies a JSON round-trip preserves the value.
    /// </summary>
    [TestMethod]
    public void Json_WhenRoundTripped_ShouldPreserveValue()
    {
        MoneyValue original = new MoneyValue(123.45m, "EUR");

        string json = JsonSerializer.Serialize(original);
        MoneyValue recovered = JsonSerializer.Deserialize<MoneyValue>(json);

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
            _ = JsonSerializer.Deserialize<MoneyValue>(json);
        });
    }
}
