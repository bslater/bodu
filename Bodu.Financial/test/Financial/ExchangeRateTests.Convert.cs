// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateTests.Convert.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;
using Bodu.Test;
using Bodu.Test.Assertions;

namespace Bodu.Financial;

public partial class ExchangeRateTests
{

    /// <summary>
    /// Verifies that <see cref="ExchangeRate.Convert(decimal)" /> multiplies the input by the stored rate.
    /// </summary>
    [TestMethod]
    public void Convert_WhenCalled_ShouldReturnAmountMultipliedByRate()
    {
        ExchangeRate rate = new(CurrencyCode.USD, CurrencyCode.AUD, s_sampleDate, 1.5m, "RBA");

        decimal converted = rate.Convert(100m);

        Assert.AreEqual(150m, converted);
    }
}
