// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RateSeriesKeyTests.Equality.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

public partial class RateSeriesKeyTests
{

    /// <summary>
    /// Verifies that two validated keys with identical components compare equal via the generated record-struct
    /// equality.
    /// </summary>
    [TestMethod]
    public void Equality_WhenComponentsMatch_ShouldReportEqual()
    {
        RateSeriesKey a = new(s_usdAud, "RBA");
        RateSeriesKey b = new(s_usdAud, "RBA");

        Assert.AreEqual(a, b);
        Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
    }
}
