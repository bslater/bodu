// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyJsonConverterTests.Lenient.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json;
using Bodu.Financial.Serialization;

namespace Bodu.Financial;

public partial class MoneyJsonConverterTests
{

    /// <summary>
    /// Verifies that the Lenient policy trims and upper-cases the currency code.
    /// </summary>
    [TestMethod]
    public void Lenient_WhenCurrencyLowercaseWithWhitespace_ShouldNormalize()
    {
        Money money = JsonSerializer.Deserialize<Money>("{\"amount\":1,\"currency\":\" usd \"}", OptionsFor(FinancialJsonPolicy.Lenient));

        Assert.AreEqual("USD", money.IsoCode);
    }
}
