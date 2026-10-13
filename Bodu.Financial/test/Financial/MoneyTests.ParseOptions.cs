// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyTests.ParseOptions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Financial.Currencies;

namespace Bodu.Financial;

public partial class MoneyTests
{
    /// <summary>
    /// Verifies that strict-ISO parsing accepts the canonical prefix and suffix forms.
    /// </summary>
    [TestMethod]
    [DataRow("USD 19.99")]
    [DataRow("19.99 USD")]
    public void ParseOptions_WhenStrictIso_ShouldParseCanonicalForms(string text)
    {
        MoneyParseOptions options = MoneyParseOptions.Default with { FormatProvider = CultureInfo.InvariantCulture };

        var money = Money.Parse(text, options);

        Assert.AreEqual(new Money(19.99m, CurrencyCode.USD), money);
    }

    /// <summary>
    /// Verifies that strict-ISO parsing rejects an unregistered currency.
    /// </summary>
    [TestMethod]
    public void ParseOptions_WhenStrictIsoAndUnregistered_ShouldReturnFalse()
    {
        Assert.IsFalse(Money.TryParse("XYZ 5.00", MoneyParseOptions.Default, out _));
    }

    /// <summary>
    /// Verifies that lenient-import parsing normalises a lower-case ISO code to upper case.
    /// </summary>
    [TestMethod]
    public void ParseOptions_WhenLenientImport_ShouldNormaliseLowercaseIso()
    {
        MoneyParseOptions options = new() { Mode = MoneyParseMode.LenientImport, FormatProvider = CultureInfo.InvariantCulture };

        var money = Money.Parse("usd 19.99", options);

        Assert.AreEqual(new Money(19.99m, CurrencyCode.USD), money);
    }

    /// <summary>
    /// Verifies that round-trip-only parsing interprets the invariant form without an explicit culture.
    /// </summary>
    [TestMethod]
    public void ParseOptions_WhenRoundTripOnly_ShouldParseInvariantForm()
    {
        var money = Money.Parse("USD 1234.56", new MoneyParseOptions { Mode = MoneyParseMode.RoundTripOnly });

        Assert.AreEqual(new Money(1234.56m, CurrencyCode.USD), money);
    }

    /// <summary>
    /// Verifies that round-trip-only parsing restores a scale finer than the currency's registered minor units, so a
    /// value written with the <c>"R"</c> format specifier parses back to the same amount and scale.
    /// </summary>
    [TestMethod]
    public void Parse_WhenRoundTripOnlyAndScaleIsFinerThanRegistry_ShouldPreserveScale()
    {
        Money value = Money.FromExplicitScale(145.678912m, CurrencyCode.USD, 6);
        string text = value.ToString("R", CultureInfo.InvariantCulture);

        Money parsed = Money.Parse(text, new MoneyParseOptions { Mode = MoneyParseMode.RoundTripOnly });

        Assert.AreEqual(145.678912m, parsed.Amount);
        Assert.AreEqual(6, parsed.MinorUnits);
        Assert.AreEqual(CurrencyCode.USD, parsed.Code);
    }

    /// <summary>
    /// Verifies that round-trip-only parsing reads the amount in invariant notation even when a format provider with a
    /// comma decimal separator is supplied, since the <c>"R"</c> form ignores the provider.
    /// </summary>
    [TestMethod]
    public void Parse_WhenRoundTripOnlyWithCommaDecimalProvider_ShouldReadInvariantNumber()
    {
        var options = new MoneyParseOptions { Mode = MoneyParseMode.RoundTripOnly, FormatProvider = new CultureInfo("de-DE") };

        Money parsed = Money.Parse("USD 1234.56", options);

        Assert.AreEqual(new Money(1234.56m, CurrencyCode.USD), parsed);
    }

    /// <summary>
    /// Verifies that round-trip-only parsing accepts the negative and whole-number forms the <c>"R"</c> format
    /// specifier produces.
    /// </summary>
    [TestMethod]
    [DataRow("USD -1234.56", CurrencyCode.USD, "-1234.56")]
    [DataRow("JPY 1234", CurrencyCode.JPY, "1234")]
    public void TryParse_WhenRoundTripOnlyAndInputIsRoundTripShape_ShouldReturnTrue(string text, CurrencyCode code, string amount)
    {
        bool parsed = Money.TryParse(text, new MoneyParseOptions { Mode = MoneyParseMode.RoundTripOnly }, out Money result);

        Assert.IsTrue(parsed);
        Assert.AreEqual(code, result.Code);
        Assert.AreEqual(decimal.Parse(amount, CultureInfo.InvariantCulture), result.Amount);
    }

    /// <summary>
    /// Verifies that round-trip-only parsing rejects text that is not the <c>"R"</c> shape (the ISO code, one space,
    /// and an invariant fixed-point amount), even when the strict ISO parser would accept it.
    /// </summary>
    [TestMethod]
    [DataRow("1234.56 USD")]
    [DataRow("USD 1,234.56")]
    [DataRow("USD  1234.56")]
    [DataRow(" USD 1234.56")]
    [DataRow("USD 1234.56 ")]
    [DataRow("USD +1234.56")]
    [DataRow("USD 1234.56-")]
    [DataRow("USD .5")]
    [DataRow("USD 5.")]
    public void TryParse_WhenRoundTripOnlyAndInputIsNotRoundTripShape_ShouldReturnFalse(string text)
    {
        bool parsed = Money.TryParse(text, new MoneyParseOptions { Mode = MoneyParseMode.RoundTripOnly }, out _);

        Assert.IsFalse(parsed);
    }

    /// <summary>
    /// Verifies that an undefined parse mode is rejected with <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void ParseOptions_WhenModeUndefined_ShouldThrowArgumentOutOfRangeException()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = Money.TryParse("USD 1.00".AsSpan(), new MoneyParseOptions { Mode = (MoneyParseMode)99 }, out _);
        });
    }

    /// <summary>
    /// Verifies that option-based parsing of blank input reports failure.
    /// </summary>
    [TestMethod]
    public void ParseOptions_WhenInputIsBlank_ShouldReturnFalse() =>
        Assert.IsFalse(Money.TryParse("   ", new MoneyParseOptions { Mode = MoneyParseMode.StrictIso }, out _));

    /// <summary>
    /// Verifies that <see cref="MoneyParseMode.LenientImport" /> upper-cases a lower-case ISO code before resolving the
    /// currency.
    /// </summary>
    [TestMethod]
    public void ParseOptions_WhenLenientImportLowercaseIso_ShouldUpperCaseAndResolve()
    {
        var options = new MoneyParseOptions { Mode = MoneyParseMode.LenientImport, FormatProvider = CultureInfo.InvariantCulture };

        Assert.IsTrue(Money.TryParse("usd 10.50", options, out Money result));
        Assert.AreEqual(new Money(10.50m, CurrencyCode.USD), result);
    }

    /// <summary>
    /// Verifies that a symbol with no accompanying numeric portion fails to compose a value.
    /// </summary>
    [TestMethod]
    public void ParseOptions_WhenSymbolHasNoAmount_ShouldReturnFalse()
    {
        var options = new MoneyParseOptions
        {
            Mode = MoneyParseMode.CultureAware,
            FormatProvider = CultureInfo.InvariantCulture,
            CurrencyLookup = new CurrencyLookupService(),
        };

        Assert.IsFalse(Money.TryParse("Ω", options, out _));
    }

    /// <summary>
    /// Verifies that the plain <see cref="Money.TryParse(string?, IFormatProvider?, out Money)" /> recognizes the ISO
    /// suffix form, rejects a non-numeric amount, and rejects an unregistered currency.
    /// </summary>
    [TestMethod]
    public void TryParse_PlainCulture_ShouldHandleSuffixAndRejectInvalid()
    {
        Assert.IsTrue(Money.TryParse("19.99 USD", CultureInfo.InvariantCulture, out Money suffix));
        Assert.AreEqual(new Money(19.99m, CurrencyCode.USD), suffix);

        Assert.IsFalse(Money.TryParse("USD .", CultureInfo.InvariantCulture, out _));
        Assert.IsFalse(Money.TryParse("XYZ 10.00", CultureInfo.InvariantCulture, out _));
    }

    /// <summary>
    /// Verifies that the plain <see cref="Money.TryParse(string?, IFormatProvider?, out Money)" /> reports failure for
    /// a null string rather than throwing.
    /// </summary>
    [TestMethod]
    public void TryParse_WhenStringIsNull_ShouldReturnFalse() =>
        Assert.IsFalse(Money.TryParse((string?)null, CultureInfo.InvariantCulture, out _));
}
