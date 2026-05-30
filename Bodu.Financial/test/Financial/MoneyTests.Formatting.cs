// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyTests.Formatting.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Financial.Currencies;

namespace Bodu.Financial;

public partial class MoneyTests
{
    /// <summary>
    /// Verifies that the default <see cref="object.ToString" /> for USD uses the ISO code followed by the amount
    /// formatted to two decimal places.
    /// </summary>
    [TestMethod]
    public void ToString_WhenDefault_ShouldReturnIsoCodeAndAmountAtMinorUnitPrecision()
    {
        var money = new Money<USD>(1234.56m);

        Assert.AreEqual("USD 1,234.56", money.ToString(null, CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// Verifies that JPY formats with no fractional digits because its minor-unit precision is zero.
    /// </summary>
    [TestMethod]
    public void ToString_WhenJpy_ShouldFormatWithoutFractionalDigits()
    {
        var money = new Money<JPY>(100m);

        Assert.AreEqual("JPY 100", money.ToString(null, CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// Verifies that BHD formats with three fractional digits.
    /// </summary>
    [TestMethod]
    public void ToString_WhenBhd_ShouldFormatWithThreeFractionalDigits()
    {
        var money = new Money<BHD>(12.345m);

        Assert.AreEqual("BHD 12.345", money.ToString(null, CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// Verifies that the <c>"N"</c> specifier omits the ISO code and returns the bare numeric portion.
    /// </summary>
    [TestMethod]
    public void ToString_WhenNSpecifier_ShouldOmitIsoCode()
    {
        var money = new Money<USD>(1234.56m);

        Assert.AreEqual("1,234.56", money.ToString("N", CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// Verifies that the <c>"F"</c> specifier omits both the ISO code and thousand separators.
    /// </summary>
    [TestMethod]
    public void ToString_WhenFSpecifier_ShouldOmitGroupingSeparators()
    {
        var money = new Money<USD>(1234.56m);

        Assert.AreEqual("1234.56", money.ToString("F", CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// Verifies that an explicit precision suffix overrides the currency's natural precision.
    /// </summary>
    [TestMethod]
    public void ToString_WhenExplicitPrecision_ShouldUseSuppliedDigitCount()
    {
        var money = new Money<USD>(19.99m);

        Assert.AreEqual("USD 19.9900", money.ToString("C4", CultureInfo.InvariantCulture));
        Assert.AreEqual("19.99", money.ToString("F2", CultureInfo.InvariantCulture));
        Assert.AreEqual("20", money.ToString("F0", CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// Verifies that an unrecognized specifier throws <see cref="FormatException" />.
    /// </summary>
    [TestMethod]
    public void ToString_WhenSpecifierUnknown_ShouldThrowFormatException()
    {
        var money = new Money<USD>(1m);

        Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = money.ToString("Z", CultureInfo.InvariantCulture);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ISpanFormattable.TryFormat" /> writes the same content as <see cref="object.ToString" />.
    /// </summary>
    [TestMethod]
    public void TryFormat_WhenSpanLargeEnough_ShouldWriteSameTextAsToString()
    {
        var money = new Money<USD>(19.99m);
        Span<char> buffer = stackalloc char[32];

        var ok = money.TryFormat(buffer, out var written, default, CultureInfo.InvariantCulture);

        Assert.IsTrue(ok);
        Assert.AreEqual("USD 19.99", buffer[..written].ToString());
    }

    /// <summary>
    /// Verifies that <see cref="ISpanFormattable.TryFormat" /> returns <see langword="false" /> when the
    /// destination is too small.
    /// </summary>
    [TestMethod]
    public void TryFormat_WhenSpanTooSmall_ShouldReturnFalse()
    {
        var money = new Money<USD>(19.99m);
        Span<char> buffer = stackalloc char[3];

        var ok = money.TryFormat(buffer, out var written, default, CultureInfo.InvariantCulture);

        Assert.IsFalse(ok);
        Assert.AreEqual(0, written);
    }

    /// <summary>
    /// Verifies that <see cref="IUtf8SpanFormattable.TryFormat" /> writes the UTF-8 representation of the same text.
    /// </summary>
    [TestMethod]
    public void TryFormatUtf8_WhenSpanLargeEnough_ShouldWriteSameBytesAsToString()
    {
        var money = new Money<USD>(19.99m);
        Span<byte> buffer = stackalloc byte[32];

        var ok = money.TryFormat(buffer, out var written, default, CultureInfo.InvariantCulture);

        Assert.IsTrue(ok);
        Assert.AreEqual("USD 19.99", System.Text.Encoding.UTF8.GetString(buffer[..written]));
    }

    /// <summary>
    /// Verifies that the <c>"L"</c> specifier uses the culture's native currency symbol when the culture's
    /// region currency matches <typeparamref name="TCurrency" />.
    /// </summary>
    [TestMethod]
    public void ToString_WhenLSpecifierAndCultureMatchesUsd_ShouldUseDollarSymbol()
    {
        var money = new Money<USD>(1234.56m);

        var result = money.ToString("L", new CultureInfo("en-US"));

        Assert.AreEqual("$1,234.56", result);
    }

    /// <summary>
    /// Verifies that the <c>"L"</c> specifier uses the culture's pattern (symbol after amount with grouping
    /// dots and a comma decimal) when formatting EUR with a German locale.
    /// </summary>
    [TestMethod]
    public void ToString_WhenLSpecifierAndCultureMatchesEur_ShouldUseEuroSymbolInLocalePattern()
    {
        var money = new Money<EUR>(1234.56m);

        var result = money.ToString("L", new CultureInfo("de-DE"));

        // de-DE: "1.234,56 €" — symbol after amount, comma decimal, period grouping.
        StringAssert.Contains(result, "€");
        StringAssert.Contains(result, "1.234,56");
    }

    /// <summary>
    /// Verifies that the <c>"L"</c> specifier substitutes the ISO code in place of the locale's symbol when the
    /// currencies differ, keeping the locale's number formatting.
    /// </summary>
    [TestMethod]
    public void ToString_WhenLSpecifierAndCultureMismatchedPrefix_ShouldSubstituteIsoBeforeAmount()
    {
        var money = new Money<JPY>(1234m);

        var result = money.ToString("L", new CultureInfo("en-US"));

        Assert.AreEqual("JPY 1,234", result);
    }

    /// <summary>
    /// Verifies that the <c>"L"</c> specifier places the ISO code after the amount in cultures whose currency
    /// pattern is suffix-based.
    /// </summary>
    [TestMethod]
    public void ToString_WhenLSpecifierAndCultureMismatchedSuffix_ShouldSubstituteIsoAfterAmount()
    {
        var money = new Money<USD>(1234.56m);

        var result = money.ToString("L", new CultureInfo("de-DE"));

        Assert.AreEqual("1.234,56 USD", result);
    }

    /// <summary>
    /// Verifies that the <c>"L"</c> specifier honors the currency's minor-unit precision rather than the
    /// culture's default, so JPY formats with zero decimal digits even under en-US.
    /// </summary>
    [TestMethod]
    public void ToString_WhenLSpecifierAndJpyUnderEnUs_ShouldRespectCurrencyMinorUnits()
    {
        var money = new Money<JPY>(2500m);

        var result = money.ToString("L", new CultureInfo("en-US"));

        Assert.AreEqual("JPY 2,500", result);
    }

    /// <summary>
    /// Verifies that the <c>"L"</c> specifier overrides both the culture's and the currency's natural precision
    /// when an explicit fractional-digit count is supplied.
    /// </summary>
    [TestMethod]
    public void ToString_WhenLSpecifierAndExplicitPrecisionZero_ShouldRoundToWholeUnits()
    {
        var money = new Money<USD>(19.99m);

        var result = money.ToString("L0", new CultureInfo("en-US"));

        Assert.AreEqual("$20", result);
    }

    /// <summary>
    /// Verifies that the <c>"L"</c> specifier falls back to the ISO-code substitution path for neutral cultures
    /// (no region available) such as <c>"en"</c>.
    /// </summary>
    [TestMethod]
    public void ToString_WhenLSpecifierAndNeutralCulture_ShouldFallBackToIsoSubstitution()
    {
        var money = new Money<USD>(19.99m);

        var result = money.ToString("L", CultureInfo.InvariantCulture);

        Assert.AreEqual("USD 19.99", result);
    }

    /// <summary>
    /// Verifies that the <c>"~"</c> prefix elides the ISO code in the default <c>"C"</c> path when the culture's
    /// currency matches <typeparamref name="TCurrency" />.
    /// </summary>
    [TestMethod]
    public void ToString_WhenTildeCAndCultureMatches_ShouldElideIsoCode()
    {
        var money = new Money<USD>(1234.56m);

        var result = money.ToString("~C", new CultureInfo("en-US"));

        Assert.AreEqual("1,234.56", result);
    }

    /// <summary>
    /// Verifies that the <c>"~"</c> prefix keeps the ISO code when the culture's currency does not match
    /// <typeparamref name="TCurrency" />, preserving disambiguation in mixed-currency contexts.
    /// </summary>
    [TestMethod]
    public void ToString_WhenTildeCAndCultureMismatched_ShouldKeepIsoCode()
    {
        var money = new Money<JPY>(1234m);

        var result = money.ToString("~C", new CultureInfo("en-US"));

        Assert.AreEqual("JPY 1,234", result);
    }

    /// <summary>
    /// Verifies that the <c>"~"</c> prefix on the <c>"L"</c> specifier elides the locale's symbol when the
    /// culture's currency matches.
    /// </summary>
    [TestMethod]
    public void ToString_WhenTildeLAndCultureMatches_ShouldElideSymbol()
    {
        var money = new Money<USD>(19.99m);

        var result = money.ToString("~L", new CultureInfo("en-US"));

        Assert.AreEqual("19.99", result);
    }

    /// <summary>
    /// Verifies that the <c>"~"</c> prefix on <c>"L"</c> still emits the ISO code in the mismatched-currency
    /// case so the value is never ambiguous.
    /// </summary>
    [TestMethod]
    public void ToString_WhenTildeLAndCultureMismatched_ShouldStillEmitIsoCode()
    {
        var money = new Money<JPY>(1234m);

        var result = money.ToString("~L", new CultureInfo("en-US"));

        Assert.AreEqual("JPY 1,234", result);
    }

    /// <summary>
    /// Verifies that the <c>"~"</c> prefix combines with an explicit precision suffix.
    /// </summary>
    [TestMethod]
    public void ToString_WhenTildeCWithExplicitPrecisionAndCultureMatches_ShouldUseElidedFormWithPrecision()
    {
        var money = new Money<USD>(19.99m);

        var result = money.ToString("~C0", new CultureInfo("en-US"));

        Assert.AreEqual("20", result);
    }
}

