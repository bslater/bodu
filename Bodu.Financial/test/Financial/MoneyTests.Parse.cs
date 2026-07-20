// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyTests.Parse.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Financial.Currencies;

namespace Bodu.Financial;

public partial class MoneyTests
{
    /// <summary>
    /// Verifies that parsing text with more fractional digits than the currency's registered minor units constructs
    /// the value at that finer scale instead of silently rounding the sub-minor-unit digits away.
    /// </summary>
    [TestMethod]
    [DataRow("USD 12.345678", DisplayName = "ISO prefix")]
    [DataRow("12.345678 USD", DisplayName = "ISO suffix")]
    public void Parse_WhenTextHasMoreDigitsThanRegistryPrecision_ShouldReportPrintedScale(string text)
    {
        Money parsed = Money.Parse(text, CultureInfo.InvariantCulture);

        Assert.AreEqual(6, parsed.MinorUnits);
        Assert.AreEqual(12.345678m, parsed.Amount);
    }

    /// <summary>
    /// Verifies that trailing zeros in the parsed text are preserved as scale, so a six-place rendering of a short
    /// value still reports six minor units.
    /// </summary>
    [TestMethod]
    public void Parse_WhenTextHasTrailingZeros_ShouldPreservePrintedScale()
    {
        Money parsed = Money.Parse("USD 12.500000", CultureInfo.InvariantCulture);

        Assert.AreEqual(6, parsed.MinorUnits);
        Assert.AreEqual("USD 12.500000", parsed.ToString("R"));
    }

    /// <summary>
    /// Verifies that text at or below the currency's registered precision parses at the registry precision, unchanged
    /// from the historical behaviour.
    /// </summary>
    [TestMethod]
    [DataRow("USD 19.99", DisplayName = "Exactly registry precision")]
    [DataRow("USD 12.5", DisplayName = "Below registry precision")]
    [DataRow("USD 20", DisplayName = "No fractional digits")]
    public void Parse_WhenTextAtOrBelowRegistryPrecision_ShouldReportRegistryMinorUnits(string text)
    {
        Money parsed = Money.Parse(text, CultureInfo.InvariantCulture);

        Assert.AreEqual(2, parsed.MinorUnits);
    }

    /// <summary>
    /// Verifies that the round-trip (<c>"R"</c>) format and <see cref="Money.Parse(string, IFormatProvider?)" /> are
    /// inverses for an explicit-scale value: formatting then parsing restores the amount, currency, and scale.
    /// </summary>
    [TestMethod]
    public void Parse_WhenRoundTrippingRFormat_ForExplicitScaleValue_ShouldRestoreScale()
    {
        Money original = Money.FromExplicitScale(145.678912m, CurrencyCode.USD, 6);

        Money restored = Money.Parse(original.ToString("R"), CultureInfo.InvariantCulture);

        Assert.AreEqual(original, restored);
        Assert.AreEqual(6, restored.MinorUnits);
    }

    /// <summary>
    /// Verifies that <see cref="Money.TryParse(string?, IFormatProvider?, out Money)" /> applies the same finer-scale
    /// inference as <see cref="Money.Parse(string, IFormatProvider?)" />.
    /// </summary>
    [TestMethod]
    public void TryParse_WhenTextHasMoreDigitsThanRegistryPrecision_ShouldReportPrintedScale()
    {
        bool ok = Money.TryParse("0.032513 USD", CultureInfo.InvariantCulture, out Money parsed);

        Assert.IsTrue(ok);
        Assert.AreEqual(6, parsed.MinorUnits);
        Assert.AreEqual(0.032513m, parsed.Amount);
    }
}
