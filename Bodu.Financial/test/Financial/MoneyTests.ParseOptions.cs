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
