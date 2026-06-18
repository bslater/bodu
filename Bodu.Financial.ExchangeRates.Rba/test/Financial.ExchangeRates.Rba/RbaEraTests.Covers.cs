// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RbaEraTests.Covers.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Rba;

public partial class RbaEraTests
{
    /// <summary>
    /// Verifies that the open-ended current era covers a far-future date.
    /// </summary>
    [TestMethod]
    public void Covers_WhenOpenEndedEra_ShouldCoverFutureDate()
    {
        RbaEra era = new("2023-current", new DateOnly(2023, 1, 1), null);

        Assert.IsTrue(era.Covers(new DateOnly(2099, 12, 31)));
        Assert.IsFalse(era.Covers(new DateOnly(2022, 12, 31)));
    }
}
