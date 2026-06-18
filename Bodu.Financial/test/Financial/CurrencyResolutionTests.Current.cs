// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CurrencyResolutionTests.Current.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Financial;

public sealed partial class CurrencyResolutionTests
{
    /// <summary>
    /// Verifies that the default ambient lookup resolves a shipped currency, matching a direct registry lookup.
    /// </summary>
    [TestMethod]
    public void Current_WhenNoScope_ShouldResolveShippedCurrency()
    {
        Assert.IsTrue(CurrencyResolution.Current.TryByIsoCode("USD", out CurrencyInfo usd));
        Assert.AreEqual("USD", usd.IsoCode);
    }
}
