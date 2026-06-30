// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CurrencyCatalogueTests.Usd.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.Currencies;

public partial class CurrencyCatalogueTests
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
}
