// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyFormatterTests.Format.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Financial.Currencies;

namespace Bodu.Financial;

public partial class MoneyFormatterTests
{
    /// <summary>
    /// Verifies that the ISO-code display renders the code and grouped amount.
    /// </summary>
    [TestMethod]
    public void Format_WhenIsoCodeDisplay_ShouldRenderCodeAndGroupedAmount()
    {
        MoneyFormatter formatter = new(new MoneyFormatOptions { FormatProvider = CultureInfo.InvariantCulture });

        Assert.AreEqual("USD 1,234.56", formatter.Format(new Money(1234.56m, CurrencyCode.USD)));
    }

    /// <summary>
    /// Verifies that the numeric-only display honours the grouping option.
    /// </summary>
    [TestMethod]
    [DataRow(true, "1,234.56")]
    [DataRow(false, "1234.56")]
    public void Format_WhenNumericOnly_ShouldHonourGrouping(bool grouping, string expected)
    {
        MoneyFormatter formatter = new(new MoneyFormatOptions
        {
            CurrencyDisplay = CurrencyDisplay.None,
            IncludeGrouping = grouping,
            FormatProvider = CultureInfo.InvariantCulture,
        });

        Assert.AreEqual(expected, formatter.Format(new Money(1234.56m, CurrencyCode.USD)));
    }

    /// <summary>
    /// Verifies that the English-name display renders the amount followed by the currency name.
    /// </summary>
    [TestMethod]
    public void Format_WhenEnglishNameDisplay_ShouldRenderName()
    {
        MoneyFormatter formatter = new(new MoneyFormatOptions
        {
            CurrencyDisplay = CurrencyDisplay.EnglishName,
            FormatProvider = CultureInfo.InvariantCulture,
        });

        Assert.AreEqual("1,234.56 US Dollar", formatter.Format(new Money(1234.56m, CurrencyCode.USD)));
    }

    /// <summary>
    /// Verifies that the minor-units override changes the rendered precision.
    /// </summary>
    [TestMethod]
    public void Format_WhenMinorUnitsOverride_ShouldChangePrecision()
    {
        MoneyFormatter formatter = new(new MoneyFormatOptions
        {
            MinorUnitsOverride = 0,
            FormatProvider = CultureInfo.InvariantCulture,
        });

        Assert.AreEqual("USD 1,235", formatter.Format(new Money(1234.56m, CurrencyCode.USD)));
    }

    /// <summary>
    /// Verifies that the formatter also renders strongly-typed values.
    /// </summary>
    [TestMethod]
    public void Format_WhenTypedMoney_ShouldRenderSameAsRuntime()
    {
        MoneyFormatter formatter = new(new MoneyFormatOptions { FormatProvider = CultureInfo.InvariantCulture });

        Assert.AreEqual("USD 1,234.56", formatter.Format(new Money<USD>(1234.56m)));
    }
}
