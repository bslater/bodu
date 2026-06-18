// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CurrencyCatalogueTests.ShippedCurrencies.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Reflection;

namespace Bodu.Financial.Currencies;

public partial class CurrencyCatalogueTests
{

    /// <summary>
    /// Verifies that a representative set of currencies is reachable as catalogue tag types.
    /// </summary>
    [TestMethod]
    [DataRow(typeof(EUR), "EUR", 2)]
    [DataRow(typeof(GBP), "GBP", 2)]
    [DataRow(typeof(AUD), "AUD", 2)]
    [DataRow(typeof(CHF), "CHF", 2)]
    [DataRow(typeof(KWD), "KWD", 3)]
    [DataRow(typeof(KRW), "KRW", 0)]
    [DataRow(typeof(CLP), "CLP", 0)]
    public void ShippedCurrencies_WhenAccessedReflectively_ShouldReportExpectedMetadata(Type currencyType, string expectedIso, int expectedMinorUnits)
    {
        string iso = (string)currencyType.GetProperty("IsoCode", BindingFlags.Public | BindingFlags.Static)!.GetValue(null)!;
        int minorUnits = (int)currencyType.GetProperty("MinorUnits", BindingFlags.Public | BindingFlags.Static)!.GetValue(null)!;

        Assert.AreEqual(expectedIso, iso);
        Assert.AreEqual(expectedMinorUnits, minorUnits);
    }
}
