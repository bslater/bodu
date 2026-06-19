// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyFormatterBuilderTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Financial.Currencies;

namespace Bodu.Financial;

/// <summary>
/// Verifies <see cref="MoneyFormatterBuilder" /> composes a <see cref="MoneyFormatter" /> matching the configured
/// options.
/// </summary>
[TestClass]
public class MoneyFormatterBuilderTests
{
    /// <summary>
    /// Verifies that the builder produces a formatter rendering the numeric-only, ungrouped form.
    /// </summary>
    [TestMethod]
    public void Build_WhenNumericOnlyWithoutGrouping_ShouldRenderBareAmount()
    {
        MoneyFormatter formatter = new MoneyFormatterBuilder()
            .WithNumericOnly()
            .WithGrouping(false)
            .WithCulture(CultureInfo.InvariantCulture)
            .Build();

        Assert.AreEqual("1234.56", formatter.Format(new Money(1234.56m, CurrencyCode.USD)));
    }

    /// <summary>
    /// Verifies that the builder's English-name configuration renders the currency name with overridden precision.
    /// </summary>
    [TestMethod]
    public void Build_WhenEnglishNameWithMinorUnits_ShouldRenderNameAndPrecision()
    {
        MoneyFormatter formatter = new MoneyFormatterBuilder()
            .WithEnglishName()
            .WithMinorUnits(0)
            .WithCulture(CultureInfo.InvariantCulture)
            .Build();

        Assert.AreEqual("1,235 US Dollar", formatter.Format(new Money(1234.56m, CurrencyCode.USD)));
    }

    /// <summary>
    /// Verifies that the ISO-code designator renders the three-letter code alongside the amount.
    /// </summary>
    [TestMethod]
    public void Build_WhenIsoCode_ShouldRenderIsoDesignator()
    {
        MoneyFormatter formatter = new MoneyFormatterBuilder()
            .WithIsoCode()
            .WithCulture(CultureInfo.InvariantCulture)
            .Build();

        StringAssert.Contains(formatter.Format(new Money(1234.56m, CurrencyCode.USD)), "USD");
    }

    /// <summary>
    /// Verifies that the symbol designator renders the culture's native currency symbol.
    /// </summary>
    [TestMethod]
    public void Build_WhenSymbol_ShouldRenderNativeSymbol()
    {
        MoneyFormatter formatter = new MoneyFormatterBuilder()
            .WithSymbol()
            .WithCulture(new CultureInfo("en-US"))
            .Build();

        StringAssert.Contains(formatter.Format(new Money(1234.56m, CurrencyCode.USD)), "$");
    }

    /// <summary>
    /// Verifies that the elide-on-match option drops the currency designator when the culture's region currency
    /// matches the amount's currency.
    /// </summary>
    [TestMethod]
    public void Build_WhenElideOnCultureMatch_ShouldDropDesignator()
    {
        MoneyFormatter formatter = new MoneyFormatterBuilder()
            .WithIsoCode()
            .ElideWhenCultureMatches()
            .WithCulture(new CultureInfo("en-US"))
            .Build();

        Assert.IsFalse(formatter.Format(new Money(1234.56m, CurrencyCode.USD)).Contains("USD", StringComparison.Ordinal));
    }
}
