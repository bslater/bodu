// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyOfTCurrencyTests.HistoricCurrency.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Reflection;
using Bodu.Financial.Currencies;

namespace Bodu.Financial;

/// <summary>
/// Verifies the historic-currency metadata surfaced through <see cref="ICurrency.IsHistoric" />,
/// <see cref="ICurrency.DemonetizedOn" />, and <see cref="ICurrency.SuccessorIsoCode" /> for the shipped
/// demonetized currency tag types.
/// </summary>
public partial class MoneyOfTCurrencyTests
{
    /// <summary>
    /// Verifies that active currencies report <see cref="ICurrency.IsHistoric" /> as <see langword="false" />.
    /// </summary>
    [TestMethod]
    public void IsHistoric_WhenActiveCurrency_ShouldReturnFalse()
    {
        Assert.IsFalse(Money<USD>.IsHistoric);
        Assert.IsFalse(Money<EUR>.IsHistoric);
        Assert.IsFalse(Money<JPY>.IsHistoric);
        Assert.IsFalse(Money<GBP>.IsHistoric);
        Assert.IsFalse(Money<AUD>.IsHistoric);
    }

    /// <summary>
    /// Verifies that the shipped historic currency tags report <see cref="ICurrency.IsHistoric" /> as
    /// <see langword="true" /> along with the expected demonetization date and successor.
    /// </summary>
    [TestMethod]
    [DataRow("DEM", "2002-02-28", "EUR")]
    [DataRow("FRF", "2002-02-17", "EUR")]
    [DataRow("ITL", "2002-02-28", "EUR")]
    [DataRow("ESP", "2002-02-28", "EUR")]
    [DataRow("NLG", "2002-01-28", "EUR")]
    [DataRow("ATS", "2002-02-28", "EUR")]
    [DataRow("HRK", "2023-01-14", "EUR")]
    [DataRow("LTL", "2015-01-15", "EUR")]
    [DataRow("LVL", "2014-01-14", "EUR")]
    [DataRow("EEK", "2011-01-14", "EUR")]
    [DataRow("VEF", "2018-08-20", "VES")]
    [DataRow("ZWL", "2024-04-08", "ZWG")]
    [DataRow("ROL", "2005-07-01", "RON")]
    public void HistoricCurrency_WhenInspectedReflectively_ShouldReportExpectedMetadata(string iso, string expectedDemonetized, string expectedSuccessor)
    {
        Type currencyType = typeof(USD).Assembly.GetType($"Bodu.Financial.Currencies.{iso}")!;

        bool isHistoric = (bool)currencyType.GetProperty("IsHistoric", BindingFlags.Public | BindingFlags.Static)!.GetValue(null)!;
        var demonetizedOn = (DateOnly?)currencyType.GetProperty("DemonetizedOn", BindingFlags.Public | BindingFlags.Static)!.GetValue(null);
        string? successor = (string?)currencyType.GetProperty("SuccessorIsoCode", BindingFlags.Public | BindingFlags.Static)!.GetValue(null);

        Assert.IsTrue(isHistoric, $"{iso} should report IsHistoric=true.");
        Assert.AreEqual(DateOnly.Parse(expectedDemonetized, System.Globalization.CultureInfo.InvariantCulture), demonetizedOn);
        Assert.AreEqual(expectedSuccessor, successor);
    }

    /// <summary>
    /// Verifies that a historic currency still participates in normal arithmetic. The demonetization metadata is
    /// informational only; legacy data must still be processable.
    /// </summary>
    [TestMethod]
    public void HistoricCurrency_WhenUsedInArithmetic_ShouldBehaveLikeAnyOtherCurrency()
    {
        var a = new Money<DEM>(100m);
        var b = new Money<DEM>(50m);

        Money<DEM> sum = a + b;
        Money<DEM> doubled = a * 2m;

        Assert.AreEqual(new Money<DEM>(150m), sum);
        Assert.AreEqual(new Money<DEM>(200m), doubled);
    }

    /// <summary>
    /// Verifies that a historic currency with zero minor units (Italian Lira, Spanish Peseta) honors its
    /// precision in arithmetic.
    /// </summary>
    [TestMethod]
    public void HistoricCurrency_WhenItalianLiraOrSpanishPeseta_ShouldRoundToWholeUnits()
    {
        var lira = new Money<ITL>(1234.56m);
        var peseta = new Money<ESP>(99.9m);

        Assert.AreEqual(1235m, lira.Amount);     // 1234.56 → 1235 banker's
        Assert.AreEqual(100m, peseta.Amount);    // 99.9 → 100 (not midpoint)
    }
}
