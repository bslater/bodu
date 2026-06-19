// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRatePairJsonConverterTests.Compact.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text.Json;
using Bodu.Financial.Currencies;
using Bodu.Financial.Serialization;

namespace Bodu.Financial;

public partial class ExchangeRatePairJsonConverterTests
{

    /// <summary>
    /// Verifies that the compact <c>"FROM/TO"</c> string form deserializes through the default Strict object reader's
    /// shared parsing path.
    /// </summary>
    [TestMethod]
    public void Compact_WhenStringForm_ShouldResolve()
    {
        ExchangeRatePair restored = JsonSerializer.Deserialize<ExchangeRatePair>("\"USD/JPY\"", OptionsFor(FinancialJsonPolicy.Compact));

        Assert.AreEqual(CurrencyCode.USD, restored.From);
        Assert.AreEqual(CurrencyCode.JPY, restored.To);
    }
}
