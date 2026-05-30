// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CurrencyCatalogueTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Reflection;

namespace Bodu.Financial.Currencies;

/// <summary>
/// Verifies the shipped ISO 4217 currency catalogue exposes the expected metadata for representative samples,
/// including boundary cases on the minor-unit precision (0, 2, 3).
/// </summary>
[TestClass]
public class CurrencyCatalogueTests
{
    /// <summary>
    /// Verifies that the shipped USD tag reports the expected ISO code and minor-unit precision.
    /// </summary>
    [TestMethod]
    public void Usd_WhenAccessed_ShouldReportTwoMinorUnits()
    {
        Assert.AreEqual("USD", USD.IsoCode);
        Assert.AreEqual(2, USD.MinorUnits);
    }

    /// <summary>
    /// Verifies that JPY reports zero minor units (the boundary case for whole-unit currencies).
    /// </summary>
    [TestMethod]
    public void Jpy_WhenAccessed_ShouldReportZeroMinorUnits()
    {
        Assert.AreEqual("JPY", JPY.IsoCode);
        Assert.AreEqual(0, JPY.MinorUnits);
    }

    /// <summary>
    /// Verifies that BHD reports three minor units (the boundary case for high-precision currencies).
    /// </summary>
    [TestMethod]
    public void Bhd_WhenAccessed_ShouldReportThreeMinorUnits()
    {
        Assert.AreEqual("BHD", BHD.IsoCode);
        Assert.AreEqual(3, BHD.MinorUnits);
    }

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

    /// <summary>
    /// Verifies that the catalogue contains the full set of shipped tag types so the source generator is in
    /// sync with <c>currencies.json</c>.
    /// </summary>
    [TestMethod]
    public void Catalogue_WhenEnumeratedViaReflection_ShouldContainAtLeastOneHundredEightyTypes()
    {
        int count = typeof(USD).Assembly
            .GetTypes()
            .Where(t => t.Namespace == "Bodu.Financial.Currencies"
                && typeof(ICurrency).IsAssignableFrom(t))
            .Count();

        Assert.IsTrue(count >= 180, $"Expected at least 180 currency tag types, found {count}.");
    }
}
