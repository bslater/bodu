// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CurrencyCatalogueTests.Jpy.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.Currencies;

public partial class CurrencyCatalogueTests
{

    /// <summary>
    /// Verifies that JPY reports zero minor units (the boundary case for whole-unit currencies).
    /// </summary>
    [TestMethod]
    public void Jpy_WhenAccessed_ShouldReportZeroMinorUnits()
    {
        Assert.AreEqual("JPY", JPY.IsoCode);
        Assert.AreEqual(0, JPY.MinorUnits);
    }
}
