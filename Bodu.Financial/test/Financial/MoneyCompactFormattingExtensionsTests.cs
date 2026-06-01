// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyCompactFormattingExtensionsTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Financial.Currencies;

namespace Bodu.Financial;

/// <summary>
/// Verifies that <see cref="MoneyCompactFormattingExtensions" /> applies magnitude suffixes (K, M, B, T) in the
/// correct culture position for each format specifier and rejects round-trip and out-of-range inputs.
/// </summary>
[TestClass]
public class MoneyCompactFormattingExtensionsTests
{
    /// <summary>
    /// Verifies that amounts below one thousand keep their natural precision with no magnitude suffix.
    /// </summary>
    [TestMethod]
    public void ToCompactString_WhenAmountBelowThousand_ShouldOmitMagnitudeSuffix()
    {
        var money = new Money<USD>(500m);

        var actual = money.ToCompactString("C", new CultureInfo("en-US"));

        Assert.AreEqual("$500.00", actual);
    }

    /// <summary>
    /// Verifies that amounts at least one thousand are scaled and given the <c>K</c> suffix.
    /// </summary>
    [TestMethod]
    public void ToCompactString_WhenAmountInThousands_ShouldUseKSuffix()
    {
        var money = new Money<USD>(1234.56m);

        var actual = money.ToCompactString("C", new CultureInfo("en-US"));

        Assert.AreEqual("$1.2K", actual);
    }

    /// <summary>
    /// Verifies that amounts at least one million are scaled and given the <c>M</c> suffix.
    /// </summary>
    [TestMethod]
    public void ToCompactString_WhenAmountInMillions_ShouldUseMSuffix()
    {
        var money = new Money<USD>(2_500_000m);

        var actual = money.ToCompactString("C", new CultureInfo("en-US"));

        Assert.AreEqual("$2.5M", actual);
    }

    /// <summary>
    /// Verifies that amounts at least one billion are scaled and given the <c>B</c> suffix.
    /// </summary>
    [TestMethod]
    public void ToCompactString_WhenAmountInBillions_ShouldUseBSuffix()
    {
        var money = new Money<USD>(2_500_000_000m);

        var actual = money.ToCompactString("C", new CultureInfo("en-US"));

        Assert.AreEqual("$2.5B", actual);
    }

    /// <summary>
    /// Verifies that amounts at least one trillion are scaled and given the <c>T</c> suffix.
    /// </summary>
    [TestMethod]
    public void ToCompactString_WhenAmountInTrillions_ShouldUseTSuffix()
    {
        var money = new Money<USD>(2_500_000_000_000m);

        var actual = money.ToCompactString("C", new CultureInfo("en-US"));

        Assert.AreEqual("$2.5T", actual);
    }

    /// <summary>
    /// Verifies that the magnitude suffix attaches to the numeric portion in cultures that place the symbol after
    /// the amount, preserving locale-correct positioning.
    /// </summary>
    [TestMethod]
    public void ToCompactString_WhenCultureUsesTrailingSymbol_ShouldAttachSuffixToNumericPortion()
    {
        var money = new Money<EUR>(1234.56m);

        var actual = money.ToCompactString("C", new CultureInfo("de-DE"));

        Assert.AreEqual("1,2K €", actual);
    }

    /// <summary>
    /// Verifies that negative compact amounts honour the culture's negative-currency pattern with the magnitude
    /// suffix bound to the numeric portion. Asserted under <see cref="CultureInfo.InvariantCulture" /> where the
    /// pattern (<c>($n)</c>) is deterministic across .NET runtimes.
    /// </summary>
    [TestMethod]
    public void ToCompactString_WhenAmountIsNegative_ShouldRespectCultureNegativePattern()
    {
        var money = new Money<USD>(-1234.56m);

        var actual = money.ToCompactString("C", CultureInfo.InvariantCulture);

        Assert.AreEqual("(USD 1.2K)", actual);
    }

    /// <summary>
    /// Verifies that the <c>"G"</c> specifier renders negative compact amounts with a leading minus sign rather than
    /// the culture's parenthesised negative pattern, keeping the ISO-prefix form unambiguous.
    /// </summary>
    [TestMethod]
    public void ToCompactString_WhenGSpecifierAndNegative_ShouldUseLeadingMinusSign()
    {
        var money = new Money<USD>(-1234.56m);

        var actual = money.ToCompactString("G", CultureInfo.InvariantCulture);

        Assert.AreEqual("USD -1.2K", actual);
    }

    /// <summary>
    /// Verifies that the <c>"G"</c> specifier emits the ISO code followed by the scaled amount and suffix.
    /// </summary>
    [TestMethod]
    public void ToCompactString_WhenGSpecifier_ShouldEmitIsoPrefixedCompactForm()
    {
        var money = new Money<USD>(1_500_000m);

        var actual = money.ToCompactString("G", CultureInfo.InvariantCulture);

        Assert.AreEqual("USD 1.5M", actual);
    }

    /// <summary>
    /// Verifies that the <c>"L"</c> specifier appends the English name after the suffixed amount.
    /// </summary>
    [TestMethod]
    public void ToCompactString_WhenLSpecifier_ShouldAppendEnglishNameAfterSuffix()
    {
        var money = new Money<USD>(1_500_000m);

        var actual = money.ToCompactString("L", CultureInfo.InvariantCulture);

        Assert.AreEqual("1.5M US Dollar", actual);
    }

    /// <summary>
    /// Verifies that the <c>"N"</c> specifier produces a bare numeric compact form with no currency designator.
    /// </summary>
    [TestMethod]
    public void ToCompactString_WhenNSpecifier_ShouldEmitBareNumericCompactForm()
    {
        var money = new Money<USD>(1_500_000m);

        var actual = money.ToCompactString("N", CultureInfo.InvariantCulture);

        Assert.AreEqual("1.5M", actual);
    }

    /// <summary>
    /// Verifies that the <c>"~C"</c> prefix elides the currency designator on the compact form when the culture
    /// matches the currency.
    /// </summary>
    [TestMethod]
    public void ToCompactString_WhenTildeCAndCultureMatches_ShouldElideSymbol()
    {
        var money = new Money<USD>(1_234_567m);

        var actual = money.ToCompactString("~C", new CultureInfo("en-US"));

        Assert.AreEqual("1.2M", actual);
    }

    /// <summary>
    /// Verifies that explicit precision is honoured on the scaled portion.
    /// </summary>
    [TestMethod]
    public void ToCompactString_WhenPrecisionIsTwo_ShouldRenderTwoFractionalDigits()
    {
        var money = new Money<USD>(1_234_567m);

        var actual = money.ToCompactString("C", new CultureInfo("en-US"), precision: 2);

        Assert.AreEqual("$1.23M", actual);
    }

    /// <summary>
    /// Verifies that a zero precision count renders the suffix immediately after the truncated integer portion.
    /// </summary>
    [TestMethod]
    public void ToCompactString_WhenPrecisionIsZero_ShouldRenderWholeNumberCompactForm()
    {
        var money = new Money<USD>(1_500_000m);

        var actual = money.ToCompactString("C", new CultureInfo("en-US"), precision: 0);

        Assert.AreEqual("$2M", actual);
    }

    /// <summary>
    /// Verifies that the compact extension rejects a negative <paramref name="precision" /> argument.
    /// </summary>
    [TestMethod]
    public void ToCompactString_WhenPrecisionIsNegative_ShouldThrowArgumentOutOfRangeException()
    {
        var money = new Money<USD>(1234m);

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = money.ToCompactString("C", CultureInfo.InvariantCulture, precision: -1);
        });
    }

    /// <summary>
    /// Verifies that the compact extension rejects the <c>"R"</c> specifier because the round-trip form cannot be
    /// combined with magnitude scaling without losing precision.
    /// </summary>
    [TestMethod]
    [DataRow("R")]
    [DataRow("~R")]
    public void ToCompactString_WhenRSpecifier_ShouldThrowFormatException(string format)
    {
        var money = new Money<USD>(1234m);

        Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = money.ToCompactString(format, CultureInfo.InvariantCulture);
        });
    }

    /// <summary>
    /// Verifies that the <see cref="Money" /> overload applies the same magnitude scaling as the typed
    /// <see cref="Money{TCurrency}" /> overload.
    /// </summary>
    [TestMethod]
    public void ToCompactString_WhenMoneyInThousands_ShouldUseKSuffix()
    {
        var money = new Money(1234.56m, "USD");

        var actual = money.ToCompactString("C", new CultureInfo("en-US"));

        Assert.AreEqual("$1.2K", actual);
    }

    /// <summary>
    /// Verifies that the <see cref="Money" /> overload appends the English name from
    /// <see cref="CurrencyRegistry" /> when the <c>"L"</c> specifier is supplied.
    /// </summary>
    [TestMethod]
    public void ToCompactString_WhenMoneyLSpecifier_ShouldAppendEnglishNameAfterSuffix()
    {
        var money = new Money(1_500_000m, "USD");

        var actual = money.ToCompactString("L", CultureInfo.InvariantCulture);

        Assert.AreEqual("1.5M US Dollar", actual);
    }

    /// <summary>
    /// Verifies that JPY (zero-decimal currency) renders compactly when the amount crosses the K threshold,
    /// honoring the requested precision in the scaled portion even though the natural minor-unit precision is zero.
    /// </summary>
    [TestMethod]
    public void ToCompactString_WhenJpyAmountInThousands_ShouldUseRequestedPrecision()
    {
        var money = new Money<JPY>(25_500m);

        var actual = money.ToCompactString("C", CultureInfo.InvariantCulture);

        Assert.AreEqual("JPY 25.5K", actual);
    }
}
