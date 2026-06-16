// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyTests.Formatting.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Financial;

/// <summary>
/// Verifies that <see cref="Money" /> formatting mirrors the <see cref="Money{TCurrency}" /> specifier vocabulary
/// (<c>G</c>, <c>C</c>, <c>L</c>, <c>R</c>, <c>N</c>, <c>F</c>, <c>D</c>) and supports the <c>~</c> prefix and
/// precision-suffix forms uniformly across the typed and runtime-tagged surfaces.
/// </summary>
public partial class MoneyTests
{
    /// <summary>
    /// Verifies that the <c>"C"</c> specifier emits the culture's native currency format when the culture's region
    /// currency matches the <see cref="Money" />'s ISO code.
    /// </summary>
    [TestMethod]
    public void ToString_WhenCSpecifierAndCultureMatches_ShouldUseCultureNativeSymbol()
    {
        var money = new Money(1234.56m, "USD");

        string actual = money.ToString("C", new CultureInfo("en-US"));

        Assert.AreEqual("$1,234.56", actual);
    }

    /// <summary>
    /// Verifies that the <c>"C"</c> specifier falls back to ISO substitution in the culture's currency-position slot
    /// when the culture's region currency does not match.
    /// </summary>
    [TestMethod]
    public void ToString_WhenCSpecifierAndCultureMismatched_ShouldSubstituteIsoCode()
    {
        var money = new Money(1234.56m, "USD");

        string actual = money.ToString("C", new CultureInfo("de-DE"));

        Assert.AreEqual("1.234,56 USD", actual);
    }

    /// <summary>
    /// Verifies that the <c>"L"</c> specifier appends the English name from <see cref="CurrencyRegistry" />.
    /// </summary>
    [TestMethod]
    public void ToString_WhenLSpecifier_ShouldAppendEnglishNameFromRegistry()
    {
        var money = new Money(1234.56m, "USD");

        string actual = money.ToString("L", CultureInfo.InvariantCulture);

        Assert.AreEqual("1,234.56 US Dollar", actual);
    }

    /// <summary>
    /// Verifies that the <c>"L"</c> specifier falls back to the ISO-code form when the runtime-tagged currency is
    /// not registered in <see cref="CurrencyRegistry" />. Unregistered currencies report zero minor units, so an
    /// integer amount is used here to keep the assertion independent of registry-driven rounding.
    /// </summary>
    [TestMethod]
    public void ToString_WhenLSpecifierAndCurrencyNotRegistered_ShouldFallBackToIsoForm()
    {
        var money = Money.FromUnchecked(1234m, "ZZZ", 0);

        string actual = money.ToString("L", CultureInfo.InvariantCulture);

        Assert.AreEqual("ZZZ 1,234", actual);
    }

    /// <summary>
    /// Verifies that the <c>"R"</c> specifier emits the invariant round-trip form for <see cref="Money" />.
    /// </summary>
    [TestMethod]
    public void ToString_WhenRSpecifier_ShouldEmitInvariantRoundTripForm()
    {
        var money = new Money(1234.56m, "USD");

        string actual = money.ToString("R", new CultureInfo("de-DE"));

        Assert.AreEqual("USD 1234.56", actual);
    }

    /// <summary>
    /// Verifies that the <c>"R"</c> output round-trips through
    /// <see cref="Money.Parse(string, IFormatProvider?)" /> under <see cref="CultureInfo.InvariantCulture" />.
    /// </summary>
    [TestMethod]
    [DataRow("USD", 1234.56)]
    [DataRow("USD", -19.99)]
    [DataRow("JPY", 1234.0)]
    [DataRow("BHD", 12.345)]
    public void ToString_WhenRSpecifier_ShouldRoundTripThroughParse(string iso, double amount)
    {
        var original = new Money((decimal)amount, iso);

        string text = original.ToString("R", CultureInfo.InvariantCulture);
        var recovered = Money.Parse(text, CultureInfo.InvariantCulture);

        Assert.AreEqual(original, recovered);
    }

    /// <summary>
    /// Verifies that the <c>"~R"</c> specifier is rejected on <see cref="Money" /> for the same reason as on
    /// the typed surface.
    /// </summary>
    [TestMethod]
    public void ToString_WhenTildeRSpecifier_ShouldThrowFormatException()
    {
        var money = new Money(1m, "USD");

        Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = money.ToString("~R", CultureInfo.InvariantCulture);
        });
    }

    /// <summary>
    /// Verifies that the <c>"~"</c> prefix on <c>"C"</c> elides the symbol when the culture matches.
    /// </summary>
    [TestMethod]
    public void ToString_WhenTildeCAndCultureMatches_ShouldElideSymbol()
    {
        var money = new Money(19.99m, "USD");

        string actual = money.ToString("~C", new CultureInfo("en-US"));

        Assert.AreEqual("19.99", actual);
    }

    /// <summary>
    /// Verifies that the single-argument <see cref="Money.ToString(string?)" /> overload formats using the current
    /// culture.
    /// </summary>
    [TestMethod]
    public void ToString_WithFormatOnly_ShouldUseCurrentCulture()
    {
        var money = new Money(1234.56m, "USD");

        Assert.AreEqual(money.ToString("N", CultureInfo.CurrentCulture), money.ToString("N"));
    }

    /// <summary>
    /// Verifies that the <c>"~G"</c> and <c>"~L"</c> elision prefixes drop the currency qualifier when the culture's
    /// region currency matches the amount's ISO code.
    /// </summary>
    [TestMethod]
    public void ToString_WhenTildeGOrTildeLAndCultureMatches_ShouldElideQualifier()
    {
        var money = new Money(19.99m, "USD");
        var enUs = new CultureInfo("en-US");

        Assert.AreEqual("19.99", money.ToString("~G", enUs));
        Assert.AreEqual("19.99", money.ToString("~L", enUs));
    }

    /// <summary>
    /// Verifies that the <c>"F"</c> and <c>"D"</c> specifiers render the fixed-point amount without grouping or a
    /// currency qualifier.
    /// </summary>
    [TestMethod]
    public void ToString_WhenFOrDSpecifier_ShouldRenderFixedPointAmount()
    {
        var money = new Money(1234.56m, "USD");

        Assert.AreEqual("1234.56", money.ToString("F", CultureInfo.InvariantCulture));
        Assert.AreEqual("1234.56", money.ToString("D", CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// Verifies that a malformed precision suffix is rejected with a <see cref="FormatException" />.
    /// </summary>
    [TestMethod]
    public void ToString_WhenPrecisionSuffixIsMalformed_ShouldThrowFormatException() =>
        Assert.ThrowsExactly<FormatException>(() => _ = new Money(1m, "USD").ToString("Nz", CultureInfo.InvariantCulture));

    /// <summary>
    /// Verifies that the <c>"C"</c> specifier substitutes the ISO code for a negative mismatched-currency amount,
    /// honouring the locale's negative-currency pattern.
    /// </summary>
    [TestMethod]
    public void ToString_WhenCSpecifierMismatchedAndNegative_ShouldSubstituteIsoCode()
    {
        var money = new Money(-1234.56m, "USD");

        string actual = money.ToString("C", new CultureInfo("de-DE"));

        StringAssert.Contains(actual, "USD");
        StringAssert.Contains(actual, "1.234,56");
    }

    /// <summary>
    /// Verifies that <see cref="Money.TryFormat(Span{char}, out int, ReadOnlySpan{char}, IFormatProvider?)" /> writes
    /// the formatted value when the destination is large enough and reports failure when it is too small.
    /// </summary>
    [TestMethod]
    public void TryFormat_Char_ShouldWriteWhenLargeEnoughAndFailWhenTooSmall()
    {
        var money = new Money(1234.56m, "USD");

        Span<char> large = stackalloc char[32];
        Assert.IsTrue(money.TryFormat(large, out int written, "N", CultureInfo.InvariantCulture));
        Assert.AreEqual("1,234.56", large[..written].ToString());

        Span<char> tiny = stackalloc char[2];
        Assert.IsFalse(money.TryFormat(tiny, out int none, "N", CultureInfo.InvariantCulture));
        Assert.AreEqual(0, none);
    }

    /// <summary>
    /// Verifies that <see cref="Money.TryFormat(Span{byte}, out int, ReadOnlySpan{char}, IFormatProvider?)" /> writes
    /// the formatted value as UTF-8 bytes.
    /// </summary>
    [TestMethod]
    public void TryFormat_Utf8_ShouldWriteFormattedBytes()
    {
        var money = new Money(1234.56m, "USD");

        Span<byte> buffer = stackalloc byte[32];
        Assert.IsTrue(money.TryFormat(buffer, out int written, "N", CultureInfo.InvariantCulture));
        Assert.AreEqual("1,234.56", System.Text.Encoding.UTF8.GetString(buffer[..written]));
    }
}
