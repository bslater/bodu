// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CurrencyCatalogueTests.Bhd.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Reflection;

namespace Bodu.Financial.Currencies;

public partial class CurrencyCatalogueTests
{

    /// <summary>
    /// Verifies that BHD reports three minor units (the boundary case for high-precision currencies).
    /// </summary>
    [TestMethod]
    public void Bhd_WhenAccessed_ShouldReportThreeMinorUnits()
    {
        Assert.AreEqual("BHD", BHD.IsoCode);
        Assert.AreEqual(3, BHD.MinorUnits);
    }
}
