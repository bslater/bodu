// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateSeriesKeyTests.Equality.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test;
using Bodu.Test.Assertions;

namespace Bodu.Financial;

public partial class ExchangeRateSeriesKeyTests
{

    /// <summary>
    /// Verifies that two validated keys with identical components compare equal via the generated record-struct
    /// equality.
    /// </summary>
    [TestMethod]
    public void Equality_WhenComponentsMatch_ShouldReportEqual()
    {
        ExchangeRateSeriesKey a = new(s_usdAud, "RBA");
        ExchangeRateSeriesKey b = new(s_usdAud, "RBA");

        Assert.AreEqual(a, b);
        Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
    }
}
