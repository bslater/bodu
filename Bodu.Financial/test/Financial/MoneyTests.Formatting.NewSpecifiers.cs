// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyTests.Formatting.NewSpecifiers.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Financial.Currencies;

namespace Bodu.Financial;

public partial class MoneyTests
{
    // ---------------------------------------------------------------------------------------------------------------
    // L specifier — English currency name appended to the numeric portion.
    // ---------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that the <c>"L"</c> specifier appends the currency's English-language name after the
    /// culture-formatted amount.
    /// </summary>
    [TestMethod]
    public void ToString_WhenLSpecifierAndUsd_ShouldAppendEnglishName()
    {
        var money = new Money<USD>(1234.56m);

        var actual = money.ToString("L", CultureInfo.InvariantCulture);

        Assert.AreEqual("1,234.56 US Dollar", actual);
    }

    /// <summary>
    /// Verifies that the <c>"L"</c> specifier honors the culture's grouping and decimal separators while keeping the
    /// English name unchanged across cultures.
    /// </summary>
    [TestMethod]
    public void ToString_WhenLSpecifierAndDeDeCulture_ShouldUseGermanGroupingWithEnglishName()
    {
        var money = new Money<EUR>(1234.56m);

        var actual = money.ToString("L", new CultureInfo("de-DE"));

        Assert.AreEqual("1.234,56 Euro", actual);
    }

    /// <summary>
    /// Verifies that the <c>"L"</c> specifier renders JPY's zero-decimal precision and English name.
    /// </summary>
    [TestMethod]
    public void ToString_WhenLSpecifierAndJpy_ShouldUseZeroDecimalsAndJapaneseYenName()
    {
        var money = new Money<JPY>(1234m);

        var actual = money.ToString("L", CultureInfo.InvariantCulture);

        Assert.AreEqual("1,234 Yen", actual);
    }

    /// <summary>
    /// Verifies that an explicit precision suffix combines with <c>"L"</c> to override the currency's natural
    /// precision while still appending the English name.
    /// </summary>
    [TestMethod]
    public void ToString_WhenLSpecifierWithExplicitPrecision_ShouldUseSuppliedDigitCount()
    {
        var money = new Money<USD>(19.99m);

        var actual = money.ToString("L0", CultureInfo.InvariantCulture);

        Assert.AreEqual("20 US Dollar", actual);
    }

    /// <summary>
    /// Verifies that the <c>"~"</c> prefix on <c>"L"</c> elides the English name entirely when the culture's region
    /// currency matches <typeparamref name="TCurrency" />.
    /// </summary>
    [TestMethod]
    public void ToString_WhenTildeLAndCultureMatches_ShouldElideEnglishName()
    {
        var money = new Money<USD>(19.99m);

        var actual = money.ToString("~L", new CultureInfo("en-US"));

        Assert.AreEqual("19.99", actual);
    }

    /// <summary>
    /// Verifies that the <c>"~"</c> prefix on <c>"L"</c> keeps the English name in mismatched-culture contexts to
    /// preserve currency disambiguation.
    /// </summary>
    [TestMethod]
    public void ToString_WhenTildeLAndCultureMismatched_ShouldKeepEnglishName()
    {
        var money = new Money<JPY>(1234m);

        var actual = money.ToString("~L", new CultureInfo("en-US"));

        Assert.AreEqual("1,234 Yen", actual);
    }

    /// <summary>
    /// Verifies that the <c>"L"</c> specifier falls back to the ISO-code form when the currency has no English name
    /// supplied (a user-defined <see cref="ICurrency" /> that does not override <see cref="ICurrency.EnglishName" />).
    /// </summary>
    [TestMethod]
    public void ToString_WhenLSpecifierAndCurrencyHasNoEnglishName_ShouldFallBackToIsoForm()
    {
        var money = new Money<NoEnglishNameCurrency>(1234.56m);

        var actual = money.ToString("L", CultureInfo.InvariantCulture);

        Assert.AreEqual("XYZ 1,234.56", actual);
    }

    // ---------------------------------------------------------------------------------------------------------------
    // R specifier — invariant round-trip form.
    // ---------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that the <c>"R"</c> specifier emits the ISO code followed by the amount under
    /// <see cref="CultureInfo.InvariantCulture" /> with no grouping, at the currency's natural precision.
    /// </summary>
    [TestMethod]
    public void ToString_WhenRSpecifier_ShouldEmitInvariantRoundTripForm()
    {
        var money = new Money<USD>(1234.56m);

        var actual = money.ToString("R", CultureInfo.InvariantCulture);

        Assert.AreEqual("USD 1234.56", actual);
    }

    /// <summary>
    /// Verifies that the <c>"R"</c> specifier ignores the supplied culture and always renders under
    /// <see cref="CultureInfo.InvariantCulture" />.
    /// </summary>
    [TestMethod]
    public void ToString_WhenRSpecifierAndDeDeCulture_ShouldIgnoreCultureAndUseInvariant()
    {
        var money = new Money<EUR>(1234.56m);

        var actual = money.ToString("R", new CultureInfo("de-DE"));

        Assert.AreEqual("EUR 1234.56", actual);
    }

    /// <summary>
    /// Verifies that the <c>"R"</c> output for a negative amount uses a leading minus sign rather than any
    /// culture-specific negative pattern.
    /// </summary>
    [TestMethod]
    public void ToString_WhenRSpecifierAndNegativeAmount_ShouldUseLeadingMinusSign()
    {
        var money = new Money<USD>(-1234.56m);

        var actual = money.ToString("R", new CultureInfo("en-US"));

        Assert.AreEqual("USD -1234.56", actual);
    }

    /// <summary>
    /// Verifies that the <c>"R"</c> output round-trips through
    /// <see cref="Money{TCurrency}.Parse(string, IFormatProvider?)" /> under <see cref="CultureInfo.InvariantCulture" />.
    /// </summary>
    [TestMethod]
    [DataRow("USD", 1234.56)]
    [DataRow("USD", 0.0)]
    [DataRow("USD", -19.99)]
    [DataRow("JPY", 1234.0)]
    [DataRow("BHD", 12.345)]
    [DataRow("BHD", -1234.567)]
    public void ToString_WhenRSpecifier_ShouldRoundTripThroughParse(string iso, double amount)
    {
        var value = (decimal)amount;
        Type currencyType = typeof(USD).Assembly.GetType($"Bodu.Financial.Currencies.{iso}")!;
        Type moneyType = typeof(Money<>).MakeGenericType(currencyType);
        var original = Activator.CreateInstance(moneyType, value)!;

        var text = (string)moneyType
            .GetMethod("ToString", [typeof(string), typeof(IFormatProvider)])!
            .Invoke(original, ["R", CultureInfo.InvariantCulture])!;

        var recovered = moneyType
            .GetMethod("Parse", [typeof(string), typeof(IFormatProvider)])!
            .Invoke(null, [text, CultureInfo.InvariantCulture])!;

        Assert.AreEqual(original, recovered);
    }

    /// <summary>
    /// Verifies that the <c>"~R"</c> specifier is rejected because the round-trip form must always include the ISO
    /// designator.
    /// </summary>
    [TestMethod]
    public void ToString_WhenTildeRSpecifier_ShouldThrowFormatException()
    {
        var money = new Money<USD>(1m);

        Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = money.ToString("~R", CultureInfo.InvariantCulture);
        });
    }

    /// <summary>
    /// Verifies that <c>"R"</c> with an explicit precision suffix is rejected because round-trip output must use
    /// the currency's natural precision.
    /// </summary>
    [TestMethod]
    [DataRow("R0")]
    [DataRow("R2")]
    [DataRow("R4")]
    public void ToString_WhenRSpecifierWithPrecisionSuffix_ShouldThrowFormatException(string format)
    {
        var money = new Money<USD>(1m);

        Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = money.ToString(format, CultureInfo.InvariantCulture);
        });
    }

    /// <summary>
    /// A custom currency that omits <see cref="ICurrency.EnglishName" />, exercising the <c>L</c>-specifier
    /// fallback to the ISO-code form for currencies without an English name.
    /// </summary>
    private sealed class NoEnglishNameCurrency : ICurrency
    {
        /// <summary>
        /// Gets the ISO-style code used for tests.
        /// </summary>
        public static string IsoCode => "XYZ";

        /// <summary>
        /// Gets the minor-unit precision.
        /// </summary>
        public static int MinorUnits => 2;
    }
}
