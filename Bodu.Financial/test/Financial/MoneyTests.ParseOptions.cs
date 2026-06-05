// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyTests.ParseOptions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

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

        Assert.AreEqual(new Money(19.99m, "USD"), money);
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

        Assert.AreEqual(new Money(19.99m, "USD"), money);
    }

    /// <summary>
    /// Verifies that lenient import with <see cref="UnknownCurrencyPolicy.AllowUnscaled" /> accepts an unregistered
    /// currency at source precision.
    /// </summary>
    [TestMethod]
    public void ParseOptions_WhenLenientImportAllowUnscaled_ShouldAcceptUnregistered()
    {
        MoneyParseOptions options = new()
        {
            Mode = MoneyParseMode.LenientImport,
            FormatProvider = CultureInfo.InvariantCulture,
            UnknownCurrency = UnknownCurrencyPolicy.AllowUnscaled,
        };

        var money = Money.Parse("XYZ 1.2345", options);

        Assert.AreEqual(1.2345m, money.Amount);
        Assert.AreEqual("XYZ", money.IsoCode);
        Assert.AreEqual(0, money.MinorUnits);
    }

    /// <summary>
    /// Verifies that round-trip-only parsing interprets the invariant form without an explicit culture.
    /// </summary>
    [TestMethod]
    public void ParseOptions_WhenRoundTripOnly_ShouldParseInvariantForm()
    {
        var money = Money.Parse("USD 1234.56", new MoneyParseOptions { Mode = MoneyParseMode.RoundTripOnly });

        Assert.AreEqual(new Money(1234.56m, "USD"), money);
    }

    /// <summary>
    /// Verifies that culture-aware parsing resolves an unambiguous currency symbol through the lookup.
    /// </summary>
    [TestMethod]
    public void ParseOptions_WhenCultureAwareWithUniqueSymbol_ShouldResolveCurrency()
    {
        CurrencyRegistry.Replace(new CurrencyInfo("XQP", 2, 0m, false, null, null) { Symbol = "Ω" });

        MoneyParseOptions options = new()
        {
            Mode = MoneyParseMode.CultureAware,
            FormatProvider = CultureInfo.InvariantCulture,
            CurrencyLookup = new CurrencyLookupService(),
        };

        var money = Money.Parse("Ω10.50", options);

        Assert.AreEqual("XQP", money.IsoCode);
        Assert.AreEqual(10.50m, money.Amount);
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
}
