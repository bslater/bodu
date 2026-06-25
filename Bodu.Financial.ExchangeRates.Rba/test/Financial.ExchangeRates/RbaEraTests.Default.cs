// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RbaEraTests.Default.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

public partial class RbaEraTests
{
    /// <summary>
    /// Verifies that the default catalogue contains the eleven published era files.
    /// </summary>
    [TestMethod]
    public void Default_ShouldContainElevenEras()
    {
        Assert.HasCount(11, RbaEra.Default);
    }
}
