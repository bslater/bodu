// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyFormatterBuilderTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

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

        Assert.AreEqual("1234.56", formatter.Format(new Money(1234.56m, "USD")));
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

        Assert.AreEqual("1,235 US Dollar", formatter.Format(new Money(1234.56m, "USD")));
    }
}
