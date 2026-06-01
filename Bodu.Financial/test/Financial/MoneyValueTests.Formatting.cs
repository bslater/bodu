// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyValueTests.Formatting.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Financial;

/// <summary>
/// Verifies that <see cref="MoneyValue" /> formatting mirrors the <see cref="Money{TCurrency}" /> specifier vocabulary
/// (<c>G</c>, <c>C</c>, <c>L</c>, <c>R</c>, <c>N</c>, <c>F</c>, <c>D</c>) and supports the <c>~</c> prefix and
/// precision-suffix forms uniformly across the typed and runtime-tagged surfaces.
/// </summary>
public partial class MoneyValueTests
{
    /// <summary>
    /// Verifies that the <c>"C"</c> specifier emits the culture's native currency format when the culture's region
    /// currency matches the <see cref="MoneyValue" />'s ISO code.
    /// </summary>
    [TestMethod]
    public void ToString_WhenCSpecifierAndCultureMatches_ShouldUseCultureNativeSymbol()
    {
        var money = new MoneyValue(1234.56m, "USD");

        var actual = money.ToString("C", new CultureInfo("en-US"));

        Assert.AreEqual("$1,234.56", actual);
    }

    /// <summary>
    /// Verifies that the <c>"C"</c> specifier falls back to ISO substitution in the culture's currency-position slot
    /// when the culture's region currency does not match.
    /// </summary>
    [TestMethod]
    public void ToString_WhenCSpecifierAndCultureMismatched_ShouldSubstituteIsoCode()
    {
        var money = new MoneyValue(1234.56m, "USD");

        var actual = money.ToString("C", new CultureInfo("de-DE"));

        Assert.AreEqual("1.234,56 USD", actual);
    }

    /// <summary>
    /// Verifies that the <c>"L"</c> specifier appends the English name from <see cref="CurrencyRegistry" />.
    /// </summary>
    [TestMethod]
    public void ToString_WhenLSpecifier_ShouldAppendEnglishNameFromRegistry()
    {
        var money = new MoneyValue(1234.56m, "USD");

        var actual = money.ToString("L", CultureInfo.InvariantCulture);

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
        var money = new MoneyValue(1234m, "ZZZ");

        var actual = money.ToString("L", CultureInfo.InvariantCulture);

        Assert.AreEqual("ZZZ 1,234", actual);
    }

    /// <summary>
    /// Verifies that the <c>"R"</c> specifier emits the invariant round-trip form for <see cref="MoneyValue" />.
    /// </summary>
    [TestMethod]
    public void ToString_WhenRSpecifier_ShouldEmitInvariantRoundTripForm()
    {
        var money = new MoneyValue(1234.56m, "USD");

        var actual = money.ToString("R", new CultureInfo("de-DE"));

        Assert.AreEqual("USD 1234.56", actual);
    }

    /// <summary>
    /// Verifies that the <c>"R"</c> output round-trips through
    /// <see cref="MoneyValue.Parse(string, IFormatProvider?)" /> under <see cref="CultureInfo.InvariantCulture" />.
    /// </summary>
    [TestMethod]
    [DataRow("USD", 1234.56)]
    [DataRow("USD", -19.99)]
    [DataRow("JPY", 1234.0)]
    [DataRow("BHD", 12.345)]
    public void ToString_WhenRSpecifier_ShouldRoundTripThroughParse(string iso, double amount)
    {
        var original = new MoneyValue((decimal)amount, iso);

        var text = original.ToString("R", CultureInfo.InvariantCulture);
        var recovered = MoneyValue.Parse(text, CultureInfo.InvariantCulture);

        Assert.AreEqual(original, recovered);
    }

    /// <summary>
    /// Verifies that the <c>"~R"</c> specifier is rejected on <see cref="MoneyValue" /> for the same reason as on
    /// the typed surface.
    /// </summary>
    [TestMethod]
    public void ToString_WhenTildeRSpecifier_ShouldThrowFormatException()
    {
        var money = new MoneyValue(1m, "USD");

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
        var money = new MoneyValue(19.99m, "USD");

        var actual = money.ToString("~C", new CultureInfo("en-US"));

        Assert.AreEqual("19.99", actual);
    }
}
