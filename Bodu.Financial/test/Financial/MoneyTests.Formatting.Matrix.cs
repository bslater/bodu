// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyTests.Formatting.Matrix.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Reflection;
using Bodu.Financial.Currencies;

namespace Bodu.Financial;

public partial class MoneyTests
{
    // ---------------------------------------------------------------------------------------------------------------
    // Default specifier (G / C / null) — ISO code + culture-grouped number across all minor-unit categories.
    // ---------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that the default format renders the ISO code followed by the amount at the currency's natural
    /// precision across the 0 / 2 / 3 minor-unit categories under the invariant culture.
    /// </summary>
    [TestMethod]
    [DataRow("USD", 0.0, "USD 0.00")]
    [DataRow("USD", 19.99, "USD 19.99")]
    [DataRow("USD", -19.99, "USD -19.99")]
    [DataRow("USD", 1234567.89, "USD 1,234,567.89")]
    [DataRow("EUR", 100.0, "EUR 100.00")]
    [DataRow("GBP", 0.01, "GBP 0.01")]
    [DataRow("JPY", 0.0, "JPY 0")]
    [DataRow("JPY", 1234.0, "JPY 1,234")]
    [DataRow("JPY", -1234.0, "JPY -1,234")]
    [DataRow("KRW", 1000000.0, "KRW 1,000,000")]
    [DataRow("CLP", 50000.0, "CLP 50,000")]
    [DataRow("BHD", 0.001, "BHD 0.001")]
    [DataRow("BHD", 12.345, "BHD 12.345")]
    [DataRow("BHD", -1.5, "BHD -1.500")]
    [DataRow("KWD", 100.0, "KWD 100.000")]
    [DataRow("OMR", 12.345, "OMR 12.345")]
    public void ToString_WhenDefaultFormatAndInvariantCulture_ShouldMatchExpected(string iso, double amount, string expected)
    {
        var actual = FormatViaReflection(iso, (decimal)amount, format: null, CultureInfo.InvariantCulture);

        Assert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Verifies that the default format honors each culture's grouping and decimal separators while keeping the
    /// ISO code unchanged, across a small set of ASCII-digit cultures whose CLDR data is stable.
    /// </summary>
    [TestMethod]
    [DataRow("USD", "en-US", 1234567.89, "USD 1,234,567.89")]
    [DataRow("EUR", "de-DE", 1234567.89, "EUR 1.234.567,89")]
    [DataRow("EUR", "nl-NL", 1234.56, "EUR 1.234,56")]
    [DataRow("JPY", "ja-JP", 1234567.0, "JPY 1,234,567")]
    public void ToString_WhenDefaultFormatAndExplicitCulture_ShouldRespectCultureGroupingAndDecimal(string iso, string cultureName, double amount, string expected)
    {
        var actual = FormatViaReflection(iso, (decimal)amount, format: null, new CultureInfo(cultureName));

        Assert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Verifies that the default format composes the ISO code with the BCL's <c>"N"</c> output for any culture,
    /// including cultures that use non-ASCII digits or alternate group separators. Asserting equality against the
    /// BCL's own <c>"N"</c> output keeps the test stable across CLDR data revisions.
    /// </summary>
    [TestMethod]
    [DataRow("USD", "fr-FR", 1234.56)]
    [DataRow("USD", "fr-FR", 1234567.89)]
    [DataRow("USD", "ar-BH", 1234.56)]
    [DataRow("BHD", "ar-BH", 12.345)]
    [DataRow("JPY", "ar-BH", 1234.0)]
    public void ToString_WhenDefaultFormatAndCultureHasNonAsciiDigitsOrSeparators_ShouldComposeIsoPlusBclNumberOutput(string iso, string cultureName, double amount)
    {
        var culture = new CultureInfo(cultureName);
        var minorUnits = GetMinorUnits(iso);
        var rounded = decimal.Round((decimal)amount, minorUnits, MidpointRounding.ToEven);
        var expected = string.Concat(iso, " ", rounded.ToString("N" + minorUnits.ToString(CultureInfo.InvariantCulture), culture));

        var actual = FormatViaReflection(iso, (decimal)amount, format: null, culture);

        Assert.AreEqual(expected, actual);
    }

    // ---------------------------------------------------------------------------------------------------------------
    // C specifier — matched cultures (region currency == TCurrency). Output equals the BCL's native "C" format.
    // ---------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that the <c>"C"</c> specifier produces the same output as the BCL's native <c>"C"</c> format when
    /// the culture's region currency matches <typeparamref name="TCurrency" />, with the currency's minor-unit
    /// precision substituted for the culture's default.
    /// </summary>
    [TestMethod]
    [DataRow("USD", "en-US", 1234.56)]
    [DataRow("USD", "en-US", 0.01)]
    [DataRow("USD", "en-US", -19.99)]
    [DataRow("USD", "en-US", 0.0)]
    [DataRow("GBP", "en-GB", 1234.56)]
    [DataRow("GBP", "en-GB", -1234.56)]
    [DataRow("EUR", "de-DE", 1234.56)]
    [DataRow("EUR", "fr-FR", 1234.56)]
    [DataRow("EUR", "nl-NL", 1234.56)]
    [DataRow("EUR", "es-ES", 1234.56)]
    [DataRow("JPY", "ja-JP", 1234.0)]
    [DataRow("JPY", "ja-JP", -1234.0)]
    [DataRow("AUD", "en-AU", 1234.56)]
    [DataRow("CAD", "en-CA", 1234.56)]
    [DataRow("CHF", "de-CH", 1234.56)]
    [DataRow("CNY", "zh-CN", 1234.56)]
    [DataRow("SEK", "sv-SE", 1234.56)]
    [DataRow("DKK", "da-DK", 1234.56)]
    [DataRow("KRW", "ko-KR", 50000.0)]
    [DataRow("BHD", "ar-BH", 12.345)]
    public void ToString_WhenCSpecifierAndCultureMatches_ShouldEqualBclNativeCurrencyFormat(string iso, string cultureName, double amount)
    {
        var culture = new CultureInfo(cultureName);
        var minorUnits = GetMinorUnits(iso);
        var rounded = decimal.Round((decimal)amount, minorUnits, MidpointRounding.ToEven);
        var nfi = (NumberFormatInfo)culture.NumberFormat.Clone();
        nfi.CurrencyDecimalDigits = minorUnits;
        var expected = rounded.ToString("C", nfi);

        var actual = FormatViaReflection(iso, (decimal)amount, "C", culture);

        Assert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Verifies a small set of hardcoded outputs for the <c>"C"</c> specifier on stable ASCII-digit cultures so
    /// future drift in CLDR data is caught for the common cases.
    /// </summary>
    [TestMethod]
    [DataRow("USD", "en-US", 1234.56, "$1,234.56")]
    [DataRow("USD", "en-US", 0.0, "$0.00")]
    [DataRow("GBP", "en-GB", 1234.56, "£1,234.56")]
    public void ToString_WhenCSpecifierAndCultureMatches_ShouldMatchHardcodedExpected(string iso, string cultureName, double amount, string expected)
    {
        var actual = FormatViaReflection(iso, (decimal)amount, "C", new CultureInfo(cultureName));

        Assert.AreEqual(expected, actual);
    }

    // ---------------------------------------------------------------------------------------------------------------
    // C specifier — mismatched cultures. Output embeds ISO in the locale's CurrencyPositivePattern slot.
    // ---------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that the <c>"C"</c> specifier embeds the ISO code in the culture's currency-position slot when
    /// the culture's region currency does not match <typeparamref name="TCurrency" />.
    /// </summary>
    [TestMethod]
    [DataRow("USD", "de-DE", 1234.56)]
    [DataRow("USD", "fr-FR", 1234.56)]
    [DataRow("USD", "ja-JP", 1234.56)]
    [DataRow("JPY", "en-US", 1234.0)]
    [DataRow("JPY", "de-DE", 1234.0)]
    [DataRow("EUR", "en-US", 1234.56)]
    [DataRow("EUR", "ja-JP", 1234.56)]
    [DataRow("GBP", "de-DE", 1234.56)]
    [DataRow("BHD", "en-US", 12.345)]
    [DataRow("KWD", "de-DE", 100.0)]
    public void ToString_WhenCSpecifierAndCultureMismatched_ShouldEmbedIsoInLocalePositionSlot(string iso, string cultureName, double amount)
    {
        var culture = new CultureInfo(cultureName);
        var minorUnits = GetMinorUnits(iso);
        var rounded = decimal.Round((decimal)amount, minorUnits, MidpointRounding.ToEven);
        var numberPart = rounded.ToString("N" + minorUnits.ToString(CultureInfo.InvariantCulture), culture);
        var isoBeforeAmount = culture.NumberFormat.CurrencyPositivePattern is 0 or 2;
        var expected = isoBeforeAmount
            ? string.Concat(iso, " ", numberPart)
            : string.Concat(numberPart, " ", iso);

        var actual = FormatViaReflection(iso, (decimal)amount, "C", culture);

        Assert.AreEqual(expected, actual);
    }

    // ---------------------------------------------------------------------------------------------------------------
    // ~ prefix — matched culture elides the designator; mismatched keeps it.
    // ---------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that the <c>"~"</c> prefix on <c>C</c> or <c>G</c> emits the bare number when the culture's
    /// currency matches <typeparamref name="TCurrency" />.
    /// </summary>
    [TestMethod]
    [DataRow("USD", "en-US", "~C", 1234.56, "1,234.56")]
    [DataRow("USD", "en-US", "~G", 1234.56, "1,234.56")]
    [DataRow("USD", "en-US", "~C", 0.0, "0.00")]
    [DataRow("USD", "en-US", "~C", -19.99, "-19.99")]
    [DataRow("GBP", "en-GB", "~C", 999.5, "999.50")]
    [DataRow("EUR", "de-DE", "~C", 1234.56, "1.234,56")]
    [DataRow("JPY", "ja-JP", "~C", 1234.0, "1,234")]
    public void ToString_WhenTildePrefixAndCultureMatches_ShouldEmitBareNumber(string iso, string cultureName, string format, double amount, string expected)
    {
        var actual = FormatViaReflection(iso, (decimal)amount, format, new CultureInfo(cultureName));

        Assert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Verifies that the <c>"~"</c> prefix on a matched culture produces the same numeric output as the bare
    /// <c>"N"</c> format under that culture, including cultures with non-ASCII digits where hardcoded expected
    /// values would be fragile.
    /// </summary>
    [TestMethod]
    [DataRow("EUR", "fr-FR", "~C", 1234.56)]
    [DataRow("BHD", "ar-BH", "~C", 12.345)]
    public void ToString_WhenTildePrefixAndMatchedNonAsciiCulture_ShouldEqualBclNNumberOutput(string iso, string cultureName, string format, double amount)
    {
        var culture = new CultureInfo(cultureName);
        var minorUnits = GetMinorUnits(iso);
        var rounded = decimal.Round((decimal)amount, minorUnits, MidpointRounding.ToEven);
        var expected = rounded.ToString("N" + minorUnits.ToString(CultureInfo.InvariantCulture), culture);

        var actual = FormatViaReflection(iso, (decimal)amount, format, culture);

        Assert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Verifies that the <c>"~"</c> prefix preserves the ISO designator when the culture's currency does not
    /// match <typeparamref name="TCurrency" /> so cross-currency values remain unambiguous.
    /// </summary>
    [TestMethod]
    [DataRow("JPY", "en-US", "~G", 1234.0, "JPY 1,234")]
    [DataRow("USD", "de-DE", "~G", 1234.56, "USD 1.234,56")]
    [DataRow("USD", "ja-JP", "~G", 1234.56, "USD 1,234.56")]
    [DataRow("EUR", "en-US", "~G", 100.0, "EUR 100.00")]
    [DataRow("JPY", "en-US", "~C", 1234.0, "JPY 1,234")]
    [DataRow("USD", "de-DE", "~C", 1234.56, "1.234,56 USD")]
    public void ToString_WhenTildePrefixAndCultureMismatched_ShouldKeepIsoDesignator(string iso, string cultureName, string format, double amount, string expected)
    {
        var actual = FormatViaReflection(iso, (decimal)amount, format, new CultureInfo(cultureName));

        Assert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Verifies that the <c>"~"</c> prefix on the designator-less specifiers (<c>N</c>, <c>F</c>, <c>D</c>) is a
    /// no-op and produces the same output as the unprefixed form.
    /// </summary>
    [TestMethod]
    [DataRow("USD", "en-US", "N", "~N", 1234.56)]
    [DataRow("USD", "en-US", "F", "~F", 1234.56)]
    [DataRow("USD", "en-US", "D", "~D", 1234.56)]
    [DataRow("JPY", "en-US", "N", "~N", 1234.0)]
    [DataRow("EUR", "de-DE", "N", "~N", 1234.56)]
    public void ToString_WhenTildePrefixOnDesignatorlessSpecifier_ShouldHaveNoEffect(string iso, string cultureName, string unprefixed, string prefixed, double amount)
    {
        var culture = new CultureInfo(cultureName);

        var a = FormatViaReflection(iso, (decimal)amount, unprefixed, culture);
        var b = FormatViaReflection(iso, (decimal)amount, prefixed, culture);

        Assert.AreEqual(a, b);
    }

    // ---------------------------------------------------------------------------------------------------------------
    // Explicit precision suffix — must override both the currency's MinorUnits and the culture's CurrencyDecimalDigits.
    // ---------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that explicit precision suffixes override the currency's natural precision across all specifiers.
    /// </summary>
    [TestMethod]
    [DataRow("USD", "C0", 19.99, "USD 20")]
    [DataRow("USD", "C1", 19.99, "USD 20.0")]
    [DataRow("USD", "C2", 19.99, "USD 19.99")]
    [DataRow("USD", "C4", 19.99, "USD 19.9900")]
    [DataRow("USD", "G0", 19.99, "USD 20")]
    [DataRow("JPY", "C2", 1234.0, "JPY 1,234.00")]
    [DataRow("BHD", "C0", 12.345, "BHD 12")]
    [DataRow("BHD", "C5", 12.345, "BHD 12.34500")]
    [DataRow("USD", "N0", 19.99, "20")]
    [DataRow("USD", "N4", 19.99, "19.9900")]
    [DataRow("USD", "F0", 19.99, "20")]
    [DataRow("USD", "F4", 19.99, "19.9900")]
    public void ToString_WhenExplicitPrecisionAndInvariantCulture_ShouldOverrideNaturalPrecision(string iso, string format, double amount, string expected)
    {
        var actual = FormatViaReflection(iso, (decimal)amount, format, CultureInfo.InvariantCulture);

        Assert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Verifies that explicit precision combines correctly with the <c>"C"</c> specifier under matched cultures
    /// whose CLDR data is stable (ASCII digits, fixed symbol).
    /// </summary>
    [TestMethod]
    [DataRow("USD", "en-US", "C0", 19.99, "$20")]
    [DataRow("USD", "en-US", "C4", 19.99, "$19.9900")]
    [DataRow("USD", "en-US", "~C0", 19.99, "20")]
    [DataRow("USD", "en-US", "~C4", 19.99, "19.9900")]
    [DataRow("GBP", "en-GB", "C0", 1234.0, "£1,234")]
    public void ToString_WhenCSpecifierWithExplicitPrecisionAndMatched_ShouldRespectPrecision(string iso, string cultureName, string format, double amount, string expected)
    {
        var actual = FormatViaReflection(iso, (decimal)amount, format, new CultureInfo(cultureName));

        Assert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Verifies that leading zeros in the precision suffix are accepted as the same value (e.g. <c>"C02"</c>
    /// equals <c>"C2"</c>).
    /// </summary>
    [TestMethod]
    public void ToString_WhenPrecisionHasLeadingZero_ShouldAcceptIt()
    {
        var a = FormatViaReflection("USD", 19.99m, "C2", CultureInfo.InvariantCulture);
        var b = FormatViaReflection("USD", 19.99m, "C02", CultureInfo.InvariantCulture);

        Assert.AreEqual(a, b);
    }

    // ---------------------------------------------------------------------------------------------------------------
    // Bare ~ — equivalent to ~G (elide-if-matched default).
    // ---------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that a format consisting only of the <c>"~"</c> prefix is treated as <c>"~G"</c>.
    /// </summary>
    [TestMethod]
    [DataRow("USD", "en-US", 1234.56, "1,234.56")]
    [DataRow("JPY", "en-US", 1234.0, "JPY 1,234")]
    [DataRow("EUR", "de-DE", 1234.56, "1.234,56")]
    [DataRow("EUR", "en-US", 1234.56, "EUR 1,234.56")]
    public void ToString_WhenFormatIsBareTilde_ShouldBehaveAsTildeG(string iso, string cultureName, double amount, string expected)
    {
        var actual = FormatViaReflection(iso, (decimal)amount, "~", new CultureInfo(cultureName));

        Assert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Verifies that an empty format string is treated as the default <c>"G"</c>.
    /// </summary>
    [TestMethod]
    [DataRow("USD", "en-US", 1234.56, "USD 1,234.56")]
    [DataRow("JPY", "ja-JP", 1234.0, "JPY 1,234")]
    public void ToString_WhenFormatIsEmptyString_ShouldBehaveAsDefault(string iso, string cultureName, double amount, string expected)
    {
        var actual = FormatViaReflection(iso, (decimal)amount, string.Empty, new CultureInfo(cultureName));

        Assert.AreEqual(expected, actual);
    }

    // ---------------------------------------------------------------------------------------------------------------
    // Neutral cultures and InvariantCulture — region behavior is platform-dependent for most neutral cultures,
    // so we assert only on InvariantCulture (always truly region-less) and a bare NumberFormatInfo (never matches).
    // ---------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that the invariant culture never matches any currency, so <c>"C"</c> falls back to the
    /// ISO-substitution form and <c>"~C"</c> retains the ISO code.
    /// </summary>
    [TestMethod]
    [DataRow("USD", "C", 1234.56, "USD 1,234.56")]
    [DataRow("USD", "~C", 1234.56, "USD 1,234.56")]
    [DataRow("JPY", "C", 1234.0, "JPY 1,234")]
    [DataRow("BHD", "C", 12.345, "BHD 12.345")]
    public void ToString_WhenInvariantCulture_ShouldNeverMatchCurrency(string iso, string format, double amount, string expected)
    {
        var actual = FormatViaReflection(iso, (decimal)amount, format, CultureInfo.InvariantCulture);

        Assert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Verifies that a bare <see cref="NumberFormatInfo" /> provider — which carries no region context — also
    /// falls back to the ISO-substitution form.
    /// </summary>
    [TestMethod]
    public void ToString_WhenProviderIsBareNumberFormatInfo_ShouldFallBackToIsoSubstitution()
    {
        var nfi = (NumberFormatInfo)new CultureInfo("en-US").NumberFormat.Clone();

        var actual = FormatViaReflection("USD", 1234.56m, "C", nfi);

        Assert.AreEqual("USD 1,234.56", actual);
    }

    // ---------------------------------------------------------------------------------------------------------------
    // Edge cases — boundary amounts.
    // ---------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that zero amounts format consistently across every specifier.
    /// </summary>
    [TestMethod]
    [DataRow("USD", "G", "USD 0.00")]
    [DataRow("USD", "N", "0.00")]
    [DataRow("USD", "F", "0.00")]
    [DataRow("USD", "D", "0.00")]
    [DataRow("USD", "G0", "USD 0")]
    [DataRow("JPY", "G", "JPY 0")]
    [DataRow("BHD", "G", "BHD 0.000")]
    public void ToString_WhenAmountIsZero_ShouldRenderAsZeroWithCurrencyPrecision(string iso, string format, string expected)
    {
        var actual = FormatViaReflection(iso, 0m, format, CultureInfo.InvariantCulture);

        Assert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Verifies that very large amounts format without overflow at typical financial magnitudes.
    /// </summary>
    [TestMethod]
    [DataRow("USD", 1000000000000.0, "USD 1,000,000,000,000.00")]
    [DataRow("JPY", 9876543210.0, "JPY 9,876,543,210")]
    public void ToString_WhenAmountIsVeryLarge_ShouldFormatWithoutTruncation(string iso, double amount, string expected)
    {
        var actual = FormatViaReflection(iso, (decimal)amount, format: null, CultureInfo.InvariantCulture);

        Assert.AreEqual(expected, actual);
    }

    /// <summary>
    /// Verifies that negative amounts render with the appropriate negative pattern under each format.
    /// </summary>
    [TestMethod]
    [DataRow("USD", "N", "-1,234.56")]
    [DataRow("USD", "F", "-1234.56")]
    [DataRow("USD", "G", "USD -1,234.56")]
    public void ToString_WhenAmountIsNegativeAndInvariantCulture_ShouldRespectFormatNegativeStyle(string iso, string format, string expected)
    {
        var actual = FormatViaReflection(iso, -1234.56m, format, CultureInfo.InvariantCulture);

        Assert.AreEqual(expected, actual);
    }

    // ---------------------------------------------------------------------------------------------------------------
    // Edge cases — invalid format strings.
    // ---------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that an unsupported specifier throws <see cref="FormatException" />.
    /// </summary>
    [TestMethod]
    [DataRow("Z")]
    [DataRow("X")]
    [DataRow("P")]
    [DataRow("E")]
    [DataRow("~Z")]
    [DataRow("~~")]
    public void ToString_WhenSpecifierUnsupported_ShouldThrowFormatException(string format)
    {
        var money = new Money<USD>(1m);

        Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = money.ToString(format, CultureInfo.InvariantCulture);
        });
    }

    /// <summary>
    /// Verifies that a malformed precision suffix throws <see cref="FormatException" />.
    /// </summary>
    [TestMethod]
    [DataRow("C-1")]
    [DataRow("C+1")]
    [DataRow("C2.5")]
    [DataRow("C 2")]
    [DataRow("Cx")]
    [DataRow("C2x")]
    [DataRow("~C-1")]
    public void ToString_WhenPrecisionMalformed_ShouldThrowFormatException(string format)
    {
        var money = new Money<USD>(1m);

        Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = money.ToString(format, CultureInfo.InvariantCulture);
        });
    }

    /// <summary>
    /// Verifies that very high precision counts that exceed <see cref="decimal" />'s native scale produce a
    /// zero-padded output rather than throwing.
    /// </summary>
    [TestMethod]
    public void ToString_WhenPrecisionExceedsDecimalScale_ShouldPadWithZeros()
    {
        var money = new Money<USD>(1.5m);

        var actual = money.ToString("F12", CultureInfo.InvariantCulture);

        Assert.AreEqual("1.500000000000", actual);
    }

    // ---------------------------------------------------------------------------------------------------------------
    // Round-trip — Parse must recover any value formatted with default / C / G / N / F / D specifiers.
    // ---------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that the default and ISO-prefixed forms round-trip cleanly through
    /// <see cref="Money{TCurrency}.Parse(string, IFormatProvider?)" />.
    /// </summary>
    [TestMethod]
    [DataRow("USD", 1234.56)]
    [DataRow("USD", 0.0)]
    [DataRow("USD", -19.99)]
    [DataRow("JPY", 1234.0)]
    [DataRow("BHD", 12.345)]
    public void RoundTrip_WhenDefaultFormatAndInvariantCulture_ShouldRecoverOriginal(string iso, double amount)
    {
        var value = (decimal)amount;
        Type currencyType = ResolveCurrencyType(iso);
        Type moneyType = typeof(Money<>).MakeGenericType(currencyType);
        var original = Activator.CreateInstance(moneyType, value)!;

        var text = (string)moneyType
            .GetMethod("ToString", [typeof(string), typeof(IFormatProvider)])!
            .Invoke(original, [null, CultureInfo.InvariantCulture])!;

        var recovered = moneyType
            .GetMethod("Parse", [typeof(string), typeof(IFormatProvider)])!
            .Invoke(null, [text, CultureInfo.InvariantCulture])!;

        Assert.AreEqual(original, recovered);
    }

    // ---------------------------------------------------------------------------------------------------------------
    // Helpers — reflective dispatch into Money<TCurrency> from a string ISO code so [DataRow] can drive the matrix.
    // ---------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Resolves a currency tag type from its ISO 4217 alphabetic code by reflecting into the shipped catalogue.
    /// </summary>
    /// <param name="iso">The three-letter ISO code.</param>
    /// <returns>The matching <see cref="ICurrency" /> tag type.</returns>
    private static Type ResolveCurrencyType(string iso) =>
        typeof(USD).Assembly.GetType($"Bodu.Financial.Currencies.{iso}")
            ?? throw new InvalidOperationException($"Currency '{iso}' is not in the shipped catalogue.");

    /// <summary>
    /// Returns the minor-unit precision of the currency identified by <paramref name="iso" />.
    /// </summary>
    /// <param name="iso">The three-letter ISO code.</param>
    /// <returns>The currency's <see cref="ICurrency.MinorUnits" /> value.</returns>
    private static int GetMinorUnits(string iso) =>
        (int)ResolveCurrencyType(iso)
            .GetProperty("MinorUnits", BindingFlags.Public | BindingFlags.Static)!
            .GetValue(null)!;

    /// <summary>
    /// Builds a <c>Money&lt;TCurrency&gt;</c> for the supplied ISO code and formats it through the public
    /// <see cref="Money{TCurrency}.ToString(string?, IFormatProvider?)" /> overload.
    /// </summary>
    /// <param name="iso">The three-letter ISO code of the currency to instantiate.</param>
    /// <param name="amount">The amount to construct the value from.</param>
    /// <param name="format">The format specifier passed to <c>ToString</c>.</param>
    /// <param name="provider">The format provider passed to <c>ToString</c>.</param>
    /// <returns>The formatted string the <c>Money</c> instance produces.</returns>
    private static string FormatViaReflection(string iso, decimal amount, string? format, IFormatProvider? provider)
    {
        Type currencyType = ResolveCurrencyType(iso);
        Type moneyType = typeof(Money<>).MakeGenericType(currencyType);
        var money = Activator.CreateInstance(moneyType, amount)!;
        return (string)moneyType
            .GetMethod("ToString", [typeof(string), typeof(IFormatProvider)])!
            .Invoke(money, [format, provider])!;
    }
}
